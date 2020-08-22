# Code Converter [![Build Status](https://icsharpcode.visualstudio.com/icsharpcode-pipelines/_apis/build/status/icsharpcode.CodeConverter?branchName=master)](https://icsharpcode.visualstudio.com/icsharpcode-pipelines/_build?definitionId=2&statusFilter=succeeded&repositoryFilter=2&branchFilter=32)

Convert code from VB.NET to C# and vice versa using Roslyn - all free and open source:
* [Visual Studio extension](https://marketplace.visualstudio.com/items?itemName=SharpDevelopTeam.CodeConverter)
* Command line `dotnet tool install ICSharpCode.CodeConverter.codeconv --global`
* [Online snippet converter](https://codeconverter.icsharpcode.net/)
* [Nuget library](https://www.nuget.org/packages/ICSharpCode.CodeConverter/) (this underpins all other free converters you'll find online)

## Visual Studio Extension
Adds context menu items to convert projects/files between VB.NET and C#. See the [wiki documentation](https://github.com/icsharpcode/CodeConverter/wiki) for help using it.

Download from [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=SharpDevelopTeam.CodeConverter) (Requires VS 2017 15.7+)

* Flexible: Convert a small selection, or a whole solution in one go, in either direction.
* Accurate: Full project context (through Roslyn) is used to get the most accurate conversion.
* Safe: Conversion runs entirely locally - your code doesn't leave your machine.
* Completely free and open source [GitHub](https://github.com/icsharpcode/CodeConverter#code-converter-) project.
* Integrated: Uses the Output window to show conversion progress / summary.
* Actively developed: User feedback helps us continuously strive for a more accurate conversion.

<p>
<img title="Selected text can be converted" alt="Selected text conversion context menu" src="https://github.com/icsharpcode/CodeConverter/raw/master/.github/img/vbToCsSelection.png" />
</p>

## Contributing
Let us know what needs improving. If you want to get involved in writing the code yourself, even better! We've already had code contributions from several first time GitHub contributors, so don't be shy! See [Contributing.md](https://github.com/icsharpcode/CodeConverter/blob/master/.github/CONTRIBUTING.md) for more info.

Currently, the VB -> C# conversion quality is higher than the C# -> VB conversion quality. This is due to demand of people raising issues and supply of developers willing to fix them. But we're very happy to support developers who want to contribute to either conversion direction. Visual Basic will have support for some project types on initial versions of .NET 5, but won't be getting new features according to the [.NET Team Blog](https://devblogs.microsoft.com/vbteam/visual-basic-support-planned-for-net-5-0/).

## Other ways to use the converter
* Latest CI build (potentially less stable):
  * [See latest build](https://icsharpcode.visualstudio.com/icsharpcode-pipelines/_build?definitionId=2&statusFilter=succeeded&repositoryFilter=2&branchFilter=32)
  * Uninstall current version, then install VSIX file inside "1 published" artifact
* Integrating the NuGet library
  * Check out the [CodeConversion class](https://github.com/icsharpcode/CodeConverter/blob/8226313a8d46d5dd73bd35f07af2212e6155d0fd/Vsix/CodeConversion.cs#L226) in the VSIX project.
  * Or check out the [ConverterController](https://github.com/icsharpcode/CodeConverter/blob/master/Web/ConverterController.cs) for a more web-focused API.

## Building/running from source
1. Ensure you have [.NET Core SDK 3.1+](https://dotnet.microsoft.com/download/dotnet-core/3.1)
2. Open the solution in Visual Studio 2017+
3. To run the website, set CodeConverter.Web as the startup project
4. To run the Visual Studio extension, set Vsix as the startup project
   * A new instance of Visual Studio will open with the extension installed

##  History
A spiritual successor of the code conversion within [SharpDevelop](https://github.com/icsharpcode/SharpDevelop) and later part of [Refactoring Essentials](https://github.com/icsharpcode/RefactoringEssentials), the code converter was separated out to avoid difficulties with different Visual Studio and Roslyn versions.

## More screenshots
<p float="left">
  <img src="https://github.com/icsharpcode/CodeConverter/raw/master/.github/img/solution.png" width="49%" />
  <img src="https://github.com/icsharpcode/CodeConverter/raw/master/.github/img/vbToCsFile.png" width="49%" /> 
  <img src="https://github.com/icsharpcode/CodeConverter/raw/master/.github/img/vbToCsProject.png" width="49%" /> 
  <img src="https://github.com/icsharpcode/CodeConverter/raw/master/.github/img/csToVbProject.png" width="49%" /> 
</p>
