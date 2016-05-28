using Nix.Store.Client.Exceptions;
using Nix.Store.Client.Sections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Engine
{
    public class LoaderFactory
    {
        public static ILoader GetLoader(LoaderType type, string appKey, string sourceLocation)
        {
            switch (type)
            {                   
                case LoaderType.Manual:
                    return new ManualLoader(appKey, null);
                case LoaderType.RemoteWeb:
                    return String.IsNullOrEmpty(sourceLocation) ? new RemoteWebLoader(appKey, null) : new RemoteWebLoader(appKey, sourceLocation, null);
                case LoaderType.XmlFile:
                default:
                    if (String.IsNullOrEmpty(sourceLocation))
                        throw new LoadConfigException(type, appKey, "No SourceLocation found");
                    return new XmlFileLoader(appKey, sourceLocation, null);                    
            }
        }

        public static ILoader GetLoader(LoaderType type, string appKey, string sourceLocation, string group)
        {
            switch (type)
            {
                case LoaderType.Manual:
                    return new ManualLoader(appKey, group);
                case LoaderType.RemoteWeb:
                    return String.IsNullOrEmpty(sourceLocation) ? new RemoteWebLoader(appKey, group) : new RemoteWebLoader(appKey, sourceLocation, group);
                case LoaderType.XmlFile:
                default:
                    if (String.IsNullOrEmpty(sourceLocation))
                        throw new LoadConfigException(type, appKey, "No SourceLocation found");
                    return new XmlFileLoader(appKey, sourceLocation, group);
            }
        }
    }
}
