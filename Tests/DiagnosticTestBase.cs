using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace CodeConverter.Tests
{
	public abstract class DiagnosticTestBase
	{
		static MetadataReference mscorlib;
		static MetadataReference systemAssembly;
		static MetadataReference systemXmlLinq;
		static MetadataReference systemCore;
		private static MetadataReference visualBasic;

		internal static MetadataReference[] DefaultMetadataReferences;

		static DiagnosticTestBase()
		{
			try {
				mscorlib = MetadataReference.CreateFromFile(typeof(Console).Assembly.Location);
				systemAssembly = MetadataReference.CreateFromFile(typeof(System.ComponentModel.BrowsableAttribute).Assembly.Location);
				systemXmlLinq = MetadataReference.CreateFromFile(typeof(System.Xml.Linq.XElement).Assembly.Location);
				systemCore = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
				visualBasic = MetadataReference.CreateFromFile(typeof(Microsoft.VisualBasic.Constants).Assembly.Location);
				DefaultMetadataReferences = new[] {
					mscorlib,
					systemAssembly,
					systemCore,
					systemXmlLinq,
					visualBasic
				};
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		}
	}
}
