using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class HoistedParameterlessFunction : IHoistedNode
    {
        private readonly TypeSyntax _returnType;
        private readonly BlockSyntax _block;

        public string Id { get; }
        public string Prefix { get; }

        public HoistedParameterlessFunction(string localFuncName, TypeSyntax returnType, BlockSyntax block)
        {
            Id = $"hs{Guid.NewGuid().ToString("N")}";
            Prefix = localFuncName;
            _returnType = returnType;
            _block = block;
        }

        public IdentifierNameSyntax TempIdentifier => SyntaxFactory.IdentifierName(Id).WithAdditionalAnnotations(HoistedNodeState.Annotation);
        public LocalFunctionStatementSyntax AsLocalFunction(string functionName) => SyntaxFactory.LocalFunctionStatement(_returnType, SyntaxFactory.Identifier(functionName)).WithBody(_block);
        public MethodDeclarationSyntax AsInstanceMethod(string functionName) => ValidSyntaxFactory.CreateParameterlessMethod(functionName, _returnType, _block);
    }
}