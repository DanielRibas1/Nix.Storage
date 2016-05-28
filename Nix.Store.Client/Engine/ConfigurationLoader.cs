using log4net;
using Nix.Store.Client.Exceptions;
using Nix.Store.Client.Sections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Engine
{
    public sealed class ConfigurationLoader
    {
        private const string REMOTE_CONFIG_TAG = "RemoteConfigUrl";
        private const string CONFIG_NAME = "Nix.Store.Client";

        private static ILog Logger = LogManager.GetLogger(typeof(ConfigurationLoader));
        private static string _remoteConfigLocation;
        public static string RemoteConfigLocation 
        { 
            get 
            { 
                if (String.IsNullOrEmpty(_remoteConfigLocation))
                    _remoteConfigLocation = ConfigurationManager.AppSettings[REMOTE_CONFIG_TAG]; 
                return _remoteConfigLocation;
            } 
        }

        public static void LoadAppConfigSection()
        {
            try
            {
                ConfigurationDefinitionSection section = (ConfigurationDefinitionSection)ConfigurationManager.GetSection(CONFIG_NAME);
                if (section != null)
                {
                    foreach (LoaderElement app in section.LoaderCollection)
                    {                                           
                        foreach (SourceElement source in app.Sources)
                        {
                            if (source.AllGroups)
                            {
                                try
                                {
                                    var loader = LoaderFactory.GetLoader(app.LoaderType, source.AppKey, app.Location);                                                                                                            
                                    SettingsManager.LoadStore(source.AppKey, loader);
                                }
                                catch (LoadConfigException lEx)
                                {
                                    Logger.Error(lEx);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(new LoadConfigException(app.LoaderType, source.AppKey, ex));
                                }
                            }
                            else
                            {
                                foreach (GroupElement group in source.Groups)
                                {
                                    try
                                    {
                                        if (String.IsNullOrEmpty(group.Id))
                                            throw new LoadConfigException(app.LoaderType, source.AppKey, "Group id is null");
                                        var loader = LoaderFactory.GetLoader(app.LoaderType, source.AppKey, app.Location, group.Id);
                                        SettingsManager.LoadStore(source.AppKey, loader);
                                    }
                                    catch (LoadConfigException lEx)
                                    {
                                        Logger.Error(lEx);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error(new LoadConfigException(app.LoaderType, source.AppKey, ex));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                throw new Exception("Fatal loading configurations, see inner exception", ex);
            }
        }               
    }
}
