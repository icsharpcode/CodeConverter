# Code Converter [![Build status](https://ci.appveyor.com/api/projects/status/w9x7r8b9otds16oj/branch/master?svg=true)](https://ci.appveyor.com/project/icsharpcode/codeconverter/branch/master) 

Convert code from VB.NET to C# and vice versa using Roslyn

## Visual Studio Extension
Adds context menu items to convert projects/files between VB.NET and C#. See the [wiki documentation](https://github.com/icsharpcode/CodeConverter/wiki) for help using it.

[Download from Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=SharpDevelopTeam.CodeConverter)

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

## Other ways to use the converter
* Extension "nightly" developer builds (potentially less stable and more effort to update): https://ci.appveyor.com/project/icsharpcode/codeconverter/branch/master

* Online snippet converter: [https://codeconverter.icsharpcode.net/](https://codeconverter.icsharpcode.net/) (less accurate due to lack of project context)

* NuGet package: [https://www.nuget.org/packages/ICSharpCode.CodeConverter/](https://www.nuget.org/packages/ICSharpCode.CodeConverter/)

  * Check out the [CodeConversion class](https://github.com/icsharpcode/CodeConverter/blob/master/Vsix/CodeConversion.cs#L188) in the VSIX project.
  * Or check out the [ConverterController](https://github.com/icsharpcode/CodeConverter/blob/master/Web/Controllers/ConverterController.cs) for a more web-focused API.

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
