using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class HoistedStatement : IHoistedNode
    {
        public StatementSyntax Statement { get; }

        public HoistedStatement(StatementSyntax statementSyntax)
        {
            Statement = statementSyntax;
        }
    }
}