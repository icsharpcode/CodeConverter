using System;
using System.Collections.Generic;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using VBFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using VBS = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CSharpToVBCodeConverter.DestVisualBasic
{
    internal class XMLVisitor : CS.CSharpSyntaxVisitor<VB.VisualBasicSyntaxNode>
    {
        private static SyntaxToken GetVBOperatorToken(string op)
        {
            switch (op)
            {
                case "==":
                    {
                        return global::VisualBasicSyntaxFactory.EqualsToken;
                    }

                case "!=":
                    {
                        return global::VisualBasicSyntaxFactory.LessThanGreaterThanToken;
                    }

                case ">":
                    {
                        return global::VisualBasicSyntaxFactory.GreaterThanToken;
                    }

                case ">=":
                    {
                        return global::VisualBasicSyntaxFactory.GreaterThanEqualsToken;
                    }

                case "<":
                    {
                        return global::VisualBasicSyntaxFactory.LessThanToken;
                    }

                case "<=":
                    {
                        return global::VisualBasicSyntaxFactory.LessThanEqualsToken;
                    }

                case "|":
                    {
                        return global::VisualBasicSyntaxFactory.OrKeyword;
                    }

                case "||":
                    {
                        return global::VisualBasicSyntaxFactory.OrElseKeyword;
                    }

                case "&":
                    {
                        return global::VisualBasicSyntaxFactory.AndKeyword;
                    }

                case "&&":
                    {
                        return global::VisualBasicSyntaxFactory.AndAlsoKeyword;
                    }

                case "+":
                    {
                        return global::VisualBasicSyntaxFactory.PlusToken;
                    }

                case "-":
                    {
                        return global::VisualBasicSyntaxFactory.MinusToken;
                    }

                case "*":
                    {
                        return global::VisualBasicSyntaxFactory.AsteriskToken;
                    }

                case "/":
                    {
                        return global::VisualBasicSyntaxFactory.SlashToken;
                    }

                case "%":
                    {
                        return global::VisualBasicSyntaxFactory.ModKeyword;
                    }

                case "=":
                    {
                        return global::VisualBasicSyntaxFactory.EqualsToken;
                    }

                case "+=":
                    {
                        return global::VisualBasicSyntaxFactory.PlusEqualsToken;
                    }

                case "-=":
                    {
                        return global::VisualBasicSyntaxFactory.MinusEqualsToken;
                    }

                case "!":
                    {
                        return global::VisualBasicSyntaxFactory.NotKeyword;
                    }

                case "~":
                    {
                        return global::VisualBasicSyntaxFactory.NotKeyword;
                    }

                default:
                    {
                        break;
                    }
            }

            throw new ArgumentOutOfRangeException(nameof(op));
        }

        private SyntaxList<VBS.XmlNodeSyntax> GatherAttributes(SyntaxList<CSS.XmlAttributeSyntax> ListOfAttributes)
        {
            var VBAttributes = new SyntaxList<VBS.XmlNodeSyntax>();
            foreach (CSS.XmlAttributeSyntax a in ListOfAttributes)
                VBAttributes = VBAttributes.Add((VBS.XmlNodeSyntax)a.Accept(this));
            return VBAttributes;
        }

        public override VB.VisualBasicSyntaxNode DefaultVisit(SyntaxNode node)
        {
            return base.DefaultVisit(node);
        }

        public override VB.VisualBasicSyntaxNode VisitConversionOperatorMemberCref(CSS.ConversionOperatorMemberCrefSyntax node)
        {
            return base.VisitConversionOperatorMemberCref(node);
        }

        public override VB.VisualBasicSyntaxNode VisitCrefBracketedParameterList(CSS.CrefBracketedParameterListSyntax node)
        {
            return base.VisitCrefBracketedParameterList(node);
        }

        public override VB.VisualBasicSyntaxNode VisitCrefParameter(CSS.CrefParameterSyntax node)
        {
            return node.Type.Accept(this);
        }

        public override VB.VisualBasicSyntaxNode VisitCrefParameterList(CSS.CrefParameterListSyntax node)
        {
            return base.VisitCrefParameterList(node);
        }

        public override VB.VisualBasicSyntaxNode VisitGenericName(CSS.GenericNameSyntax node)
        {
            var Identifier = VBFactory.Identifier(node.Identifier.ToString());
            var TypeList = new List<VBS.TypeSyntax>();
            foreach (CSS.TypeSyntax a in node.TypeArgumentList.Arguments)
            {
                VBS.TypeSyntax TypeIdentifier = (VBS.TypeSyntax)a.Accept(this);
                TypeList.Add(TypeIdentifier);
            }
            return VBFactory.GenericName(Identifier, VBFactory.TypeArgumentList(TypeList.ToArray()));
        }

        public override VB.VisualBasicSyntaxNode VisitIdentifierName(CSS.IdentifierNameSyntax node)
        {
            var Identifier = VBFactory.IdentifierName(node.Identifier.ToString());
            return Identifier;
        }

        public override VB.VisualBasicSyntaxNode VisitNameMemberCref(CSS.NameMemberCrefSyntax node)
        {
            var Name = node.Name.Accept(this);
            var CrefParameters = new List<VBS.CrefSignaturePartSyntax>();
            VBS.CrefSignatureSyntax Signature = null;
            if (node.Parameters != null)
            {
                foreach (CSS.CrefParameterSyntax p in node.Parameters.Parameters)
                {
                    VBS.TypeSyntax TypeSyntax1 = (VBS.TypeSyntax)p.Accept(this);
                    CrefParameters.Add(VBFactory.CrefSignaturePart(modifier: default(SyntaxToken), TypeSyntax1));
                }
                Signature = VBFactory.CrefSignature(CrefParameters.ToArray());
            }
            return VBFactory.CrefReference((VBS.TypeSyntax)Name, signature: Signature, asClause: null);
        }

        public override VB.VisualBasicSyntaxNode VisitOperatorMemberCref(CSS.OperatorMemberCrefSyntax node)
        {
            var CrefOperator = GetVBOperatorToken(node.OperatorToken.ValueText);
            return VBFactory.CrefOperatorReference(CrefOperator.WithLeadingTrivia(global::VisualBasicSyntaxFactory.SpaceTrivia));
        }

        public override VB.VisualBasicSyntaxNode VisitPredefinedType(CSS.PredefinedTypeSyntax node)
        {
            var Token = VBUtil.ConvertTypesTokenToKind(CS.CSharpExtensions.Kind(node.Keyword), true);
            var switchExpr = Token.RawKind;
            switch (switchExpr)
            {
                case (int)VB.SyntaxKind.EmptyToken:
                    {
                        return VBFactory.ParseTypeName(node.ToString());
                    }

                case (int)VB.SyntaxKind.NothingKeyword:
                    {
                        return global::VisualBasicSyntaxFactory.NothingExpression;
                    }

                default:
                    {
                        return VBFactory.PredefinedType(Token);
                    }
            }
        }

        public override VB.VisualBasicSyntaxNode VisitQualifiedCref(CSS.QualifiedCrefSyntax QualifiedCref)
        {
            var IdentifierOrTypeName = QualifiedCref.Container.Accept(this);
            VBS.CrefReferenceSyntax Value = (VBS.CrefReferenceSyntax)QualifiedCref.Member.Accept(this);
            VBS.NameSyntax Identifier;
            Identifier = IdentifierOrTypeName is VBS.NameSyntax ? (VBS.NameSyntax)IdentifierOrTypeName : VBFactory.IdentifierName(IdentifierOrTypeName.ToString());
            var QualifiedNameSyntax = VBFactory.QualifiedName(left: Identifier, global::VisualBasicSyntaxFactory.DotToken, right: (VBS.SimpleNameSyntax)Value.Name);
            if (Value.Signature == null)
            {
                return QualifiedNameSyntax;
            }
            return VBFactory.CrefReference(QualifiedNameSyntax, Value.Signature, null);
        }

        public override VB.VisualBasicSyntaxNode VisitQualifiedName(CSS.QualifiedNameSyntax node)
        {
            return VBFactory.QualifiedName((VBS.NameSyntax)node.Left.Accept(this), (VBS.SimpleNameSyntax)node.Right.Accept(this));
        }

        public override VB.VisualBasicSyntaxNode VisitTypeCref(CSS.TypeCrefSyntax node)
        {
            return node.Type.Accept(this);
        }

        public override VB.VisualBasicSyntaxNode VisitXmlCDataSection(CSS.XmlCDataSectionSyntax node)
        {
            var TextTokens = DestVisualBasic.TriviaListSupport.TranslateTokenList(node.TextTokens);
            return VBFactory.XmlCDataSection(global::VisualBasicSyntaxFactory.BeginCDataToken, TextTokens, global::VisualBasicSyntaxFactory.EndCDataToken);
        }

        public override VB.VisualBasicSyntaxNode VisitXmlComment(CSS.XmlCommentSyntax node)
        {
            return base.VisitXmlComment(node);
        }

        public override VB.VisualBasicSyntaxNode VisitXmlCrefAttribute(CSS.XmlCrefAttributeSyntax node)
        {
            VBS.XmlNameSyntax Name = (VBS.XmlNameSyntax)node.Name.Accept(this);

            var cref = node.Cref.Accept(this);
            var SyntaxTokens = new SyntaxTokenList();
            SyntaxTokens = SyntaxTokens.AddRange(cref.DescendantTokens());
            VBS.XmlNodeSyntax Value = VBFactory.XmlString(global::VisualBasicSyntaxFactory.DoubleQuoteToken, SyntaxTokens, global::VisualBasicSyntaxFactory.DoubleQuoteToken);
            return VBFactory.XmlAttribute(Name, Value);
        }

        public override VB.VisualBasicSyntaxNode VisitXmlElement(CSS.XmlElementSyntax node)
        {
            var Content = new SyntaxList<VBS.XmlNodeSyntax>();
            VBS.XmlElementStartTagSyntax StartTag = (VBS.XmlElementStartTagSyntax)node.StartTag.Accept(this);

            bool NoEndTag = string.IsNullOrWhiteSpace(node.EndTag.Name.LocalName.ValueText);
            var EndTag = NoEndTag ? VBFactory.XmlElementEndTag(((VBS.XmlNameSyntax)StartTag.Name)) : VBFactory.XmlElementEndTag((VBS.XmlNameSyntax)node.EndTag.Name.Accept(this));
            try
            {
                for (int i = 0, loopTo = node.Content.Count - 1; i <= loopTo; i++)
                {
                    var C = node.Content[i];
                    VBS.XmlNodeSyntax Node1 = (VBS.XmlNodeSyntax)C.Accept(this);
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

        public override VB.VisualBasicSyntaxNode VisitXmlElementEndTag(CSS.XmlElementEndTagSyntax node)
        {
            return VBFactory.XmlElementEndTag((VBS.XmlNameSyntax)node.Name.Accept(this));
        }

        public override VB.VisualBasicSyntaxNode VisitXmlElementStartTag(CSS.XmlElementStartTagSyntax node)
        {
            var ListOfAttributes = GatherAttributes(node.Attributes);
            return VBFactory.XmlElementStartTag((VBS.XmlNodeSyntax)node.Name.Accept(this), ListOfAttributes);
        }

        public override VB.VisualBasicSyntaxNode VisitXmlEmptyElement(CSS.XmlEmptyElementSyntax node)
        {
            try
            {
                VBS.XmlNodeSyntax Name = (VBS.XmlNodeSyntax)node.Name.Accept(this);
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

        public override VB.VisualBasicSyntaxNode VisitXmlName(CSS.XmlNameSyntax node)
        {
            VBS.XmlPrefixSyntax Prefix;
            Prefix = node.Prefix == null ? null : (VBS.XmlPrefixSyntax)node.Prefix.Accept(this);
            var localName = VBFactory.XmlNameToken(node.LocalName.ValueText, default(VB.SyntaxKind));
            return VBFactory.XmlName(Prefix, localName);
        }

        public override VB.VisualBasicSyntaxNode VisitXmlNameAttribute(CSS.XmlNameAttributeSyntax node)
        {
            var Name = ((VBS.XmlNodeSyntax)node.Name.Accept(this));
            string ValueString = node.Identifier.ToString();
            VBS.XmlNodeSyntax Value = VBFactory.XmlString(global::VisualBasicSyntaxFactory.DoubleQuoteToken, SyntaxTokenList.Create(VBFactory.XmlTextLiteralToken(ValueString, ValueString)), global::VisualBasicSyntaxFactory.DoubleQuoteToken);
            return VBFactory.XmlAttribute(Name, Value);
        }

        public override VB.VisualBasicSyntaxNode VisitXmlPrefix(CSS.XmlPrefixSyntax node)
        {
            return base.VisitXmlPrefix(node);
        }

        public override VB.VisualBasicSyntaxNode VisitXmlProcessingInstruction(CSS.XmlProcessingInstructionSyntax node)
        {
            return base.VisitXmlProcessingInstruction(node);
        }

        public override VB.VisualBasicSyntaxNode VisitXmlText(CSS.XmlTextSyntax node)
        {
            var TextTokens = DestVisualBasic.TriviaListSupport.TranslateTokenList(node.TextTokens);
            var XmlText = VBFactory.XmlText(TextTokens);
            return XmlText;
        }

        public override VB.VisualBasicSyntaxNode VisitXmlTextAttribute(CSS.XmlTextAttributeSyntax node)
        {
            VBS.XmlNodeSyntax Name = (VBS.XmlNodeSyntax)node.Name.Accept(this);
            var TextTokens = DestVisualBasic.TriviaListSupport.TranslateTokenList(node.TextTokens);
            var XmlText = VBFactory.XmlText(TextTokens);
            VBS.XmlNodeSyntax Value = VBFactory.XmlString(global::VisualBasicSyntaxFactory.DoubleQuoteToken, SyntaxTokenList.Create(VBFactory.XmlTextLiteralToken(XmlText.ToString(), XmlText.ToString())), global::VisualBasicSyntaxFactory.DoubleQuoteToken);
            return VBFactory.XmlAttribute(Name, Value);
        }
    }
}

