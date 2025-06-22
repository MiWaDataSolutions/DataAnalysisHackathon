using DataAnalystBackend.Shared.DataAccess.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.Interfaces.Services
{
    public interface IDataSessionFileService
    {
        Task<DataFileSessionStatus> GetLatestFileProcessedState(Guid dataSessionId);

        Task SetDataFileProcessed(Guid dataSessionId);

        Task SetDataFileInprogress(Guid dataSessionId);
    }
}
