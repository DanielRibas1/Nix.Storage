using Nix.Store.Client.Sections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Engine
{
    public interface ILoader
    {
        string SourceLocation { get; set; }
        string AppKey { get; set; }
        LoaderType LoaderType { get; }
        string Group { get; set; }
        ConcurrentDictionary<string, IDictionary<string, object>> Load();
    }
}
