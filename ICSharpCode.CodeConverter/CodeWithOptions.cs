using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter
{
    public class CodeWithOptions
    {
        public static readonly IReadOnlyCollection<MetadataReference> DefaultMetadataReferences = GetRefs(
            typeof(System.Text.Encoding),
            typeof(System.ComponentModel.BrowsableAttribute),
            typeof(System.Dynamic.DynamicObject),
            typeof(System.Data.DataRow),
            typeof(System.Net.Http.HttpClient),
            typeof(System.Web.HttpUtility),
            typeof(System.Xml.XmlElement),
            typeof(System.Xml.Linq.XElement),
            typeof(Microsoft.VisualBasic.Constants)).ToArray();

        private static IEnumerable<MetadataReference> GetRefs(params Type[] types)
        {
            return types.Select(type => MetadataReference.CreateFromFile(type.Assembly.Location));
        }

        public string Text { get; private set; }
        public string FromLanguage { get; private set; }
        public int FromLanguageVersion { get; private set; }
        public string ToLanguage { get; private set; }
        public int ToLanguageVersion { get; private set; }

        public IReadOnlyCollection<MetadataReference> References { get; set; } = new List<MetadataReference>();

        public CodeWithOptions(string text)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            FromLanguage = LanguageNames.CSharp;
            ToLanguage = LanguageNames.VisualBasic;
            FromLanguageVersion = 6;
            ToLanguageVersion = 14;
        }

        public CodeWithOptions SetFromLanguage(string name = LanguageNames.CSharp, int version = 6)
        {
            FromLanguage = name;
            FromLanguageVersion = version;
            return this;
        }

        public CodeWithOptions SetToLanguage(string name = LanguageNames.VisualBasic, int version = 14)
        {
            ToLanguage = name;
            ToLanguageVersion = version;
            return this;
        }

        public CodeWithOptions WithDefaultReferences()
        {
            References = DefaultMetadataReferences;
            return this;
        }
    }
}
