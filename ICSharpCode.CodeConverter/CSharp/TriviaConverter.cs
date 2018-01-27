using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class TriviaConverter
    {
        private readonly Dictionary<SyntaxToken, SyntaxToken> trailingPortsDelegatedToParent = new Dictionary<SyntaxToken, SyntaxToken>();
        
        public T PortConvertedTrivia<T>(SyntaxNode sourceNode, T destination) where T : SyntaxNode
        {
            if (destination == null || sourceNode == null) return destination;

            destination = sourceNode.HasLeadingTrivia
                ? destination.WithLeadingTrivia(sourceNode.GetLeadingTrivia().ConvertTrivia())
                : destination;
            
            var lastSourceToken = sourceNode.GetLastToken();

            var descendantNodes = destination.DescendantNodes();//TODO Check/fix perf
            var missedPortsWhichAreChildren = trailingPortsDelegatedToParent
                .Where(tnp => tnp.Key != lastSourceToken)
                //TODO find a better way of testing a node is essentially the same one as the one we intended to replace
                .Where(tnp => descendantNodes.Select(f => f.FullSpan).Contains(tnp.Value.FullSpan))//TODO Check/fix perf
                .ToList();
            foreach (var missedPort in missedPortsWhichAreChildren.ToList()) {
                var missedPortValue = missedPort.Value;
                var missedPortKey = missedPort.Key;
                destination = destination.ReplaceToken(missedPortValue,
                missedPortValue.WithConvertedTrailingTriviaFrom(missedPortKey));
                MarkAsPorted(missedPort.Key);
            }
            
            if (!lastSourceToken.HasTrailingTrivia) return destination;
            if (lastSourceToken == sourceNode.Parent?.GetLastToken()) {
                DelegateToParent(lastSourceToken, destination.GetLastToken());
                return destination;
            }

            MarkAsPorted(lastSourceToken);
            var convertedTrivia = lastSourceToken.TrailingTrivia.ConvertTrivia();
            return destination.WithTrailingTrivia(convertedTrivia);
        }

        public void DelegateToParent(SyntaxToken lastSourceToken, SyntaxToken destination)
        {
            trailingPortsDelegatedToParent[lastSourceToken] = destination;
        }

        public void MarkAsPorted(SyntaxToken lastSourceToken)
        {
            trailingPortsDelegatedToParent.Remove(lastSourceToken);
        }
    }
}