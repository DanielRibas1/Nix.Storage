using Nix.Store.Client.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Nix.Store.Server
{    
    public class FeedService : IFeedService
    {
        private DatabaseEngine _databaseEngine; 

        public FeedService()
        {
            var connStr = System.Configuration.ConfigurationManager.AppSettings.Get("ConfigBDConnectionString");
            _databaseEngine = new DatabaseEngine(connStr);
        }

        public string GetRawAppSettings(string appKey)
        {
            return _databaseEngine.GetRawAppSettings(appKey);
        }

        public IDictionary<string, IDictionary<string, object>> GetNetAppSettings(string appKey)
        {
            return _databaseEngine.GetAppSettings(appKey);
        }

        public IDictionary<string, IDictionary<string, object>> GetNetGroupAppSettings(string appKey, string groupName)
        {
            return _databaseEngine.GetAppGroupSettings(appKey, groupName);
        }
    }
}
