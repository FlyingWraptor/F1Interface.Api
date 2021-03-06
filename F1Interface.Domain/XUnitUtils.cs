#if DEBUG
using System;
using System.Linq;
using System.Reflection;

namespace F1Interface.Domain
{
    internal static class XUnitUtils
    {
        internal static bool IsUnitTesting { get; set; }

        static XUnitUtils()
        {
            if (AppDomain.CurrentDomain.GetAssemblies()
                        .Any(x => x.FullName.StartsWith("F1Interface.Tests")))
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.FullName.ToLowerInvariant().StartsWith("xunit."))
                    {
                        IsUnitTesting = true;
                        break;
                    }
                }
            }
        }
    }
}
#endif