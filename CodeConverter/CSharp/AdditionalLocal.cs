using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class AdditionalLocal
    {
        public string Prefix { get; }
        public string Id { get; }
        public ExpressionSyntax Initializer { get; }
        public TypeSyntax Type { get; }

        public AdditionalLocal(string prefix, ExpressionSyntax initializer, TypeSyntax type)
        {
            Prefix = prefix;
            Id = $"ph{Guid.NewGuid().ToString("N")}";
            Initializer = initializer;
            Type = type;
        }

        public IdentifierNameSyntax IdentifierName => SyntaxFactory.IdentifierName(Id).WithAdditionalAnnotations(AdditionalLocals.Annotation);
    }
}
