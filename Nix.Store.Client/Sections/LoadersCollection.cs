using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Sections
{
    public class LoadersCollection : ConfigurationElementCollection
    {       

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new LoaderElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((LoaderElement)element).Location;
        }

        new public LoaderElement this[string name]
        {
            get
            {
                return (LoaderElement)BaseGet(name);
            }
        }     
    }
}
