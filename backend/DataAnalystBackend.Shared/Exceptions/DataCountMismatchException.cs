using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.Exceptions
{
    public class DataCountMismatchException : Exception
    {
        public DataCountMismatchException()
        {
            
        }

        public DataCountMismatchException(Guid dataSessionId, int expectedCount, int actualCount, int errorRow)
            :base($"There was a mismatch between the amount of columns in the header and the amount of columns in the row {errorRow}. Expected Row Count: {expectedCount}, Actual Row Count {actualCount}")
        {
            
        }
    }
}
