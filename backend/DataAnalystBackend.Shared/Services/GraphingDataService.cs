using DataAnalystBackend.Shared.DataAccess;
using DataAnalystBackend.Shared.DataAccess.Models;
using DataAnalystBackend.Shared.Exceptions;
using DataAnalystBackend.Shared.Interfaces.Services;
using DataAnalystBackend.Shared.Interfaces.Services.Models;
using DataAnalystBackend.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.Services
{
    public class GraphingDataService : IGraphingDataService
    {
        private readonly ApplicationDbContext _context;
        public GraphingDataService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<string, List<GraphModel>>> GetGraphs(string userId, Guid dataSessionId)
        {
            User? user = await _context.Users.SingleOrDefaultAsync(o => o.GoogleId == userId);
            if (user == null)
                throw new RecordNotFoundException(nameof(User), Guid.Empty);

            DataSession? dataSession = await _context.DataSessions.SingleOrDefaultAsync(o => o.UserId == userId && o.Id == dataSessionId);
            if (dataSession == null)
                throw new RecordNotFoundException(nameof(DataSession), Guid.Empty);

            string graphViewsQuery = $"SELECT table_name FROM information_schema.views WHERE table_schema = '{dataSession.SchemaName}' AND table_name ILIKE '%graph%';";
            string graphViews = await DatabaseUtilities.RunSelectDelimitedAsync(user.UserDatabaseConnectionString, graphViewsQuery);
            List<string> listOfGraphViews = graphViews.Split("[|]").ToList();
            Dictionary<string, List<GraphModel>> graphModels = new Dictionary<string, List<GraphModel>>();

            foreach (var graph in listOfGraphViews)
            {
                string columnsQuery = $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{dataSession.SchemaName}' AND table_name = '{graph}' ORDER BY ordinal_position;";
                string graphColumns = await DatabaseUtilities.RunSelectDelimitedAsync(user.UserDatabaseConnectionString, columnsQuery);

                string[] graphColumnsArr = graphColumns.Split("[|]");

                string graphsDataQuery = $"SELECT * FROM {dataSession.SchemaName}.{graph};";
                string graphsDataQueryResult = await DatabaseUtilities.RunSelectDelimitedAsync(user.UserDatabaseConnectionString, graphsDataQuery);

                string[] graphValuesArr = graphsDataQueryResult.Split("[|]");

                List<GraphModel> graphValues = new List<GraphModel>();

                foreach (var graphValue in graphValuesArr)
                {
                    string[] value = graphValue.Split("||");
                    GraphModel graphModel = new GraphModel()
                    {
                        XAxis = value[0],
                        YAxis = decimal.Parse(value[1])
                    };
                    graphValues.Add(graphModel);
                }

                graphModels.Add(graph, graphValues);
            }

            return graphModels;
        }

        public async Task<List<KPIModel>> GetKPIs(string userId, Guid dataSessionId)
        {
            User? user = await _context.Users.SingleOrDefaultAsync(o => o.GoogleId == userId);
            if (user == null)
                throw new RecordNotFoundException(nameof(User), Guid.Empty);

            DataSession? dataSession = await _context.DataSessions.SingleOrDefaultAsync(o => o.UserId == userId && o.Id == dataSessionId);
            if (dataSession == null)
                throw new RecordNotFoundException(nameof(DataSession), Guid.Empty);

            string kpiViewsQuery = $"SELECT table_name FROM information_schema.views WHERE table_schema = '{dataSession.SchemaName}' AND table_name ILIKE '%kpi%';";
            string kpiViews = await DatabaseUtilities.RunSelectDelimitedAsync(user.UserDatabaseConnectionString, kpiViewsQuery);
            List<string> listOfKpiViews = kpiViews.Split("[|]").ToList();
            List<KPIModel> kPIModels = new List<KPIModel>();

            foreach (var kpiView in listOfKpiViews)
            {
                string kpi30DaysColumnQuery = $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{dataSession.SchemaName}' AND table_name = '{kpiView}' AND column_name ILIKE '%30%' LIMIT 1;";
                string kpi30DaysColumnResult = await DatabaseUtilities.RunSelectDelimitedAsync(user.UserDatabaseConnectionString, kpi30DaysColumnQuery);

                string kpi30DaysQuery = $"SELECT {kpi30DaysColumnResult} FROM {dataSession.SchemaName}.{kpiView} LIMIT 1;";
                string kpi30DaysQueryResult = await DatabaseUtilities.RunSelectDelimitedAsync(user.UserDatabaseConnectionString, kpi30DaysQuery);

                string kpiAverageColumnQuery = $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{dataSession.SchemaName}' AND table_name = '{kpiView}' AND column_name ILIKE '%avg%' LIMIT 1;";
                string kpiAverageColumnResult = await DatabaseUtilities.RunSelectDelimitedAsync(user.UserDatabaseConnectionString, kpi30DaysColumnQuery);

                string kpiAverageQuery = $"SELECT {kpi30DaysColumnResult} FROM {dataSession.SchemaName}.{kpiView} LIMIT 1;";
                string kpiAverageQueryResult = await DatabaseUtilities.RunSelectDelimitedAsync(user.UserDatabaseConnectionString, kpi30DaysQuery);

                KPIModel model = new KPIModel()
                {
                    KPIName = kpiView,
                    Last30Days = decimal.Parse(kpi30DaysQueryResult),
                    Last3MonthsAverage = string.IsNullOrWhiteSpace(kpiAverageQueryResult) ? decimal.Parse(kpi30DaysQueryResult) : decimal.Parse(kpiAverageQueryResult),
                };
                kPIModels.Add(model);
            }

            return kPIModels;
        }


    }
}
