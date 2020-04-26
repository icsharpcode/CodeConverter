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


        public static IEnumerable<StatementSyntax> ReplaceNames(IEnumerable<StatementSyntax> csNodes, Dictionary<string, string> newNames)
        {
            csNodes = csNodes.Select(csNode => ReplaceNames(csNode, newNames)).ToList();
            return csNodes;
        }

        public static T ReplaceNames<T>(T csNode, Dictionary<string, string> newNames) where T: SyntaxNode
        {
            return csNode.ReplaceNodes(csNode.GetAnnotatedNodes(HoistedNodeState.Annotation), (_, withReplaced) => {
                var idns = (IdentifierNameSyntax)withReplaced;
                if (newNames.TryGetValue(idns.Identifier.ValueText, out var newName)) {
                    return idns.WithoutAnnotations(HoistedNodeState.Annotation).WithIdentifier(SyntaxFactory.Identifier(newName));
                }
                return idns;
            });
        }
    }
}
