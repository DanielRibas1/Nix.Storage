using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Exceptions
{
    public class LazyDeserializationNeededException : Exception
    {        
        public string TypeRequested { get; private set; }

        public LazyDeserializationNeededException(string typeRequested)
        {            
            this.TypeRequested = typeRequested;
        }
    }
}
