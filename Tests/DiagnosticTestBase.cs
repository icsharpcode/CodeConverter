using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace CodeConverter.Tests
{
    public abstract class DiagnosticTestBase
    {
        static MetadataReference _mscorlib;
        static MetadataReference _systemAssembly;
        static MetadataReference _systemXmlLinq;
        static MetadataReference _systemCore;
        private static MetadataReference _visualBasic;

        internal static MetadataReference[] DefaultMetadataReferences;

        static DiagnosticTestBase()
        {
            try {
                _mscorlib = MetadataReference.CreateFromFile(typeof(Console).Assembly.Location);
                _systemAssembly = MetadataReference.CreateFromFile(typeof(System.ComponentModel.BrowsableAttribute).Assembly.Location);
                _systemXmlLinq = MetadataReference.CreateFromFile(typeof(System.Xml.Linq.XElement).Assembly.Location);
                _systemCore = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
                _visualBasic = MetadataReference.CreateFromFile(typeof(Microsoft.VisualBasic.Constants).Assembly.Location);
                DefaultMetadataReferences = new[] {
                    _mscorlib,
                    _systemAssembly,
                    _systemCore,
                    _systemXmlLinq,
                    _visualBasic
                };
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }
    }
}
