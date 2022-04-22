using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

internal record EventDescriptor(IdentifierNameSyntax VBEventName, IEventSymbol SymbolOrNull);