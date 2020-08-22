using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CodeConv.Shared.Util
{
    internal static class LooseAssemblyResolver
    {
        public void Install(this AppDomain appDomain) => appDomain.AssemblyResolve += LoadAnyVersion;

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
