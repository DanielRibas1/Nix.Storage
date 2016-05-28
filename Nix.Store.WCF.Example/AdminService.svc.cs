using Nix.Store.Client.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Xml.Linq;

namespace Nix.Store.Server
{
    public class AdminService : IAdminService
    {
        private DatabaseEngine _databaseEngine;
        
        public AdminService()
        {
            var connStr = System.Configuration.ConfigurationManager.AppSettings.Get("ConfigBDConnectionString");
            _databaseEngine = new DatabaseEngine(connStr);
        }

        public void SetXmlSettings(string requestAgent, string rawXmlSettings)
        {
            Application decoded = null;
            using (var reader = new StringReader(rawXmlSettings))
            {
                decoded = _databaseEngine.DecodeSettings(XDocument.Load(reader, LoadOptions.PreserveWhitespace));
            }

            _databaseEngine.SetAppFullSettings(requestAgent, decoded);
        }

        public string GetXmlSettings(string appKey)
        {
            return _databaseEngine.GetRawAppSettings(appKey);
        }


        public List<string> GetAvailableAppKeys()
        {
            return _databaseEngine.GetAppKeys();
        }
    }
}
