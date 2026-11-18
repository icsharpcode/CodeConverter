using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Decides whether a converted handler must maintain <c>Err</c> (<c>ProjectData.SetProjectError</c>/<c>ClearProjectError</c>).
/// VB's lowering always does, but in C# it's noise unless something reads <c>Err</c>: the handler's own type, or an "Err helper",
/// a method with no <c>On Error</c> of its own that reads <c>Err</c>, so can only be reading an error that some other method caught.
/// </summary>
/// <remarks>
/// One pass per project, done the first time a method with <c>On Error</c> asks, and shared by every document converted from that project.
/// Only reads count: <c>Err.Raise</c> and <c>Err.Clear</c> don't depend on an earlier error. A read in a <c>Catch</c> block reads that block's own error, so doesn't make an Err helper.
/// Reads through reflection, late binding or from another project aren't seen.
/// </remarks>
internal sealed class ErrUsage
{
    private static readonly string[] NonReadingMembers = { "Raise", "Clear" };

    private readonly AsyncLazy<ProjectErrReads> _projectErrReads;

    public ErrUsage(Project project) =>
        _projectErrReads = new AsyncLazy<ProjectErrReads>(() => FindErrReadsAsync(project), JoinableTaskFactorySingleton.Instance);

    public async Task<bool> IsMaintainedInAsync(INamedTypeSymbol containingType)
    {
        var reads = await _projectErrReads.GetValueAsync();
        return reads.ProjectHasErrHelper || containingType != null && reads.TypesReadingErr.Contains(containingType.GetFullMetadataName());
    }

    private record ProjectErrReads(bool ProjectHasErrHelper, HashSet<string> TypesReadingErr);

    private static async Task<ProjectErrReads> FindErrReadsAsync(Project project)
    {
        var compilation = await project.GetCompilationAsync();
        var typesReadingErr = new HashSet<string>(StringComparer.Ordinal);
        var hasErrHelper = false;
        foreach (var tree in compilation.SyntaxTrees) {
            var root = await tree.GetRootAsync();
            var candidates = root.DescendantNodes().OfType<VBSyntax.IdentifierNameSyntax>()
                .Where(n => string.Equals(n.Identifier.ValueText, "Err", StringComparison.OrdinalIgnoreCase) && !IsNonReadingMemberAccess(n))
                .ToArray();
            if (!candidates.Any()) continue;
            var semanticModel = compilation.GetSemanticModel(tree, true);
            foreach (var errRead in candidates.Where(n => IsInformationErr(semanticModel.GetSymbolInfo(n).Symbol))) {
                if (semanticModel.GetEnclosingSymbol(errRead.SpanStart)?.ContainingType is { } type) {
                    typesReadingErr.Add(type.GetFullMetadataName());
                }
                hasErrHelper |= IsErrHelperRead(errRead);
            }
        }
        return new ProjectErrReads(hasErrHelper, typesReadingErr);
    }

    private static bool IsNonReadingMemberAccess(VBSyntax.IdentifierNameSyntax errName) =>
        errName.Parent is VBSyntax.MemberAccessExpressionSyntax memberAccess && memberAccess.Expression == errName &&
        NonReadingMembers.Contains(memberAccess.Name.Identifier.ValueText, StringComparer.OrdinalIgnoreCase);

    private static bool IsInformationErr(ISymbol symbol) =>
        symbol is IMethodSymbol { Name: "Err" } method && method.ContainingType?.GetFullMetadataName() == "Microsoft.VisualBasic.Information";

    private static bool IsErrHelperRead(VBSyntax.IdentifierNameSyntax errRead)
    {
        if (errRead.GetAncestor<VBSyntax.CatchBlockSyntax>() != null) return false;
        var method = errRead.GetAncestor<VBSyntax.MethodBlockBaseSyntax>();
        return method == null || !OnErrorModel.ContainsOnErrorOrResume(method);
    }
}
