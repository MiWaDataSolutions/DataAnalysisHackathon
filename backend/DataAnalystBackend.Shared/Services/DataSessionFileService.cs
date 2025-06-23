using DataAnalystBackend.Shared.DataAccess;
using DataAnalystBackend.Shared.DataAccess.Enums;
using DataAnalystBackend.Shared.DataAccess.Models;
using DataAnalystBackend.Shared.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.Services
{
    public class DataSessionFileService : IDataSessionFileService
    {
        private readonly ApplicationDbContext _context;

        public DataSessionFileService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DataFileSessionStatus> GetLatestFileProcessedState(Guid dataSessionId)
        {
            return (await _context.DataSessionsFiles.OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync(o => o.DataSessionId == dataSessionId))?.ProcessedStatus ?? DataFileSessionStatus.Uploaded;
        }

        public async Task SetDataFileProcessed(Guid dataSessionId)
        {
            DataSessionFile dsf = await _context.DataSessionsFiles.OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync(o => o.DataSessionId == dataSessionId);
            dsf.ProcessedStatus = DataFileSessionStatus.Completed;

            await _context.SaveChangesAsync();
        }

        public async Task SetDataFileInprogress(Guid dataSessionId)
        {
            DataSessionFile dsf = await _context.DataSessionsFiles.OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync(o => o.DataSessionId == dataSessionId);
            dsf.ProcessedStatus = DataFileSessionStatus.Inprogress;

            await _context.SaveChangesAsync();
        }
    }
}
