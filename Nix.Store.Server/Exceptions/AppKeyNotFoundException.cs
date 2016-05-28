using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nix.Store.Server.Exceptions
{
    public class AppKeyNotFoundException : Exception
    {
        public string AppKey { get; private set; }

        public AppKeyNotFoundException(string appKey)
            : base(String.Format("AppKey {0} not found in the Server configuration store", appKey))
        {
            this.AppKey = appKey;
            this.Data.Add("appKey", AppKey);
        }
    }
}
