using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace CodeConverter.Tests
{
    public abstract class DiagnosticTestBase
    {
        internal static MetadataReference[] DefaultMetadataReferences;

        static DiagnosticTestBase()
        {
            try {
                DefaultMetadataReferences = GetRefs(
                    typeof(System.Text.Encoding),
                    typeof(System.ComponentModel.BrowsableAttribute),
                    typeof(System.Dynamic.DynamicObject),
                    typeof(System.Data.DataRow),
                    typeof(System.Data.DataRowExtensions),
                    typeof(System.Net.Http.HttpClient),
                    typeof(System.Xml.XmlElement),
                    typeof(System.Xml.Linq.XElement),
                    typeof(Microsoft.VisualBasic.Constants)).ToArray();
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        private static IEnumerable<MetadataReference> GetRefs(params Type[] types)
        {
            return types.Select(type => MetadataReference.CreateFromFile(type.Assembly.Location));
        }
    }
}
