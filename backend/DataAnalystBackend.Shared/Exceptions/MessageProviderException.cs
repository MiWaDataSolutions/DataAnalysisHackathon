using System;
using System.Collections.Generic;
using System.Text;

namespace DataAnalystBackend.Shared.Exceptions
{
    public class MessageProviderException: Exception
    {
        public MessageProviderException()
        {
            
        }

        public MessageProviderException(string message)
            :base(message) 
        {
            
        }
    }
}
