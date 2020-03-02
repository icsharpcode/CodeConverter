using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter
{
    public class CodeWithOptions
    {
        private static readonly HashSet<Type> _typesToFindAssemblyReferencesFrom = new HashSet<Type> {
            typeof(System.Text.Encoding),
            typeof(System.ComponentModel.DefaultValueAttribute),
            typeof(System.Dynamic.DynamicObject),
            typeof(System.Net.Http.HttpClient),
            typeof(System.Xml.XmlConvert),
            typeof(System.Xml.Linq.XElement),
            typeof(Microsoft.VisualBasic.Constants)};

        private static IReadOnlyCollection<PortableExecutableReference> NetStandard3MetadataReferences => GetRefs(_typesToFindAssemblyReferencesFrom).ToArray();

        private static IEnumerable<PortableExecutableReference> GetRefs(IReadOnlyCollection<Type> types)
        {
            return types.Select(type => MetadataReference.CreateFromFile(type.Assembly.Location));
        }

        public string Text { get; private set; }
        public string FromLanguage { get; private set; }
        public int FromLanguageVersion { get; private set; }
        public string ToLanguage { get; private set; }
        public int ToLanguageVersion { get; private set; }

        public IReadOnlyCollection<PortableExecutableReference> References { get; set; } = new List<PortableExecutableReference>();

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

        public CodeWithOptions WithTypeReferences(IReadOnlyCollection<PortableExecutableReference> references = null)
        {
            References = references ?? NetStandard3MetadataReferences;
            return this;
        }
    }
}
