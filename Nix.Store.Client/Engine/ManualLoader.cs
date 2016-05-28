using Nix.Store.Client.Sections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Engine
{
    public class ManualLoader : ILoader
    {
        private const string MANUAL_SOURCE = "Runtime";

        public string SourceLocation { get; set; }
        public string AppKey { get; set; }
        public LoaderType LoaderType { get { return LoaderType.Manual; } }
        public string Group { get; set; }

        public ManualLoader(string appKey, string group)
        {
            this.AppKey = appKey;
            this.Group = group;
            this.SourceLocation = MANUAL_SOURCE;
        }

        public ConcurrentDictionary<string, IDictionary<string, object>> Load()
        {
            throw new NotSupportedException();
        }
    }
}
