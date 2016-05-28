using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Exceptions
{
    public class StoreNotFoundException : Exception
    {
        public StoreNotFoundException(string appKey)
            : base(String.Format("Config store {0} not found in configuration module", appKey))
        {
        }
    }
}
