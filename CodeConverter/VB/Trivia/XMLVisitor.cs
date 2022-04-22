using VBFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;

namespace ICSharpCode.CodeConverter.VB.Trivia;

internal class XMLVisitor : CS.CSharpSyntaxVisitor<VBasic.VisualBasicSyntaxNode>
{
    private static SyntaxToken GetVBOperatorToken(string op)
    {
        switch (op)
        {
            case "==":
            {
                return VisualBasicSyntaxFactory.EqualsToken;
            }

            case "!=":
            {
                return VisualBasicSyntaxFactory.LessThanGreaterThanToken;
            }

            case ">":
            {
                return VisualBasicSyntaxFactory.GreaterThanToken;
            }

            case ">=":
            {
                return VisualBasicSyntaxFactory.GreaterThanEqualsToken;
            }

            case "<":
            {
                return VisualBasicSyntaxFactory.LessThanToken;
            }

            case "<=":
            {
                return VisualBasicSyntaxFactory.LessThanEqualsToken;
            }

            case "|":
            {
                return VisualBasicSyntaxFactory.OrKeyword;
            }

            case "||":
            {
                return VisualBasicSyntaxFactory.OrElseKeyword;
            }

            case "&":
            {
                return VisualBasicSyntaxFactory.AndKeyword;
            }

            case "&&":
            {
                return VisualBasicSyntaxFactory.AndAlsoKeyword;
            }

            case "+":
            {
                return VisualBasicSyntaxFactory.PlusToken;
            }

            case "-":
            {
                return VisualBasicSyntaxFactory.MinusToken;
            }

            case "*":
            {
                return VisualBasicSyntaxFactory.AsteriskToken;
            }

            case "/":
            {
                return VisualBasicSyntaxFactory.SlashToken;
            }

            case "%":
            {
                return VisualBasicSyntaxFactory.ModKeyword;
            }

            case "=":
            {
                return VisualBasicSyntaxFactory.EqualsToken;
            }

            case "+=":
            {
                return VisualBasicSyntaxFactory.PlusEqualsToken;
            }

            case "-=":
            {
                return VisualBasicSyntaxFactory.MinusEqualsToken;
            }

            case "!":
            {
                return VisualBasicSyntaxFactory.NotKeyword;
            }

            case "~":
            {
                return VisualBasicSyntaxFactory.NotKeyword;
            }

            default:
            {
                break;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(op));
    }

    private SyntaxList<VBSyntax.XmlNodeSyntax> GatherAttributes(SyntaxList<CSSyntax.XmlAttributeSyntax> ListOfAttributes)
    {
        var VBAttributes = new SyntaxList<VBSyntax.XmlNodeSyntax>();
        foreach (CSSyntax.XmlAttributeSyntax a in ListOfAttributes)
            VBAttributes = VBAttributes.Add((VBSyntax.XmlNodeSyntax)a.Accept(this));
        return VBAttributes;
    }

    public override VBasic.VisualBasicSyntaxNode DefaultVisit(SyntaxNode node)
    {
        return base.DefaultVisit(node);
    }

    public override VBasic.VisualBasicSyntaxNode VisitConversionOperatorMemberCref(CSSyntax.ConversionOperatorMemberCrefSyntax node)
    {
        return base.VisitConversionOperatorMemberCref(node);
    }

    public override VBasic.VisualBasicSyntaxNode VisitCrefBracketedParameterList(CSSyntax.CrefBracketedParameterListSyntax node)
    {
        return base.VisitCrefBracketedParameterList(node);
    }

    public override VBasic.VisualBasicSyntaxNode VisitCrefParameter(CSSyntax.CrefParameterSyntax node)
    {
        return node.Type.Accept(this);
    }

    public override VBasic.VisualBasicSyntaxNode VisitCrefParameterList(CSSyntax.CrefParameterListSyntax node)
    {
        return base.VisitCrefParameterList(node);
    }

    public override VBasic.VisualBasicSyntaxNode VisitGenericName(CSSyntax.GenericNameSyntax node)
    {
        var Identifier = VBFactory.Identifier(node.Identifier.ToString());
        var TypeList = new List<VBSyntax.TypeSyntax>();
        foreach (CSSyntax.TypeSyntax a in node.TypeArgumentList.Arguments)
        {
            VBSyntax.TypeSyntax TypeIdentifier = (VBSyntax.TypeSyntax)a.Accept(this);
            TypeList.Add(TypeIdentifier);
        }
        return VBFactory.GenericName(Identifier, VBFactory.TypeArgumentList(TypeList.ToArray()));
    }

    public override VBasic.VisualBasicSyntaxNode VisitIdentifierName(CSSyntax.IdentifierNameSyntax node)
    {
        var Identifier = VBFactory.IdentifierName(node.Identifier.ToString());
        return Identifier;
    }

    public override VBasic.VisualBasicSyntaxNode VisitNameMemberCref(CSSyntax.NameMemberCrefSyntax node)
    {
        var Name = node.Name.Accept(this);
        var CrefParameters = new List<VBSyntax.CrefSignaturePartSyntax>();
        VBSyntax.CrefSignatureSyntax Signature = null;
        if (node.Parameters != null)
        {
            foreach (CSSyntax.CrefParameterSyntax p in node.Parameters.Parameters)
            {
                VBSyntax.TypeSyntax TypeSyntax1 = (VBSyntax.TypeSyntax)p.Accept(this);
                CrefParameters.Add(VBFactory.CrefSignaturePart(modifier: default(SyntaxToken), TypeSyntax1));
            }
            Signature = VBFactory.CrefSignature(CrefParameters.ToArray());
        }
        return VBFactory.CrefReference((VBSyntax.TypeSyntax)Name, signature: Signature, asClause: null);
    }

    public override VBasic.VisualBasicSyntaxNode VisitOperatorMemberCref(CSSyntax.OperatorMemberCrefSyntax node)
    {
        var CrefOperator = GetVBOperatorToken(node.OperatorToken.ValueText);
        return VBFactory.CrefOperatorReference(CrefOperator.WithLeadingTrivia(VisualBasicSyntaxFactory.SpaceTrivia));
    }

    public override VBasic.VisualBasicSyntaxNode VisitPredefinedType(CSSyntax.PredefinedTypeSyntax node)
    {
        var Token = VBUtil.ConvertTypesTokenToKind(CS.CSharpExtensions.Kind(node.Keyword), true);
        var switchExpr = Token.RawKind;
        switch (switchExpr)
        {
            case (int)VBasic.SyntaxKind.EmptyToken:
            {
                return VBFactory.ParseTypeName(node.ToString());
            }

            case (int)VBasic.SyntaxKind.NothingKeyword:
            {
                return VisualBasicSyntaxFactory.NothingExpression;
            }

            default:
            {
                return VBFactory.PredefinedType(Token);
            }
        }
    }

    public override VBasic.VisualBasicSyntaxNode VisitQualifiedCref(CSSyntax.QualifiedCrefSyntax QualifiedCref)
    {
        var IdentifierOrTypeName = QualifiedCref.Container.Accept(this);
        VBSyntax.CrefReferenceSyntax Value = (VBSyntax.CrefReferenceSyntax)QualifiedCref.Member.Accept(this);
        VBSyntax.NameSyntax Identifier;
        Identifier = IdentifierOrTypeName is VBSyntax.NameSyntax ? (VBSyntax.NameSyntax)IdentifierOrTypeName : VBFactory.IdentifierName(IdentifierOrTypeName.ToString());
        var QualifiedNameSyntax = VBFactory.QualifiedName(left: Identifier, VisualBasicSyntaxFactory.DotToken, right: (VBSyntax.SimpleNameSyntax)Value.Name);
        if (Value.Signature == null)
        {
            return QualifiedNameSyntax;
        }
        return VBFactory.CrefReference(QualifiedNameSyntax, Value.Signature, null);
    }

    public override VBasic.VisualBasicSyntaxNode VisitQualifiedName(CSSyntax.QualifiedNameSyntax node)
    {
        return VBFactory.QualifiedName((VBSyntax.NameSyntax)node.Left.Accept(this), (VBSyntax.SimpleNameSyntax)node.Right.Accept(this));
    }

    public override VBasic.VisualBasicSyntaxNode VisitTypeCref(CSSyntax.TypeCrefSyntax node)
    {
        return node.Type.Accept(this);
    }

    public override VBasic.VisualBasicSyntaxNode VisitXmlCDataSection(CSSyntax.XmlCDataSectionSyntax node)
    {
        var TextTokens = TriviaListSupport.TranslateTokenList(node.TextTokens);
        return VBFactory.XmlCDataSection(VisualBasicSyntaxFactory.BeginCDataToken, TextTokens, VisualBasicSyntaxFactory.EndCDataToken);
    }

    public override VBasic.VisualBasicSyntaxNode VisitXmlComment(CSSyntax.XmlCommentSyntax node)
    {
        return base.VisitXmlComment(node);
    }

    public override VBasic.VisualBasicSyntaxNode VisitXmlCrefAttribute(CSSyntax.XmlCrefAttributeSyntax node)
    {
        VBSyntax.XmlNameSyntax Name = (VBSyntax.XmlNameSyntax)node.Name.Accept(this);

        var cref = node.Cref.Accept(this);
        var SyntaxTokens = new SyntaxTokenList();
        SyntaxTokens = SyntaxTokens.AddRange(cref.DescendantTokens());
        VBSyntax.XmlNodeSyntax Value = VBFactory.XmlString(VisualBasicSyntaxFactory.DoubleQuoteToken, SyntaxTokens, VisualBasicSyntaxFactory.DoubleQuoteToken);
        return VBFactory.XmlAttribute(Name, Value);
    }

    public override VBasic.VisualBasicSyntaxNode VisitXmlElement(CSSyntax.XmlElementSyntax node)
    {
        var Content = new SyntaxList<VBSyntax.XmlNodeSyntax>();
        VBSyntax.XmlElementStartTagSyntax StartTag = (VBSyntax.XmlElementStartTagSyntax)node.StartTag.Accept(this);

        bool NoEndTag = string.IsNullOrWhiteSpace(node.EndTag.Name.LocalName.ValueText);
        var EndTag = NoEndTag ? VBFactory.XmlElementEndTag(((VBSyntax.XmlNameSyntax)StartTag.Name)) : VBFactory.XmlElementEndTag((VBSyntax.XmlNameSyntax)node.EndTag.Name.Accept(this));
        try
        {
            for (int i = 0, loopTo = node.Content.Count - 1; i <= loopTo; i++)
            {
                var C = node.Content[i];
                VBSyntax.XmlNodeSyntax Node1 = (VBSyntax.XmlNodeSyntax)C.Accept(this);
                if (NoEndTag)
                {
                    var LastToken = Node1.GetLastToken();
                    if (LastToken.ValueText.IsNewLine())
                    {
                        Node1 = Node1.ReplaceToken(LastToken, default(SyntaxToken));
                    }
                }
                Content = Content.Add(Node1);
            }

            if (node.EndTag?.HasLeadingTrivia ==true && node.EndTag.GetLeadingTrivia()[0].IsKind(CS.SyntaxKind.DocumentationCommentExteriorTrivia) == true)
            {
                var NewLeadingTriviaList = new SyntaxTriviaList();
                NewLeadingTriviaList = NewLeadingTriviaList.Add(VBFactory.DocumentationCommentExteriorTrivia(node.EndTag.GetLeadingTrivia()[0].ToString().Replace("///", "'''")));
                var NewTokenList = new SyntaxTokenList();
                NewTokenList = NewTokenList.Add(VBFactory.XmlTextLiteralToken(NewLeadingTriviaList, " ", " ", new SyntaxTriviaList()));
                Content = Content.Add(VBFactory.XmlText(NewTokenList));
                EndTag = EndTag.WithoutLeadingTrivia();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {

        }
        var XmlElement = VBFactory.XmlElement(StartTag, Content, EndTag);
        return XmlElement;
    }

    public override VBasic.VisualBasicSyntaxNode VisitXmlElementEndTag(CSSyntax.XmlElementEndTagSyntax node)
    {
        return VBFactory.XmlElementEndTag((VBSyntax.XmlNameSyntax)node.Name.Accept(this));
    }

    public override VBasic.VisualBasicSyntaxNode VisitXmlElementStartTag(CSSyntax.XmlElementStartTagSyntax node)
    {
        var ListOfAttributes = GatherAttributes(node.Attributes);
        return VBFactory.XmlElementStartTag((VBSyntax.XmlNodeSyntax)node.Name.Accept(this), ListOfAttributes);
    }

    public override VBasic.VisualBasicSyntaxNode VisitXmlEmptyElement(CSSyntax.XmlEmptyElementSyntax node)
    {
        try
        {
            VBSyntax.XmlNodeSyntax Name = (VBSyntax.XmlNodeSyntax)node.Name.Accept(this);
            var ListOfAttributes = GatherAttributes(node.Attributes);
            return VBFactory.XmlEmptyElement(Name, ListOfAttributes);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return VBFactory.XmlText(node.GetText().ToString());
        }
    }

    public override VBasic.VisualBasicSyntaxNode VisitXmlName(CSSyntax.XmlNameSyntax node)
    {
        VBSyntax.XmlPrefixSyntax Prefix;
        Prefix = node.Prefix == null ? null : (VBSyntax.XmlPrefixSyntax)node.Prefix.Accept(this);
        var localName = VBFactory.XmlNameToken(node.LocalName.ValueText, default(VBasic.SyntaxKind));
        return VBFactory.XmlName(Prefix, localName);
    }

    public override VBasic.VisualBasicSyntaxNode VisitXmlNameAttribute(CSSyntax.XmlNameAttributeSyntax node)
    {
        var Name = ((VBSyntax.XmlNodeSyntax)node.Name.Accept(this));
        string ValueString = node.Identifier.ToString();
        VBSyntax.XmlNodeSyntax Value = VBFactory.XmlString(VisualBasicSyntaxFactory.DoubleQuoteToken, SyntaxTokenList.Create(VBFactory.XmlTextLiteralToken(ValueString, ValueString)), VisualBasicSyntaxFactory.DoubleQuoteToken);
        return VBFactory.XmlAttribute(Name, Value);
    }

    public override VBasic.VisualBasicSyntaxNode VisitXmlPrefix(CSSyntax.XmlPrefixSyntax node)
    {
        return base.VisitXmlPrefix(node);
    }

    public override VBasic.VisualBasicSyntaxNode VisitXmlProcessingInstruction(CSSyntax.XmlProcessingInstructionSyntax node)
    {
        return base.VisitXmlProcessingInstruction(node);
    }

    public override VBasic.VisualBasicSyntaxNode VisitXmlText(CSSyntax.XmlTextSyntax node)
    {
        var TextTokens = TriviaListSupport.TranslateTokenList(node.TextTokens);
        var XmlText = VBFactory.XmlText(TextTokens);
        return XmlText;
    }

    public override VBasic.VisualBasicSyntaxNode VisitXmlTextAttribute(CSSyntax.XmlTextAttributeSyntax node)
    {
        VBSyntax.XmlNodeSyntax Name = (VBSyntax.XmlNodeSyntax)node.Name.Accept(this);
        var TextTokens = TriviaListSupport.TranslateTokenList(node.TextTokens);
        var XmlText = VBFactory.XmlText(TextTokens);
        VBSyntax.XmlNodeSyntax Value = VBFactory.XmlString(VisualBasicSyntaxFactory.DoubleQuoteToken, SyntaxTokenList.Create(VBFactory.XmlTextLiteralToken(XmlText.ToString(), XmlText.ToString())), VisualBasicSyntaxFactory.DoubleQuoteToken);
        return VBFactory.XmlAttribute(Name, Value);
    }
}