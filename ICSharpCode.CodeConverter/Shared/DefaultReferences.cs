using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    /// <summary>
    /// This file requires net standard 2.0 or above. Therefore it should be linked into projects referencing the converter to get a wider range of references.
    /// </summary>
    public class DefaultReferences
    {
        private static readonly Type[] TypesToLoadAssembliesFor = {
            typeof(System.Text.Encoding),
            typeof(Enumerable),
            typeof(System.ComponentModel.BrowsableAttribute),
            typeof(System.Dynamic.DynamicObject),
            typeof(System.Data.DataRow),
            typeof(System.Net.Http.HttpClient),
            typeof(System.Web.HttpUtility),
            typeof(System.Xml.XmlElement),
            typeof(System.Xml.Linq.XElement),
            typeof(Microsoft.VisualBasic.Constants)
        };

        public static IReadOnlyCollection<PortableExecutableReference> NetStandard2 => GetRefs(TypesToLoadAssembliesFor).ToArray();

        private static IEnumerable<PortableExecutableReference> GetRefs(IReadOnlyCollection<Type> types)
        {
            return types.Select(type => MetadataReference.CreateFromFile(type.Assembly.Location));
        }
    }
}
