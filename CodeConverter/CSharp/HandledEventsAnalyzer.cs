using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class HandledEventsAnalyzer
    {
        private readonly CommonConversions _commonConversions;
        private readonly INamedTypeSymbol _type;
        private readonly SemanticModel _semanticModel;

        private HandledEventsAnalyzer(CommonConversions commonConversions, INamedTypeSymbol type)
        {
            _commonConversions = commonConversions;
            _semanticModel = commonConversions.SemanticModel;
            _type = type;
        }

        public static Task<HandledEventsAnalysis> AnalyzeAsync(CommonConversions commonConversions, INamedTypeSymbol type)
        {
            return new HandledEventsAnalyzer(commonConversions, type).AnalyzeAsync();
        }
        private async Task<HandledEventsAnalysis> AnalyzeAsync()
        {
            var ancestorPropsMembersByName = _type.GetBaseTypesAndThis().SelectMany(t => t.GetMembers().Where(MemberCanHandleEvents))
                .ToLookup(m => m.Name)
                .ToDictionary(m => m.Key, g => g.First()); // Uses the fact that ToLookup maintains addition order to get the closest declaration

            var methodWithHandleses = _type.GetMembers().OfType<IMethodSymbol>()
                .Where(m => HandledEvents(m).Any())
                .Select(m => {
                    var ids = HandledEvents(m)
                        .Select(p => (GetCSharpIdentifierText(p.EventContainer), _commonConversions.ConvertIdentifier(p.EventMember.Identifier, sourceTriviaMapKind: SourceTriviaMapKind.None), p.Event,
                            p.ParametersToDiscard))
                        .ToList();
                    var csFormIds = ids.Where(id => id.Item1 == "this" || id.Item1 == "base").ToList();
                    var csPropIds = ids.Except(csFormIds).ToList();
                    if (!csPropIds.Any() && !csFormIds.Any()) return null;
                    var csMethodId = SyntaxFactory.Identifier(m.Name);
                    return new MethodWithHandles(_commonConversions.CsSyntaxGenerator, csMethodId, csPropIds, csFormIds);
                }).Where(x => x != null);

            IPropertySymbol[] writtenWithEventsProperties;
            if (!_type.IsSealed) {
                var baseMembers = methodWithHandleses.Select(name => ancestorPropsMembersByName[name.MethodCSharpId.Text].FirstOrDefault()).Where(x => x != null);
                writtenWithEventsProperties = await _type.GetMembers().OfType<IPropertySymbol>().ToAsyncEnumerable().WhereAwait(async p => !await IsNeverWrittenOrOverriddenAsync(p)).ToArrayAsync();
            } else {
                writtenWithEventsProperties = Array.Empty<IPropertySymbol>();
            }

            var handledMethodsFromPropertyWithEventName = methodWithHandleses
                .SelectMany(m => m.HandledPropertyEventCSharpIds.Select(h => (EventPropertyName: h.EventContainerName, MethodWithHandles: m)))
                .ToLookup(m => m.EventPropertyName, m => m.MethodWithHandles);
            var propertiesWithEvents = writtenWithEventsProperties.ToDictionary(p => p.Name, p => (p: p, handledMethodsFromPropertyWithEventName[p.Name].ToArray()));
            var containerFieldsConvertedToProperties = methodWithHandleses
                .SelectMany(m => m.HandledPropertyEventCSharpIds, (_, handled) => handled.EventContainerName)
                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
            return new HandledEventsAnalysis(_commonConversions, propertiesWithEvents, containerFieldsConvertedToProperties);
        }

        private async Task<bool> IsNeverWrittenOrOverriddenAsync(ISymbol symbol, Location allowedLocation = null)
        {
            var projectSolution = _commonConversions.Document.Project.Solution;
            if (!await projectSolution.IsNeverWrittenAsync(symbol, allowedLocation)) return false;
            var explicitPropertyOverrides = await SymbolFinder.FindOverridesAsync(symbol, projectSolution);
            if (explicitPropertyOverrides.Any()) return false;
            var classOverrides = (await SymbolFinder.FindOverridesAsync(symbol.ContainingType, projectSolution)).ToArray();
            if (classOverrides.Any()) return false;
            return true;
        }

        /// <summary>
        /// VBasic.VisualBasicExtensions.HandledEvents(m) seems to optimize away some events, so just detect from syntax
        /// </summary>
        private List<(Microsoft.CodeAnalysis.VisualBasic.Syntax.EventContainerSyntax EventContainer, Microsoft.CodeAnalysis.VisualBasic.Syntax.IdentifierNameSyntax EventMember, IEventSymbol Event, int ParametersToDiscard)> HandledEvents(IMethodSymbol m)
        {
            return m.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodStatementSyntax>()
                .Where(mbb => mbb.HandlesClause?.Events.Any() == true)
                .SelectMany(mbb => HandledEvent(mbb))
                .ToList();
        }

        private IEnumerable<(Microsoft.CodeAnalysis.VisualBasic.Syntax.EventContainerSyntax EventContainer, Microsoft.CodeAnalysis.VisualBasic.Syntax.IdentifierNameSyntax EventMember, IEventSymbol Event, int ParametersToDiscard)> HandledEvent(Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodStatementSyntax mbb)
        {
            var mayRequireDiscardedParameters = !mbb.ParameterList.Parameters.Any();
            //TODO: PERF: Get group by syntax tree and get semantic model once in case it doesn't get successfully cached
            var semanticModel = mbb.SyntaxTree == _commonConversions.SemanticModel.SyntaxTree ? _semanticModel : _commonConversions.Compilation.GetSemanticModel(mbb.SyntaxTree, ignoreAccessibility: true);
            return mbb.HandlesClause.Events.Select(e => {
                var symbol = semanticModel.GetSymbolInfo(e.EventMember).Symbol as IEventSymbol;
                var toDiscard = mayRequireDiscardedParameters ? symbol?.Type.GetDelegateInvokeMethod()?.GetParameters().Count() ?? 0 : 0;
                return (e.EventContainer, e.EventMember, Event: symbol, toDiscard);
            });
        }
        private string GetCSharpIdentifierText(Microsoft.CodeAnalysis.VisualBasic.Syntax.EventContainerSyntax p)
        {
            switch (p) {
                //For me, trying to use "MyClass" in a Handles expression is a syntax error. Events aren't overridable anyway so I'm not sure how this would get used.
                case Microsoft.CodeAnalysis.VisualBasic.Syntax.KeywordEventContainerSyntax kecs when kecs.Keyword.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.MyBaseKeyword):
                    return "base";
                case Microsoft.CodeAnalysis.VisualBasic.Syntax.KeywordEventContainerSyntax _:
                    return "this";
                case Microsoft.CodeAnalysis.VisualBasic.Syntax.WithEventsEventContainerSyntax weecs:
                    return CommonConversions.CsEscapedIdentifier(weecs.Identifier.Text).Text;
                case Microsoft.CodeAnalysis.VisualBasic.Syntax.WithEventsPropertyEventContainerSyntax wepecs:
                    return CommonConversions.CsEscapedIdentifier(wepecs.Property.Identifier.Text).Text;
                default:
                    throw new ArgumentOutOfRangeException(nameof(p), p, $"Unrecognized event container: `{p}`");
            }
        }

        private static bool MemberCanHandleEvents(ISymbol m) => m.IsKind(SymbolKind.Property);
    }
}