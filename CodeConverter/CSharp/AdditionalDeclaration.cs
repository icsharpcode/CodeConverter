using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class AdditionalDeclaration : IHoistedNode
    {
        public string Prefix { get; }
        public string Id { get; }
        public ExpressionSyntax Initializer { get; }
        public TypeSyntax Type { get; }

        public AdditionalDeclaration(string prefix, ExpressionSyntax initializer, TypeSyntax type)
        {
            Prefix = prefix;
            Id = $"ph{Guid.NewGuid().ToString("N")}";
            Initializer = initializer;
            Type = type;
        }

        public IdentifierNameSyntax IdentifierName => SyntaxFactory.IdentifierName(Id).WithAdditionalAnnotations(HoistedNodeState.Annotation);
    }
}
