using DataAnalystBackend.Shared.Interfaces.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.Interfaces.Services
{
    public interface IGraphingDataService
    {
        Task<List<KPIModel>> GetKPIs(string userId, Guid dataSessionId);

        Task<Dictionary<string, List<GraphModel>>> GetGraphs(string userId, Guid dataSessionId);
    }
}
