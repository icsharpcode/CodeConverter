# Code Converter [![Build Status](https://icsharpcode.visualstudio.com/icsharpcode-pipelines/_apis/build/status/icsharpcode.CodeConverter?branchName=master)](https://icsharpcode.visualstudio.com/icsharpcode-pipelines/_build/latest?definitionId=2&branchName=master)

Convert code from VB.NET to C# and vice versa via Roslyn using a [Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=SharpDevelopTeam.CodeConverter) or [Online snippet converter](https://codeconverter.icsharpcode.net/)

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

Currently, the VB -> C# conversion quality is higher than the C# -> VB conversion quality. This is due to demand of people raising issues and supply of developers willing to fix them. But we're very happy to support developers who want to contribute to either conversion direction.

## Other ways to use the converter
* VSIX inside artifact drop from latest CI builds (potentially less stable): https://icsharpcode.visualstudio.com/icsharpcode-pipelines/_build/latest?definitionId=2&branchName=master&status=succeeded

* Online snippet converter: [https://codeconverter.icsharpcode.net/](https://codeconverter.icsharpcode.net/) (less accurate due to lack of project context)

* NuGet package: [https://www.nuget.org/packages/ICSharpCode.CodeConverter/](https://www.nuget.org/packages/ICSharpCode.CodeConverter/)

  * Check out the [CodeConversion class](https://github.com/icsharpcode/CodeConverter/blob/8226313a8d46d5dd73bd35f07af2212e6155d0fd/Vsix/CodeConversion.cs#L226) in the VSIX project.
  * Or check out the [ConverterController](https://github.com/icsharpcode/CodeConverter/blob/master/CodeConverter.Web/ConverterController.cs) for a more web-focused API.

## Building/running from source
1. Ensure you have [.NET Core SDK 3.1+](https://dotnet.microsoft.com/download/dotnet-core/3.1)
2. Open the solution in Visual Studio 2017+
3. To run the website, set CodeConverter.Web as the startup project
4. To run the Visual Studio extension, set Vsix as the startup project
   * A new instance of Visual Studio will open with the extension installed

##  History
This was previously part of [Refactoring Essentials](https://github.com/icsharpcode/RefactoringEssentials). However, because of the way analyzers are tied to Visual Studio and Roslyn versions
made it super-hard to co-evolve the code converter bits. That is why we teased the converters out and they are now a self-contained entity.

## More screenshots
<p float="left">
  <img src="https://github.com/icsharpcode/CodeConverter/raw/master/.github/img/solution.png" width="49%" />
  <img src="https://github.com/icsharpcode/CodeConverter/raw/master/.github/img/vbToCsFile.png" width="49%" /> 
  <img src="https://github.com/icsharpcode/CodeConverter/raw/master/.github/img/vbToCsProject.png" width="49%" /> 
  <img src="https://github.com/icsharpcode/CodeConverter/raw/master/.github/img/csToVbProject.png" width="49%" /> 
</p>
