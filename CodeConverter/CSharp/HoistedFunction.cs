using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

internal class HoistedFunction : IHoistedNode
{
    private readonly TypeSyntax _returnType;
    private readonly BlockSyntax _block;
    private readonly ParameterListSyntax _parameters;

    public string Id { get; }
    public string Prefix { get; }

    public HoistedFunction(string localFuncName, TypeSyntax returnType, BlockSyntax block, ParameterListSyntax parameters)
    {
        Id = $"hs{Guid.NewGuid().ToString("N")}";
        Prefix = localFuncName;
        _returnType = returnType;
        _block = block;
        _parameters = parameters;
    }

    public IdentifierNameSyntax TempIdentifier => ValidSyntaxFactory.IdentifierName(Id).WithAdditionalAnnotations(PerScopeState.AdditionalLocalAnnotation);
    public LocalFunctionStatementSyntax AsLocalFunction(string functionName) => SyntaxFactory.LocalFunctionStatement(_returnType, SyntaxFactory.Identifier(functionName)).WithParameterList(_parameters).WithBody(_block);
    public MethodDeclarationSyntax AsInstanceMethod(string functionName) => ValidSyntaxFactory.CreateMethod(functionName, _returnType, _parameters, _block);
}