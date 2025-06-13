using DataAnalysisHackathonBackend.Models;

namespace DataAnalysisHackathonBackend.Interfaces.Services
{
    public interface IDataSessionService
    {
        Task<List<DataSession>> GetDataSessionsAsync(string userId);

        Task<DataSession> GetDataSessionAsync(Guid dataSessionId, string userId);

        Task<Guid> CreateDataSession(DataSession dataSession, string userId);

        Task DeleteDataSessionAsync(Guid dataSessionId, string userId);

        Task UpdateDataSession(Guid dataSessionId, string dataSessionName, string userId);
    }
}
