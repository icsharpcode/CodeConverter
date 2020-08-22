using System;
using System.Reflection;

namespace CodeConv.Shared.Util
{
    internal static class AppDomainExtensions
    {
        public static void UseVersionAgnosticAssemblyResolution(this AppDomain appDomain) => appDomain.AssemblyResolve += LoadAnyVersion;

        private static Assembly? LoadAnyVersion(object? sender, ResolveEventArgs? args)
        {
            if (args?.Name == null) return null;
            var requestedAssemblyName = new AssemblyName(args.Name);
            if (requestedAssemblyName.Version != null && requestedAssemblyName.Name != null) {
                try {
                    return Assembly.Load(new AssemblyName(requestedAssemblyName.Name) { CultureName = requestedAssemblyName.CultureName });
                } catch (Exception) {
                    return null; //Give other handlers a chance
                }
            }
            return null;

        }
    }
}
