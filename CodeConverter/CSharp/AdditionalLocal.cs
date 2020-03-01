using System;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class AdditionalLocal
    {
        public string Prefix { get; }
        public string ID { get; }
        public ExpressionSyntax Initializer { get; }
        public TypeSyntax Type { get; }

        public AdditionalLocal(string prefix, ExpressionSyntax initializer, TypeSyntax type)
        {
            Prefix = prefix;
            ID = $"ph{Guid.NewGuid().ToString("N")}";
            Initializer = initializer;
            Type = type;
        }
    }
}
