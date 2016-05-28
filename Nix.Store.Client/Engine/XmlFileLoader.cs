using log4net;
using Nix.Store.Client.Constants;
using Nix.Store.Client.Exceptions;
using Nix.Store.Client.Sections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Nix.Store.Client.Engine
{
    [Serializable]
    public class XmlFileLoader : XmlLoader, ILoader
    {       
        public string SourceLocation { get; set; }
        public string AppKey { get; set; }
        public LoaderType LoaderType { get { return LoaderType.XmlFile; } }        
        public string Group { get; set; } 

        internal XmlFileLoader(string appKey, string sourceLocation, string group)
        {
            this.AppKey = appKey;
            if (!Uri.IsWellFormedUriString(sourceLocation, UriKind.RelativeOrAbsolute))
                throw new UriFormatException(sourceLocation);
            this.SourceLocation = LocateFile(sourceLocation);
            this.Group = group;
        }

        public ConcurrentDictionary<string, IDictionary<string, object>> Load()
        {
            var settings = new ConcurrentDictionary<string, IDictionary<string, object>>();
            var document = XDocument.Load(SourceLocation);
            var config = document.Element(XName.Get(XmlTags.DOCUMENT_TAG));
            if (config != null)
            {
                var settingsGroups = config.Elements(XName.Get(XmlTags.GROUPS_TAG));
                if (settingsGroups != null && settingsGroups.Any())
                {
                    foreach (XElement group in settingsGroups.Elements(XName.Get(XmlTags.GROUP_TAG)))
                    {
                        var bagType = group.Attribute(XName.Get(XmlTags.GROUP_KEY));                        
                        if (bagType == null)
                            throw new LoadConfigException(this.LoaderType, this.AppKey, "bagType attribute is Null");
                        LoadSection(ref settings, bagType.Value, group);
                    }
                    return settings;
                }          
            }
            return null;
        }

        private string LocateFile(string location)
        {
            if (File.Exists(location))
                return location;
            else if (!Path.IsPathRooted(location))
            {
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.RelativeSearchPath, location)))
                    return Path.Combine(AppDomain.CurrentDomain.RelativeSearchPath, location);
            }
            throw new FileNotFoundException("Xml Setting file not found", location);
        }
    }
}
