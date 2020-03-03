#nullable enable
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util.FromRoslyn
{
    /// <remarks>
    /// From Microsoft.CodeAnalysis.Shared.Extensions
    /// </remarks>
    internal static partial class IMethodSymbolExtensions
    {
        public static bool CompatibleSignatureToDelegate(this IMethodSymbol method, INamedTypeSymbol delegateType)
        {
            var invoke = delegateType.DelegateInvokeMethod;
            if (invoke == null) {
                // It's possible to get events with no invoke method from metadata.  We will assume
                // that no method can be an event handler for one.
                return false;
            }

            if (method.Parameters.Length != invoke.Parameters.Length) {
                return false;
            }

            if (method.ReturnsVoid != invoke.ReturnsVoid) {
                return false;
            }

            if (!method.ReturnType.InheritsFromOrEquals(invoke.ReturnType)) {
                return false;
            }

            for (var i = 0; i < method.Parameters.Length; i++) {
                if (!invoke.Parameters[i].Type.InheritsFromOrEquals(method.Parameters[i].Type)) {
                    return false;
                }
            }

            return true;
        }
    }
}