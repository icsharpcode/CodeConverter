using System;
using System.Reflection;

namespace CodeConv.Shared.Util
{
    internal static class AppDomainExtensions
    {
        private static bool _hasRegisteredAssemblyResolveEvent;
        private static bool _useVersionAgnosticAssemblyResolution;
        public static void UseVersionAgnosticAssemblyResolution(this AppDomain appDomain, bool enable = true)
        {
            _useVersionAgnosticAssemblyResolution = enable;
            if (_useVersionAgnosticAssemblyResolution && !_hasRegisteredAssemblyResolveEvent) {
                appDomain.AssemblyResolve += LoadAnyVersion;
                _hasRegisteredAssemblyResolveEvent = true;
            }
        }

        private static Assembly? LoadAnyVersion(object? sender, ResolveEventArgs? args)
        {
            if (!_useVersionAgnosticAssemblyResolution || args?.Name == null) return null;
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
