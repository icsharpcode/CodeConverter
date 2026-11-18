using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

internal class HoistedFunction : IHoistedNode
{
    private readonly TypeSyntax _returnType;
    private readonly BlockSyntax _block;
    private readonly ParameterListSyntax _parameters;
    private readonly bool _isStatic;

    public string Id { get; }
    public string Prefix { get; }

    /// <param name="isStatic">Only used when hoisted to a member: it must be an instance method to be called from an instance context such as a constructor</param>
    public HoistedFunction(string localFuncName, TypeSyntax returnType, BlockSyntax block, ParameterListSyntax parameters, bool isStatic = true)
    {
        Id = $"hs{Guid.NewGuid().ToString("N")}";
        Prefix = localFuncName;
        _returnType = returnType;
        _block = block;
        _parameters = parameters;
        _isStatic = isStatic;
    }

    public IdentifierNameSyntax TempIdentifier => ValidSyntaxFactory.IdentifierName(Id).WithAdditionalAnnotations(PerScopeState.AdditionalLocalAnnotation);
    public LocalFunctionStatementSyntax AsLocalFunction(string functionName) => SyntaxFactory.LocalFunctionStatement(_returnType, SyntaxFactory.Identifier(functionName)).WithParameterList(_parameters).WithBody(_block);
    public MethodDeclarationSyntax AsInstanceMethod(string functionName) => ValidSyntaxFactory.CreateMethod(functionName, _returnType, _parameters, _block, _isStatic);
}