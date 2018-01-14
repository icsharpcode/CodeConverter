using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class TriviaConverter
    {
        private readonly Dictionary<SyntaxToken, SyntaxNode> trailingPortsDelegatedToParent = new Dictionary<SyntaxToken, SyntaxNode>();
        
        public T PortConvertedTrivia<T>(SyntaxNode sourceNode, T destination) where T : SyntaxNode
        {
            if (destination == null || sourceNode == null) return destination;

            destination = sourceNode.HasLeadingTrivia
                ? destination.WithLeadingTrivia(sourceNode.GetLeadingTrivia().ConvertTrivia())
                : destination;

            if (!sourceNode.HasTrailingTrivia) return destination;

            var lastSourceToken = sourceNode.GetLastToken();

            var descendantNodes = destination.DescendantNodes();//TODO Check the perf of this, seems pretty bad at a glance
            var missedPortsWhichAreChildren = trailingPortsDelegatedToParent
                .Where(tnp => tnp.Key != lastSourceToken)
                .Where(tnp => descendantNodes.Contains(tnp.Value))
                .ToList();
            foreach (var missedPort in missedPortsWhichAreChildren.ToList()) {
                destination = destination.ReplaceNode(missedPort.Value,
                missedPort.Value.WithTrailingTrivia(missedPort.Key.TrailingTrivia));
                trailingPortsDelegatedToParent.Remove(missedPort.Key);
            }

            if (lastSourceToken == sourceNode.Parent.GetLastToken()) {
                trailingPortsDelegatedToParent[lastSourceToken] = destination;
                return destination;
            }

            trailingPortsDelegatedToParent.Remove(lastSourceToken);
            var convertedTrivia = lastSourceToken.TrailingTrivia.ConvertTrivia();
            return destination.WithTrailingTrivia(convertedTrivia);
        }
    }
}