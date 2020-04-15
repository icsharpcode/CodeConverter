using System;
using System.Reflection;

namespace CodeConv.Shared.Util
{
    internal static class AppDomainExtensions
    {
        public static void UseVersionAgnosticAssemblyResolution(this AppDomain appDomain) => appDomain.AssemblyResolve += LoadAnyVersion;

        private static Assembly? LoadAnyVersion(object sender, ResolveEventArgs args)
        {
            var requestedAssemblyName = new AssemblyName(args.Name);
            if (requestedAssemblyName.Version != null) {
                return Assembly.Load(new AssemblyName(requestedAssemblyName.Name) { CultureName = requestedAssemblyName.CultureName });
            }
            return null;

        }
    }
}
