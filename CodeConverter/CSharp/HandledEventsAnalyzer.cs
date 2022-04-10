using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

internal class HandledEventsAnalyzer
{
    private readonly CommonConversions _commonConversions;
    private readonly INamedTypeSymbol _type;
    private readonly Location _initializeComponentLocationOrNull;
    private readonly ILookup<ITypeSymbol, ITypeSymbol> _typeToInheritors;
    private readonly SemanticModel _semanticModel;

    private HandledEventsAnalyzer(CommonConversions commonConversions, INamedTypeSymbol type, Location initializeComponentLocationOrNull, ILookup<ITypeSymbol, ITypeSymbol> typeToInheritors)
    {
        _commonConversions = commonConversions;
        _semanticModel = commonConversions.SemanticModel;
        _type = type;
        _initializeComponentLocationOrNull = initializeComponentLocationOrNull;
        _typeToInheritors = typeToInheritors;
    }

    public static Task<HandledEventsAnalysis> AnalyzeAsync(CommonConversions commonConversions, INamedTypeSymbol type, IMethodSymbol designerGeneratedInitializeComponentOrNull,
        ILookup<ITypeSymbol, ITypeSymbol> typeToInheritors)
    {
        var initializeComponentLocationOrNull = designerGeneratedInitializeComponentOrNull?.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<MethodStatementSyntax>().OrderByDescending(m => m.Span.Length).Select(s => s.Parent.GetLocation()).FirstOrDefault();
            
        return new HandledEventsAnalyzer(commonConversions, type, initializeComponentLocationOrNull, typeToInheritors).AnalyzeAsync();
    }

    private async Task<HandledEventsAnalysis> AnalyzeAsync()
    {
#pragma warning disable RS1024 // Compare symbols correctly - bug in analyzer https://github.com/dotnet/roslyn-analyzers/issues/3427#issuecomment-929104517
        var ancestorPropsMembersByName = _type.GetBaseTypesAndThis().SelectMany(t => t.GetMembers().Where(m => m.IsKind(SymbolKind.Property)))
            .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(m => m.Key, g => g.First(), StringComparer.OrdinalIgnoreCase); // Uses the fact that GroupBy maintains addition order to get the closest declaration
#pragma warning restore RS1024 // Compare symbols correctly

        var writtenWithEventsProperties = await ancestorPropsMembersByName.Values.OfType<IPropertySymbol>().ToAsyncEnumerable().ToDictionaryAwaitAsync(async p => p.Name, async p => (p, await IsNeverWrittenOrOverriddenAsync(p)), StringComparer.OrdinalIgnoreCase);

        var eventContainerToMethods = _type.GetMembers().OfType<IMethodSymbol>()
            .SelectMany(HandledEvents)
            .ToLookup(eventAndMethod => eventAndMethod.EventContainer);
        var eventsThatMayNeedInheritors = writtenWithEventsProperties
            .Where(p => !p.Value.Item2 && p.Value.p.ContainingType.Equals(_type, SymbolEqualityComparer.IncludeNullability))
            .Select(p =>
                (EventContainer: new HandledEventsAnalysis.EventContainer(HandledEventsAnalysis.EventContainerKind.Property, p.Key), PropertyDetails: p.Value, Array.Empty<(EventDescriptor Event, IMethodSymbol HandlingMethod, int ParametersToDiscard)>()))
            .Where(e => !eventContainerToMethods.Contains(e.EventContainer));
        var eventsThatHaveDirectHandlesSubscriptions = eventContainerToMethods //TODO Make event container grouping case insensitive, or resolve property and use that
            .Select(g => 
                (EventContainer: g.Key, PropertyDetails: g.Key.PropertyName != null && writtenWithEventsProperties.TryGetValue(g.Key.PropertyName, out var p) ? p : default, g.Select(x => (x.Event, x.HandlingMethod, ParametersToDiscard(x.Event.SymbolOrNull, x.HandlingMethod))).ToArray()));
        var csharpEventContainersRequiringDelegatingProperties = eventsThatHaveDirectHandlesSubscriptions.Concat(eventsThatMayNeedInheritors);

        return new HandledEventsAnalysis(_commonConversions, _type, csharpEventContainersRequiringDelegatingProperties);
    }


    private static int ParametersToDiscard(IEventSymbol e, IMethodSymbol handlingMethod)
    {
        var mayRequireDiscardedParameters = !handlingMethod.Parameters.Any();
        var toDiscard = mayRequireDiscardedParameters ? e?.Type.GetDelegateInvokeMethod()?.GetParameters().Length ?? 0 : 0;
        return toDiscard;
    }

    private async Task<bool> IsNeverWrittenOrOverriddenAsync(ISymbol symbol)
    {
        var projectSolution = _commonConversions.Document.Project.Solution;
        if (!await projectSolution.IsNeverWrittenAsync(symbol, _initializeComponentLocationOrNull)) return false;
        return !_typeToInheritors.Contains(symbol.ContainingType);
    }

    /// <summary>
    /// VBasic.VisualBasicExtensions.HandledEvents(m) seems to optimize away some events, so just detect from syntax
    /// </summary>
    private IEnumerable<(HandledEventsAnalysis.EventContainer EventContainer, EventDescriptor Event, IMethodSymbol HandlingMethod)> HandledEvents(IMethodSymbol m)
    {
        return m.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<MethodStatementSyntax>()
            .GroupBy(method => method.SyntaxTree)
            .Select(g => {
                var semanticModel = Equals(_semanticModel.SyntaxTree, g.Key) ? _semanticModel : _commonConversions.Compilation.GetSemanticModel(g.Key, true);
                return (SemanticModel: semanticModel, MethodDeclarations: g.ToArray());
            })
            .SelectMany(mbb => HandledEvent(mbb.SemanticModel, mbb.MethodDeclarations, m));
    }

    private static IEnumerable<(HandledEventsAnalysis.EventContainer EventContainer, EventDescriptor Event, IMethodSymbol HandlingMethod)> HandledEvent(SemanticModel semanticModel,
        MethodStatementSyntax[] mbb, IMethodSymbol methodSymbol)
    {
        return mbb.Where(mss => mss.HandlesClause?.Events.Any() == true)
            .SelectMany(mss => mss.HandlesClause.Events, (_, e) => {
                var eventSymbol = semanticModel.GetSymbolInfo(e.EventMember).Symbol as IEventSymbol;
                // TODO: Need to either use the semantic model containing the event symbol, or bundle up the Event member with the possible symbol here for later use (otherwise it's null)
                return (CreateEventContainer(e.EventContainer, semanticModel), new EventDescriptor(e.EventMember, eventSymbol), HandlingMethod: methodSymbol);
            });
    }
    private static HandledEventsAnalysis.EventContainer CreateEventContainer(EventContainerSyntax p, SemanticModel semanticModel)
    {
        switch (p) {
            //For me, trying to use "MyClass" in a Handles expression is a syntax error. Events aren't overridable anyway so I'm not sure how this would get used.
            case KeywordEventContainerSyntax kecs when kecs.Keyword.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.MyBaseKeyword):
                return new HandledEventsAnalysis.EventContainer(HandledEventsAnalysis.EventContainerKind.Base, null);
            case KeywordEventContainerSyntax _:
                return new HandledEventsAnalysis.EventContainer(HandledEventsAnalysis.EventContainerKind.This, null);
            case WithEventsEventContainerSyntax weecs:
                return new HandledEventsAnalysis.EventContainer(HandledEventsAnalysis.EventContainerKind.Property, semanticModel.GetSymbolInfo(weecs).Symbol.Name);
            case WithEventsPropertyEventContainerSyntax wepecs:
                return new HandledEventsAnalysis.EventContainer(HandledEventsAnalysis.EventContainerKind.Property, wepecs.Property.Identifier.Text);
            default:
                throw new ArgumentOutOfRangeException(nameof(p), p, $"Unrecognized event container: `{p}`");
        }
    }
}