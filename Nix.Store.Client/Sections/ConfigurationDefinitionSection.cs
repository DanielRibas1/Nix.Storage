using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Nix.Store.Client.Sections
{
    public sealed class ConfigurationDefinitionSection : ConfigurationSection
    {
        [ConfigurationProperty("loaders", IsDefaultCollection=true)]
        [ConfigurationCollection(typeof(LoadersCollection), AddItemName="add", ClearItemsName="clear", RemoveItemName="remove")]
        public LoadersCollection LoaderCollection
        {
            get
            {
                var loaderCollection = (LoadersCollection)base["loaders"];
                return loaderCollection;
            }
        }
    }
}
