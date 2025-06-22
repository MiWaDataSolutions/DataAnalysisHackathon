using DataAnalystBackend.Shared.DataAccess;
using DataAnalystBackend.Shared.DataAccess.Models;
using DataAnalystBackend.Shared.Exceptions;
using DataAnalystBackend.Shared.Interfaces;
using DataAnalystBackend.Shared.Interfaces.Services;
using DataAnalystBackend.Shared.MessagingProviders.Models;
using DataAnalystBackend.Shared.MessagingProviders.Models.Enums;
using DataAnalystBackend.Shared.Services.RPC;
using DataAnalystBackend.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client.Events;
using System.Text;

namespace DataAnalystBackend.Shared.Services
{
    public class DataSessionService : IDataSessionService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _defaultDatabaseString;
        private readonly IMessagingProvider _messagingProvider;
        private string _prefix;

        public DataSessionService(ApplicationDbContext context, IConfiguration configuration, IMessagingProvider messagingProvider)
        {
            _context = context;
            _defaultDatabaseString = configuration.GetRequiredSection("DefaultUserDatabaseConnection").Value;
            _prefix = configuration.GetValue<string>("RabbitMQ:Prefix");
            _messagingProvider = messagingProvider;
        }

        public async Task<Guid> CreateDataSession(DataSession dataSession, string userId)
        {
            if (await _context.Users.AnyAsync(o => o.GoogleId == userId))
            {
                dataSession.User = null;
                dataSession.UserId = userId;
                await _context.DataSessions.AddAsync(dataSession);
                await _context.SaveChangesAsync();

                return dataSession.Id;
            }

            return Guid.Empty;
        }

        public async Task DeleteDataSessionAsync(Guid dataSessionId, string userId)
        {
            if (await _context.Users.AnyAsync(o => o.GoogleId == userId))
            {
                DataSession? dataSession = await _context.DataSessions.SingleOrDefaultAsync(o => o.Id == dataSessionId && o.UserId == userId);
                if (dataSession == null)
                    throw new RecordNotFoundException(nameof(DataSession), dataSessionId);
                
                _context.DataSessions.Remove(dataSession);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<DataSession> GetDataSessionAsync(Guid dataSessionId, string userId)
        {
            if (await _context.Users.AnyAsync(o => o.GoogleId == userId))
            {
                DataSession? dataSession = await _context.DataSessions.SingleOrDefaultAsync(o => o.Id == dataSessionId && o.UserId == userId);
                if (dataSession == null)
                    throw new RecordNotFoundException(nameof(DataSession), dataSessionId);

                return dataSession;
            }

            throw new RecordNotFoundException(nameof(User), Guid.Empty);
        }

        public async Task<List<DataSession>> GetDataSessionsAsync(string userId)
        {
            if (await _context.Users.AnyAsync(o => o.GoogleId == userId))
                return await _context.DataSessions.Where(o => o.UserId == userId).ToListAsync();

            throw new RecordNotFoundException(nameof(User), Guid.Empty);
        }

        public async Task StartGeneration<TModel>(string fileName, Guid dataSessionId, string userId, bool initialFileHasHeaders)
        {
            User? user = await _context.Users.SingleOrDefaultAsync(o => o.GoogleId == userId);
            if (user == null)
                throw new RecordNotFoundException(nameof(User), Guid.Empty);

            DataSession? dataSession = await _context.DataSessions.SingleOrDefaultAsync(o => o.Id == dataSessionId && o.UserId == userId);

            if (dataSession == null)
                throw new RecordNotFoundException(nameof(DataSession), dataSessionId);

            dataSession.InitialFileHasHeaders = initialFileHasHeaders;

            await _context.SaveChangesAsync();

            if (string.IsNullOrWhiteSpace(user.UserDatabaseConnectionString))
            {
                user.UserDatabaseConnectionString = $"{_defaultDatabaseString}_user_{userId}";
                await _context.SaveChangesAsync();

                string databaseCreationSQL = $"CREATE DATABASE data_analyst_user_{userId}";
                await _context.Database.ExecuteSqlRawAsync(databaseCreationSQL);
            }

            string schemaCreationSQL = $"CREATE SCHEMA IF NOT EXISTS {dataSession.SchemaName}";
            await DatabaseUtilities.ExecuteSqlOnOtherDatabaseAsync(user.UserDatabaseConnectionString, schemaCreationSQL);

            DataSessionFile dataSessionFile = await _context.DataSessionsFiles.SingleOrDefaultAsync(o => o.DataSessionId == dataSessionId);

            if (dataSessionFile == null)
                throw new RecordNotFoundException(nameof(DataSessionFile), dataSessionId);


            using (MemoryStream decompressedFile = new MemoryStream(GeneralUtilities.DecompressFile(dataSessionFile.FileData)))
            using (StreamReader sr = new StreamReader(decompressedFile))
            {
                bool isHeaderRow;
                if (initialFileHasHeaders)
                    isHeaderRow = true;
                else
                    isHeaderRow = false;

                string? line = string.Empty;
                int columnCount = 0;
                int rowNumber = 1;
                while ((line = await sr.ReadLineAsync()) != null)
                {
                    if (isHeaderRow)
                    {
                        string[] columns = line.Split(",");
                        StringBuilder bronzeTableCreateScript = new StringBuilder();
                        bronzeTableCreateScript.AppendLine($"CREATE TABLE IF NOT EXISTS {dataSession.SchemaName}.bronze (");

                        foreach (var column in columns)
                            bronzeTableCreateScript.AppendLine($"{column.ToLower().Replace(' ', '_')} text,");

                        string sql = $"{bronzeTableCreateScript.ToString().TrimEnd('\n').TrimEnd('\r').TrimEnd(',')})";
                        await DatabaseUtilities.ExecuteSqlOnOtherDatabaseAsync(user.UserDatabaseConnectionString, sql);

                        columnCount = columns.Length;
                        isHeaderRow = false;
                        rowNumber++;
                        continue;
                    }

                    string[] data = line.Split(",");
                    if (data.Length != columnCount)
                        throw new DataCountMismatchException(dataSessionId, columnCount, data.Length, rowNumber);

                    StringBuilder row = new StringBuilder();
                    row.AppendLine("(");
                    foreach (var column in data)
                        row.Append($"'{column.Replace('\'', '`')}',");

                    string insertStatement = $"INSERT INTO {dataSession.SchemaName}.bronze VALUES{row.ToString().TrimEnd(',')})";
                    await DatabaseUtilities.ExecuteSqlOnOtherDatabaseAsync(user.UserDatabaseConnectionString, insertStatement);

                    rowNumber++;
                }
            }
            await _messagingProvider.PublishMessageAsync(new Message<StartDataSessionMessage>() { MessageType = MessageType.DataSessionStartSession, Data = new StartDataSessionMessage() { DataSessionId = dataSessionId, UserId = userId, DataSessionSchema = dataSession.SchemaName, UserConnString = user.UserDatabaseConnectionString.Replace("localhost", "host.docker.internal") } });
        }

        public async Task UpdateDataSession(Guid dataSessionId, string dataSessionName, string userId)
        {
            if (!await _context.Users.AnyAsync(o => o.GoogleId == userId))
                throw new RecordNotFoundException(nameof(User), Guid.Empty);
            
            DataSession? dataSessionToUpdate = await _context.DataSessions.SingleOrDefaultAsync(o => o.Id == dataSessionId && o.UserId == userId);
            if (dataSessionToUpdate == null)
                throw new RecordNotFoundException(nameof(DataSession), dataSessionId);

            dataSessionToUpdate.LastUpdatedAt = DateTime.UtcNow;
            dataSessionToUpdate.Name = dataSessionName;

            await _context.SaveChangesAsync();
        }
    }
}
