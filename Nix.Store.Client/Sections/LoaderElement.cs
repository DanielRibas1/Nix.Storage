using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Sections
{
    public class LoaderElement : ConfigurationElement
    {
        [ConfigurationProperty("type", DefaultValue = "xmlfile")]
        [TypeConverter(typeof(CaseInsensitiveEnumConfigConverter<LoaderType>))]
        public LoaderType LoaderType
        {
            get
            {
                return (LoaderType)this["type"];
            }
            set
            {
                this["type"] = (LoaderType)value;
            }
        }

        [ConfigurationProperty("location", IsRequired = false, IsKey = true)]
        public string Location
        {
            get
            {
                return (string)this["location"];
            }
            set
            {
                this["location"] = value;
            }
        }         

        [ConfigurationProperty("sources")]
        [ConfigurationCollection(typeof(SourcesCollection), AddItemName="add", ClearItemsName="clear", RemoveItemName="remove")]
        public SourcesCollection Sources
        {
            get
            {
                var sources = (SourcesCollection)base["sources"];
                return sources;
            }            
        }


    }
}
