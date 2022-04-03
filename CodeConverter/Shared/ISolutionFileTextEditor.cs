﻿namespace ICSharpCode.CodeConverter.Shared;

using System.Collections.Generic;

public interface ISolutionFileTextEditor
{
    List<(string Find, string Replace, bool FirstOnly)> GetProjectFileProjectReferenceReplacements(
        IEnumerable<(string Name, string RelativeProjPath, string ProjContents)> projTuples, string sourceSolutionContents);
}