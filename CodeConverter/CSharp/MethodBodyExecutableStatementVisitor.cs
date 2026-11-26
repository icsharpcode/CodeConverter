using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.Text;
using ICSharpCode.CodeConverter.CSharp.ByRefArguments;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using ICSharpCode.CodeConverter.VB;
using ICSharpCode.CodeConverter.CSharp.OnError;
using ComparisonKind = ICSharpCode.CodeConverter.CSharp.VisualBasicEqualityComparison.ComparisonKind;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// Executable statements - which includes executable blocks such as if statements
/// Maintains state relevant to the called method-like object. A fresh one must be used for each method, and the same one must be reused for statements in the same method
/// </summary>
internal class MethodBodyExecutableStatementVisitor : VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>>
{
    private readonly VBasic.VisualBasicSyntaxNode _methodNode;
    private readonly SemanticModel _semanticModel;
    private readonly CommentConvertingVisitorWrapper _expressionVisitor;
    private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;
    private readonly Stack<ExpressionSyntax> _withBlockLhs;
    private readonly HashSet<string> _extraUsingDirectives;
    private readonly HandledEventsAnalysis _handledEventsAnalysis;
    private readonly HashSet<string> _generatedNames = new();
    private readonly HashSet<ILocalSymbol> _localsToInlineInLoop;
    private readonly PerScopeState _perScopeState;
    private VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>> _statementVisitor;
    private readonly WriteBackPlacement _writeBackPlacement;

    public bool IsIterator { get; set; }
    public IdentifierNameSyntax ReturnVariable { get; set; }
    public bool HasReturnVariable => ReturnVariable != null;
    /// <summary>How nested statements are converted: every statement is visited through this, whatever its depth.</summary>
    public VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>> CommentConvertingVisitor { get; private set; }

    /// <summary>Set by the On Error conversion for the shape it emits. Until then, On Error and Resume statements can't be converted.</summary>
    public IOnErrorStatementConverter OnErrorStatementConverter { get; set; }

    private CommonConversions CommonConversions { get; }

    /// <summary>
    /// Lets another class change the conversion of each statement at every nesting depth, without this visitor knowing why.
    /// The decorator sits inside the comment conversion, so comments attach to whatever it returns for the statement.
    /// </summary>
    public void DecorateStatementVisitor(Func<VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>>, VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>>> decorate)
    {
        _statementVisitor = decorate(_statementVisitor);
        CommentConvertingVisitor = new CommentConvertingMethodBodyVisitor(_statementVisitor);
    }

    public static async Task<MethodBodyExecutableStatementVisitor> CreateAsync(VisualBasicSyntaxNode node, SemanticModel semanticModel,
        CommentConvertingVisitorWrapper triviaConvertingExpressionVisitor, CommonConversions commonConversions, VisualBasicEqualityComparison visualBasicEqualityComparison,
        Stack<ExpressionSyntax> withBlockLhs, HashSet<string> extraUsingDirectives,
        ITypeContext typeContext, bool isIterator, IdentifierNameSyntax csReturnVariable)
    {
        var solution = commonConversions.Document.Project.Solution;
        var declarationsToInlineInLoop = await solution.GetDescendantsToInlineInLoopAsync(semanticModel, node);
        return new MethodBodyExecutableStatementVisitor(node, semanticModel, triviaConvertingExpressionVisitor, commonConversions, visualBasicEqualityComparison, withBlockLhs, extraUsingDirectives, typeContext, declarationsToInlineInLoop) {
            IsIterator = isIterator,
            ReturnVariable = csReturnVariable,
        };
    }

    private MethodBodyExecutableStatementVisitor(VisualBasicSyntaxNode methodNode, SemanticModel semanticModel,
        CommentConvertingVisitorWrapper expressionVisitor, CommonConversions commonConversions,
        VisualBasicEqualityComparison visualBasicEqualityComparison,
        Stack<ExpressionSyntax> withBlockLhs, HashSet<string> extraUsingDirectives,
        ITypeContext typeContext, HashSet<ILocalSymbol> localsToInlineInLoop)
    {
        _methodNode = methodNode;
        _semanticModel = semanticModel;
        _expressionVisitor = expressionVisitor;
        _visualBasicEqualityComparison = visualBasicEqualityComparison;
        CommonConversions = commonConversions;
        _withBlockLhs = withBlockLhs;
        _extraUsingDirectives = extraUsingDirectives;
        _handledEventsAnalysis = typeContext.HandledEventsAnalysis;
        _perScopeState = typeContext.PerScopeState;
        _writeBackPlacement = new WriteBackPlacement(semanticModel, _generatedNames, typeContext, commonConversions);
        _statementVisitor = new PerScopeStateVisitorDecorator(this, _perScopeState, semanticModel, _generatedNames);
        CommentConvertingVisitor = new CommentConvertingMethodBodyVisitor(_statementVisitor);
        _localsToInlineInLoop = localsToInlineInLoop;
    }

    public override async Task<SyntaxList<StatementSyntax>> DefaultVisit(SyntaxNode node)
    {
        throw new NotImplementedException($"Conversion for {VBasic.VisualBasicExtensions.Kind(node)} not implemented, please report this issue")
            .WithNodeInformation(node);
    }

    public override Task<SyntaxList<StatementSyntax>> VisitOnErrorGoToStatement(VBSyntax.OnErrorGoToStatementSyntax node) =>
        OnErrorStatementConverter is { } converter ? Task.FromResult(converter.ConvertOnError(node)) : DefaultVisit(node);

    public override Task<SyntaxList<StatementSyntax>> VisitOnErrorResumeNextStatement(VBSyntax.OnErrorResumeNextStatementSyntax node) =>
        OnErrorStatementConverter is { } converter ? Task.FromResult(converter.ConvertOnError(node)) : DefaultVisit(node);

    public override Task<SyntaxList<StatementSyntax>> VisitResumeStatement(VBSyntax.ResumeStatementSyntax node) =>
        OnErrorStatementConverter is { } converter ? Task.FromResult(converter.ConvertResume(node)) : DefaultVisit(node);

    public override async Task<SyntaxList<StatementSyntax>> VisitStopOrEndStatement(VBSyntax.StopOrEndStatementSyntax node)
    {
        return SingleStatement(SyntaxFactory.ParseStatement(ConvertStopOrEndToCSharpStatementText(node)));
    }

    private string ConvertStopOrEndToCSharpStatementText(VBSyntax.StopOrEndStatementSyntax node)
    {
        switch (VBasic.VisualBasicExtensions.Kind(node.StopOrEndKeyword)) {
            case VBasic.SyntaxKind.StopKeyword:
                _extraUsingDirectives.Add("System.Diagnostics");
                return "Debugger.Break();";
            case VBasic.SyntaxKind.EndKeyword:
                _extraUsingDirectives.Add("System");
                return "Environment.Exit(0);";
            default:
                throw new NotImplementedException(VBasic.VisualBasicExtensions.Kind(node.StopOrEndKeyword) + " not implemented!");
        }
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitLocalDeclarationStatement(VBSyntax.LocalDeclarationStatementSyntax node)
    {  
        var modifiers = CommonConversions.ConvertModifiers(node.Declarators[0].Names[0], node.Modifiers, TokenContext.Local);
        var isConst = modifiers.Any(a => a.IsKind(SyntaxKind.ConstKeyword));
        var isVBStatic = node.Modifiers.Any(a => a.IsKind(VBasic.SyntaxKind.StaticKeyword));

        var declarations = new List<StatementSyntax>();

        foreach (var declarator in node.Declarators) {
            var (variables, methods) = await SplitVariableDeclarationsAsync(declarator, preferExplicitType: isConst || isVBStatic);
            var localDeclarationStatementSyntaxs = variables.Select(declAndType => SyntaxFactory.LocalDeclarationStatement(modifiers, declAndType.Decl));
            if (isVBStatic) {
                foreach (var decl in localDeclarationStatementSyntaxs) {
                    var variable = decl.Declaration.Variables.Single();
                    var initializeValue = variable.Initializer?.Value;
                    string methodName;
                    SyntaxTokenList methodModifiers;

                    MethodKind parentAccessorKind = MethodKind.Ordinary;
                    if (_methodNode is VBSyntax.MethodBlockSyntax methodBlock) {
                        var methodStatement = methodBlock.BlockStatement as VBSyntax.MethodStatementSyntax;
                        methodModifiers = methodStatement.Modifiers;
                        methodName = methodStatement.Identifier.Text;
                    } else if (_methodNode is VBSyntax.ConstructorBlockSyntax constructorBlock) {
                        methodModifiers = constructorBlock.BlockStatement.Modifiers;
                        methodName = null;
                    } else if (_methodNode is VBSyntax.AccessorBlockSyntax accessorBlock) {
                        var propertyBlock = accessorBlock.Parent as VBSyntax.PropertyBlockSyntax;
                        methodName = propertyBlock.PropertyStatement.Identifier.Text;
                        parentAccessorKind = accessorBlock.IsKind(VBasic.SyntaxKind.GetAccessorBlock) ? MethodKind.PropertyGet : MethodKind.PropertySet;
                        methodModifiers = propertyBlock.PropertyStatement.Modifiers;
                    } else {
                        throw new NotImplementedException(_methodNode.GetType() + " not implemented!");
                    }

                    var isVbShared = methodModifiers.Any(a => a.IsKind(VBasic.SyntaxKind.SharedKeyword));
                    _perScopeState.HoistToTopLevel(new HoistedFieldFromVbStaticVariable(methodName, variable.Identifier.Text, parentAccessorKind, initializeValue, decl.Declaration.Type, isVbShared));
                }
            } else {
                var shouldPullVariablesBeforeLoop = _perScopeState.IsInsideLoop() && declarator.Initializer is null && declarator.AsClause is not VBSyntax.AsNewClauseSyntax;
                if (shouldPullVariablesBeforeLoop) {
                    localDeclarationStatementSyntaxs = HoistVariablesBeforeLoopWhenNeeded(variables)
                        .Select(variableDecl => SyntaxFactory.LocalDeclarationStatement(modifiers, variableDecl));
                }

                declarations.AddRange(localDeclarationStatementSyntaxs);
            }
            var localFunctions = methods.Cast<LocalFunctionStatementSyntax>();
            declarations.AddRange(localFunctions);
        }

        return SyntaxFactory.List(declarations);
    }

    /// <summary>
    /// Declares a local that was converted in a loop body before the outermost loop instead, as a VB local keeps its value from one iteration to the next.
    /// Call it after the declaration has been converted, as the scope of the declaration itself has then been left.
    /// </summary>
    /// <returns>False when the declaration isn't in a loop, so the caller declares it where it is.</returns>
    public bool TryHoistOutOfLoop(string name, TypeSyntax type)
    {
        if (!_perScopeState.IsWithinLoopBody()) return false;
        _perScopeState.Hoist(new HoistedDefaultInitializedLoopVariable(name, SyntaxFactory.DefaultExpression(type), type, _perScopeState.IsWithinNestedLoopBody()));
        return true;
    }

    private IEnumerable<VariableDeclarationSyntax> HoistVariablesBeforeLoopWhenNeeded(IEnumerable<CommonConversions.VariablesDeclaration> variablesDeclarations)
    {
        foreach (var variablesDecl in variablesDeclarations) {
            var variablesToRemove = new List<CSSyntax.VariableDeclaratorSyntax>();
            foreach (var (csVariable, vbVariable) in variablesDecl.Variables) {
                var symbol = _semanticModel.GetDeclaredSymbol(vbVariable);
                var assignedBeforeRead = _semanticModel.IsDefinitelyAssignedBeforeRead(symbol, vbVariable);
                if (!assignedBeforeRead) {
                    _perScopeState.Hoist(new HoistedDefaultInitializedLoopVariable(
                        csVariable.Identifier.Text,
                        // e.g. "b As Boolean" has no initializer but can turn into "var b = default(bool)"
                        csVariable.Initializer?.Value,
                        variablesDecl.Decl.Type,
                        _perScopeState.IsInsideNestedLoop()));
                    variablesToRemove.Add(csVariable);
                }
            }

            var variablesToDeclareLocally = variablesDecl.Variables.Select(t => t.CsVar).Except(variablesToRemove).ToArray();
            if(variablesToDeclareLocally.Any()) {
                yield return variablesDecl.Decl.WithVariables(SyntaxFactory.SeparatedList(variablesToDeclareLocally));
            }
        }
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitAddRemoveHandlerStatement(VBSyntax.AddRemoveHandlerStatementSyntax node)
    {
        var syntaxKind = ConvertAddRemoveHandlerToCSharpSyntaxKind(node);
        return SingleStatement(SyntaxFactory.AssignmentExpression(syntaxKind,
            await node.EventExpression.AcceptAsync<ExpressionSyntax>(_expressionVisitor),
            await node.DelegateExpression.AcceptAsync<ExpressionSyntax>(_expressionVisitor)));
    }

    private static SyntaxKind ConvertAddRemoveHandlerToCSharpSyntaxKind(VBSyntax.AddRemoveHandlerStatementSyntax node)
    {
        switch (node.Kind()) {
            case VBasic.SyntaxKind.AddHandlerStatement:
                return SyntaxKind.AddAssignmentExpression;
            case VBasic.SyntaxKind.RemoveHandlerStatement:
                return SyntaxKind.SubtractAssignmentExpression;
            default:
                throw new NotImplementedException(node.Kind() + " not implemented!");
        }
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitExpressionStatement(VBSyntax.ExpressionStatementSyntax node)
    {
        if (node.Expression is VBSyntax.InvocationExpressionSyntax invoke && invoke.Expression is VBSyntax.MemberAccessExpressionSyntax access && access.Expression is VBSyntax.MyBaseExpressionSyntax && access.Name.Identifier.ValueText.Equals("Finalize", StringComparison.OrdinalIgnoreCase)) {
            return new SyntaxList<StatementSyntax>();
        }

        return SingleStatement(await node.Expression.AcceptAsync<CSharpSyntaxNode>(_expressionVisitor));
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitAssignmentStatement(VBSyntax.AssignmentStatementSyntax node)
    {
        if (node.IsKind(VBasic.SyntaxKind.MidAssignmentStatement) && node.Left is VBSyntax.MidExpressionSyntax mes) {
            return await ConvertMidAssignmentAsync(node, mes);
        }

        var lhs = await node.Left.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
        var lOperation = _semanticModel.GetOperation(node.Left);

        var (parameterizedPropertyAccessMethod, _) = await CommonConversions.GetParameterizedPropertyAccessMethodAsync(lOperation);

        // If it's a simple assignment, we can return early as it's already handled by ConvertInvocationExpression
        if (parameterizedPropertyAccessMethod != null && node.IsKind(VBasic.SyntaxKind.SimpleAssignmentStatement)) {
            return SingleStatement(lhs);
        }

        // For compound assignments, we want to expand it to the setter, but parameterizedPropertyAccessMethod above
        // returned 'get_Item' or 'set_Item' depending on operation context.
        // We know for sure the left-hand side is a getter invocation for compound assignments (e.g. this.get_Item(0) += 2),
        // but we need the setter name to build the final expression.
        string setMethodName = null;
        IMethodSymbol setMethod = null;
        if (lOperation is IPropertyReferenceOperation pro && pro.Arguments.Any() && !Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions.IsDefault(pro.Property)) {
            setMethod = pro.Property.SetMethod;
            setMethodName = setMethod?.Name;
        }

        // Anything the left side needs hoisted is evaluated before anything the right side does
        var leftSideHoistIndex = _perScopeState.HoistedNodeCount;
        var rhs = await node.Right.AcceptAsync<ExpressionSyntax>(_expressionVisitor);

        if (node.Left is VBSyntax.IdentifierNameSyntax id &&
            _methodNode is VBSyntax.MethodBlockSyntax mb &&
            HasReturnVariable &&
            id.Identifier.ValueText.Equals(mb.SubOrFunctionStatement.Identifier.ValueText, StringComparison.OrdinalIgnoreCase)) {
            lhs = ReturnVariable;
        }

        var csRight = rhs;
        rhs = ConvertAssignedValue(node, lhs, csRight, setMethodName, out var kind);
        var isSplitCompound = !node.IsKind(VBasic.SyntaxKind.SimpleAssignmentStatement) && (kind == SyntaxKind.SimpleAssignmentExpression || setMethodName != null);
        if (isSplitCompound && HoistLeftSideOperandsOrNull(node.Left, lhs, leftSideHoistIndex) is { } hoistedLhs) {
            lhs = hoistedLhs;
            rhs = ConvertAssignedValue(node, lhs, csRight, setMethodName, out kind);
        }

        if (setMethodName != null) {
            if (lhs is InvocationExpressionSyntax ies) {
                ExpressionSyntax exprToReplace = ies.Expression;
                if (exprToReplace is MemberAccessExpressionSyntax maes) {
                    var newName = SyntaxFactory.IdentifierName(setMethodName).WithTriviaFrom(maes.Name);
                    var stripThis = maes.Expression is ThisExpressionSyntax
                        && node.Left.SkipIntoParens() is not VBSyntax.MemberAccessExpressionSyntax {
                            Expression: not (VBSyntax.MeExpressionSyntax or VBSyntax.MyClassExpressionSyntax)
                        };
                    exprToReplace = stripThis ? newName.WithTriviaFrom(maes) : maes.WithName(newName);
                } else if (exprToReplace is IdentifierNameSyntax) {
                    exprToReplace = SyntaxFactory.IdentifierName(setMethodName).WithTriviaFrom(exprToReplace);
                }
                var newArgList = ies.ArgumentList.AddArguments(CreateSetterValueArgument(ies.ArgumentList, setMethod, rhs));
                var newLhs = ies.WithExpression(exprToReplace).WithArgumentList(newArgList);
                var postAssign = GetPostAssignmentStatements(node);
                return postAssign.Insert(0, SyntaxFactory.ExpressionStatement(newLhs));
            }
            return SingleStatement(lhs);
        }

        var postAssignment = GetPostAssignmentStatements(node);
        StatementSyntax AssignTo(ExpressionSyntax value) => SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(kind, lhs, value));
        if (kind == SyntaxKind.SimpleAssignmentExpression && _writeBackPlacement.IsSimpleTargetReadByWriteBack(node)) {
            return SyntaxFactory.List(_perScopeState.WithWriteBacksBefore(rhs, "ret", () => GetResultTypeSyntax(node.Right), AssignTo).Concat(postAssignment));
        }
        return postAssignment.Insert(0, AssignTo(rhs));
    }

    /// <summary>
    /// VB evaluates the receiver and arguments of a compound assignment's left side once, then the right side.
    /// Split into lhs = lhs op rhs, C# would evaluate them twice, so any that may have side effects (or be changed by the right side) are evaluated into locals first, in VB's order.
    /// Once one is, any before it is too unless it's a local or parameter, since evaluating the later one may change e.g. the field the earlier one reads.
    /// </summary>
    /// <remarks>
    /// A member of a structure (or of a type parameter, which may be one) is changed in its storage, so that storage is broken down the same way rather than copied:
    /// an array element is referred to again through its array and indices in locals, which works whatever the array's runtime type (a ref to an element of a covariant array throws),
    /// and in async methods and iterators. Only storage that can't be broken down is a ref local, and not in a statement with an Await, since one can't be held across it.
    /// </remarks>
    /// <param name="hoistIndex">Where among the statement's hoisted locals these go: before any the right side needs</param>
    /// <returns>Null where the left side can be evaluated twice, e.g. a local, a field of Me, or an array element with local or constant indices</returns>
    private ExpressionSyntax HoistLeftSideOperandsOrNull(VBSyntax.ExpressionSyntax vbLeft, ExpressionSyntax csLeft, int hoistIndex)
    {
        var operands = new List<LeftSideOperand>();
        if (!TryCollectOperands(_semanticModel.GetOperation(vbLeft), csLeft, operands)) return null;

        var lastToHoist = operands.FindLastIndex(o => o.IsStorage || !IsEvaluatedTheSameTwice(o.Operation) && o.Expression is not LiteralExpressionSyntax);
        if (lastToHoist < 0) return null;
        var toHoist = operands.Take(lastToHoist + 1).Where((o, i) => i == lastToHoist || o.IsStorage || !IsStableReference(o.Operation)).ToArray();
        if (toHoist.Any(o => o.IsStorage) && vbLeft.Parent.DescendantNodes().OfType<VBSyntax.AwaitExpressionSyntax>().Any()) return null;

        var replacements = new Dictionary<SyntaxNode, ExpressionSyntax>();
        foreach (var operand in toHoist) {
            var declaration = operand.IsStorage
                ? new AdditionalDeclaration("receiver", SyntaxFactory.RefExpression(operand.Expression), SyntaxFactory.RefType(ValidSyntaxFactory.VarType))
                : new AdditionalDeclaration(operand.NameBase, operand.Expression, null);
            _perScopeState.HoistAt(hoistIndex++, declaration);
            replacements.Add(operand.Expression, operand.IsStorage
                ? RefLocalReferences.Mark(declaration.IdentifierName, CommonConversions.GetTypeSyntax(operand.Operation.Type))
                : declaration.IdentifierName);
        }
        return csLeft.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]);
    }

    /// <param name="IsStorage">Storage of a structure that can only be referred to again by reference</param>
    private record LeftSideOperand(IOperation Operation, ExpressionSyntax Expression, string NameBase, bool IsStorage = false);

    /// <summary>
    /// The operands VB evaluates to find the storage <paramref name="operation"/> refers to, in VB's order, each paired with the C# expression it became
    /// </summary>
    /// <returns>False where the C# doesn't have the shape the VB does</returns>
    private bool TryCollectOperands(IOperation operation, ExpressionSyntax csExpression, List<LeftSideOperand> operands)
    {
        var cs = csExpression.SkipIntoParens();
        switch (operation) {
            case IArrayElementReferenceOperation element when cs is ElementAccessExpressionSyntax elementAccess && elementAccess.ArgumentList.Arguments.Count == element.Indices.Length:
                operands.Add(new LeftSideOperand(element.ArrayReference, elementAccess.Expression, "receiver"));
                operands.AddRange(element.Indices.Select((index, i) => new LeftSideOperand(index, elementAccess.ArgumentList.Arguments[i].Expression, "index")));
                return true;
            case IFieldReferenceOperation { Field.IsStatic: false } field when cs is MemberAccessExpressionSyntax memberAccess:
                return TryCollectReceiverOperands(field.Instance, memberAccess.Expression, operands);
            case IPropertyReferenceOperation property:
                var (csReceiver, csArguments) = cs switch {
                    ElementAccessExpressionSyntax elementAccess => (elementAccess.Expression, elementAccess.ArgumentList.Arguments),
                    InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax method } invocation => (method.Expression, invocation.ArgumentList.Arguments),
                    InvocationExpressionSyntax invocation => (null, invocation.ArgumentList.Arguments),
                    MemberAccessExpressionSyntax memberAccess => (memberAccess.Expression, default(SeparatedSyntaxList<ArgumentSyntax>)),
                    _ => (null, default)
                };
                // A Shared property's arguments are evaluated all the same, without a receiver (VB doesn't evaluate the instance expression, and C# refers to the type)
                if (csReceiver != null && !property.Property.IsStatic && !TryCollectReceiverOperands(property.Instance, csReceiver, operands)) return false;
                // VB evaluates the arguments in parameter order, whatever order named ones are written in
                foreach (var argument in property.Arguments) {
                    var csArgument = csArguments.Select((a, i) => (Argument: a, Position: i))
                        .FirstOrDefault(a => string.Equals(a.Argument.NameColon?.Name.Identifier.ValueText ?? (a.Position < property.Property.Parameters.Length ? property.Property.Parameters[a.Position].Name : null),
                            argument.Parameter?.Name, StringComparison.OrdinalIgnoreCase)).Argument;
                    if (csArgument != null) operands.Add(new LeftSideOperand(argument.Value, csArgument.Expression, "index"));
                }
                return true;
            case IDynamicMemberReferenceOperation lateBoundMember when cs is MemberAccessExpressionSyntax memberAccess:
                // A late-bound member is accessed through a cast to dynamic: the receiver is what's cast
                var csInstance = memberAccess.Expression.SkipIntoParens() is CastExpressionSyntax { Type: IdentifierNameSyntax { Identifier.ValueText: "dynamic" } } dynamicCast
                    ? dynamicCast.Expression
                    : memberAccess.Expression;
                operands.Add(new LeftSideOperand(lateBoundMember.Instance, csInstance, "receiver"));
                return true;
            default:
                // Nothing to evaluate again, e.g. a local or a Shared member (VB doesn't evaluate the instance expression of one, and C# refers to the type instead)
                return true;
        }
    }

    /// <summary>
    /// An object is referred to by its value; a structure's storage (or a type parameter's, which may be one) is broken down further, so it's changed in place rather than in a copy
    /// </summary>
    private bool TryCollectReceiverOperands(IOperation instance, ExpressionSyntax csInstance, List<LeftSideOperand> operands)
    {
        switch (instance) {
            case null or ILocalReferenceOperation or IParameterReferenceOperation or IInstanceReferenceOperation:
                return true;
            case { Type.IsReferenceType: true }:
                operands.Add(new LeftSideOperand(instance, csInstance, "receiver"));
                return true;
            case IArrayElementReferenceOperation or IFieldReferenceOperation:
                return TryCollectOperands(instance, csInstance, operands);
            case var storage when storage.IsAssignableExpression():
                operands.Add(new LeftSideOperand(storage, csInstance, "receiver", IsStorage: true));
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Refers to the same object however much else is evaluated in between
    /// </summary>
    private static bool IsStableReference(IOperation operation) =>
        operation is null or ILiteralOperation or IInstanceReferenceOperation or ILocalReferenceOperation or IParameterReferenceOperation || operation.ConstantValue.HasValue;

    /// <summary>
    /// Constants, Me, locals and parameters, fields of those, and intrinsic operators on them give the same value when evaluated again with no side effects.
    /// </summary>
    private static bool IsEvaluatedTheSameTwice(IOperation operation) => operation switch {
        null or ILiteralOperation or IInstanceReferenceOperation or ILocalReferenceOperation or IParameterReferenceOperation => true,
        { ConstantValue.HasValue: true } => true,
        IFieldReferenceOperation field => IsEvaluatedTheSameTwice(field.Instance),
        IConversionOperation { OperatorMethod: null } conversion => IsEvaluatedTheSameTwice(conversion.Operand),
        IParenthesizedOperation parenthesized => IsEvaluatedTheSameTwice(parenthesized.Operand),
        IBinaryOperation { OperatorMethod: null } binary => IsEvaluatedTheSameTwice(binary.LeftOperand) && IsEvaluatedTheSameTwice(binary.RightOperand),
        IUnaryOperation { OperatorMethod: null } unary => IsEvaluatedTheSameTwice(unary.Operand),
        _ => false
    };

    /// <summary>
    /// A setter's value comes after its optional parameters, so where the getter's call leaves any out, or names its arguments, the value is named (else it would fill an optional parameter, or follow a named argument out of position, CS8323)
    /// </summary>
    private ArgumentSyntax CreateSetterValueArgument(ArgumentListSyntax getterArguments, IMethodSymbol setMethod, ExpressionSyntax value)
    {
        var argument = SyntaxFactory.Argument(value);
        var setterParameters = setMethod?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
        var needsName = getterArguments.Arguments.Any(a => a.NameColon != null) || getterArguments.Arguments.Count < setterParameters.Length - 1;
        if (!needsName || !setterParameters.Any()) return argument;
        // A setter converted from this source names it value, as a C# setter's is; one in a referenced assembly keeps its own name
        var valueName = setMethod.Locations.Any(l => l.IsInSource) ? "value" : CommonConversions.CsEscapedIdentifier(setterParameters.Last().Name).Text;
        return argument.WithNameColon(SyntaxFactory.NameColon(ValidSyntaxFactory.IdentifierName(valueName)));
    }

    /// <summary>
    /// The value to assign, and the C# assignment's kind: a compound assignment may need splitting into lhs = lhs op rhs
    /// </summary>
    private ExpressionSyntax ConvertAssignedValue(VBSyntax.AssignmentStatementSyntax node, ExpressionSyntax lhs, ExpressionSyntax rhs, string setMethodName, out SyntaxKind kind)
    {
        var originalRhs = rhs;
        if (node.IsKind(VBasic.SyntaxKind.ExponentiateAssignmentStatement)) {
            rhs = SyntaxFactory.InvocationExpression(
                ValidSyntaxFactory.MemberAccess(nameof(Math), nameof(Math.Pow)),
                ExpressionSyntaxExtensions.CreateArgList(lhs, rhs));
        }
        kind = node.Kind().ConvertToken();

        var lhsTypeInfo = _semanticModel.GetTypeInfo(node.Left);
        var rhsTypeInfo = _semanticModel.GetTypeInfo(node.Right);

        var typeConvertedRhs = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Right, rhs);
        if (GetLateBoundOperatorOrNull(node, lhsTypeInfo, rhsTypeInfo) is { } lateBoundOperator) {
            var lateBound = lateBoundOperator.Invoke(_extraUsingDirectives, lhs, originalRhs);
            var objectType = _semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
            kind = SyntaxKind.SimpleAssignmentExpression;
            return CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Right, lateBound, forceSourceType: objectType, forceTargetType: lhsTypeInfo.Type);
        }
        return ConvertNonLateBoundCompoundRhs(node, lhs, typeConvertedRhs, lhsTypeInfo, rhsTypeInfo, setMethodName, ref kind);
    }

    /// <summary>
    /// A C# compound operator casts its result back to the left side's type, so it's only split out where the conversion back differs, or for a setter call, which doesn't cast.
    /// </summary>
    private ExpressionSyntax ConvertNonLateBoundCompoundRhs(VBSyntax.AssignmentStatementSyntax node, ExpressionSyntax lhs, ExpressionSyntax typeConvertedRhs, TypeInfo lhsTypeInfo, TypeInfo rhsTypeInfo, string setMethodName, ref SyntaxKind kind)
    {
        var isShift = node.IsKind(VBasic.SyntaxKind.LeftShiftAssignmentStatement, VBasic.SyntaxKind.RightShiftAssignmentStatement);
        var shiftedType = isShift ? CommonConversions.TypeConversionAnalyzer.GetVbShiftedType(lhsTypeInfo.Type) : null;
        if (isShift) {
            typeConvertedRhs = CommonConversions.TypeConversionAnalyzer.MaskShiftCountIfNeeded(shiftedType, node.Right, typeConvertedRhs);
        }

        // Split out compound operator if type conversion needed on result
        if (TypeConversionAnalyzer.GetNonCompoundOrNull(kind) is {} nonCompound) {
                
            // The right operand is converted to the operator's type, except a shift count, which is always Integer
            var nonCompoundResultType = shiftedType ?? rhsTypeInfo.ConvertedType;
            // Unlike a C# compound operator, a setter call doesn't cast the promoted int back
            var csNonCompoundResultType = setMethodName == null ? nonCompoundResultType : CommonConversions.TypeConversionAnalyzer.GetCSharpOperatorResultType(nonCompoundResultType);
            var nonCompoundLhs = shiftedType != null && !SymbolEqualityComparer.Default.Equals(shiftedType, lhsTypeInfo.Type)
                ? CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Left, lhs, forceTargetType: shiftedType)
                : lhs;
            // The simplifier removes these parentheses where the right side doesn't need them, e.g. unlike a conditional
            var nonCompoundRhs = SyntaxFactory.BinaryExpression(nonCompound, nonCompoundLhs, typeConvertedRhs.AddParens());
            var typeConvertedNonCompoundRhs = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Right, nonCompoundRhs, forceSourceType: csNonCompoundResultType, forceTargetType: lhsTypeInfo.Type);
            if (nonCompoundRhs != typeConvertedNonCompoundRhs || setMethodName != null) {
                kind = SyntaxKind.SimpleAssignmentExpression;
                typeConvertedRhs = typeConvertedNonCompoundRhs;
            }
        } else if (node.IsKind(VBasic.SyntaxKind.ExponentiateAssignmentStatement)) {
            // ExponentiateAssignmentStatement evaluates to Math.Pow invocation (a Double) which might need converting back to lhsType
            var typeConvertedNonCompoundRhs = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Right, typeConvertedRhs, forceSourceType: _semanticModel.Compilation.GetTypeByMetadataName("System.Double"), forceTargetType: lhsTypeInfo.Type);
            kind = SyntaxKind.SimpleAssignmentExpression;
            typeConvertedRhs = typeConvertedNonCompoundRhs;
        }

        return typeConvertedRhs;
    }

    /// <summary>
    /// With an Object operand, VB binds the operator at runtime (like Operators.LeftShiftObject for b &lt;&lt; o), which C# has no compound operator for.
    /// Keyed on the VB statement, since e.g. &amp;= and += both become C#'s +=, and /= and \= both become /=.
    /// </summary>
    private static KnownMethod? GetLateBoundOperatorOrNull(VBSyntax.AssignmentStatementSyntax node, TypeInfo lhsTypeInfo, TypeInfo rhsTypeInfo)
    {
        if (lhsTypeInfo.Type?.SpecialType != SpecialType.System_Object && rhsTypeInfo.Type?.SpecialType != SpecialType.System_Object) return null;
        string methodName = node.Kind() switch {
            VBasic.SyntaxKind.AddAssignmentStatement => "AddObject",
            VBasic.SyntaxKind.SubtractAssignmentStatement => "SubtractObject",
            VBasic.SyntaxKind.MultiplyAssignmentStatement => "MultiplyObject",
            VBasic.SyntaxKind.DivideAssignmentStatement => "DivideObject",
            VBasic.SyntaxKind.IntegerDivideAssignmentStatement => "IntDivideObject",
            VBasic.SyntaxKind.ExponentiateAssignmentStatement => "ExponentObject",
            VBasic.SyntaxKind.ConcatenateAssignmentStatement => "ConcatenateObject",
            VBasic.SyntaxKind.LeftShiftAssignmentStatement => "LeftShiftObject",
            VBasic.SyntaxKind.RightShiftAssignmentStatement => "RightShiftObject",
            _ => null
        };
        return methodName == null ? null : new KnownMethod("Microsoft.VisualBasic.CompilerServices", "Operators", methodName);
    }

    private async Task<SyntaxList<StatementSyntax>> ConvertMidAssignmentAsync(VBSyntax.AssignmentStatementSyntax node, VBSyntax.MidExpressionSyntax mes)
    {
        _extraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
        var midFunction = ValidSyntaxFactory.MemberAccess("StringType", "MidStmtStr");
        var midArgList = await mes.ArgumentList.AcceptAsync<ArgumentListSyntax>(_expressionVisitor);
        var (reusable, statements, _) = await GetExpressionWithoutSideEffectsAsync(node.Right, "midTmp");
        if (midArgList.Arguments.Count == 2) {
            var length = ValidSyntaxFactory.MemberAccess(reusable, "Length");
            midArgList = midArgList.AddArguments(SyntaxFactory.Argument(length));
        }
        midArgList = midArgList.AddArguments(SyntaxFactory.Argument(reusable));
        var invokeMid = SyntaxFactory.InvocationExpression(midFunction, midArgList);
        return statements.Add(SyntaxFactory.ExpressionStatement(invokeMid));
    }

    /// <remarks>
    /// <see cref="CommonConversions.ConvertIdentifier"/> ensures we convert the property access to a field access
    /// </remarks>
    private SyntaxList<StatementSyntax> GetPostAssignmentStatements(VBSyntax.AssignmentStatementSyntax node)
    {
        var potentialPropertySymbol = _semanticModel.GetSymbolInfo(node.Left).ExtractBestMatch<ISymbol>();
        return GetPostAssignmentStatements(node, potentialPropertySymbol);
    }

    /// <summary>
    /// Make winforms designer work: https://github.com/icsharpcode/CodeConverter/issues/321
    /// </summary>
    public SyntaxList<StatementSyntax> GetPostAssignmentStatements(Microsoft.CodeAnalysis.VisualBasic.Syntax.AssignmentStatementSyntax node, ISymbol potentialPropertySymbol)
    {
        if (CommonConversions.WinformsConversions.MayNeedToInlinePropertyAccess(node, potentialPropertySymbol)) {
            return _handledEventsAnalysis.GetPostAssignmentStatements(potentialPropertySymbol);
        }

        return SyntaxFactory.List<StatementSyntax>();
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitEraseStatement(VBSyntax.EraseStatementSyntax node)
    {
        var eraseStatements = await node.Expressions.SelectAsync<VBSyntax.ExpressionSyntax, StatementSyntax>(async arrayExpression => {
            var lhs = await arrayExpression.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
            var rhs = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
            var assignmentExpressionSyntax =
                SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, lhs,
                    rhs);
            return SyntaxFactory.ExpressionStatement(assignmentExpressionSyntax);
        });
        return SyntaxFactory.List(eraseStatements);
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitReDimStatement(VBSyntax.ReDimStatementSyntax node)
    {
        return SyntaxFactory.List(await node.Clauses.SelectManyAsync(async arrayExpression => (IEnumerable<StatementSyntax>) await ConvertRedimClauseAsync(arrayExpression)));
    }

    /// <remarks>
    /// RedimClauseSyntax isn't an executable statement, therefore this isn't a "Visit" method.
    /// Since it returns multiple statements it's easiest for it to be here in the current architecture.
    /// </remarks>
    private async Task<SyntaxList<StatementSyntax>> ConvertRedimClauseAsync(VBSyntax.RedimClauseSyntax node)
    {
        bool preserve = node.Parent is VBSyntax.ReDimStatementSyntax rdss && rdss.PreserveKeyword.IsKind(VBasic.SyntaxKind.PreserveKeyword);

        var csTargetArrayExpression = await node.Expression.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
        var convertedBounds = (await CommonConversions.ConvertArrayBoundsAsync(node.ArrayBounds)).Sizes.ToList();
        if (preserve && convertedBounds.Count == 1) {
            bool isProperty = _semanticModel.GetSymbolInfo(node.Expression).Symbol?.IsKind(SymbolKind.Property) == true;
            var arrayToResize = isProperty ? CreateLocalVariableWithUniqueName(node.Expression, "arg" + csTargetArrayExpression.ToString().Split('.').Last(), csTargetArrayExpression) : default;
            var resizeArg = isProperty ? (ExpressionSyntax)arrayToResize.Reference : csTargetArrayExpression;

            var argumentList = new[] { resizeArg, convertedBounds.Single() }.CreateCsArgList(SyntaxKind.RefKeyword);
            var arrayResize = SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(nameof(Array), nameof(Array.Resize)), argumentList);

            if (isProperty) {
                var assignment = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, csTargetArrayExpression, arrayToResize.Reference);
                return SyntaxFactory.List(new StatementSyntax[] { arrayToResize.Declaration, SyntaxFactory.ExpressionStatement(arrayResize), SyntaxFactory.ExpressionStatement(assignment) });
            }

            return SingleStatement(arrayResize);
        }
        var newArrayAssignment = CreateNewArrayAssignment(node.Expression, csTargetArrayExpression, convertedBounds);
        if (!preserve) return SingleStatement(newArrayAssignment);

        var lastIdentifierText = node.Expression.DescendantNodesAndSelf().OfType<VBSyntax.IdentifierNameSyntax>().Last().Identifier.Text;
        string variableNameBase = "old" + lastIdentifierText.ToPascalCase();
        var expr = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, csTargetArrayExpression);
        var (stmt, oldTargetExpression) = CreateLocalVariableWithUniqueName(node.Expression, variableNameBase, expr);
            
        var arrayCopyIfNotNull = CreateConditionalArrayCopy(node, oldTargetExpression, csTargetArrayExpression, convertedBounds);

        return SyntaxFactory.List(new[] { stmt, newArrayAssignment, arrayCopyIfNotNull});
    }

    /// <summary>
    /// Cut down version of Microsoft.VisualBasic.CompilerServices.Utils.CopyArray
    /// </summary>
    private IfStatementSyntax CreateConditionalArrayCopy(VBasic.VisualBasicSyntaxNode originalVbNode,
        IdentifierNameSyntax sourceArrayExpression,
        ExpressionSyntax targetArrayExpression,
        List<ExpressionSyntax> convertedBounds)
    {
        var sourceLength = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, sourceArrayExpression, ValidSyntaxFactory.IdentifierName("Length"));
        var arrayCopyStatement = convertedBounds.Count == 1
            ? CreateArrayCopyWithMinOfLengths(sourceArrayExpression, sourceLength, targetArrayExpression, convertedBounds.Single())
            : CreateArrayCopy(originalVbNode, sourceArrayExpression, sourceLength, targetArrayExpression, convertedBounds);

        var oldTargetNotEqualToNull = CommonConversions.NotNothingComparison(sourceArrayExpression, true);
        return SyntaxFactory.IfStatement(oldTargetNotEqualToNull, arrayCopyStatement);
    }

    /// <summary>
    /// Array copy for multiple array dimensions represented by <paramref name="convertedBounds"/>
    /// </summary>
    /// <remarks>
    /// Exception cases will sometimes silently succeed in the converted code,
    ///  but existing VB code relying on the exception thrown from a multidimensional redim preserve on
    ///  different rank arrays is hopefully rare enough that it's worth saving a few lines of code
    /// </remarks>
    private StatementSyntax CreateArrayCopy(VBasic.VisualBasicSyntaxNode originalVbNode,
        IdentifierNameSyntax sourceArrayExpression,
        MemberAccessExpressionSyntax sourceLength,
        ExpressionSyntax targetArrayExpression, ICollection convertedBounds)
    {
        var lastSourceLengthArgs = ExpressionSyntaxExtensions.CreateArgList(CommonConversions.Literal(convertedBounds.Count - 1));
        var sourceLastRankLength = SyntaxFactory.InvocationExpression(
            SyntaxFactory.ParseExpression($"{sourceArrayExpression.Identifier}.GetLength"), lastSourceLengthArgs);
        var targetLastRankLength =
            SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression($"{targetArrayExpression}.GetLength"),
                lastSourceLengthArgs);
        var length = SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("Math.Min"), ExpressionSyntaxExtensions.CreateArgList(sourceLastRankLength, targetLastRankLength));

        var loopVariableName = GetUniqueVariableNameInScope(originalVbNode, "i");
        var loopVariableIdentifier = ValidSyntaxFactory.IdentifierName(loopVariableName);
        var sourceStartForThisIteration =
            SyntaxFactory.BinaryExpression(SyntaxKind.MultiplyExpression, loopVariableIdentifier, sourceLastRankLength);
        var targetStartForThisIteration =
            SyntaxFactory.BinaryExpression(SyntaxKind.MultiplyExpression, loopVariableIdentifier, targetLastRankLength);

        var arrayCopy = CreateArrayCopyWithStartingPoints(sourceArrayExpression, sourceStartForThisIteration, targetArrayExpression,
            targetStartForThisIteration, length);

        var sourceArrayCount = SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression,
            SyntaxFactory.BinaryExpression(SyntaxKind.DivideExpression, sourceLength, sourceLastRankLength), CommonConversions.Literal(1));

        return CreateForZeroToValueLoop(loopVariableIdentifier, arrayCopy, sourceArrayCount);
    }

    private static ForStatementSyntax CreateForZeroToValueLoop(SimpleNameSyntax loopVariableIdentifier, StatementSyntax loopStatement, ExpressionSyntax inclusiveLoopUpperBound)
    {
        var loopVariableAssignment = CommonConversions.CreateVariableDeclarationAndAssignment(loopVariableIdentifier.Identifier.Text, CommonConversions.Literal(0));
        var lessThanSourceBounds = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression,
            loopVariableIdentifier, inclusiveLoopUpperBound);
        var incrementors = SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
            SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreIncrementExpression, loopVariableIdentifier));
        var forStatementSyntax = SyntaxFactory.ForStatement(loopVariableAssignment,
            SyntaxFactory.SeparatedList<ExpressionSyntax>(),
            lessThanSourceBounds, incrementors, loopStatement);
        return forStatementSyntax;
    }

    private static ExpressionStatementSyntax CreateArrayCopyWithMinOfLengths(
        IdentifierNameSyntax sourceExpression, ExpressionSyntax sourceLength,
        ExpressionSyntax targetExpression, ExpressionSyntax targetLength)
    {
        var minLength = SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("Math.Min"), ExpressionSyntaxExtensions.CreateArgList(targetLength, sourceLength));
        var copyArgList = ExpressionSyntaxExtensions.CreateArgList(sourceExpression, targetExpression, minLength);
        var arrayCopy = SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("Array.Copy"), copyArgList);
        return SyntaxFactory.ExpressionStatement(arrayCopy);
    }

    private static ExpressionStatementSyntax CreateArrayCopyWithStartingPoints(
        IdentifierNameSyntax sourceExpression, ExpressionSyntax sourceStart,
        ExpressionSyntax targetExpression, ExpressionSyntax targetStart, ExpressionSyntax length)
    {
        var copyArgList = ExpressionSyntaxExtensions.CreateArgList(sourceExpression, sourceStart, targetExpression, targetStart, length);
        var arrayCopy = SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("Array.Copy"), copyArgList);
        return SyntaxFactory.ExpressionStatement(arrayCopy);
    }

    private ExpressionStatementSyntax CreateNewArrayAssignment(VBSyntax.ExpressionSyntax vbArrayExpression,
        ExpressionSyntax csArrayExpression, List<ExpressionSyntax> convertedBounds)
    {
        var convertedType = (IArrayTypeSymbol) _semanticModel.GetTypeInfo(vbArrayExpression).ConvertedType;
        var arrayRankSpecifierSyntax = SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SeparatedList(convertedBounds));
        var rankSpecifiers = SyntaxFactory.SingletonList(arrayRankSpecifierSyntax);
        while (convertedType.ElementType is IArrayTypeSymbol ats) {
            convertedType = ats;
            rankSpecifiers = rankSpecifiers.Add(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression())));
        };
        var typeSyntax = CommonConversions.GetTypeSyntax(convertedType.ElementType);
        var arrayCreation =
            SyntaxFactory.ArrayCreationExpression(SyntaxFactory.ArrayType(typeSyntax, rankSpecifiers));
        var assignmentExpressionSyntax =
            SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, csArrayExpression, arrayCreation);
        var newArrayAssignment = SyntaxFactory.ExpressionStatement(assignmentExpressionSyntax);
        return newArrayAssignment;
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitThrowStatement(VBSyntax.ThrowStatementSyntax node)
    {
        var csExpression = await node.Expression.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
        return WithWriteBacksBeforeJump(node.Expression, csExpression, "ex", SyntaxFactory.ThrowStatement);
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitReturnStatement(VBSyntax.ReturnStatementSyntax node)
    {
        if (IsIterator)
            return SingleStatement(SyntaxFactory.YieldStatement(SyntaxKind.YieldBreakStatement));

        var csExpression = await node.Expression.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
        csExpression = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, csExpression);
        return WithWriteBacksBeforeJump(node.Expression, csExpression, "ret", SyntaxFactory.ReturnStatement);
    }

    /// <summary>
    /// Write-backs are normally emitted after the converted statement, which after a jump would be unreachable (or, for yield, delayed until the consumer resumes).
    /// </summary>
    private SyntaxList<StatementSyntax> WithWriteBacksBeforeJump(VBSyntax.ExpressionSyntax vbExpression, ExpressionSyntax csExpression, string resultPrefix,
        Func<ExpressionSyntax, StatementSyntax> createJump)
    {
        if (csExpression is null) return SingleStatement(createJump(null));
        return SyntaxFactory.List(_perScopeState.WithWriteBacksBefore(csExpression, resultPrefix, () => GetResultTypeSyntax(vbExpression), createJump));
    }

    /// <summary>
    /// <c>var</c> is only safe when the expression's own type is the one the jump needs: e.g. Nothing or a lambda has no natural type in C#.
    /// </summary>
    private TypeSyntax GetResultTypeSyntax(VBSyntax.ExpressionSyntax vbExpression)
    {
        var typeInfo = _semanticModel.GetTypeInfo(vbExpression);
        var isNaturalType = typeInfo.Type is { } type && SymbolEqualityComparer.Default.Equals(type, typeInfo.ConvertedType);
        return CommonConversions.GetTypeSyntax(typeInfo.ConvertedType, useImplicitType: isNaturalType);
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitContinueStatement(VBSyntax.ContinueStatementSyntax node)
    {
        var vbBlockKeywordKind = VBasic.VisualBasicExtensions.Kind(node.BlockKeyword);
        return SyntaxFactory.List(_perScopeState.ConvertContinue(vbBlockKeywordKind));
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitYieldStatement(VBSyntax.YieldStatementSyntax node)
    {
        var csExpression = await node.Expression.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
        return WithWriteBacksBeforeJump(node.Expression, csExpression, "ret", e => SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, e));
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitExitStatement(VBSyntax.ExitStatementSyntax node)
    {
        var vbBlockKeywordKind = VBasic.VisualBasicExtensions.Kind(node.BlockKeyword);
        switch (vbBlockKeywordKind) {
            case VBasic.SyntaxKind.SubKeyword:
            case VBasic.SyntaxKind.PropertyKeyword when node.GetAncestor<VBSyntax.AccessorBlockSyntax>()?.IsKind(VBasic.SyntaxKind.GetAccessorBlock) != true:
                return SingleStatement(SyntaxFactory.ReturnStatement());
            case VBasic.SyntaxKind.FunctionKeyword:
            case VBasic.SyntaxKind.PropertyKeyword when node.GetAncestor<VBSyntax.AccessorBlockSyntax>()?.IsKind(VBasic.SyntaxKind.GetAccessorBlock) == true:
                return SingleStatement(CreateMethodExitStatement(node));
            default:
                return SyntaxFactory.List(_perScopeState.ConvertExit(vbBlockKeywordKind));
        }
    }

    /// <summary>
    /// What leaving the method containing <paramref name="node"/> without a <c>Return</c> statement does: for a function, return its return variable or the default value.
    /// </summary>
    public StatementSyntax CreateMethodExitStatement(VBasic.VisualBasicSyntaxNode node)
    {
        VBasic.VisualBasicSyntaxNode typeContainer = node.GetAncestor<VBSyntax.LambdaExpressionSyntax>()
                                                     ?? (VBasic.VisualBasicSyntaxNode)node.GetAncestor<VBSyntax.MethodBlockSyntax>()
                                                     ?? node.GetAncestor<VBSyntax.AccessorBlockSyntax>();
        var enclosingMethodInfo = typeContainer switch {
            VBSyntax.LambdaExpressionSyntax e => _semanticModel.GetSymbolInfo(e).Symbol,
            VBSyntax.MethodBlockSyntax e => _semanticModel.GetDeclaredSymbol(e.SubOrFunctionStatement),
            VBSyntax.AccessorBlockSyntax e => _semanticModel.GetDeclaredSymbol(e.AccessorStatement),
            _ => null
        } as IMethodSymbol;

        if (IsIterator) return SyntaxFactory.YieldStatement(SyntaxKind.YieldBreakStatement);

        if (!enclosingMethodInfo.ReturnsVoidOrAsyncTask()) {
            ExpressionSyntax expr = HasReturnVariable ? ReturnVariable : SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);
            return SyntaxFactory.ReturnStatement(expr);
        }

        return SyntaxFactory.ReturnStatement();
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitRaiseEventStatement(VBSyntax.RaiseEventStatementSyntax node)
    {
        var argumentListSyntax = await node.ArgumentList.AcceptAsync<ArgumentListSyntax>(_expressionVisitor) ?? SyntaxFactory.ArgumentList();

        var symbolInfo = _semanticModel.GetSymbolInfo(node.Name).ExtractBestMatch<IEventSymbol>();
        if (symbolInfo?.RaiseMethod != null) {
            return SingleStatement(SyntaxFactory.InvocationExpression(
                ValidSyntaxFactory.IdentifierName($"On{symbolInfo.Name}"),
                argumentListSyntax));
        }

        var memberBindingExpressionSyntax = SyntaxFactory.MemberBindingExpression(ValidSyntaxFactory.IdentifierName("Invoke"));
        var conditionalAccessExpressionSyntax = SyntaxFactory.ConditionalAccessExpression(
            await node.Name.AcceptAsync<NameSyntax>(_expressionVisitor),
            SyntaxFactory.InvocationExpression(memberBindingExpressionSyntax, argumentListSyntax)
        );
        return SingleStatement(
            conditionalAccessExpressionSyntax
        );
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitSingleLineIfStatement(VBSyntax.SingleLineIfStatementSyntax node)
    {
        var condition = await ConvertConditionAsync(node.Condition);
        var block = SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements));
        ElseClauseSyntax elseClause = null;

        if (node.ElseClause != null) {
            var elseBlock = SyntaxFactory.Block(await ConvertStatementsAsync(node.ElseClause.Statements));
            elseClause = SyntaxFactory.ElseClause(elseBlock.UnpackNonNestedBlock());
        }
        return SingleStatement(SyntaxFactory.IfStatement(condition, block.UnpackNonNestedBlock(), elseClause));
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitMultiLineIfBlock(VBSyntax.MultiLineIfBlockSyntax node)
    {
        var condition = await ConvertConditionAsync(node.IfStatement.Condition);
        var block = SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements));

        var elseClause = await ConvertElseClauseAsync(node.ElseBlock);
        elseClause = elseClause.WithVbSourceMappingFrom(node.ElseBlock); //Special case where explicit mapping is needed since block becomes clause so cannot be easily visited

        var elseIfBlocks = await node.ElseIfBlocks.SelectAsync(async elseIf => await ConvertElseIfAsync(elseIf));
        foreach (var elseIf in Enumerable.Reverse(elseIfBlocks)) {
            var ifStmt = SyntaxFactory.IfStatement(elseIf.ElseIfCondition, elseIf.ElseBlock, elseClause);
            elseClause = SyntaxFactory.ElseClause(ifStmt);
        }

        return SingleStatement(SyntaxFactory.IfStatement(condition, block, elseClause));
    }

    private async Task<(ExpressionSyntax ElseIfCondition, BlockSyntax ElseBlock)> ConvertElseIfAsync(VBSyntax.ElseIfBlockSyntax elseIf)
    {
        var elseBlock = SyntaxFactory.Block(await ConvertStatementsAsync(elseIf.Statements));
        var elseIfCondition = await ConvertConditionAsync(elseIf.ElseIfStatement.Condition);
        return (elseIfCondition, elseBlock);
    }

    /// <summary>
    /// VB converts a condition to Boolean (e.g. Nothing to False), whereas C# requires it to already be bool
    /// </summary>
    private async Task<ExpressionSyntax> ConvertConditionAsync(VBSyntax.ExpressionSyntax vbCondition)
    {
        var condition = await vbCondition.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
        return CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(vbCondition, condition, forceTargetType: CommonConversions.KnownTypes.Boolean);
    }

    private async Task<ElseClauseSyntax> ConvertElseClauseAsync(VBSyntax.ElseBlockSyntax elseBlock)
    {
        if (elseBlock == null) return null;

        var csStatements = await ConvertStatementsAsync(elseBlock.Statements);
        if (csStatements.TryUnpackSingleStatement(out var stmt) && stmt.IsKind(SyntaxKind.IfStatement)) {
            // so that you get a neat "else if" at the end
            return SyntaxFactory.ElseClause(stmt);
        }

        return SyntaxFactory.ElseClause(SyntaxFactory.Block(csStatements));
    }

    private async Task<StatementSyntax[]> ConvertStatementsAsync(SyntaxList<VBSyntax.StatementSyntax> statementSyntaxs)
    {
        return await statementSyntaxs.SelectManyAsync(async s => (IEnumerable<StatementSyntax>)await s.Accept(CommentConvertingVisitor));
    }

    /// <summary>
    /// See https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/statements/for-next-statement#BKMK_Counter
    /// </summary>
    public override async Task<SyntaxList<StatementSyntax>> VisitForBlock(VBSyntax.ForBlockSyntax node)
    {
        var stmt = node.ForStatement;
        VariableDeclarationSyntax declaration = null;
        ExpressionSyntax id;
        var controlVarSymbol  = _semanticModel.GetSymbolInfo(stmt.ControlVariable).Symbol ?? (_semanticModel.GetOperation(stmt.ControlVariable) as IVariableDeclaratorOperation)?.Symbol;
            
        // If missing semantic info, the compiler just guesses object, let's try to improve on that guess:
        var controlVarType = controlVarSymbol?.GetSymbolType().Yield().Concat(
            new SyntaxNode[] {stmt.ControlVariable, stmt.FromValue, stmt.ToValue, stmt.StepClause?.StepValue}
                .Select(exp => _semanticModel.GetTypeInfo(exp).Type)
        ).FirstOrDefault(t => t != null && t.SpecialType != SpecialType.System_Object);
        var startValue = await stmt.FromValue.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
        startValue = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(stmt.FromValue, startValue?.SkipIntoParens(), forceTargetType: controlVarType);
            
        var initializers = new List<ExpressionSyntax>();
        var controlVarTypeSyntax = CommonConversions.GetTypeSyntax(controlVarType);
        if (stmt.ControlVariable is VBSyntax.VariableDeclaratorSyntax v) {
            declaration = (await SplitVariableDeclarationsAsync(v)).Variables.Single().Decl;
            declaration = declaration.WithVariables(SyntaxFactory.SingletonSeparatedList(declaration.Variables[0].WithInitializer(SyntaxFactory.EqualsValueClause(startValue))));
            id = ValidSyntaxFactory.IdentifierName(declaration.Variables[0].Identifier);
        } else {
            id = await stmt.ControlVariable.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
                
            if (controlVarSymbol != null && controlVarSymbol.DeclaringSyntaxReferences.Any(r => r.Span.OverlapsWith(stmt.ControlVariable.Span))) {
                declaration = CommonConversions.CreateVariableDeclarationAndAssignment(controlVarSymbol.Name, startValue, controlVarTypeSyntax);
            } else {
                startValue = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, id, startValue);
                initializers.Add(startValue);
            }
        }

           
        var preLoopStatements = new List<StatementSyntax>();
        var csToValue = await stmt.ToValue.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
        csToValue = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(stmt.ToValue, csToValue?.SkipIntoParens(), forceTargetType: controlVarType);

        // In Visual Basic, the To expression is only evaluated once, but in C# will be evaluated every loop.
        // If it could evaluate differently or has side effects, it must be extracted as a variable
        if (!_semanticModel.GetConstantValue(stmt.ToValue).HasValue) {
            var loopToVariableName = GetUniqueVariableNameInScope(node, "loopTo");
            var toVariableId = ValidSyntaxFactory.IdentifierName(loopToVariableName);

            var loopToAssignment = CommonConversions.CreateVariableDeclarator(loopToVariableName, csToValue);
            if (initializers.Any()) {
                var loopEndDeclaration = SyntaxFactory.LocalDeclarationStatement(
                    CommonConversions.CreateVariableDeclarationAndAssignment(loopToVariableName, csToValue));
                // Does not do anything about porting newline trivia upwards to maintain spacing above the loop
                preLoopStatements.Add(loopEndDeclaration);
            } else {
                declaration = declaration == null
                    ? SyntaxFactory.VariableDeclaration(controlVarTypeSyntax,
                        SyntaxFactory.SingletonSeparatedList(loopToAssignment))
                    : declaration.AddVariables(loopToAssignment).WithType(controlVarTypeSyntax);
            }

            csToValue = toVariableId;
        }

        var (csCondition, csStep) = await ConvertConditionAndStepClauseAsync(stmt, id, csToValue, controlVarType);

        var block = SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements));
        var forStatementSyntax = SyntaxFactory.ForStatement(
            declaration,
            SyntaxFactory.SeparatedList(initializers),
            csCondition,
            SyntaxFactory.SingletonSeparatedList(csStep),
            block.UnpackNonNestedBlock());
        return SyntaxFactory.List(preLoopStatements.Concat(new[] { forStatementSyntax }));
    }

    private async Task<(IReadOnlyCollection<CommonConversions.VariablesDeclaration> Variables, IReadOnlyCollection<CSharpSyntaxNode> Methods)> SplitVariableDeclarationsAsync(VBSyntax.VariableDeclaratorSyntax v, bool preferExplicitType = false)
    {
        return await CommonConversions.SplitVariableDeclarationsAsync(v, _localsToInlineInLoop, preferExplicitType);
    }

    private async Task<(ExpressionSyntax, ExpressionSyntax)> ConvertConditionAndStepClauseAsync(VBSyntax.ForStatementSyntax stmt, ExpressionSyntax id, ExpressionSyntax csToValue, ITypeSymbol controlVarType)
    {
        var vbStepValue = stmt.StepClause?.StepValue;
        var csStepValue = await (stmt.StepClause?.StepValue).AcceptAsync<ExpressionSyntax>(_expressionVisitor);
        // For an enum, you need to add on an integer for example:
        var forceStepType = controlVarType is INamedTypeSymbol nt && nt.IsEnumType() ? nt.EnumUnderlyingType : controlVarType;
        csStepValue = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(vbStepValue, csStepValue?.SkipIntoParens(), forceTargetType: forceStepType);

        var nonNegativeCondition = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression, id, csToValue);
        var negativeCondition = SyntaxFactory.BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, id, csToValue);
        if (csStepValue == null) {
            return (nonNegativeCondition, SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, id));
        }

        ExpressionSyntax csCondition;
        ExpressionSyntax csStep = SyntaxFactory.AssignmentExpression(SyntaxKind.AddAssignmentExpression, id, csStepValue);
        var vbStepConstValue = _semanticModel.GetConstantValue(vbStepValue);
        var constValue = !vbStepConstValue.HasValue ? null : (dynamic)vbStepConstValue.Value;
        if (constValue == null) {
            var ifStepNonNegative = SyntaxFactory.BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, csStepValue, CommonConversions.Literal(0));
            csCondition = SyntaxFactory.ConditionalExpression(ifStepNonNegative, nonNegativeCondition, negativeCondition);
        } else if (constValue < 0) {
            csCondition = negativeCondition;
            if (csStepValue is PrefixUnaryExpressionSyntax pues && pues.OperatorToken.IsKind(SyntaxKind.MinusToken)) {
                csStep = SyntaxFactory.AssignmentExpression(SyntaxKind.SubtractAssignmentExpression, id, pues.Operand);
            }
        } else {
            csCondition = nonNegativeCondition;
        }

        return (csCondition, csStep);
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitForEachBlock(VBSyntax.ForEachBlockSyntax node)
    {
        var stmt = node.ForEachStatement;

        TypeSyntax type;
        SyntaxToken id;
        List<StatementSyntax> statements = new List<StatementSyntax>();
        if (stmt.ControlVariable is VBSyntax.VariableDeclaratorSyntax vds) {
            var declaration = (await SplitVariableDeclarationsAsync(vds)).Variables.Single().Decl;
            type = declaration.Type;
            id = declaration.Variables.Single().Identifier;
        } else if (_semanticModel.GetSymbolInfo(stmt.ControlVariable).Symbol is { } varSymbol) {
            var variableType = varSymbol.GetSymbolType();
            var explicitCastWouldHaveNoEffect = variableType?.SpecialType == SpecialType.System_Object || _semanticModel.GetTypeInfo(stmt.Expression).ConvertedType.IsEnumerableOfExactType(variableType);
            type = CommonConversions.GetTypeSyntax(varSymbol.GetSymbolType(), explicitCastWouldHaveNoEffect);
            var v = await stmt.ControlVariable.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
            if (_localsToInlineInLoop.Contains(varSymbol, SymbolEqualityComparer.IncludeNullability) && v is IdentifierNameSyntax vId) {
                id = vId.Identifier;
            } else {
                id = CommonConversions.CsEscapedIdentifier(GetUniqueVariableNameInScope(node, "current" + varSymbol.Name.ToPascalCase()));
                statements.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, v, ValidSyntaxFactory.IdentifierName(id))));
            }
        } else {
            var v = await stmt.ControlVariable.AcceptAsync<IdentifierNameSyntax>(_expressionVisitor);
            id = v.Identifier;
            type = ValidSyntaxFactory.VarType;
        }

        var block = SyntaxFactory.Block(statements.Concat(await ConvertStatementsAsync(node.Statements)));
        var csExpression = await stmt.Expression.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
        return SingleStatement(SyntaxFactory.ForEachStatement(
            type,
            id,
            CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(stmt.Expression, csExpression),
            block.UnpackNonNestedBlock()
        ));
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitLabelStatement(VBSyntax.LabelStatementSyntax node)
    {
        return SingleStatement(SyntaxFactory.LabeledStatement(CommonConversions.CsEscapedIdentifier(node.LabelToken.Text), SyntaxFactory.EmptyStatement()));
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitGoToStatement(VBSyntax.GoToStatementSyntax node)
    {
        return SingleStatement(SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement,
            ValidSyntaxFactory.IdentifierName((node.Label.LabelToken.Text))));
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitSelectBlock(VBSyntax.SelectBlockSyntax node)
    {
        var vbExpr = node.SelectStatement.Expression;
        var vbEquality = CommonConversions.VisualBasicEqualityComparison;

        var csSwitchExpr = await vbExpr.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
        csSwitchExpr = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(vbExpr, csSwitchExpr);
        var switchExprTypeInfo = _semanticModel.GetTypeInfo(vbExpr);
        var isObjectComparison = switchExprTypeInfo.ConvertedType.SpecialType == SpecialType.System_Object;
        var isStringComparison = !isObjectComparison && switchExprTypeInfo.ConvertedType?.SpecialType == SpecialType.System_String || switchExprTypeInfo.ConvertedType?.IsArrayOf(SpecialType.System_Char) == true;
        var caseInsensitiveStringComparison = vbEquality.OptionCompareTextCaseInsensitive &&
                                              isStringComparison;
        if (isStringComparison) {
            csSwitchExpr = vbEquality.VbCoerceToNonNullString(vbExpr, csSwitchExpr, switchExprTypeInfo);
        }

        var usedConstantValues = new HashSet<object>();
        var sections = new List<SwitchSectionSyntax>();
        foreach (var block in node.CaseBlocks) {
            var labels = new List<SwitchLabelSyntax>();
            foreach (var c in block.CaseStatement.Cases) {
                if (c is VBSyntax.SimpleCaseClauseSyntax s) {
                    var originalExpressionSyntax = await s.Value.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
                    var caseTypeInfo = _semanticModel.GetTypeInfo(s.Value);
                    var typeConversionKind = CommonConversions.TypeConversionAnalyzer.AnalyzeConversion(s.Value);
                    var correctTypeExpressionSyntax = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(s.Value, originalExpressionSyntax, typeConversionKind, true, true);
                    var constantValue = _semanticModel.GetConstantValue(s.Value);
                    var notAlreadyUsed = !constantValue.HasValue || usedConstantValues.Add(constantValue.Value);

                    // Pass both halves in case we can optimize away the check based on the switch expr
                    var wrapForStringComparison = isStringComparison && (caseInsensitiveStringComparison ||
                                                                         vbEquality.VbCoerceToNonNullString(vbExpr, csSwitchExpr, switchExprTypeInfo, true, s.Value, originalExpressionSyntax, caseTypeInfo, false).rhs != originalExpressionSyntax);

                    // CSharp requires an explicit cast from the base type (e.g. int) in most cases switching on an enum
                    var isBooleanCase = caseTypeInfo.Type?.SpecialType == SpecialType.System_Boolean;
                    bool enumRelated = IsEnumOrNullableEnum(switchExprTypeInfo.ConvertedType) || IsEnumOrNullableEnum(caseTypeInfo.Type);
                    bool convertingEnum = IsEnumOrNullableEnum(switchExprTypeInfo.ConvertedType) ^ IsEnumOrNullableEnum(caseTypeInfo.Type);
                    var csExpressionToUse = !isObjectComparison && !isBooleanCase && (convertingEnum || !enumRelated && correctTypeExpressionSyntax.IsConst)
                        ? correctTypeExpressionSyntax.Expr
                        : originalExpressionSyntax;

                    var caseSwitchLabelSyntax = !isObjectComparison && !wrapForStringComparison && correctTypeExpressionSyntax.IsConst && notAlreadyUsed
                        ? (SwitchLabelSyntax)SyntaxFactory.CaseSwitchLabel(csExpressionToUse)
                        : WrapInCasePatternSwitchLabelSyntax(node, s.Value, csExpressionToUse, isBooleanCase);
                    labels.Add(caseSwitchLabelSyntax);
                } else if (c is VBSyntax.ElseCaseClauseSyntax) {
                    labels.Add(SyntaxFactory.DefaultSwitchLabel());
                } else if (c is VBSyntax.RelationalCaseClauseSyntax relational) {

                    var operatorKind = VBasic.VisualBasicExtensions.Kind(relational);
                    var csRelationalValue = await relational.Value.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
                    CasePatternSwitchLabelSyntax caseSwitchLabelSyntax;
                    if (isObjectComparison) {
                        caseSwitchLabelSyntax = WrapInCasePatternSwitchLabelSyntax(node, relational.Value, csRelationalValue, false, operatorKind);
                    }
                    else if (!isStringComparison && _semanticModel.GetConstantValue(relational.Value).HasValue) {
                        csRelationalValue = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(relational.Value, csRelationalValue);
                        var operatorToken = SyntaxFactory.Token(GetRelationalPatternTokenKind(operatorKind));
                        caseSwitchLabelSyntax = SyntaxFactory.CasePatternSwitchLabel(
                            SyntaxFactory.RelationalPattern(operatorToken, csRelationalValue),
                            SyntaxFactory.Token(SyntaxKind.ColonToken));
                    }
                    else {
                        var varName = CommonConversions.CsEscapedIdentifier(GetUniqueVariableNameInScope(node, "case"));
                        ExpressionSyntax csLeft = ValidSyntaxFactory.IdentifierName(varName);
                        csRelationalValue = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(relational.Value, csRelationalValue);
                        var binaryExp = SyntaxFactory.BinaryExpression(operatorKind.ConvertToken(), csLeft, csRelationalValue);
                        caseSwitchLabelSyntax = VarWhen(varName, binaryExp);
                    }
                    labels.Add(caseSwitchLabelSyntax);
                } else if (c is VBSyntax.RangeCaseClauseSyntax range) {
                    var varName = CommonConversions.CsEscapedIdentifier(GetUniqueVariableNameInScope(node, "case"));
                    ExpressionSyntax csCaseVar = ValidSyntaxFactory.IdentifierName(varName);
                    var lowerBound = await range.LowerBound.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
                    ExpressionSyntax lowerBoundCheck;
                    if (isObjectComparison) {
                        var caseTypeInfo = _semanticModel.GetTypeInfo(range.LowerBound);
                        lowerBoundCheck = ComparisonAdjustedForStringComparison(node, range.LowerBound, caseTypeInfo, lowerBound, csCaseVar, switchExprTypeInfo, ComparisonKind.LessThanOrEqual);
                    } else {
                        lowerBound = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(range.LowerBound, lowerBound);
                        var (lowerBoundForComparison, csCaseVarForLower) = CastBothToUnderlyingTypeIfEnum(switchExprTypeInfo.ConvertedType, lowerBound, csCaseVar);
                        lowerBoundCheck = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression, lowerBoundForComparison, csCaseVarForLower);
                    }
                    var upperBound = await range.UpperBound.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
                    ExpressionSyntax upperBoundCheck;
                    if (isObjectComparison) {
                        var caseTypeInfo = _semanticModel.GetTypeInfo(range.UpperBound);
                        upperBoundCheck = ComparisonAdjustedForStringComparison(node, range.UpperBound, switchExprTypeInfo, csCaseVar, upperBound, caseTypeInfo, ComparisonKind.LessThanOrEqual);
                    } else {
                        upperBound = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(range.UpperBound, upperBound);
                        var (csCaseVarForUpper, upperBoundForComparison) = CastBothToUnderlyingTypeIfEnum(switchExprTypeInfo.ConvertedType, csCaseVar, upperBound);
                        upperBoundCheck = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression, csCaseVarForUpper, upperBoundForComparison);
                    }
                    var withinBounds = SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, lowerBoundCheck, upperBoundCheck);
                    labels.Add(VarWhen(varName, withinBounds));
                } else {
                    throw new NotSupportedException(c.Kind().ToString());
                }
            }

            var csBlockStatements = (await ConvertStatementsAsync(block.Statements)).ToList();
            if (!DefinitelyExits(csBlockStatements.LastOrDefault())) {
                csBlockStatements.Add(SyntaxFactory.BreakStatement());
            }
            var hasLocalDeclaration = csBlockStatements.Any(s => s.IsKind(SyntaxKind.LocalDeclarationStatement));
            var list = hasLocalDeclaration
                ? SingleStatement(SyntaxFactory.Block(csBlockStatements))
                : SyntaxFactory.List(csBlockStatements);
            sections.Add(SyntaxFactory.SwitchSection(SyntaxFactory.List(labels), list));
        }

        var switchStatementSyntax = ValidSyntaxFactory.SwitchStatement(csSwitchExpr, sections);
        return SingleStatement(switchStatementSyntax);
    }

    private static bool DefinitelyExits(StatementSyntax statement)
    {
        if (statement == null) {
            return false;
        }

        StatementSyntax GetLastStatement(StatementSyntax node) => node is BlockSyntax block ? block.Statements.LastOrDefault() : node;
        bool IsExitStatement(StatementSyntax node)
        {
            node = GetLastStatement(node);
            return node != null && node.IsKind(SyntaxKind.ReturnStatement, SyntaxKind.BreakStatement, SyntaxKind.ThrowStatement, SyntaxKind.ContinueStatement);
        }

        if (IsExitStatement(statement)) {
            return true;
        }

        if (statement is not IfStatementSyntax ifStatement) {
            return false;
        }

        while(ifStatement.Else != null) {
            if (!IsExitStatement(ifStatement.Statement)) {
                return false;
            }

            if (ifStatement.Else.Statement is IfStatementSyntax x) {
                ifStatement = x;
            } else {
                return IsExitStatement(ifStatement.Else.Statement);
            }
        }

        return false;
    }

    private static bool IsEnumOrNullableEnum(ITypeSymbol convertedType) =>
        convertedType?.IsEnumType() == true || convertedType?.GetNullableUnderlyingType()?.IsEnumType() == true;

    /// <summary>
    /// When the switch expression is an enum, C# does not support &lt;= comparisons directly on enum values.
    /// Cast both sides to the enum's underlying integer type so the comparison compiles.
    /// </summary>
    private (ExpressionSyntax Left, ExpressionSyntax Right) CastBothToUnderlyingTypeIfEnum(ITypeSymbol switchExprType, ExpressionSyntax left, ExpressionSyntax right)
    {
        var enumType = switchExprType?.IsEnumType() == true ? switchExprType as INamedTypeSymbol
            : switchExprType?.GetNullableUnderlyingType() as INamedTypeSymbol;
        if (enumType?.EnumUnderlyingType is not { } underlyingType) {
            return (left, right);
        }
        var typeSyntax = CommonConversions.GetTypeSyntax(underlyingType);
        return (ValidSyntaxFactory.CastExpression(typeSyntax, left), ValidSyntaxFactory.CastExpression(typeSyntax, right));
    }

    private static CasePatternSwitchLabelSyntax VarWhen(SyntaxToken varName, ExpressionSyntax binaryExp)
    {
        var patternMatch = ValidSyntaxFactory.VarPattern(varName);
        return SyntaxFactory.CasePatternSwitchLabel(patternMatch,
            SyntaxFactory.WhenClause(binaryExp), SyntaxFactory.Token(SyntaxKind.ColonToken));
    }

    private async Task<(ExpressionSyntax Reusable, SyntaxList<StatementSyntax> Statements, ExpressionSyntax SingleUse)> GetExpressionWithoutSideEffectsAsync(VBSyntax.ExpressionSyntax vbExpr, string variableNameBase)
    {
        var expr = await vbExpr.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
        expr = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(vbExpr, expr);
        SyntaxList<StatementSyntax> stmts = SyntaxFactory.List<StatementSyntax>();
        ExpressionSyntax exprWithoutSideEffects;
        ExpressionSyntax reusableExprWithoutSideEffects;
        if (IsReusableReadOnlyLocalKind(_semanticModel.GetSymbolInfo(vbExpr).Symbol) || await CanEvaluateMultipleTimesAsync(vbExpr)) {
            exprWithoutSideEffects = expr;
            reusableExprWithoutSideEffects = expr.WithoutSourceMapping();
        } else {
            TypeSyntax forceType = null;
            // VB's With uses a structure variable in place, but only copies a reference, so it keeps the original object even if the variable is reassigned
            if (_semanticModel.GetTypeInfo(vbExpr).Type?.IsReferenceType != true && _semanticModel.GetOperation(vbExpr.SkipIntoParens()).IsAssignableExpression()) {
                forceType = SyntaxFactory.RefType(ValidSyntaxFactory.VarType);
                expr = SyntaxFactory.RefExpression(expr);
            }

            var (stmt, id) = CreateLocalVariableWithUniqueName(vbExpr, variableNameBase, expr, forceType);
            if (forceType != null) id = RefLocalReferences.Mark(id, CommonConversions.GetTypeSyntax(_semanticModel.GetTypeInfo(vbExpr).Type));
            stmts = stmts.Add(stmt);
            reusableExprWithoutSideEffects = exprWithoutSideEffects = id;
        }

        return (reusableExprWithoutSideEffects, stmts, exprWithoutSideEffects);
    }

    private static bool IsReusableReadOnlyLocalKind(ISymbol symbol) => symbol is ILocalSymbol ls && (VBasic.VisualBasicExtensions.IsForEach(ls) || ls.IsUsing);

    private (StatementSyntax Declaration, IdentifierNameSyntax Reference) CreateLocalVariableWithUniqueName(VBSyntax.ExpressionSyntax vbExpr, string variableNameBase, ExpressionSyntax expr, TypeSyntax forceType = null)
    {
        var contextNode = vbExpr.GetAncestor<VBSyntax.MethodBlockBaseSyntax>() ?? (VBasic.VisualBasicSyntaxNode) vbExpr.Parent;
        return CreateLocalVariableWithUniqueName(contextNode, variableNameBase, expr, forceType);
    }

    private (StatementSyntax Declaration, IdentifierNameSyntax Reference) CreateLocalVariableWithUniqueName(VisualBasicSyntaxNode contextNode, string variableNameBase, ExpressionSyntax expr, TypeSyntax forceType = null)
    {
        var varName = GetUniqueVariableNameInScope(contextNode, variableNameBase);
        var stmt = CommonConversions.CreateLocalVariableDeclarationAndAssignment(varName, expr, forceType);
        return (stmt, ValidSyntaxFactory.IdentifierName(varName));
    }

    private async Task<bool> CanEvaluateMultipleTimesAsync(VBSyntax.ExpressionSyntax vbExpr)
    {
        return _semanticModel.GetConstantValue(vbExpr).HasValue || vbExpr.IsKind(VBasic.SyntaxKind.MeExpression) || vbExpr.SkipIntoParens() is VBSyntax.NameSyntax ns && await IsNeverMutatedAsync(ns);
    }

    private async Task<bool> IsNeverMutatedAsync(VBSyntax.NameSyntax ns)
    {
        var allowedLocation = Location.Create(ns.SyntaxTree, TextSpan.FromBounds(ns.GetAncestor<VBSyntax.MethodBlockBaseSyntax>().SpanStart, ns.Span.End));
        var symbol = _semanticModel.GetSymbolInfo(ns).Symbol;
        //Perf optimization: Looking across the whole solution is expensive, so assume non-local symbols are written somewhere
        return symbol.MatchesKind(SymbolKind.Parameter, SymbolKind.Local) && await CommonConversions.Document.Project.Solution.IsNeverWrittenAsync(symbol, allowedLocation);
    }

    private CasePatternSwitchLabelSyntax WrapInCasePatternSwitchLabelSyntax(VBSyntax.SelectBlockSyntax node, VBSyntax.ExpressionSyntax vbCase, ExpressionSyntax expression, bool treatAsBoolean = false, VBasic.SyntaxKind caseClauseKind = VBasic.SyntaxKind.CaseEqualsClause)
    {
        var typeInfo = _semanticModel.GetTypeInfo(node.SelectStatement.Expression);

        DeclarationPatternSyntax patternMatch;
        if (typeInfo.ConvertedType.SpecialType == SpecialType.System_Boolean || treatAsBoolean) {
            patternMatch = SyntaxFactory.DeclarationPattern(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                SyntaxFactory.DiscardDesignation());
        } else {
            var varName = CommonConversions.CsEscapedIdentifier(GetUniqueVariableNameInScope(node, "case"));
            patternMatch = ValidSyntaxFactory.VarPattern(varName);
            ExpressionSyntax csLeft = ValidSyntaxFactory.IdentifierName(varName), csRight = expression;
            var caseTypeInfo = _semanticModel.GetTypeInfo(vbCase);
            expression = ComparisonAdjustedForStringComparison(node, vbCase, typeInfo, csLeft, csRight, caseTypeInfo, GetComparisonKind(caseClauseKind));
        }

        var colonToken = SyntaxFactory.Token(SyntaxKind.ColonToken);
        return SyntaxFactory.CasePatternSwitchLabel(patternMatch, SyntaxFactory.WhenClause(expression), colonToken);
    }

    private ComparisonKind GetComparisonKind(VBasic.SyntaxKind caseClauseKind) => caseClauseKind switch {
        VBasic.SyntaxKind.CaseLessThanClause => ComparisonKind.LessThan,
        VBasic.SyntaxKind.CaseLessThanOrEqualClause => ComparisonKind.LessThanOrEqual,
        VBasic.SyntaxKind.CaseEqualsClause => ComparisonKind.Equals,
        VBasic.SyntaxKind.CaseNotEqualsClause => ComparisonKind.NotEquals,
        VBasic.SyntaxKind.CaseGreaterThanOrEqualClause => ComparisonKind.GreaterThanOrEqual,
        VBasic.SyntaxKind.CaseGreaterThanClause => ComparisonKind.GreaterThan,
        _ => throw new ArgumentOutOfRangeException(nameof(caseClauseKind), caseClauseKind, null)
    };

    private static CS.SyntaxKind GetRelationalPatternTokenKind(VBasic.SyntaxKind caseClauseKind) => caseClauseKind switch {
        VBasic.SyntaxKind.CaseLessThanClause => CS.SyntaxKind.LessThanToken,
        VBasic.SyntaxKind.CaseLessThanOrEqualClause => CS.SyntaxKind.LessThanEqualsToken,
        VBasic.SyntaxKind.CaseGreaterThanOrEqualClause => CS.SyntaxKind.GreaterThanEqualsToken,
        VBasic.SyntaxKind.CaseGreaterThanClause => CS.SyntaxKind.GreaterThanToken,
        _ => throw new ArgumentOutOfRangeException(nameof(caseClauseKind), caseClauseKind, null)
    };

    private ExpressionSyntax ComparisonAdjustedForStringComparison(VBSyntax.SelectBlockSyntax node, VBSyntax.ExpressionSyntax vbCase, TypeInfo lhsTypeInfo, ExpressionSyntax csLeft, ExpressionSyntax csRight, TypeInfo rhsTypeInfo, ComparisonKind comparisonKind)
    {
        var vbEquality = CommonConversions.VisualBasicEqualityComparison;
        switch (_visualBasicEqualityComparison.GetObjectEqualityType(lhsTypeInfo, rhsTypeInfo)) {
            case VisualBasicEqualityComparison.RequiredType.Object:
                return vbEquality.GetFullExpressionForVbObjectComparison(csLeft, csRight, comparisonKind);
            case VisualBasicEqualityComparison.RequiredType.StringOnly:
                // We know lhs isn't null, because we always coalesce it in the switch expression
                (csLeft, csRight) = vbEquality
                    .AdjustForVbStringComparison(node.SelectStatement.Expression, csLeft, lhsTypeInfo, true, vbCase, csRight, rhsTypeInfo, false);
                break;
        }
        return SyntaxFactory.BinaryExpression(GetSyntaxKind(comparisonKind), csLeft, csRight);
    }

    private CS.SyntaxKind GetSyntaxKind(ComparisonKind comparisonKind) => comparisonKind switch {
        ComparisonKind.LessThan => CS.SyntaxKind.LessThanExpression,
        ComparisonKind.LessThanOrEqual => CS.SyntaxKind.LessThanOrEqualExpression,
        ComparisonKind.Equals => CS.SyntaxKind.EqualsExpression,
        ComparisonKind.NotEquals => CS.SyntaxKind.NotEqualsExpression,
        ComparisonKind.GreaterThanOrEqual => CS.SyntaxKind.GreaterThanOrEqualExpression,
        ComparisonKind.GreaterThan => CS.SyntaxKind.GreaterThanExpression,
        _ => throw new ArgumentOutOfRangeException(nameof(comparisonKind), comparisonKind, null)
    };

    public override async Task<SyntaxList<StatementSyntax>> VisitWithBlock(VBSyntax.WithBlockSyntax node)
    {
        var (lhsExpression, prefixDeclarations, _) = await GetExpressionWithoutSideEffectsAsync(node.WithStatement.Expression, "withBlock");

        _withBlockLhs.Push(lhsExpression);
        try {
            var statements = await ConvertStatementsAsync(node.Statements);

            var statementSyntaxs = SyntaxFactory.List(prefixDeclarations.Concat(statements));
            return prefixDeclarations.Any()
                ? SingleStatement(SyntaxFactory.Block(statementSyntaxs))
                : statementSyntaxs;
        } finally {
            _withBlockLhs.Pop();
        }
    }

    public string GetUniqueVariableNameInScope(VBasic.VisualBasicSyntaxNode node, string variableNameBase)
    {
        return NameGenerator.CS.GetUniqueVariableNameInScope(_semanticModel, _generatedNames, node, variableNameBase);
    }

    public string GetUniqueLabelNameInScope(VBasic.VisualBasicSyntaxNode node, string labelNameBase)
    {
        return NameGenerator.CS.GetUniqueLabelNameInScope(_semanticModel, _generatedNames, node, labelNameBase);
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitTryBlock(VBSyntax.TryBlockSyntax node)
    {
        var block = SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements));
        return SingleStatement(
            SyntaxFactory.TryStatement(
                block,
                SyntaxFactory.List(await node.CatchBlocks.SelectAsync(async c => await ConvertCatchBlockAsync(c))),
                await ConvertFinallyBlockAsync(node.FinallyBlock)
            )
        );

        async Task<CatchClauseSyntax> ConvertCatchBlockAsync(VBasic.Syntax.CatchBlockSyntax node)
        {
            var stmt = node.CatchStatement;
            CatchDeclarationSyntax catcher = null;
            if (stmt.AsClause != null) {
                catcher = SyntaxFactory.CatchDeclaration(
                    ConvertTypeSyntax(stmt.AsClause.Type),
                    CommonConversions.ConvertIdentifier(stmt.IdentifierName.Identifier)
                );
            }

            var filter = await ConvertCatchFilterClauseAsync(stmt.WhenClause);
            var stmts = await node.Statements.SelectManyAsync(async s => (IEnumerable<StatementSyntax>)await s.Accept(CommentConvertingVisitor));
            return SyntaxFactory.CatchClause(
                catcher,
                filter,
                SyntaxFactory.Block(stmts)
            );
        }

        async Task<CatchFilterClauseSyntax> ConvertCatchFilterClauseAsync(VBasic.Syntax.CatchFilterClauseSyntax node)
        {
            if (node == null) return null;
            return SyntaxFactory.CatchFilterClause(await ConvertConditionAsync(node.Filter));
        }

        async Task<FinallyClauseSyntax> ConvertFinallyBlockAsync(VBasic.Syntax.FinallyBlockSyntax node)
        {
            if (node == null) return null;
            var stmts = await node.Statements.SelectManyAsync(async s => (IEnumerable<StatementSyntax>)await s.Accept(CommentConvertingVisitor));
            return SyntaxFactory.FinallyClause(SyntaxFactory.Block(stmts));
        }
    }

    private TypeSyntax ConvertTypeSyntax(VBSyntax.TypeSyntax vbType)
    {
        if (_semanticModel.GetSymbolInfo(vbType).Symbol is ITypeSymbol typeSymbol)
            return CommonConversions.GetTypeSyntax(typeSymbol);
        return SyntaxFactory.ParseTypeName(vbType.ToString());
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitSyncLockBlock(VBSyntax.SyncLockBlockSyntax node)
    {
        return SingleStatement(SyntaxFactory.LockStatement(
            await node.SyncLockStatement.Expression.AcceptAsync<ExpressionSyntax>(_expressionVisitor),
            SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements)).UnpackNonNestedBlock()
        ));
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitUsingBlock(VBSyntax.UsingBlockSyntax node)
    {
        var statementSyntax = SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements));
        if (node.UsingStatement.Expression == null) {
            StatementSyntax stmt = statementSyntax;
            foreach (var v in node.UsingStatement.Variables.Reverse())
            foreach (var declaration in (await SplitVariableDeclarationsAsync(v)).Variables.Reverse())
                stmt = SyntaxFactory.UsingStatement(declaration.Decl, null, stmt);
            return SingleStatement(stmt);
        }

        var expr = await node.UsingStatement.Expression.AcceptAsync<ExpressionSyntax>(_expressionVisitor);
        var unpackPossiblyNestedBlock = statementSyntax.UnpackPossiblyNestedBlock(); // Allow reduced indentation for multiple usings in a row
        return SingleStatement(SyntaxFactory.UsingStatement(null, expr, unpackPossiblyNestedBlock));
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitWhileBlock(VBSyntax.WhileBlockSyntax node)
    {
        return SingleStatement(SyntaxFactory.WhileStatement(
            await ConvertConditionAsync(node.WhileStatement.Condition),
            SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements)).UnpackNonNestedBlock()
        ));
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitDoLoopBlock(VBSyntax.DoLoopBlockSyntax node)
    {
        var statements = SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements)).UnpackNonNestedBlock();

        if (node.DoStatement.WhileOrUntilClause is { } topClause) {
            return SingleStatement(SyntaxFactory.WhileStatement(await ConvertLoopContinueConditionAsync(topClause), statements));
        }

        var bottomCondition = node.LoopStatement.WhileOrUntilClause is { } bottomClause
            ? await ConvertLoopContinueConditionAsync(bottomClause)
            : SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
        return SingleStatement(SyntaxFactory.DoStatement(statements, bottomCondition));
    }

    /// <summary>
    /// C# only has While loops, so an Until loop continues while its condition is false.
    /// VB converts the Until condition to Boolean (Nothing to False) before negating it, so an Until loop continues while it's Nothing.
    /// </summary>
    private async Task<ExpressionSyntax> ConvertLoopContinueConditionAsync(VBSyntax.WhileOrUntilClauseSyntax clause)
    {
        var condition = await ConvertConditionAsync(clause.Condition);
        return clause.WhileOrUntilKeyword.IsKind(VBasic.SyntaxKind.UntilKeyword) ? condition.InvertCondition() : condition;
    }

    public override async Task<SyntaxList<StatementSyntax>> VisitCallStatement(VBSyntax.CallStatementSyntax node)
    {
        return SingleStatement(await node.Invocation.AcceptAsync<CSharpSyntaxNode>(_expressionVisitor));
    }

    private static SyntaxList<StatementSyntax> SingleStatement(StatementSyntax statement)
    {
        return SyntaxFactory.SingletonList(statement);
    }

    private static SyntaxList<StatementSyntax> SingleStatement(CSharpSyntaxNode node)
    {
        return SingleStatement(node is StatementSyntax statement
            ? statement
            : SyntaxFactory.ExpressionStatement((ExpressionSyntax)node));
    }
}