using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Entry point for converting a method body that contains <c>On Error</c> or <c>Resume</c>. See README.md in this directory.
/// </summary>
/// <remarks>
/// Each shape in <see cref="OnErrorShapeKind"/> gets an emitter here; until it has one, its methods keep the output of <see cref="PendingShapeFallback"/>.
/// </remarks>
internal static class OnErrorMethodBodyConverter
{
    /// <returns>The converted body, or null when the statements should be converted one by one as usual.</returns>
    public static async Task<IReadOnlyCollection<StatementSyntax>> ConvertOrNullAsync(OnErrorConversionContext context, IReadOnlyCollection<VBSyntax.StatementSyntax> statements)
    {
        var model = OnErrorModel.Create(statements, context.SemanticModel);
        var shape = OnErrorShapeSelector.Select(model, context.SemanticModel);
        switch (shape.Kind) {
            case OnErrorShapeKind.None:
                return null;
            case OnErrorShapeKind.Structured:
                return await new StructuredShapeEmitter(context, model, shape.Structured, await CreateErrStatementsAsync(context)).EmitAsync();
            case OnErrorShapeKind.ResumeNext or OnErrorShapeKind.Dynamic when ResumeNextShapeEmitter.CanEmit(model, context.SemanticModel):
                return await new ResumeNextShapeEmitter(context, model, await CreateErrStatementsAsync(context)).EmitAsync();
            case OnErrorShapeKind.HandlerFunction when HandlerFunctionPlanner.TryPlan(model, context.SemanticModel) is { } plan:
                return await new HandlerFunctionShapeEmitter(context, model, plan, await CreateErrStatementsAsync(context)).EmitAsync();
            default:
                return await PendingShapeFallback.ConvertOrNullAsync(statements, context.ConvertAsync);
        }
    }

    private static async Task<ErrStatements> CreateErrStatementsAsync(OnErrorConversionContext context) =>
        new(await context.CommonConversions.ErrUsage.IsMaintainedInAsync(context.ContainingType), context.ExtraUsingDirectives);
}
