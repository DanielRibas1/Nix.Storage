using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Sections
{
    public class SourceElement : ConfigurationElement
    {
        [ConfigurationProperty("appkey", IsKey = true, IsRequired = true)]
        public string AppKey
        {
            get
            {
                return (string)this["appkey"];
            }
            set
            {
                this["appkey"] = value;
            }
        }

        [ConfigurationProperty("allGroups", IsRequired=false, DefaultValue= "false")]
        public bool AllGroups
        {
            get
            {
                return (bool)this["allGroups"];
            }
            set
            {
                this["allGroups"] = value;
            }
        }

        [ConfigurationProperty("description", IsRequired=false)]
        public string Description
        {
            get
            {
                return (string)this["description"];
            }
            set
            {
                this["description"] = value;
            }
        }

        [ConfigurationProperty("groups", IsRequired=false)]
        [ConfigurationCollection(typeof(GroupsCollection), AddItemName="add")]
        public GroupsCollection Groups
        {
            get
            {
                var groups = (GroupsCollection)base["groups"];
                return groups;
            }
        }

    }
}
