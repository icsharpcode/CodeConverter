using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.CodeConverter.Util
{
#if NR6
    public
#endif
    internal static class StringExtensions
    {
        public static string ConsistentNewlines(this string str)
        {
            return str.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        public static bool LooksLikeInterfaceName(this string name)
        {
            return name.Length >= 3 && name[0] == 'I' && Char.IsUpper(name[1]) && Char.IsLower(name[2]);
        }

        public static bool LooksLikeTypeParameterName(this string name)
        {
            return name.Length >= 3 && name[0] == 'T' && Char.IsUpper(name[1]) && Char.IsLower(name[2]);
        }

        private static readonly Func<char, char> s_toLower = Char.ToLower;
        private static readonly Func<char, char> s_toUpper = Char.ToUpper;

        public static string ToPascalCase(
            this string shortName,
            bool trimLeadingTypePrefix = true)
        {
            return ConvertCase(shortName, trimLeadingTypePrefix, s_toUpper);
        }

        public static string ToCamelCase(
            this string shortName,
            bool trimLeadingTypePrefix = true)
        {
            return ConvertCase(shortName, trimLeadingTypePrefix, s_toLower);
        }

        private static string ConvertCase(
            this string shortName,
            bool trimLeadingTypePrefix,
            Func<char, char> convert)
        {
            // Special case the common .net pattern of "IFoo" as a type name.  In this case we
            // want to generate "foo" as the parameter name.
            if (!String.IsNullOrEmpty(shortName)) {
                if (trimLeadingTypePrefix && (shortName.LooksLikeInterfaceName() || shortName.LooksLikeTypeParameterName())) {
                    return convert(shortName[1]) + shortName.Substring(2);
                }

                if (convert(shortName[0]) != shortName[0]) {
                    return convert(shortName[0]) + shortName.Substring(1);
                }
            }

            return shortName;
        }

        // String isn't IEnumerable<char> in the current Portable profile.
        internal static bool All(this string arg, Predicate<char> predicate)
        {
            foreach (char c in arg) {
                if (!predicate(c)) {
                    return false;
                }
            }

            return true;
        }

        public static string ReplaceEnd(this string originalContainingReplacement, KeyValuePair<string, string> replacement)
        {
            return originalContainingReplacement.Substring(0, originalContainingReplacement.Length - replacement.Key.Length) + replacement.Value;
        }

        public static string WithHalfWidthLatinCharacters(this string str)
        {
            return str.Normalize(NormalizationForm.FormKD);
        }
    }
}
