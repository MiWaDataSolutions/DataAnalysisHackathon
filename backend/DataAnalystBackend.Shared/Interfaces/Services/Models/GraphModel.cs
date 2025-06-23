using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.Interfaces.Services.Models
{
    public class GraphModel
    {
        public string GraphName { get; set; }

        public string XAxis { get; set; }

        public string XAxisName { get; set; }

        public decimal YAxis { get; set; }

        public string YAxisName { get; set; }
    }
}
