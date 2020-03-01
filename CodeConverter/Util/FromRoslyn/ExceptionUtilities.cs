using System;

namespace ICSharpCode.CodeConverter.Util.FromRoslyn
{
    /// <remarks>
    /// From https://github.com/dotnet/roslyn/blob/159707383710936bc0730a25be652081a2350878/src/Compilers/Core/Portable/InternalUtilities/ExceptionUtilities.cs#L12
    /// </remarks>
    internal static class ExceptionUtilities
    {
        /// <summary>
        /// Creates an <see cref="InvalidOperationException"/> with information about an unexpected value.
        /// </summary>
        /// <param name="o">The unexpected value.</param>
        /// <returns>The <see cref="InvalidOperationException"/>, which should be thrown by the caller.</returns>
        internal static Exception UnexpectedValue(object o)
        {
            string output = string.Format("Unexpected value '{0}' of type '{1}'", o, (o != null) ? o.GetType().FullName : "<unknown>");

            // We do not throw from here because we don't want all Watson reports to be bucketed to this call.
            return new InvalidOperationException(output);
        }

        internal static Exception Unreachable {
            get { return new InvalidOperationException("This program location is thought to be unreachable."); }
        }
    }
}

