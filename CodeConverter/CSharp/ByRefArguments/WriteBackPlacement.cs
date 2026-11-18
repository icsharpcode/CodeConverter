using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static ICSharpCode.CodeConverter.CSharp.SemanticModelExtensions;

namespace ICSharpCode.CodeConverter.CSharp.ByRefArguments;

/// <summary>
/// Decides whether a call's ByRef temporaries can be declared before, and written back after, the converted statement,
/// and otherwise moves the call into a local function that writes back straight after it, as VB does.
/// </summary>
internal class WriteBackPlacement
{
    private readonly SemanticModel _semanticModel;
    private readonly HashSet<string> _generatedNames;
    private readonly ITypeContext _typeContext;

    public WriteBackPlacement(SemanticModel semanticModel, HashSet<string> generatedNames, ITypeContext typeContext, CommonConversions commonConversions)
    {
        _semanticModel = semanticModel;
        _generatedNames = generatedNames;
        _typeContext = typeContext;
        CommonConversions = commonConversions;
    }

    private CommonConversions CommonConversions { get; }

    /// <summary>
    /// The VB compiler actually just hoists the conditions within the same method, but that leads to the original logic looking very different.
    /// This should be equivalent but keep closer to the look of the original source code.
    /// See https://github.com/icsharpcode/CodeConverter/issues/310 and https://github.com/icsharpcode/CodeConverter/issues/324
    /// </summary>
    public async Task<InvocationExpressionSyntax> HoistAndCallLocalFunctionAsync(VBasic.VisualBasicSyntaxNode invocation, IMethodSymbol invocationSymbol, ExpressionSyntax csExpression)
    {
        // Each local function is its own scope, so only names visible at the call can clash, e.g. a parameter used in the argument
        var namesInLocalFunction = new HashSet<string>();
        var retVariableName = NameGenerator.CS.GetUniqueVariableNameInScope(_semanticModel, namesInLocalFunction, invocation, "ret");
        var localFuncName = $"local{invocationSymbol.Name}";

        var callAndStoreResult = CommonConversions.CreateLocalVariableDeclarationAndAssignment(retVariableName, csExpression);

        var statements = await _typeContext.PerScopeState.CreateLocalsAsync(invocation, new[] { callAndStoreResult }, _generatedNames, _semanticModel);

        var block = SyntaxFactory.Block(
            statements.Concat(SyntaxFactory.ReturnStatement(ValidSyntaxFactory.IdentifierName(retVariableName)).Yield())
        );
        var returnType = CommonConversions.GetTypeSyntax(invocationSymbol.ReturnType);

        var enclosingSymbol = _semanticModel.GetEnclosingSymbol(invocation.SpanStart);
        var operation = _semanticModel.GetOperation(invocation);
        var arguments = new List<ArgumentSyntax>();
        var parameters = new List<ParameterSyntax>();
        if (IsInStructAndUsesThis(enclosingSymbol, operation)) {
            var selfName = NameGenerator.CS.GetUniqueVariableNameInScope(_semanticModel, namesInLocalFunction, invocation, "self");
            block = block.ReplaceNodes(block.DescendantNodes().OfType<ThisExpressionSyntax>(), (_, _) => ValidSyntaxFactory.IdentifierName(selfName));
            arguments.Add(SyntaxFactory.Argument(SyntaxFactory.ThisExpression()).WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword)));
            parameters.Add(RefParameter(selfName, CommonConversions.GetTypeSyntax(enclosingSymbol.ContainingType)));
        }

        // Parent ByRef parameters cannot be captured, including references in receivers and nested argument expressions.
        var refParametersOfParent = (operation?.DescendantsAndSelf()
            .OfType<IParameterReferenceOperation>().Select(p => p.Parameter) ?? Enumerable.Empty<IParameterSymbol>())
            .Where(p => p.RefKind != RefKind.None && SymbolEqualityComparer.Default.Equals(p.ContainingSymbol, enclosingSymbol))
            .Distinct<IParameterSymbol>(SymbolEqualityComparer.Default).ToList();
        AddArgumentsAndParameters(refParametersOfParent);

        foreach (var (reference, type) in RefLocalReferences.FindIn(block)) {
            arguments.Add(SyntaxFactory.Argument(reference).WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword)));
            parameters.Add(RefParameter(reference.Identifier.ValueText, type));
        }

        var localFunc = _typeContext.PerScopeState.Hoist(new HoistedFunction(localFuncName, returnType, block, SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters))));
        return SyntaxFactory.InvocationExpression(localFunc.TempIdentifier, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

        static ParameterSyntax RefParameter(string name, TypeSyntax type) =>
            SyntaxFactory.Parameter(SyntaxFactory.Identifier(name))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword)))
                .WithType(type);

        void AddArgumentsAndParameters(List<IParameterSymbol> parameterSymbols)
        {
            foreach (var p in parameterSymbols) {
                var arg = (ArgumentSyntax)CommonConversions.CsSyntaxGenerator.Argument(p.RefKind, SyntaxFactory.IdentifierName(p.Name));
                arguments.Add(arg);
                var par = (ParameterSyntax)CommonConversions.CsSyntaxGenerator.ParameterDeclaration(p);
                parameters.Add(par.WithDefault(null));
            }
        }
    }

    public bool RequiresLocalFunction(VBSyntax.InvocationExpressionSyntax invocation, IMethodSymbol invocationSymbol)
    {
        if (IsLoopCondition(invocation)) {
            // A loop condition needs fresh argument temporaries and copy-back on every evaluation.
            return invocationSymbol.ReducedFrom?.Parameters.FirstOrDefault() is { RefKind: not RefKind.None } &&
                   invocation.Expression is VBSyntax.MemberAccessExpressionSyntax member &&
                   _semanticModel.GetRefConversionForExpression(member.Expression) != RefConversion.Inline ||
                   invocation.ArgumentList?.Arguments.Any(a =>
                       CommonConversions.GetRefConversionType(a, invocation.ArgumentList, invocationSymbol.Parameters, out _, out _) != RefConversion.Inline) == true ||
                   _semanticModel.GetOperation(invocation) is IInvocationOperation operation && operation.Arguments.Any(a =>
                       a.ArgumentKind == ArgumentKind.DefaultValue && a.Parameter.RefKind != RefKind.None);
        }

        if (invocation.ArgumentList == null) return false;
        var definitelyExecutedAfterPrevious = DefinitelyExecutedAfterPreviousStatement(invocation);
        var assignmentChangesWhatIsWrittenBack = AssignmentChangesWhatIsWrittenBack(invocation, invocationSymbol);
        var nextStatementDefinitelyExecuted = NextStatementDefinitelyExecutedAfter(invocation) && !assignmentChangesWhatIsWrittenBack;
        if (definitelyExecutedAfterPrevious && nextStatementDefinitelyExecuted) return false;
        if (assignmentChangesWhatIsWrittenBack && GetWrittenBackReceiver(invocation, invocationSymbol) is not null) return true;
        var possibleInline = definitelyExecutedAfterPrevious ? RefConversion.PreAssigment : RefConversion.Inline;
        return invocation.ArgumentList.Arguments.Any(a => RequiresLocalFunction(possibleInline, invocation, invocationSymbol, a));

        bool RequiresLocalFunction(RefConversion possibleInline, VBSyntax.InvocationExpressionSyntax invocation, IMethodSymbol invocationSymbol, VBSyntax.ArgumentSyntax a)
        {
            var refConversion = CommonConversions.GetRefConversionType(a, invocation.ArgumentList, invocationSymbol.Parameters, out string _, out _);
            if (RefConversion.Inline == refConversion || possibleInline == refConversion) return false;
            if (!(a is VBSyntax.SimpleArgumentSyntax sas)) return false;
            var argExpression = sas.Expression.SkipIntoParens();
            // A copy of a class's Me is the same reference whenever it's taken, but a structure's Me is copied by value, so it must be taken at the call
            if (argExpression is VBSyntax.InstanceExpressionSyntax) return _semanticModel.GetTypeInfo(argExpression).Type?.IsValueType == true;
            return !_semanticModel.GetConstantValue(argExpression).HasValue;
        }
    }

    /// <summary>
    /// VB writes back straight after the call, before the enclosing assignment stores its result.
    /// A write-back emitted after the assignment statement would overwrite the assigned target (<c>h.P = F(h.P)</c>),
    /// or write to a different place if the assignment changes something the write-back target reads (<c>x = F(h.Items(x))</c>, <c>h = F(h.P)</c>).
    /// A simple target is handled by the statement instead (see <see cref="IsSimpleTargetReadByWriteBack"/>), so only a compound assignment
    /// or a target with a receiver or index to evaluate first needs the local function.
    /// </summary>
    private bool AssignmentChangesWhatIsWrittenBack(VBSyntax.InvocationExpressionSyntax invocation, IMethodSymbol invocationSymbol)
    {
        if (invocation.Ancestors().FirstOrDefault(n => n is VBSyntax.StatementSyntax or VBSyntax.LambdaExpressionSyntax) is not VBSyntax.AssignmentStatementSyntax assignment ||
            !assignment.Right.Span.Contains(invocation.Span) || IsSimpleTarget(assignment)) {
            return false;
        }

        return WritesBackSomethingReading(invocation, invocationSymbol, GetAssignedSymbol(assignment));
    }

    /// <summary>
    /// For <c>P = F(P)</c> or <c>x = F(h.Items(x))</c>, where the target has nothing to evaluate before the call,
    /// <c>var ret = F(...); write-backs; P = ret;</c> keeps VB's order without a local function (which couldn't use <c>this</c> in a struct).
    /// </summary>
    public bool IsSimpleTargetReadByWriteBack(VBSyntax.AssignmentStatementSyntax assignment)
    {
        if (!IsSimpleTarget(assignment)) return false;
        var assignedSymbol = GetAssignedSymbol(assignment);
        return assignment.Right.DescendantNodesAndSelf(n => n is not VBSyntax.LambdaExpressionSyntax).OfType<VBSyntax.InvocationExpressionSyntax>()
            .Any(i => i.ArgumentList != null && _semanticModel.GetSymbolInfo(i).Symbol is IMethodSymbol method && WritesBackSomethingReading(i, method, assignedSymbol));
    }

    /// <summary>
    /// The receiver of an extension method whose first parameter is ByRef, e.g. <c>h.P</c> in <c>h.P.Ext()</c>, is written back like an argument.
    /// </summary>
    private VBSyntax.ExpressionSyntax GetWrittenBackReceiver(VBSyntax.InvocationExpressionSyntax invocation, IMethodSymbol invocationSymbol) =>
        invocationSymbol.ReducedFrom?.Parameters.FirstOrDefault() is { RefKind: not RefKind.None } &&
        invocation.Expression is VBSyntax.MemberAccessExpressionSyntax { Expression: { } receiver } &&
        _semanticModel.GetRefConversionForExpression(receiver) == RefConversion.PreAndPostAssignment
            ? receiver
            : null;

    private static bool IsSimpleTarget(VBSyntax.AssignmentStatementSyntax assignment) =>
        assignment.IsKind(VBasic.SyntaxKind.SimpleAssignmentStatement) &&
        assignment.Left.SkipIntoParens() is VBSyntax.IdentifierNameSyntax or VBSyntax.MemberAccessExpressionSyntax { Expression: VBSyntax.MeExpressionSyntax or VBSyntax.MyClassExpressionSyntax };

    private ISymbol GetAssignedSymbol(VBSyntax.AssignmentStatementSyntax assignment) =>
        _semanticModel.GetSymbolInfo(assignment.Left).Symbol ??
        (assignment.Left is VBSyntax.InvocationExpressionSyntax { Expression: var indexed } ? _semanticModel.GetSymbolInfo(indexed).Symbol : null);

    private bool WritesBackSomethingReading(VBSyntax.InvocationExpressionSyntax invocation, IMethodSymbol invocationSymbol, ISymbol assignedSymbol)
    {
        var writtenBackExpressions = invocation.ArgumentList.Arguments.OfType<VBSyntax.SimpleArgumentSyntax>()
            .Where(a => CommonConversions.GetRefConversionType(a, invocation.ArgumentList, invocationSymbol.Parameters, out _, out _) == RefConversion.PreAndPostAssignment)
            .Select(a => a.Expression)
            .Concat(GetWrittenBackReceiver(invocation, invocationSymbol) is { } receiver ? new[] { receiver } : Array.Empty<VBSyntax.ExpressionSyntax>());
        return writtenBackExpressions.Any(e => assignedSymbol is null ||
            e.DescendantNodesAndSelf().OfType<VBSyntax.ExpressionSyntax>().Any(n => SymbolEqualityComparer.Default.Equals(_semanticModel.GetSymbolInfo(n).Symbol, assignedSymbol)));
    }

    public static bool IsLoopCondition(SyntaxNode node)
    {
        var containingStatement = node.Ancestors().FirstOrDefault(n => n is VBSyntax.StatementSyntax or VBSyntax.LambdaExpressionSyntax);
        return containingStatement is VBSyntax.WhileStatementSyntax or VBSyntax.DoStatementSyntax or VBSyntax.LoopStatementSyntax;
    }

    /// <summary>
    /// A local function in a struct can't use <c>this</c> (CS1673), but it can take it as a <c>ref</c> parameter: <c>local(ref this)</c>.
    /// That's legal wherever VB can use <c>Me</c> in a structure: methods, property and event accessors, constructors (C# 11 structs are auto-default),
    /// iterators and async methods (which work on a copy in both languages). VB has no read-only members, where <c>ref this</c> would be CS1605,
    /// and doesn't allow <c>Me</c> in a lambda in a structure (BC36638), so the call is never inside one.
    /// A <c>With</c> block's implicit receiver is also an instance reference, but not to <c>this</c>.
    /// </summary>
    private static bool IsInStructAndUsesThis(ISymbol enclosingSymbol, IOperation operation) =>
        enclosingSymbol is { IsStatic: false, ContainingType.TypeKind: TypeKind.Struct } &&
        operation?.DescendantsAndSelf().OfType<IInstanceReferenceOperation>().Any(i => i.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance) != false;

    private bool DefinitelyExecutedAfterPreviousStatement(VBSyntax.InvocationExpressionSyntax invocation)
    {
        SyntaxNode parent = invocation;
        while (true) {
            parent = parent.Parent;
            switch (parent) {
                case VBSyntax.ParenthesizedExpressionSyntax _:
                    continue;
                case VBSyntax.BinaryExpressionSyntax binaryExpression:
                    if (binaryExpression.Left == invocation) continue;
                    else return false;
                case VBSyntax.ArgumentSyntax argumentSyntax:
                    // Being the leftmost invocation of an unqualified method call ensures no other code is executed. Could add other cases here, such as a method call on a local variable name, or "this.". A method call on a property is not acceptable.
                    if (argumentSyntax.Parent.Parent is VBSyntax.InvocationExpressionSyntax parentInvocation && parentInvocation.ArgumentList.Arguments.First() == argumentSyntax && FirstArgDefinitelyEvaluated(parentInvocation)) continue;
                    else return false;
                case VBSyntax.VariableDeclaratorSyntax declarator when IsInLocalDeclarationWithMultipleDeclarators(declarator, out var declarators):
                    if (declarators.First() == declarator) continue;
                    else return false;
                case VBSyntax.ElseIfStatementSyntax _:
                case VBSyntax.ExpressionSyntax _:
                    return false;
                case VBSyntax.StatementSyntax _:
                    return true;
            }
        }
    }

    private bool FirstArgDefinitelyEvaluated(VBSyntax.InvocationExpressionSyntax parentInvocation) =>
        parentInvocation.Expression.SkipIntoParens() switch {
            VBSyntax.IdentifierNameSyntax _ => true,
            VBSyntax.MemberAccessExpressionSyntax maes => maes.Expression is { } exp && !MayThrow(exp),
            _ => true
        };

    /// <summary>
    /// Safe overapproximation of whether an expression may throw.
    /// </summary>
    private bool MayThrow(VBSyntax.ExpressionSyntax expression)
    {
        expression = expression.SkipIntoParens();
        if (expression is VBSyntax.InstanceExpressionSyntax) return false;
        var symbol = _semanticModel.GetSymbolInfo(expression).Symbol;
        return !symbol.IsKind(SymbolKind.Local) && !symbol.IsKind(SymbolKind.Field);
    }

    /// <summary>
    /// Conservative version of _semanticModel.AnalyzeControlFlow(invocation).ExitPoints to account for exceptions.
    /// A block's header statement is followed by its body, so a write-back placed after the converted block would run too late
    /// (or, for a later Case clause, run when the clause was never evaluated).
    /// </summary>
    private static bool NextStatementDefinitelyExecutedAfter(VBSyntax.InvocationExpressionSyntax invocation)
    {
        SyntaxNode parent = invocation;
        while (true) {
            parent = parent.Parent;
            switch (parent) {
                case VBSyntax.ParenthesizedExpressionSyntax _:
                    continue;
                case VBSyntax.BinaryExpressionSyntax binaryExpression:
                    if (binaryExpression.Right == invocation) continue;
                    else return false;
                case VBSyntax.VariableDeclaratorSyntax declarator when IsInLocalDeclarationWithMultipleDeclarators(declarator, out var declarators):
                    if (declarators.Last() == declarator) continue;
                    else return false;
                case VBSyntax.IfStatementSyntax _:
                case VBSyntax.ElseIfStatementSyntax _:
                case VBSyntax.SingleLineIfStatementSyntax _:
                case VBSyntax.SelectStatementSyntax _:
                case VBSyntax.CaseStatementSyntax _:
                case VBSyntax.ForStatementSyntax _:
                case VBSyntax.ForEachStatementSyntax _:
                case VBSyntax.UsingStatementSyntax _:
                case VBSyntax.SyncLockStatementSyntax _:
                case VBSyntax.WithStatementSyntax _:
                case VBSyntax.CatchStatementSyntax _:
                    return false;
                case VBSyntax.ExpressionSyntax _:
                case VBSyntax.StatementSyntax _:
                    return true;
            }
        }
    }


    /// <summary>
    /// VB evaluates each declarator of <c>Dim a = F(p.X), b = F(p.X)</c> in turn, writing back before the next one starts,
    /// but temporaries and write-backs are hoisted around the whole converted declaration.
    /// A Static local's declarators become fields, where a local function couldn't be called, so those keep the old placement.
    /// </summary>
    private static bool IsInLocalDeclarationWithMultipleDeclarators(VBSyntax.VariableDeclaratorSyntax declarator, out SeparatedSyntaxList<VBSyntax.VariableDeclaratorSyntax> declarators)
    {
        declarators = declarator.Parent is VBSyntax.LocalDeclarationStatementSyntax localDeclaration &&
                      !localDeclaration.Modifiers.Any(m => m.IsKind(VBasic.SyntaxKind.StaticKeyword))
            ? localDeclaration.Declarators
            : default;
        return declarators.Count > 1;
    }
}
