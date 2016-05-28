using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nix.Store.Server.Exceptions
{
    public class GroupKeyNotFoundException : Exception
    {
        public string AppKey { get; private set; }
        public string GroupName { get; private set; }

        public GroupKeyNotFoundException(string appKey, string groupName)
            : base(String.Format("Group {0} inside Application {1} configuration not found", groupName, appKey))
        {
            this.AppKey = appKey;
            this.GroupName = groupName;
            this.Data.Add("appKey", AppKey);
            this.Data.Add("groupName", GroupName);
        }
    }
}
