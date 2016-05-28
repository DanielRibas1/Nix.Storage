using log4net;
using Nix.Store.Client.Contract;
using Nix.Store.Client.Exceptions;
using Nix.Store.Client.Sections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Nix.Store.Client.Engine
{
    public class RemoteWebLoader : ILoader
    {
        private static ILog Logger = LogManager.GetLogger(typeof(RemoteWebLoader));

        public const string APPSETTINGS_OPEN_TIMEOUT_KEY = "RemoteWebLoaderOpenTimeout";
        public const string APPSETTINGS_CLOSE_TIMEOUT_KEY = "RemoteWebLoaderCloseTimeout";
        public const string APPSETTINGS_SEND_TIMEOUT_KEY = "RemoteWebLoaderSendTimeout";
        public const string APPSETTINGS_MAX_RECEIVED_SIZE = "RemoteWebLoaderMaxReceivedSize";
        public const string APPSETTINGS_REMOTE_CONFIG_URL = "RemoteConfigUrl";

        private const int DEFAULT_OPEN_TIMEOUT = 120;
        private const int DEFAULT_CLOSE_TIMEOUT = 80;
        private const int DEFAULT_SEND_TIMEOUT = 20;
        private const int DEFAULT_MAX_RECIVED_SIZE = int.MaxValue;

        static int OpenTimeout;
        static int CloseTimeout;
        static int SendTimeout;
        static int MaxReceivedSize;

        public string SourceLocation { get; set; }
        public string AppKey { get; set; }
        public LoaderType LoaderType { get { return LoaderType.RemoteWeb; } }
        public string Group { get; set; }
        public bool IsDefaultSource { get; set; }

        static RemoteWebLoader()
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                try { OpenTimeout = int.Parse(appSettings[APPSETTINGS_OPEN_TIMEOUT_KEY]); }
                catch { OpenTimeout = DEFAULT_OPEN_TIMEOUT; }
                try { CloseTimeout = int.Parse(appSettings[APPSETTINGS_CLOSE_TIMEOUT_KEY]); }
                catch { CloseTimeout = DEFAULT_CLOSE_TIMEOUT; }
                try { SendTimeout = int.Parse(appSettings[APPSETTINGS_SEND_TIMEOUT_KEY]); }
                catch { SendTimeout = DEFAULT_SEND_TIMEOUT; }
                try { MaxReceivedSize = int.Parse(appSettings[APPSETTINGS_MAX_RECEIVED_SIZE]); }
                catch { MaxReceivedSize = DEFAULT_MAX_RECIVED_SIZE; }
                Logger.Info("Successfully loaded Remote Web Channel Values from App.Config / Web.Config");
            }
            catch (Exception ex)
            {
                Logger.Warn("Unable to get AppSetting Values for Remote Web Channel Connection, Assigning Default Values", ex);
                OpenTimeout = DEFAULT_OPEN_TIMEOUT;
                CloseTimeout = DEFAULT_CLOSE_TIMEOUT;
                SendTimeout = DEFAULT_SEND_TIMEOUT;
                MaxReceivedSize = DEFAULT_SEND_TIMEOUT;
            }
        }
        internal RemoteWebLoader(string appKey, string group)
        {
            this.AppKey = appKey;
            this.IsDefaultSource = true;
            this.SourceLocation = ConfigurationManager.AppSettings[APPSETTINGS_REMOTE_CONFIG_URL];
            this.Group = group;
        }

        internal RemoteWebLoader(string appKey, string sourceLocation, string group)
        {
            this.AppKey = appKey;
            this.IsDefaultSource = false;
            if (!Uri.IsWellFormedUriString(sourceLocation, UriKind.Absolute))
                throw new UriFormatException(sourceLocation);
            this.SourceLocation = sourceLocation;
            this.Group = group;
        }

        public ConcurrentDictionary<string, IDictionary<string, object>> Load()
        {
            var s = Stopwatch.StartNew();
            try
            {
                var channel = GetFeedServiceChannel();
                Logger.Debug("FeedService Channel Successfully Created.");
                IDictionary<string, IDictionary<string, object>> settings = null;
                if (String.IsNullOrEmpty(Group))
                {                    
                    settings = channel.GetNetAppSettings(AppKey);
                    s.Stop();
                    Logger.DebugFormat("{0} Settings Retrieved in {1}", AppKey, s.Elapsed.ToString("c"));
                }
                else
                {
                    settings = channel.GetNetGroupAppSettings(AppKey, Group);
                    s.Stop();
                    Logger.DebugFormat("{0}.{1} Settings Retrieved in {2}", AppKey, Group, s.Elapsed.ToString("c"));
                }
                
                if (settings is IDictionary<string, IDictionary<string, object>>)
                {
                    if (settings is ConcurrentDictionary<string, IDictionary<string, object>>)
                        return (ConcurrentDictionary<string, IDictionary<string, object>>)settings;
                    else
                        return new ConcurrentDictionary<string, IDictionary<string, object>>(
                            ((IDictionary<string, IDictionary<string, object>>)settings)
                            .Cast<KeyValuePair<string, IDictionary<string, object>>>());
                }                    
                throw new LoadConfigException(this.LoaderType, this.AppKey, "Recived Type is not Expected");   
            }
            catch (Exception ex)
            {
                s.Stop();
                throw new LoadConfigException(this.LoaderType, this.AppKey, ex);                
            }
        }

        private IFeedService GetFeedServiceChannel()
        {            
            var binding = new BasicHttpBinding();                                   
            binding.OpenTimeout = TimeSpan.FromSeconds(OpenTimeout);
            binding.CloseTimeout = TimeSpan.FromSeconds(CloseTimeout);
            binding.SendTimeout = TimeSpan.FromSeconds(SendTimeout);
            binding.MaxReceivedMessageSize = MaxReceivedSize; 
           
            var endpoint = new EndpointAddress(SourceLocation);            
            var channelFactory = new ChannelFactory<IFeedService>(binding, endpoint);            
            return channelFactory.CreateChannel();
        }
    }
}
