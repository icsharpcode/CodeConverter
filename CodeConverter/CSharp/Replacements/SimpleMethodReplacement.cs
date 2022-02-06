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