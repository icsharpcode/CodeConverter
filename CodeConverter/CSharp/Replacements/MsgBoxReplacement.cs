using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.Replacements
{
    internal class MsgBoxReplacement
    {
        private readonly IMethodSymbol _symbol;
        private readonly IReadOnlyCollection<ArgumentSyntax> _arguments;
        private readonly SemanticModel _semanticModel;
        private readonly HashSet<string> _extraUsingDirectives;
        private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;

        public MsgBoxReplacement(IMethodSymbol symbol, IReadOnlyCollection<ArgumentSyntax> arguments, SemanticModel semanticModel, HashSet<string> extraUsingDirectives, VisualBasicEqualityComparison visualBasicEqualityComparison)
        {
            _symbol = symbol;
            _arguments = arguments;
            _semanticModel = semanticModel;
            _extraUsingDirectives = extraUsingDirectives;
            _visualBasicEqualityComparison = visualBasicEqualityComparison;
        }

        public ExpressionSyntax Replace()
        {
            var (prompt, style, title) = GetMsgBoxArguments();
            if (prompt == null) return null; // Prompt is required

            var (messageBoxButtons, messageBoxIcon) = ConvertStyle(style);
            if (messageBoxButtons == null && messageBoxIcon == null && style != null)
            {
                // Could not convert style, fallback to default
                return null;
            }

            _extraUsingDirectives.Add("System.Windows.Forms");

            var showExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("MessageBox"),
                SyntaxFactory.IdentifierName("Show")
            );

            var newArguments = new List<ArgumentSyntax>
            {
                prompt
            };

            if (title != null)
            {
                newArguments.Add(title);
            } else {
                newArguments.Add(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(""))));
            }

            newArguments.Add(SyntaxFactory.Argument(messageBoxButtons ?? MemberAccess("OK")));

            if (messageBoxIcon != null)
            {
                newArguments.Add(SyntaxFactory.Argument(messageBoxIcon));
            }

            return SyntaxFactory.InvocationExpression(showExpression, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(newArguments)));
        }

        private (ArgumentSyntax Prompt, ArgumentSyntax Buttons, ArgumentSyntax Title) GetMsgBoxArguments()
        {
            var args = _arguments.ToList();
            var prompt = args.Count > 0 ? args[0] : null;
            var buttons = args.Count > 1 ? args[1] : null;
            var title = args.Count > 2 ? args[2] : null;
            return (prompt, buttons, title);
        }

        private (ExpressionSyntax Buttons, ExpressionSyntax Icon) ConvertStyle(ArgumentSyntax styleArgument)
        {
            if (styleArgument == null)
            {
                return (MemberAccess("OK"), null);
            }

            var styleExpression = styleArgument.Expression;
            var constantValue = _semanticModel.GetConstantValue(styleExpression);

            if (!constantValue.HasValue)
            {
                // If we can't determine the style at compile time, we can't convert it.
                // Fallback to Interaction.MsgBox will be handled by the caller.
                return (null, null);
            }

            var styleValue = (int)constantValue.Value;

            var buttons = GetMessageBoxButtons(styleValue);
            var icon = GetMessageBoxIcon(styleValue);

            return (buttons, icon);
        }

        private ExpressionSyntax GetMessageBoxButtons(int style)
        {
            if ((style & 0x1) == 0x1) return MemberAccess("OKCancel");
            if ((style & 0x2) == 0x2) return MemberAccess("AbortRetryIgnore");
            if ((style & 0x3) == 0x3) return MemberAccess("YesNoCancel");
            if ((style & 0x4) == 0x4) return MemberAccess("YesNo");
            if ((style & 0x5) == 0x5) return MemberAccess("RetryCancel");
            return MemberAccess("OK");
        }

        private ExpressionSyntax GetMessageBoxIcon(int style)
        {
            if ((style & 0x10) == 0x10) return MemberAccess("Critical", "MessageBoxIcon");
            if ((style & 0x20) == 0x20) return MemberAccess("Question", "MessageBoxIcon");
            if ((style & 0x30) == 0x30) return MemberAccess("Exclamation", "MessageBoxIcon");
            if ((style & 0x40) == 0x40) return MemberAccess("Information", "MessageBoxIcon");
            return null;
        }

        private ExpressionSyntax MemberAccess(string memberName, string typeName = "MessageBoxButtons")
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(typeName),
                SyntaxFactory.IdentifierName(memberName)
            );
        }

        public static bool IsBestMsgBoxMatch(IMethodSymbol symbol)
        {
            return symbol?.ContainingType?.Name == "Interaction" &&
                   symbol.Name == "MsgBox" &&
                   symbol.ContainingNamespace?.ToDisplayString() == "Microsoft.VisualBasic";
        }
    }
}
