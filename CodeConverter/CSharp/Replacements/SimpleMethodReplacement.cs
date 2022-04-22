using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.Replacements;

internal class SimpleMethodReplacement
{
    private const string AnnotationKind = nameof(SimpleMethodReplacement) + "placeholder";
    private static readonly IdentifierNameSyntax FirstArgPlaceholder = SyntaxFactory.IdentifierName("placeholder0").WithAdditionalAnnotations(new SyntaxAnnotation(AnnotationKind, "0"));

    private static readonly IReadOnlyDictionary<string, SimpleMethodReplacement> MethodReplacements = new SimpleMethodReplacement[] {
        new("Microsoft.VisualBasic.MyServices.FileSystemProxy.CurrentDirectory", "System.IO.Directory.GetCurrentDirectory"),
        new("Microsoft.VisualBasic.MyServices.FileSystemProxy.CombinePath", "System.IO.Path.Combine"),
        new("Microsoft.VisualBasic.MyServices.FileSystemProxy.GetDirectoryInfo", "System.IO.DirectoryInfo", true),
        new("Microsoft.VisualBasic.MyServices.FileSystemProxy.GetDriveInfo", "System.IO.DriveInfo", true),
        new("Microsoft.VisualBasic.MyServices.FileSystemProxy.GetFileInfo", "System.IO.FileInfo", true),
        new("Microsoft.VisualBasic.MyServices.FileSystemProxy.GetName", "System.IO.Path.GetFileName"),
        new("Microsoft.VisualBasic.MyServices.FileSystemProxy.GetTempFileName", "System.IO.Path.GetTempFileName"),
        new("Microsoft.VisualBasic.MyServices.FileSystemProxy.ReadAllBytes", "System.IO.File.ReadAllBytes"),
        new("Microsoft.VisualBasic.MyServices.FileSystemProxy.ReadAllText", "System.IO.File.ReadAllText"),
        new("Microsoft.VisualBasic.MyServices.FileSystemProxy.DirectoryExists", "System.IO.Directory.Exists"),
        new("Microsoft.VisualBasic.MyServices.FileSystemProxy.FileExists", "System.IO.File.Exists"),
        new("Microsoft.VisualBasic.MyServices.FileSystemProxy.DeleteFile", "System.IO.File.Delete"),
        new("Microsoft.VisualBasic.MyServices.SpecialDirectoriesProxy.Temp", "System.IO.Path.GetTempPath"),
        new("Microsoft.VisualBasic.MyServices.SpecialDirectoriesProxy.MyDocuments", SpecialFolderSyntax("Personal")),
        new("Microsoft.VisualBasic.MyServices.SpecialDirectoriesProxy.MyMusic", SpecialFolderSyntax("MyMusic")),
        new("Microsoft.VisualBasic.MyServices.SpecialDirectoriesProxy.MyPictures", SpecialFolderSyntax("MyPictures")),
        new("Microsoft.VisualBasic.MyServices.SpecialDirectoriesProxy.Desktop", SpecialFolderSyntax("Desktop")),
        new("Microsoft.VisualBasic.MyServices.SpecialDirectoriesProxy.Programs", SpecialFolderSyntax("Programs")),
        new("Microsoft.VisualBasic.MyServices.SpecialDirectoriesProxy.ProgramFiles", SpecialFolderSyntax("ProgramFiles")),
        new("Microsoft.VisualBasic.Devices.ComputerInfo.InstalledUICulture", "System.Globalization.CultureInfo.InstalledUICulture", replaceWithProperty: true),
        new("Microsoft.VisualBasic.Devices.ComputerInfo.OSFullName", "System.Runtime.InteropServices.RuntimeInformation.OSDescription", replaceWithProperty: true),
        new("Microsoft.VisualBasic.Devices.ComputerInfo.SPlatform", "System.Environment.OSVersion.Platform.ToString"),
        new("Microsoft.VisualBasic.Devices.ComputerInfo.OSVersion", "System.Environment.OSVersion.Version.ToString"),
        new("Microsoft.VisualBasic.DateAndTime.Now", "System.DateTime.Now", replaceWithProperty: true),
        new("Microsoft.VisualBasic.DateAndTime.Today", "System.DateTime.Today", replaceWithProperty: true),
        new("Microsoft.VisualBasic.DateAndTime.Year", "System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetYear"),
        new("Microsoft.VisualBasic.DateAndTime.Month", "System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetMonth"),
        new("Microsoft.VisualBasic.DateAndTime.Day", "System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetDayOfMonth"),
        new("Microsoft.VisualBasic.DateAndTime.Hour", "System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetHour"),
        new("Microsoft.VisualBasic.DateAndTime.Minute", "System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetMinute"),
        new("Microsoft.VisualBasic.DateAndTime.Second", "System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetSecond"),
        new ("Microsoft.VisualBasic.Information.IsArray", IsSystemType(nameof(Array))),
        new ("Microsoft.VisualBasic.Information.IsDBNull", IsSystemType(nameof(DBNull))),
        new("Microsoft.VisualBasic.Information.IsError", IsSystemType(nameof(Exception))),
        new("Microsoft.VisualBasic.Information.IsNothing", Equals(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))),
        // Can use IsNotExpression in CodeAnalysis 3+
        new("Microsoft.VisualBasic.Information.IsReference", SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, IsSystemType(nameof(ValueType)))),
    }.ToDictionary(x => x._toReplace.Last(), StringComparer.OrdinalIgnoreCase);

    private static ExpressionSyntax SpecialFolderSyntax(string specialFolderEnumName)
    {
        return SyntaxFactory.InvocationExpression(
            ValidSyntaxFactory.MemberAccess(nameof(System), nameof(Environment), nameof(Environment.GetFolderPath)),
            ValidSyntaxFactory.MemberAccess(nameof(System), nameof(Environment), nameof(Environment.SpecialFolder), specialFolderEnumName).Yield().CreateCsArgList()
        );
    }

    private static ExpressionSyntax IsSystemType(string isTypeName)
    {
        var binaryExpressionSyntax = Is(SystemType(isTypeName));
        return SyntaxFactory.ParenthesizedExpression(binaryExpressionSyntax);
    }

    private static ExpressionSyntax Equals(ExpressionSyntax rhsExpression) =>
        SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, FirstArgPlaceholder, rhsExpression));

    private static BinaryExpressionSyntax Is(ExpressionSyntax typeSyntax) =>
        SyntaxFactory.BinaryExpression(SyntaxKind.IsExpression, FirstArgPlaceholder, typeSyntax);

    private static QualifiedNameSyntax SystemType(string isTypeName) =>
        SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName(nameof(System)), SyntaxFactory.IdentifierName(isTypeName));

    private readonly string[] _toReplace;
    private readonly ExpressionSyntax _replaceWith;

    private SimpleMethodReplacement(string toReplace, string replaceWith, bool replaceWithObjectCreation = false, bool replaceWithProperty = false)
    {
        _toReplace = toReplace.Split('.');
        _replaceWith = replaceWithObjectCreation ? SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(replaceWith))
            : replaceWithProperty ? ValidSyntaxFactory.MemberAccess(replaceWith.Split('.'))
            : SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(replaceWith.Split('.')));
    }

    private SimpleMethodReplacement(string toReplace, ExpressionSyntax replaceWith)
    {
        _toReplace = toReplace.Split('.');
        _replaceWith = replaceWith;
    }

    public static bool TryGet(ISymbol symbol, out SimpleMethodReplacement r)
    {
        r = null;
        return symbol != null && MethodReplacements.TryGetValue(symbol.Name, out r);
    }

    public ExpressionSyntax ReplaceIfMatches(ISymbol symbol, IEnumerable<ArgumentSyntax> args, bool isAddressOf)
    {
        if (QualifiedMethodNameMatches(symbol, _toReplace))
        {
            if (isAddressOf) {
                if (_replaceWith is InvocationExpressionSyntax ies) return ies.Expression;
                return null;
            }

            var argumentListSyntax = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(args));
            if (argumentListSyntax.Arguments.Any()) {
                return _replaceWith switch {
                    ObjectCreationExpressionSyntax oces => oces.WithArgumentList(argumentListSyntax),
                    InvocationExpressionSyntax ies => ies.WithArgumentList(argumentListSyntax),
                    var zeroOrSingleArgExpression => ReplacePlaceholderArgs(zeroOrSingleArgExpression, argumentListSyntax)
                };
            }

            return _replaceWith;
        }

        return null;
    }

    private static ExpressionSyntax ReplacePlaceholderArgs(ExpressionSyntax zeroOrSingleArgExpression, ArgumentListSyntax argumentListSyntax)
    {
        for (var index = 0; index < argumentListSyntax.Arguments.Count; index++) {
            var expression = argumentListSyntax.Arguments[index].Expression;
            var nodeToReplace = zeroOrSingleArgExpression.GetAnnotatedNodes(AnnotationKind).Single(x => x.GetAnnotations(AnnotationKind).Single().Data == index.ToString(CultureInfo.InvariantCulture));
            zeroOrSingleArgExpression = zeroOrSingleArgExpression.ReplaceNode(nodeToReplace, expression);
        }

        return zeroOrSingleArgExpression;
    }

    private static bool QualifiedMethodNameMatches(ISymbol symbol, params string[] parts)
    {
        if (symbol == null) return false;
        foreach (var part in parts.Reverse()) {
            if (!part.Equals(symbol.Name, StringComparison.OrdinalIgnoreCase)) return false;
            symbol = symbol.ContainingSymbol;
        }

        return !symbol.ContainingSymbol.CanBeReferencedByName;
    }
}