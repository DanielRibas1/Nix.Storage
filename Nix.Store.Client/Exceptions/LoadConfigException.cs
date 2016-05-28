using Nix.Store.Client.Sections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Exceptions
{
    public class LoadConfigException : Exception
    {
        public LoadConfigException(LoaderType loaderType, string appKey, string reason)
            : base(String.Format("Fail loading  {0} via {1}. Reason: {2}", appKey, loaderType.ToString(), reason))
        {

        }

        public LoadConfigException(LoaderType loaderType, string appKey, Exception innerException)
            : base(String.Format("Fail loading  {0} via {1}. Details: {2}", appKey, loaderType.ToString(), innerException.ToString()))
        {

        }
    }
}
