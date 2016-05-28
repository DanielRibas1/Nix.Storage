using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Exceptions
{
    public class PropertyNotFoundExcpetion : Exception
    {
        public string PropertyKey { get; set; }
        public string Group { get; set; }
        public PropertyNotFoundExcpetion(string group, string key)
            : base(String.Format("Key {0} not found on PropertyBag {1}", key, group))
        {
            this.PropertyKey = key;
            this.Group = group;
        }
    }
}
