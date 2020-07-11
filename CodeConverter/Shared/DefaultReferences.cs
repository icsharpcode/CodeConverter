using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    /// <summary>
    /// This file requires net standard 2.0 or above. Therefore it should be linked into projects referencing the converter to get a wider range of references.
    /// </summary>
    public static class DefaultReferences
    {
        private static readonly Assembly[] DefaultAssemblies = new []{
            typeof(object),
            typeof(IEnumerable),
            typeof(IEnumerable<>),
            typeof(ErrorEventArgs),
            typeof(System.Text.Encoding),
            typeof(Enumerable),
            typeof(System.ComponentModel.BrowsableAttribute),
            typeof(System.Dynamic.DynamicObject),
            typeof(System.Data.DataRow),
            typeof(System.Data.DataTableExtensions),
            typeof(System.Net.Http.HttpClient),
            typeof(System.Web.HttpUtility),
            typeof(System.Xml.XmlElement),
            typeof(System.Xml.Linq.XElement),
            typeof(Microsoft.VisualBasic.Constants),
            typeof(System.Data.SqlClient.SqlCommand),
        }.Select(t => t.Assembly).Concat(
            new[] { Assembly.Load("System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a") }
            ).ToArray();

        private static Dictionary<string, (string Location, string[] ReferenceNames)> _assemblyInfoCache = new Dictionary<string, (string Location, string[] ReferenceNames)>();

        public static IReadOnlyCollection<PortableExecutableReference> NetStandard2 { get; } =
            With(Enumerable.Empty<Assembly>()).ToArray();

        public static IReadOnlyCollection<PortableExecutableReference> With(IEnumerable<Assembly> assemblies) =>
            GetRefs(GetPathsForAllReferences(DefaultAssemblies.Concat(assemblies))).ToArray();

        private static IEnumerable<PortableExecutableReference> GetRefs(IEnumerable<string> assemblyLocations) =>
            assemblyLocations.Select(a => MetadataReference.CreateFromFile(a));

        private static IReadOnlyCollection<string> GetPathsForAllReferences(IEnumerable<Assembly> enumerable)
        {
            // Add to cache while loaded assembly is present
            foreach (var assembly in enumerable) GetAssemblyInfo(assembly);

            var fullNamesToAdd = new Queue<string>(enumerable.Select(a => a.FullName));
            var assemblyNameToPath = new Dictionary<string, string>();
            while (fullNamesToAdd.Any()) {
                var fullName = fullNamesToAdd.Dequeue();
                if (!assemblyNameToPath.ContainsKey(fullName)) {
                    var (location, referenceNames) = GetAssemblyInfo(fullName);
                    foreach (var reference in referenceNames) {
                        fullNamesToAdd.Enqueue(reference);
                    }
                    if (location != null && File.Exists(location)) {
                        assemblyNameToPath.Add(fullName, location);
                    }
                }
            }

            return assemblyNameToPath.Values;
        }

        private static (string Location, string[] ReferenceNames) GetAssemblyInfo(string assemblyName)
        {
            if (_assemblyInfoCache.TryGetValue(assemblyName, out var assemblyInfo)) {
                return assemblyInfo;
            } else if (LoadOrNull(assemblyName) is { } a) {
                return GetAssemblyInfo(a);
            }

            assemblyInfo = (default(string), Array.Empty<string>());
            _assemblyInfoCache.Add(assemblyName, assemblyInfo);
            return assemblyInfo;
        }

        private static Assembly LoadOrNull(string assemblyName)
        {
            try {
                return Assembly.Load(assemblyName);
            } catch (Exception) {
                return null; //TODO Log
            }
        }

        private static (string Location, string[] ReferenceNames) GetAssemblyInfo(Assembly assembly)
        {
            if (!_assemblyInfoCache.TryGetValue(assembly.FullName, out var assemblyInfo)) {
                var referenceNames = assembly.GetReferencedAssemblies().Select(a => a.FullName).ToArray();
                assemblyInfo = (assembly.IsDynamic ? null : assembly.Location, referenceNames);
                _assemblyInfoCache[assembly.FullName] = assemblyInfo;
            }
            return assemblyInfo;
        }
    }
}
