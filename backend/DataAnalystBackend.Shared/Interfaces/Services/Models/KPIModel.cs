using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.Interfaces.Services.Models
{
    public class KPIModel
    {
        public string KPIName { get; set; }

        public decimal Last30Days { get; set; }

        public decimal Last90Days { get; set; }

        public decimal Last3MonthsAverage { get; set; }
    }
}
