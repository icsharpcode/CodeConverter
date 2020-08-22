# codeconv

```
dotnet tool install ICSharpCode.CodeConverter.CodeConv -g
```

.NET Core 3.1 Tool 

```
codeconv -h

Convert code from VB.NET to C# or C# to VB.NET

Usage: codeconv [options] <Source solution path>

Arguments:
  Source solution path                         The solution containing project(s) to be converted.

Options:
  -h|--help                                    Show help information
  -i|--include                                 Regex matching project file paths to convert. Can be used multiple times
  -e|--exclude                                 Regex matching project file paths to exclude from conversion. Can be used
                                               multiple times
  -t|--target-language <CS | VB>               The language to convert to.
  -f|--force                                   Wipe the output directory before conversion
  --core-only                                  Force dot net core build if converting only .NET Core projects and seeing
                                               pre-conversion compile errors
  -b|--best-effort                             Overrides warnings about compilation issues with input, and attempts a
                                               best effort conversion anyway
  -o|--output-directory                        Empty or non-existent directory to copy the solution directory to, then
                                               write the output.
  -p|--build-property <Configuration=Release>  Set build properties in format: propertyName=propertyValue. Can be used
                                               multiple times

Remarks:
  Converts all projects in a solution from VB.NET to C#.
  Please backup / commit your files to source control before use.
  We recommend running the conversion in-place (i.e. not specifying an output directory) for best performance.
  See https://github.com/icsharpcode/CodeConverter for the source code, issues, Visual Studio extension and other info.
```

## Design

The tool can run in both netcore and netframework because:
-   The conversion needs to be able run in a dot net core process so it works cross platform
    -   So linux users can convert dot net core projects
-   The conversion needs to be able to run in a net framework process because:
    -   Dot net core msbuild can't load framework projects (i.e. 95% of the world's VB and C#)
    -   Dot net core processes can't load net framework msbuild ([not planned to change](https://github.com/icsharpcode/CodeConverter/blob/master/CommandLine/CodeConv.Shared/CodeConvProgram.cs#L73-L81))
    
## Future

The package is about 27MB. The size is made up by the Roslyn dependencies. It includes most of Roslyn, but not MSBuild (which must be installed on the machine). In future we could get Roslyn from the installation too like the VS extension does. It just needs careful management of versions. The dot net framework version can match the vs extension dependency versions. The dot net core one can depend on later versions.
