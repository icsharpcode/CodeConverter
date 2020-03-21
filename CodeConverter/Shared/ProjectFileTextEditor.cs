using System.Text.RegularExpressions;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class ProjectFileTextEditor
    {
        /// <summary>
        /// Hide pre-conversion files, and ensure files we've just created aren't hidden
        /// </summary>
        public static string WithUpdatedDefaultItemExcludes(string s, string extensionNotToExclude, string extensionToExclude)
        {
            string verbatimExcludeToRemove = Regex.Escape($@"$(ProjectDir)**\*.{extensionNotToExclude}");
            var matchDefaultItemExcludes =
                new Regex($@"(<DefaultItemExcludes>.*){verbatimExcludeToRemove}(.*<\/DefaultItemExcludes>)");
            if (matchDefaultItemExcludes.IsMatch(s)) {
                s = matchDefaultItemExcludes.Replace(s, $@"$1$(ProjectDir)**\*.{extensionToExclude}$2", 1);
            } else {
                var firstPropertyGroupEnd = new Regex(@"(\s*</PropertyGroup>)");
                s = firstPropertyGroupEnd.Replace(s,
                    "\r\n" + $@"    <DefaultItemExcludes>$(DefaultItemExcludes);$(ProjectDir)**\*.{extensionToExclude}</DefaultItemExcludes>$1", 1);
            }

            return s;
        }
    }
}