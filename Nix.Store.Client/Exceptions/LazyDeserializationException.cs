using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Exceptions
{
    public class LazyDeserializationException : Exception
    {

        public string SettingKey { get; private set; }
        public string TypeName { get; private set; }

        public LazyDeserializationException(string key, string typeName)
            : base(String.Format(""))
        {
            this.SettingKey = key;
            this.TypeName = typeName;
        }
    }
}
