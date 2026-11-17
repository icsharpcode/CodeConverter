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
                var err = new ErrStatements(await context.CommonConversions.ErrUsage.IsMaintainedInAsync(context.ContainingType), context.ExtraUsingDirectives);
                return await new StructuredShapeEmitter(context, model, shape.Structured, err).EmitAsync();
            default:
                return await PendingShapeFallback.ConvertOrNullAsync(statements, context.ConvertAsync);
        }
    }
}
