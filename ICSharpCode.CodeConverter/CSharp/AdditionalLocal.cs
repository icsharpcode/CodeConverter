using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class AdditionalLocal
    {
        public static SyntaxAnnotation Annotation = new SyntaxAnnotation("CodeconverterAdditionalLocal");

        public string Prefix { get; private set; }
        public string ID { get; private set; }
        public ExpressionSyntax Initializer { get; private set; }
        
        public AdditionalLocal(string prefix, ExpressionSyntax initializer)
        {
            Prefix = prefix;
            ID = $"ph{Guid.NewGuid().ToString("N")}";
            Initializer = initializer;
        }
    }
}
