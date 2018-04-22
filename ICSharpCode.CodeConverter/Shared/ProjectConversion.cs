using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Shared
{
    public class ProjectConversion<TLanguageConversion> where TLanguageConversion : ILanguageConversion, new()
    {
        private readonly string _solutionDir;
        private readonly Compilation _sourceCompilation;
        private readonly IEnumerable<SyntaxTree> _syntaxTreesToConvert;
        private static readonly AdhocWorkspace AdhocWorkspace = new AdhocWorkspace();
        private readonly ConcurrentDictionary<string, string> _errors = new ConcurrentDictionary<string, string>();
        private readonly Dictionary<string, SyntaxTree> _firstPassResults = new Dictionary<string, SyntaxTree>();
        private readonly TLanguageConversion _languageConversion;
        private readonly bool _handlePartialConversion;

        private static readonly IReadOnlyCollection<(string, string)> VbToCsTypeGuidReplacements = new [] {
            ("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}", "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"), // Standard project
            ("{CB4CE8C6-1BDB-4DC7-A4D3-65A1999772F8}", "{20D4826A-C6FA-45DB-90F4-C717570B9F32}"), // Legacy (2003) Smart Device
            ("{68B1623D-7FB9-47D8-8664-7ECEA3297D4F}", "{4D628B5B-2FBC-4AA6-8C16-197242AEB884}"), // Smart Device
            ("{D59BE175-2ED0-4C54-BE3D-CDAA9F3214C8}", "{14822709-B5A1-4724-98CA-57A101D1B079}"), // Workflow
            ("{593B0543-81F6-4436-BA1E-4747859CAAE2}", "{EC05E597-79D4-47f3-ADA0-324C4F7C7484}"), // SharePoint
            ("{DB03555F-0C8B-43BE-9FF9-57896B3C5E56}", "{C089C8C0-30E0-4E22-80C0-CE093F111A43}") // Store App Windows Phone 8.1 Silverlight
        };

        private static IReadOnlyCollection<(string, string)> CsToVbTypeGuidReplacements => VbToCsTypeGuidReplacements.Select((vbCs, i) => (vbCs.Item2, vbCs.Item1)).ToArray();

        private ProjectConversion(Compilation sourceCompilation, string solutionDir)
            : this(sourceCompilation, sourceCompilation.SyntaxTrees.Where(t => t.FilePath.StartsWith(solutionDir)))
        {
            _solutionDir = solutionDir;
        }

        private ProjectConversion(Compilation sourceCompilation, IEnumerable<SyntaxTree> syntaxTreesToConvert)
        {
            _languageConversion = new TLanguageConversion();
            this._sourceCompilation = sourceCompilation;
            _syntaxTreesToConvert = syntaxTreesToConvert.ToList();
            _handlePartialConversion = _syntaxTreesToConvert.Count() == 1;
        }

        public static ConversionResult ConvertText(string text, IReadOnlyCollection<MetadataReference> references)
        {
            var languageConversion = new TLanguageConversion();
            var syntaxTree = languageConversion.CreateTree(text);
            var compilation = languageConversion.CreateCompilationFromTree(syntaxTree, references);
            return ConvertSingle(compilation, syntaxTree, new TextSpan(0, 0)).GetAwaiter().GetResult();
        }

        public static async Task<ConversionResult> ConvertSingle(Compilation compilation, SyntaxTree syntaxTree, TextSpan selected)
        {
            if (selected.Length > 0) {
                var annotatedSyntaxTree = await GetSyntaxTreeWithAnnotatedSelection(syntaxTree, selected);
                compilation = compilation.ReplaceSyntaxTree(syntaxTree, annotatedSyntaxTree);
                syntaxTree = annotatedSyntaxTree;
            }

            var conversion = new ProjectConversion<TLanguageConversion>(compilation, new [] {syntaxTree});
            var conversionResults = ConvertProject(conversion).ToList();
            var codeResult = conversionResults.SingleOrDefault(x => !string.IsNullOrWhiteSpace(x.ConvertedCode)) 
                             ?? conversionResults.First();
            codeResult.Exceptions = conversionResults.SelectMany(x => x.Exceptions).ToArray();
            return codeResult;
        }

        public static IEnumerable<ConversionResult> ConvertProjects(IReadOnlyCollection<Project> projects)
        {
            var solutionFilePath = projects.First().Solution.FilePath;
            var solutionDir = Path.GetDirectoryName(solutionFilePath);
            IReadOnlyCollection<(string, string)> replacements = CreateNewSolution(solutionFilePath, projects);

            foreach (var project in projects) {
                CreateNewProjFile(project, replacements);
                var compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
                var projectConversion = new ProjectConversion<TLanguageConversion>(compilation, solutionDir);
                foreach (var conversionResult in ConvertProject(projectConversion)) yield return conversionResult;
            }
        }

        private static void CreateNewProjFile(Project project,
            IReadOnlyCollection<(string, string)> replacements)
        {
            switch (project.Language)
            {
                case LanguageNames.VisualBasic:
                    replacements = replacements
                        .Concat(new[] {("\\\\Microsoft.VisualBasic.targets", "\\Microsoft.CSharp.targets")})
                        .Concat(new[] {(".vb\"", ".cs\""), (".vb<", ".cs<")})
                        .ToList();
                    break;
                case LanguageNames.CSharp:
                    replacements = replacements
                        .Concat(new[] { ("\\\\Microsoft.CSharp.targets", "\\Microsoft.VisualBasic.targets") })
                        .Concat(new[] { (".cs\"", ".vb\""), (".cs<", ".vb<") })
                        .ToList();
                    break;
            }

            var newProjectPath = TogglePathExtension(project.FilePath);
            var newProjectText = File.ReadAllText(project.FilePath);
            foreach (var (oldValue, newValue) in replacements) {
                newProjectText = Regex.Replace(newProjectText, oldValue, newValue, RegexOptions.IgnoreCase);
            }
            File.WriteAllText(newProjectPath, newProjectText);
        }

        private static string TogglePathExtension(string filePath)
        {
            var originalExtension = Path.GetExtension(filePath);
            return Path.ChangeExtension(filePath, GetConvertedExtension(originalExtension));
        }

        private static string GetConvertedExtension(string originalExtension)
        {
            switch (originalExtension)
            {
                case ".csproj":
                    return ".vbproj";
                case ".vbproj":
                    return ".csproj";
                case ".cs":
                    return ".vb";
                case ".vb":
                    return ".cs";
                default:
                    throw new ArgumentOutOfRangeException(nameof(originalExtension), originalExtension, null);
            }
        }

        private static IReadOnlyCollection<(string, string)> CreateNewSolution(string solutionFilePath, IReadOnlyCollection<Project> projects)
        {
            var contents = File.ReadAllText(solutionFilePath);
            var replacements = new List<(string, string)>();
            foreach (var project in projects) {
                var projFilename = Path.GetFileName(project.FilePath);
                var newProjFilename = TogglePathExtension(projFilename);
                replacements.Add((projFilename, newProjFilename));
                replacements.Add(GetProjectGuidReplacement(projFilename, contents));
            }

            var projectTypeReplacements = projects.SelectMany(GetProjectTypeReplacement).ToList();
            foreach (var (oldValue, newValue) in replacements.Concat(projectTypeReplacements)) {
                contents = Regex.Replace(contents, oldValue, newValue, RegexOptions.IgnoreCase);
            }
            File.WriteAllText(Path.ChangeExtension(solutionFilePath, ".converted.sln"), contents);
            return replacements;
        }

        private static (string, string) GetProjectGuidReplacement(string projFilename, string contents)
        {
            var projGuidRegex = new Regex(projFilename + @""", ""({[0-9A-Fa-f\-]{32,36}})("")");
            var projGuidMatch = projGuidRegex.Match(contents);
            var oldGuid = projGuidMatch.Groups[1].Value;
            var newGuid = GetDeterministicGuidFrom(new Guid(oldGuid));
            var projectGuidReplacement = (oldGuid, newGuid.ToString("B").ToUpperInvariant());
            return projectGuidReplacement;
        }

        private static IEnumerable<(string, string)> GetProjectTypeReplacement(Project project)
        {
            var vbToCsTypeGuidReplacements = project.Language == LanguageNames.VisualBasic
                ? VbToCsTypeGuidReplacements
                : CsToVbTypeGuidReplacements;
            //TODO: Allow arbitrary amounts of whitespace in case hand-edited
            return vbToCsTypeGuidReplacements.Select((sourceGuid, targetGuid) => ($"Project(\"{sourceGuid}\") = \"{project.Name}\"", $"Project(\"{targetGuid}\") = \"{project.Name}\""));
        }

        public static Guid GetDeterministicGuidFrom(Guid guidToConvert)
        {
            var codeConverterStaticGuid = new Guid("{B224816B-CC58-4FF1-8258-CA7E629734A0}");
            var deterministicNewBytes = codeConverterStaticGuid.ToByteArray().Zip(guidToConvert.ToByteArray(),
                    (fromFirst, fromSecond) => (byte)(fromFirst ^ fromSecond));
            return new Guid(deterministicNewBytes.ToArray());
        }

        private static IEnumerable<ConversionResult> ConvertProject(ProjectConversion<TLanguageConversion> projectConversion)
        {
            foreach (var pathNodePair in projectConversion.Convert())
            {
                var errors = projectConversion._errors.TryRemove(pathNodePair.Key, out var nonFatalException)
                    ? new[] {nonFatalException}
                    : new string[0];
                yield return new ConversionResult(pathNodePair.Value.ToFullString()) { SourcePathOrNull = pathNodePair.Key, Exceptions = errors };
            }

            foreach (var error in projectConversion._errors)
            {
                yield return new ConversionResult {SourcePathOrNull = error.Key, Exceptions = new []{ error.Value } };
            }
        }

        private Dictionary<string, SyntaxNode> Convert()
        {
            FirstPass();
            var secondPassByFilePath = SecondPass();
#if DEBUG && false
            AddProjectWarnings();
#endif
            return secondPassByFilePath;
        }

        private Dictionary<string, SyntaxNode> SecondPass()
        {
            var secondPassByFilePath = new Dictionary<string, SyntaxNode>();
            foreach (var firstPassResult in _firstPassResults) {
                var treeFilePath = firstPassResult.Key;
                try {
                    secondPassByFilePath.Add(treeFilePath, SingleSecondPass(firstPassResult));
                }  catch (Exception e) {
                    secondPassByFilePath.Add(treeFilePath, Format(firstPassResult.Value.GetRoot()));
                    _errors.TryAdd(treeFilePath, e.ToString());
                }
            }
            return secondPassByFilePath;
        }

        private void AddProjectWarnings()
        {
            var nonFatalWarningsOrNull = _languageConversion.GetWarningsOrNull();
            if (!string.IsNullOrWhiteSpace(nonFatalWarningsOrNull))
            {
                var warningsDescription = Path.Combine(_solutionDir ?? "", _sourceCompilation.AssemblyName, "ConversionWarnings.txt");
                _errors.TryAdd(warningsDescription, nonFatalWarningsOrNull);
            }
        }

        private SyntaxNode SingleSecondPass(KeyValuePair<string, SyntaxTree> cs)
        {
            var secondPassNode = _languageConversion.SingleSecondPass(cs);
            return Format(secondPassNode);
        }

        private void FirstPass()
        {
            foreach (var tree in _syntaxTreesToConvert)
            {
                var treeFilePath = tree.FilePath ?? "";
                try {
                    SingleFirstPass(tree, treeFilePath);
                    var errorAnnotations = tree.GetRoot().GetAnnotations(AnnotationConstants.ConversionErrorAnnotationKind).ToList();
                    if (errorAnnotations.Any()) {
                        _errors.TryAdd(treeFilePath,
                            string.Join(Environment.NewLine, errorAnnotations.Select(a => a.Data))
                        );
                    }
                }
                catch (Exception e)
                {
                    _errors.TryAdd(treeFilePath, e.ToString());
                }
            }
        }

        private void SingleFirstPass(SyntaxTree tree, string treeFilePath)
        {
            var currentSourceCompilation = this._sourceCompilation;
            var newTree = MakeFullCompilationUnit(tree);
            if (newTree != tree) {
                currentSourceCompilation = currentSourceCompilation.ReplaceSyntaxTree(tree, newTree);
                tree = newTree;
            }
            var convertedTree = _languageConversion.SingleFirstPass(currentSourceCompilation, tree);
            _firstPassResults.Add(treeFilePath, convertedTree);
        }

        private SyntaxTree MakeFullCompilationUnit(SyntaxTree tree)
        {
            if (!_handlePartialConversion) return tree;
            var root = tree.GetRoot();
            var rootChildren = root.ChildNodes().ToList();
            var requiresSurroundingClass = rootChildren.Any(_languageConversion.MustBeContainedByClass);
            var requiresSurroundingMethod = rootChildren.All(_languageConversion.MustBeContainedByMethod);

            if (requiresSurroundingMethod || requiresSurroundingClass) {
                var text = root.GetText().ToString();
                if (requiresSurroundingMethod) text = _languageConversion.WithSurroundingMethod(text);
                text = _languageConversion.WithSurroundingClass(text);

                var fullCompilationUnit = _languageConversion.CreateTree(text).GetRoot();

                var selectedNode = _languageConversion.GetSurroundedNode(fullCompilationUnit.DescendantNodes(), requiresSurroundingMethod);
                tree = fullCompilationUnit.WithAnnotatedNode(selectedNode, AnnotationConstants.SelectedNodeAnnotationKind, AnnotationConstants.AnnotatedNodeIsParentData);
            }

            return tree;
        }

        private static async Task<SyntaxTree> GetSyntaxTreeWithAnnotatedSelection(SyntaxTree syntaxTree, TextSpan selected)
        {
            var root = await syntaxTree.GetRootAsync();
            var selectedNode = root.FindNode(selected);
            return root.WithAnnotatedNode(selectedNode, AnnotationConstants.SelectedNodeAnnotationKind);
        }

        private SyntaxNode Format(SyntaxNode resultNode)
        {
            SyntaxNode selectedNode = _handlePartialConversion ? GetSelectedNode(resultNode) : resultNode;
            return Formatter.Format(selectedNode ?? resultNode, AdhocWorkspace);
        }

        private SyntaxNode GetSelectedNode(SyntaxNode resultNode)
        {
            var selectedNode = resultNode.GetAnnotatedNodes(AnnotationConstants.SelectedNodeAnnotationKind)
                .SingleOrDefault();
            if (selectedNode != null)
            {
                var children = _languageConversion.FindSingleImportantChild(selectedNode);
                if (selectedNode.GetAnnotations(AnnotationConstants.SelectedNodeAnnotationKind)
                        .Any(n => n.Data == AnnotationConstants.AnnotatedNodeIsParentData)
                    && children.Count == 1)
                {
                    selectedNode = children.Single();
                }
            }

            return selectedNode ?? resultNode;
        }
    }
}