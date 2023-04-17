using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ICSharpCode.CodeConverter.Common;

internal static class ProjectFileTextEditor
{
    /// <summary>
    /// Hide pre-conversion files, and ensure files we've just created aren't hidden
    /// </summary>
    public static void WithUpdatedDefaultItemExcludes(XDocument xmlDoc, XNamespace xmlNs, string extensionNotToExclude, string extensionToExclude)
    {
        string verbatimExcludeToRemove = Regex.Escape($@"$(ProjectDir)**\*.{extensionNotToExclude}");
        var matchDefaultItemExcludes = new Regex($@"(.*){verbatimExcludeToRemove}(.*)");
        var defaultItemExcludes = xmlDoc.Descendants("DefaultItemExcludes").FirstOrDefault(e => matchDefaultItemExcludes.IsMatch(e.Value));
        if (defaultItemExcludes != null) {
            defaultItemExcludes.Value = matchDefaultItemExcludes.Replace(defaultItemExcludes.Value, $@"$1$(ProjectDir)**\*.{extensionToExclude}$2");
        } else {
            var firstPropertyGroup = xmlDoc.Descendants(xmlNs + "PropertyGroup").FirstOrDefault();
            defaultItemExcludes = new XElement(xmlNs + "DefaultItemExcludes", $"$(DefaultItemExcludes);$(ProjectDir)**\\*.{extensionToExclude}");
            firstPropertyGroup?.Add(defaultItemExcludes);
        }
    }
}