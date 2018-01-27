using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.VB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Xunit;

namespace CodeConverter.Tests
{
    public class ConverterTestBase
    {
        void CSharpWorkspaceSetup(out TestWorkspace workspace, out Document doc, CSharpParseOptions parseOptions = null)
        {
            workspace = new TestWorkspace();
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            if (parseOptions == null) {
                parseOptions = new CSharpParseOptions(
                    Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp6,
                    DocumentationMode.Diagnose | DocumentationMode.Parse,
                    SourceCodeKind.Regular,
                    ImmutableArray.Create("DEBUG", "TEST")
                );
            }
            workspace.Options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false);
            workspace.Open(ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                "TestProject",
                "TestProject",
                LanguageNames.CSharp,
                null,
                null,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    false,
                    "",
                    "",
                    "Script",
                    new[] { "System", "System.Collections.Generic", "System.Linq" },
                    OptimizationLevel.Debug,
                    false,
                    true
                ),
                parseOptions,
                new[] {
                    DocumentInfo.Create(
                        documentId,
                        "a.cs",
                        null,
                        SourceCodeKind.Regular
                    )
                },
                null,
                DiagnosticTestBase.DefaultMetadataReferences
            )
            );
            doc = workspace.CurrentSolution.GetProject(projectId).GetDocument(documentId);
        }

        void CSharpWorkspaceSetup(string text, out TestWorkspace workspace, out Document doc, CSharpParseOptions parseOptions = null)
        {
            workspace = new TestWorkspace();
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            if (parseOptions == null) {
                parseOptions = new CSharpParseOptions(
                    Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp6,
                    DocumentationMode.Diagnose | DocumentationMode.Parse,
                    SourceCodeKind.Regular,
                    ImmutableArray.Create("DEBUG", "TEST")
                );
            }
            workspace.Options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false);
            workspace.Open(ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                "TestProject",
                "TestProject",
                LanguageNames.CSharp,
                null,
                null,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    false,
                    "",
                    "",
                    "Script",
                    new[] { "System", "System.Collections.Generic", "System.Linq" },
                    OptimizationLevel.Debug,
                    false,
                    true
                ),
                parseOptions,
                new[] {
                    DocumentInfo.Create(
                        documentId,
                        "a.cs",
                        null,
                        SourceCodeKind.Regular,
                        TextLoader.From(TextAndVersion.Create(SourceText.From(text), VersionStamp.Create()))
                    )
                },
                null,
                DiagnosticTestBase.DefaultMetadataReferences
            )
            );
            doc = workspace.CurrentSolution.GetProject(projectId).GetDocument(documentId);
        }

        void VBWorkspaceSetup(out TestWorkspace workspace, out Document doc, VisualBasicParseOptions parseOptions = null)
        {
            workspace = new TestWorkspace();
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            if (parseOptions == null) {
                parseOptions = new VisualBasicParseOptions(
                    Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14,
                    DocumentationMode.Diagnose | DocumentationMode.Parse,
                    SourceCodeKind.Regular
                );
            }
            workspace.Options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false);
            var compilationOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithRootNamespace("TestProject")
                .WithGlobalImports(GlobalImport.Parse("System", "System.Collections.Generic", "System.Linq", "Microsoft.VisualBasic"));
            workspace.Open(ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                "TestProject",
                "TestProject",
                LanguageNames.VisualBasic,
                null,
                null,
                compilationOptions,
                parseOptions,
                new[] {
                    DocumentInfo.Create(
                        documentId,
                        "a.vb",
                        null,
                        SourceCodeKind.Regular
                    )
                },
                null,
                DiagnosticTestBase.DefaultMetadataReferences
            )
            );
            doc = workspace.CurrentSolution.GetProject(projectId).GetDocument(documentId);
        }

        void VBWorkspaceSetup(string text, out TestWorkspace workspace, out Document doc, VisualBasicParseOptions parseOptions = null)
        {
            workspace = new TestWorkspace();
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            if (parseOptions == null) {
                parseOptions = new VisualBasicParseOptions(
                    Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14,
                    DocumentationMode.Diagnose | DocumentationMode.Parse,
                    SourceCodeKind.Regular
                );
            }
            workspace.Options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false);
            var compilationOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithRootNamespace("TestProject")
                .WithGlobalImports(GlobalImport.Parse("System", "System.Collections.Generic", "System.Linq", "Microsoft.VisualBasic"));
            workspace.Open(ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                "TestProject",
                "TestProject",
                LanguageNames.VisualBasic,
                null,
                null,
                compilationOptions,
                parseOptions,
                new[] {
                    DocumentInfo.Create(
                        documentId,
                        "a.vb",
                        null,
                        SourceCodeKind.Regular,
                        TextLoader.From(TextAndVersion.Create(SourceText.From(text), VersionStamp.Create()))
                    )
                },
                null,
                DiagnosticTestBase.DefaultMetadataReferences
            )
            );
            doc = workspace.CurrentSolution.GetProject(projectId).GetDocument(documentId);
        }

        public void TestConversionCSharpToVisualBasic(string csharpCode, string expectedVisualBasicCode, CSharpParseOptions csharpOptions = null, VisualBasicParseOptions vbOptions = null)
        {
            TestWorkspace csharpWorkspace, vbWorkspace;
            Document inputDocument, outputDocument;
            CSharpWorkspaceSetup(csharpCode, out csharpWorkspace, out inputDocument, csharpOptions);
            VBWorkspaceSetup(out vbWorkspace, out outputDocument, vbOptions);
            var outputNode = Convert((CSharpSyntaxNode)inputDocument.GetSyntaxRootAsync().GetAwaiter().GetResult(), inputDocument.GetSemanticModelAsync().GetAwaiter().GetResult(), outputDocument);

            var txt = outputDocument.WithSyntaxRoot(Formatter.Format(outputNode, vbWorkspace)).GetTextAsync().GetAwaiter().GetResult().ToString();
            txt = Utils.HomogenizeEol(txt).TrimEnd();
            expectedVisualBasicCode = Utils.HomogenizeEol(expectedVisualBasicCode).TrimEnd();
            if (expectedVisualBasicCode != txt) {
                Console.WriteLine("expected:");
                Console.WriteLine(expectedVisualBasicCode);
                Console.WriteLine("got:");
                Console.WriteLine(txt);
                Console.WriteLine("diff:");
                int l = Math.Max(expectedVisualBasicCode.Length, txt.Length);
                StringBuilder diff = new StringBuilder(l);
                for (int i = 0; i < l; i++) {
                    if (i >= expectedVisualBasicCode.Length || i >= txt.Length || expectedVisualBasicCode[i] != txt[i])
                        diff.Append('x');
                    else
                        diff.Append(expectedVisualBasicCode[i]);
                }
                Console.WriteLine(diff.ToString());
                Assert.True(false);
            }
        }

        public void TestConversionVisualBasicToCSharp(string visualBasicCode, string expectedCsharpCode)
        {
            TestConversionVisualBasicToCSharpWithoutComments(visualBasicCode, expectedCsharpCode);
            TestConversionVisualBasicToCSharpWithoutComments(AddLineNumberComments(visualBasicCode, "' ", false), AddLineNumberComments(expectedCsharpCode, "// ", true));
        }

        public void TestConversionVisualBasicToCSharpWithoutComments(string visualBasicCode, string expectedCsharpCode, CSharpParseOptions csharpOptions = null, VisualBasicParseOptions vbOptions = null)
        {
            TestWorkspace csharpWorkspace, vbWorkspace;
            Document inputDocument, outputDocument;
            VBWorkspaceSetup(visualBasicCode, out vbWorkspace, out inputDocument, vbOptions);
            CSharpWorkspaceSetup(out csharpWorkspace, out outputDocument, csharpOptions);
            var outputNode = Convert((VisualBasicSyntaxNode)inputDocument.GetSyntaxRootAsync().Result, inputDocument.GetSemanticModelAsync().Result, outputDocument);

            var txt = outputDocument.WithSyntaxRoot(Formatter.Format(outputNode, vbWorkspace)).GetTextAsync().Result.ToString();
            txt = Utils.HomogenizeEol(txt).TrimEnd();
            expectedCsharpCode = Utils.HomogenizeEol(expectedCsharpCode).TrimEnd();
            if (expectedCsharpCode != txt) {
                int l = Math.Max(expectedCsharpCode.Length, txt.Length);
                StringBuilder sb = new StringBuilder(l * 4);
                sb.AppendLine("expected:");
                sb.AppendLine(expectedCsharpCode);
                sb.AppendLine("got:");
                sb.AppendLine(txt);
                sb.AppendLine("diff:");
                for (int i = 0; i < l; i++) {
                    if (i >= expectedCsharpCode.Length || i >= txt.Length || expectedCsharpCode[i] != txt[i])
                        sb.Append('x');
                    else
                        sb.Append(expectedCsharpCode[i]);
                }
                sb.AppendLine();
                sb.AppendLine("vb:");
                sb.AppendLine(visualBasicCode);
                Assert.True(false, sb.ToString());
            }
        }

        private static string AddLineNumberComments(string code, string singleLineCommentStart, bool isTarget)
        {
            int skipped = 0;
            var lines = code.Split('\r'); // Don't split at very start
            bool started = false;

            var newLines = lines.Select((s, i) => {

                var prevLine = i > 0 ? lines[i - 1] : "";
                var nextLine = i < lines.Length - 1 ? lines[i + 1] : "";

                //Don't start until first line mentioning class
                started |= s.IndexOf("class ", StringComparison.InvariantCultureIgnoreCase) + s.IndexOf("module ", StringComparison.InvariantCultureIgnoreCase) > -2;

                //Lines which don't map directly should be tested independently
                if (!started ||
                isTarget && HasNoSourceLine(prevLine, s, nextLine)
                || !isTarget && HasNoTargetLine(prevLine, s, nextLine)) {
                    skipped++;
                    return s;
                }

                //Try to indent based on next line
                if (s.Trim() == "" && i > 0) {
                    s = new string(Enumerable.Repeat(' ', lines[i - 1].Trim('\n').Length).ToArray());
                }

                return s + singleLineCommentStart + (i - skipped).ToString();
            });
            return string.Join("\r", newLines);
        }

        private static bool HasNoSourceLine(string prevLine, string line, string nextLine)
        {
            return line.Trim() == "{" 
                || nextLine.Contains("where T");
        }

        private static bool HasNoTargetLine(string prevLine, string line, string nextLine)
        {
            return IsVbInheritsOrImplements(nextLine)
                //Allow a blank line in VB after these statements that doesn't appear in the C# since C# has braces to act as a separator
                || string.IsNullOrWhiteSpace(line) && IsVbInheritsOrImplements(prevLine);
        }

        private static bool IsVbInheritsOrImplements(string line)
        {
            return line.Contains("Inherits") || line.Contains("Implements");
        }

        VisualBasicSyntaxNode Convert(CSharpSyntaxNode input, SemanticModel semanticModel, Document targetDocument)
        {
            return CSharpConverter.Convert(input, semanticModel, targetDocument);
        }

        CSharpSyntaxNode Convert(VisualBasicSyntaxNode input, SemanticModel semanticModel, Document targetDocument)
        {
            return VisualBasicConverter.ConvertSingle((VisualBasicCompilation) semanticModel.Compilation, (VisualBasicSyntaxTree) input.SyntaxTree);
        }
    }
}
