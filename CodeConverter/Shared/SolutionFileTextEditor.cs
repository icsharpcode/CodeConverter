using System.Text.RegularExpressions;

namespace ICSharpCode.CodeConverter.Common;

public class SolutionFileTextEditor : ISolutionFileTextEditor
{
    public static List<(string Find, string Replace, bool FirstOnly)> GetSolutionFileProjectReferenceReplacements(
        IEnumerable<(string Name, string RelativeProjPath)> projTuples, string sourceSolutionContents,
        IReadOnlyCollection<(string, string)> projTypeGuidMappings)
    {
        if (string.IsNullOrWhiteSpace(sourceSolutionContents)) return new List<(string Find, string Replace, bool FirstOnly)>();

        var projectReferenceReplacements = new List<(string Find, string Replace, bool FirstOnly)>();
        foreach ((string name, string relativeProjPath) in projTuples)
        {
            var newProjPath = @"""" + PathConverter.TogglePathExtension(relativeProjPath) + @""", ";
            var projPathEscaped = @"""" + Regex.Escape(relativeProjPath);
            var projName = GetProjNameWithoutFramework(name);

            (string oldType, string newType) = GetProjectTypeReplacement(projTypeGuidMappings, projName, projPathEscaped,
                sourceSolutionContents);
            (string oldGuid, string newGuid, bool firstOnly) = GetProjectGuidReplacement(projPathEscaped, sourceSolutionContents);

            var oldProjRefReplacement = oldType + projPathEscaped + @""", """ + oldGuid + @"""";
            var newProjRefReplacement = newType + newProjPath + @"""" + newGuid + @"""";

            projectReferenceReplacements.Add((oldProjRefReplacement, newProjRefReplacement, false));

            // this is needed for the guid replacement in the SolutionConfigurationPlatforms GlobalSection 
            projectReferenceReplacements.Add((oldGuid, newGuid, firstOnly));
        }

        return projectReferenceReplacements;
    }

    public List<(string Find, string Replace, bool FirstOnly)> GetProjectFileProjectReferenceReplacements(
        IEnumerable<(string Name, string RelativeProjPath, string ProjContents)> projTuples, string sourceSolutionContents)
    {
        var projectReferenceReplacements = new List<(string Find, string Replace, bool FirstOnly)>();
        foreach ((string _, string relativeProjPath, string projContents) in projTuples)
        {
            var escapedProjPath = Regex.Escape(relativeProjPath);
            var projRefRegex = new Regex(@"(\\|"")" + escapedProjPath);

            var projRefMatch = projRefRegex.Match(projContents);
            var characterBeforePath = projRefMatch.Groups[1].Value;

            var extendedEscProjPath = Regex.Escape(characterBeforePath) + escapedProjPath;
            var newProjPath = characterBeforePath + PathConverter.TogglePathExtension(relativeProjPath);

            projectReferenceReplacements.Add((extendedEscProjPath, newProjPath, false));
            if (string.IsNullOrWhiteSpace(sourceSolutionContents)) {
                continue;
            }

            projectReferenceReplacements.Add(GetProjectGuidReplacement(escapedProjPath, sourceSolutionContents));
        }

        return projectReferenceReplacements;
    }

    // Gets the project name without the framework specifier (in case of multiple target frameworks)
    private static string GetProjNameWithoutFramework(string projName)
    {
        return Regex.Replace(projName, @"\s\(net.*\)", "");
    }

    private static (string oldProjTypeReference, string newProjTypeReference) GetProjectTypeReplacement(
        IEnumerable<(string oldTypeGuid, string newTypeGuid)> projTypeGuidMappings, string projName, string projPath,
        string contents)
    {
        foreach ((string oldTypeGuid, string newTypeGuid) in projTypeGuidMappings)
        {
            var oldProjTypeReference = $@"Project\s*\(\s*""{oldTypeGuid}""\s*\)\s*=\s*""{projName}"", ";
            var projGuidRegex = new Regex($@"({oldProjTypeReference})({projPath})");
            var projTypeReference = projGuidRegex.Match(contents);
            if(!projTypeReference.Success) continue;

            var oldReference = Regex.Escape(projTypeReference.Groups[1].Value);
            var newProjTypeReference = $@"Project(""{newTypeGuid}"") = ""{projName}"", ";

            return (oldReference, newProjTypeReference);
        }

        return default;
    }

    private static (string Find, string Replace, bool FirstOnly) GetProjectGuidReplacement(string projPath,
        string contents)
    {
        var guidPattern = projPath + @""", ""({[0-9A-Fa-f\-]{32,36}})("")";
        var projGuidRegex = new Regex(guidPattern);
        var projGuidMatch = projGuidRegex.Match(contents);

        if (!projGuidMatch.Success) {
            throw new OperationCanceledException($"{nameof(guidPattern)} {guidPattern} doesn't match with" +
                                                 $" sourceSlnFileContents {contents}");
        }

        var oldGuid = projGuidMatch.Groups[1].Value;
        var newGuid = GetDeterministicGuidFrom(new Guid(oldGuid));
        return (oldGuid, newGuid.ToString("B").ToUpperInvariant(), false);
    }

    private static Guid GetDeterministicGuidFrom(Guid guidToConvert)
    {
        var codeConverterStaticGuid = new Guid("{B224816B-CC58-4FF1-8258-CA7E629734A0}");
        var deterministicNewBytes = codeConverterStaticGuid.ToByteArray().Zip(guidToConvert.ToByteArray(),
            (fromFirst, fromSecond) => (byte)(fromFirst ^ fromSecond));
        return new Guid(deterministicNewBytes.ToArray());
    }
}