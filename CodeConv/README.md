# codeconv

```
dotnet tool install ICSharpCode.CodeConverter.CodeConv -g
```

.NET 10 Tool 

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

The tool doesn't support netframework because dot net core's build process can't build net framework projects. The old version of this tool loaded a separate net framework command line referencing msbuild, but that was hard work to maintain. If there's high demand, it could be resurrected, but it's probably best for people to upgrade to a recent version of dot net first anyway.
