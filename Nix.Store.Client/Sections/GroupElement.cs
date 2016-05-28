using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Sections
{
    public class GroupElement : ConfigurationElement
    {
        [ConfigurationProperty("id", IsRequired = true, IsKey= true)]
        public string Id
        {
            get
            {
                return (string)this["id"];
            }
            set
            {
                this["id"] = value;
            }
        }    
    }
}
