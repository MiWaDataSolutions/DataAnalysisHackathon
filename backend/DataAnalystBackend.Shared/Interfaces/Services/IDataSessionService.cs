using DataAnalystBackend.Shared.DataAccess.Models;
using RabbitMQ.Client.Events;

namespace DataAnalystBackend.Shared.Interfaces.Services
{
    public interface IDataSessionService
    {
        Task<List<DataSession>> GetDataSessionsAsync(string userId);

        Task<DataSession> GetDataSessionAsync(Guid dataSessionId, string userId);

        Task<Guid> CreateDataSession(DataSession dataSession, string userId);

        Task DeleteDataSessionAsync(Guid dataSessionId, string userId);

        Task UpdateDataSession(Guid dataSessionId, string dataSessionName, string userId);

        Task StartGeneration<TModel>(string fileName, Guid dataSessionId, string userId, bool initialFileHasHeaders);
    }
}
