global using Microsoft.CodeAnalysis;
global using ICSharpCode.CodeConverter.Util;
global using ICSharpCode.CodeConverter.Common;

// Since so many names clash between CS and VB, it's best to use these aliases to be clear which one is meant
global using VBasic = Microsoft.CodeAnalysis.VisualBasic;
global using CS = Microsoft.CodeAnalysis.CSharp;
global using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
global using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;

// For common unambiguous names, no need to qualify
global using VisualBasicSyntaxNode = Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode;
global using CSharpSyntaxNode = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxNode;
