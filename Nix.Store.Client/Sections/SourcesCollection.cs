using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Sections
{
    public class SourcesCollection : ConfigurationElementCollection
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
            return new SourceElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SourceElement)element).AppKey;
        }       

        new public SourceElement this[string name]
        {
            get
            {
                return (SourceElement)BaseGet(name);
            }
        }     

    }
}
