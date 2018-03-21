# Code Converter [![Build status](https://ci.appveyor.com/api/projects/status/w9x7r8b9otds16oj/branch/master?svg=true)](https://ci.appveyor.com/project/icsharpcode/codeconverter/branch/master) 

Convert code from C# to VB.NET and vice versa using Roslyn

## Using the code converter

* **Visual Studio Extension (recommended): [https://marketplace.visualstudio.com/items?itemName=SharpDevelopTeam.CodeConverter](https://marketplace.visualstudio.com/items?itemName=SharpDevelopTeam.CodeConverter)**

* Online snippet converter: [https://roslyncodeconverter.azurewebsites.net](https://roslyncodeconverter.azurewebsites.net) (less accurate due to lack of project context)

* Extension "nightly" developer builds (potentially less stable and more effort to update): https://ci.appveyor.com/project/icsharpcode/codeconverter/branch/master

## Developing against the Code Converter library (NuGet)

NuGet package: [https://www.nuget.org/packages/ICSharpCode.CodeConverter/](https://www.nuget.org/packages/ICSharpCode.CodeConverter/)

Check out the [ConverterController](https://github.com/icsharpcode/CodeConverter/blob/master/Web/Controllers/ConverterController.cs) in the Web project - this is the easiest place to get started.
Alternatively - with a bit of VS glue code - the [CodeConversion class](https://github.com/icsharpcode/CodeConverter/blob/master/Vsix/CodeConversion.cs) in the VSIX project.

##  History

It started as part of [Refactoring Essentials](https://github.com/icsharpcode/RefactoringEssentials). However, because of the way analyzers are tied to Visual Studio and Roslyn versions
made it super-hard to co-evolve the code converter bits. That is why we teased the converters out and they are now a self-contained entity.
