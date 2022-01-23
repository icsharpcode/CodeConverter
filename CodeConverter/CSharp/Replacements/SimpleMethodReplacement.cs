using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.Replacements;

internal record SimpleMethodReplacement
{
    private static readonly IReadOnlyDictionary<string, SimpleMethodReplacement> MethodReplacements = new SimpleMethodReplacement[] {
        new("My.Computer.FileSystem.CurrentDirectory", "System.IO.Directory.GetCurrentDirectory"),
        new("My.Computer.FileSystem.CombinePath", "System.IO.Path.Combine"),
        new("My.Computer.FileSystem.GetDirectoryInfo", "System.IO.DirectoryInfo", true),
        new("My.Computer.FileSystem.GetDriveInfo", "System.IO.DriveInfo", true),
        new("My.Computer.FileSystem.GetFileInfo", "System.IO.FileInfo", true),
        new("My.Computer.FileSystem.GetName", "System.IO.Path.GetFileName"),
        new("My.Computer.FileSystem.GetTempFileName", "System.IO.Path.GetTempFileName"),
        new("My.Computer.FileSystem.ReadAllBytes", "System.IO.File.ReadAllBytes"),
        new("My.Computer.FileSystem.ReadAllText", "System.IO.File.ReadAllText"),
        new("My.Computer.FileSystem.DirectoryExists", "System.IO.Directory.Exists"),
        new("My.Computer.FileSystem.FileExists", "System.IO.File.Exists"),
        new("My.Computer.FileSystem.DeleteFile", "System.IO.File.Delete"),
        new("My.Computer.FileSystem.SpecialDirectories.Temp", "System.IO.Path.GetTempPath"),
        new("My.Computer.Info.InstalledUICulture", "Globalization.CultureInfo.InstalledUICulture"),
        new("My.Computer.Info.OSFullName", "System.Runtime.InteropServices.RuntimeInformation.OSDescription"),
        new("My.Computer.Info.SPlatform", "System.Environment.OSVersion.Platform.ToString"),
        new("My.Computer.Info.OSVersion", "System.Environment.OSVersion.Version.ToString"),
        new("Microsoft.VisualBasic.DateAndTime.Now", "System.DateTime.Now", replaceWithProperty: true),
        new("Microsoft.VisualBasic.DateAndTime.Today", "System.DateTime.Today", replaceWithProperty: true),
        new("Microsoft.VisualBasic.DateAndTime.Year", "System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetYear"),
        new("Microsoft.VisualBasic.DateAndTime.Month", "System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetMonth"),
        new("Microsoft.VisualBasic.DateAndTime.Day", "System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetDayOfMonth"),
        new("Microsoft.VisualBasic.DateAndTime.Hour", "System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetHour"),
        new("Microsoft.VisualBasic.DateAndTime.Minute", "System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetMinute"),
        new("Microsoft.VisualBasic.DateAndTime.Second", "System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetSecond"),
    }.ToDictionary(x => x._toReplace.Last(), StringComparer.OrdinalIgnoreCase);

    private readonly string[] _toReplace;
    private readonly ExpressionSyntax _replaceWith;

    private SimpleMethodReplacement(string toReplace, string replaceWith, bool replaceWithObjectCreation = false, bool replaceWithProperty = false)
    {
        _toReplace = toReplace.Split('.');
        _replaceWith = replaceWithObjectCreation ? SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(replaceWith))
            : replaceWithProperty ? ValidSyntaxFactory.MemberAccess(replaceWith.Split('.'))
            : SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(replaceWith.Split('.')));
    }

    public static bool TryGet(ISymbol symbol, out SimpleMethodReplacement r) => MethodReplacements.TryGetValue(symbol.Name, out r);

    public ExpressionSyntax ReplaceIfMatches(ISymbol symbol, ExpressionSyntax cSharpSyntaxNode, IEnumerable<ArgumentSyntax> args)
    {
        if (QualifiedMethodNameMatches(symbol, _toReplace))
        {
            var argumentListSyntax = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(args));
            return _replaceWith switch {
                ObjectCreationExpressionSyntax oces => oces.WithArgumentList(argumentListSyntax),
                InvocationExpressionSyntax ies => ies.WithArgumentList(argumentListSyntax),
                var x => x
            };
        }

        return cSharpSyntaxNode;
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