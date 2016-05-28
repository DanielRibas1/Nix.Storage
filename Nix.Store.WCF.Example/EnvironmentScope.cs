using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nix.Store.Server
{
    public static class EnvironmentScope
    {
        public static string Current { get; private set; }
        static EnvironmentScope()
        {
#if TESTR3X
            Current = "prodr3x";
#elif PRODR3X
            Current = "testr3x";
#else
            Current = "trainr3x";
#endif
        }
    }
}