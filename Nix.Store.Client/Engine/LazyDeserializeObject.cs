using Nix.Store.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;

namespace Nix.Store.Client.Engine
{    
    [DataContract]
    public class LazyDeserializeObject
    {
        private XmlLoader _loader;
        [DataMember]
        public string Key { get; private set; }
        [DataMember]
        public string TypeName { get; private set; }
        [DataMember]
        public XElement SerializedElement { get; private set; }

        public LazyDeserializeObject(string key, string typeName, XElement element)
        {
            this.Key = key;
            this.TypeName = typeName;
            this.SerializedElement = element;
        }

        public object Deserialize()
        {
            _loader = new XmlLoader();
            string key;
            object obj;
            if (_loader.TryCreateSettingObject(SerializedElement, out key, out obj))
                return obj;
            else
                throw new LazyDeserializationException(Key, TypeName);
        }
        
    }
}
