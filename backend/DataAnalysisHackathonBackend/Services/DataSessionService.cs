using DataAnalysisHackathonBackend.Data;
using DataAnalysisHackathonBackend.Exceptions;
using DataAnalysisHackathonBackend.Interfaces.Services;
using DataAnalysisHackathonBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAnalysisHackathonBackend.Services
{
    public class DataSessionService : IDataSessionService
    {
        private readonly ApplicationDbContext _context;

        public DataSessionService(ApplicationDbContext context)
        {
            _context = context;            
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
