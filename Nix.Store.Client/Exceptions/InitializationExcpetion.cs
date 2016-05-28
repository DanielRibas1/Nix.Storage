using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Exceptions
{
    public class InitializationExcpetion : Exception
    {
        public InitializationExcpetion(Exception innerEx)
            : base("Fail to intiialze Configuration Module, see details at inner exception", innerEx)
        {
        }
    }
}
