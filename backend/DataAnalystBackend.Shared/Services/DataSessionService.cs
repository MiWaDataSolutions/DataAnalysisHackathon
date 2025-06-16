using DataAnalystBackend.Shared.DataAccess;
using DataAnalystBackend.Shared.DataAccess.Models;
using DataAnalystBackend.Shared.Exceptions;
using DataAnalystBackend.Shared.Interfaces;
using DataAnalystBackend.Shared.Interfaces.Services;
using DataAnalystBackend.Shared.Services.RPC;
using DataAnalystBackend.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client.Events;

namespace DataAnalystBackend.Shared.Services
{
    public class DataSessionService : IDataSessionService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _defaultDatabaseString;
        private readonly RpcClient _rpcClient;
        private string _prefix;

        public DataSessionService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _defaultDatabaseString = configuration.GetRequiredSection("DefaultUserDatabaseConnection").Value;
            _rpcClient = new RpcClient(configuration);
            _prefix = configuration.GetValue<string>("RabbitMQ:Prefix");
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

        public async Task StartGeneration<TModel>(string fileName, Guid dataSessionId, string userId, Func<TModel, BasicDeliverEventArgs, Task> consumeMethod)
        {
            User? user = await _context.Users.SingleOrDefaultAsync(o => o.GoogleId == userId);
            if (user == null)
                throw new RecordNotFoundException(nameof(User), Guid.Empty);

            DataSession? dataSession = await _context.DataSessions.SingleOrDefaultAsync(o => o.Id == dataSessionId && o.UserId == userId);

            if (dataSession == null)
                throw new RecordNotFoundException(nameof(DataSession), dataSessionId);

            if (string.IsNullOrWhiteSpace(user.UserDatabaseConnectionString))
            {
                user.UserDatabaseConnectionString = $"{_defaultDatabaseString}_user_{userId}";
                await _context.SaveChangesAsync();

                string databaseCreationSQL = $"CREATE DATABASE data_analyst_user_{userId}";
                await _context.Database.ExecuteSqlRawAsync(databaseCreationSQL);
            }

            string schemaCreationSQL = $"CREATE SCHEMA IF NOT EXISTS {dataSession.SchemaName}";
            await DatabaseUtilities.ExecuteSqlOnOtherDatabaseAsync(user.UserDatabaseConnectionString, schemaCreationSQL);

            await _rpcClient.StartAsync(consumeMethod);
            await _rpcClient.CallAsync("test", $"{_prefix}-{IMessagingProvider.DATA_SESSION_GENERATE_NAME}");
        }

        public async Task UpdateDataSession(Guid dataSessionId, string dataSessionName, string userId)
        {
            if (await _context.Users.AnyAsync(o => o.GoogleId == userId))
            {
                DataSession? dataSessionToUpdate = await _context.DataSessions.SingleOrDefaultAsync(o => o.Id == dataSessionId && o.UserId == userId);
                if (dataSessionToUpdate == null)
                    throw new RecordNotFoundException(nameof(DataSession), dataSessionId);

                dataSessionToUpdate.LastUpdatedAt = DateTime.UtcNow;
                dataSessionToUpdate.Name = dataSessionName;

                await _context.SaveChangesAsync();
            }

            throw new RecordNotFoundException(nameof(User), Guid.Empty);
        }
    }
}
