# codeconv

```
dotnet tool install codeconv -g
```

.NET Core 3.1 Tool 

```
codeconv -h

Convert code from VB.NET to C# or C# to VB.NET

Usage: codeconv [arguments] [options]

Arguments:
  Solution path                                       The solution containing project(s) to be converted.

Options:
  -h|--help                                           Show help information
  -i|--include                                        A regex matching project file paths to convert
  -e|--exclude                                        A regex matching project file paths to exclude from conversion
  -t|--target-language <CS | VB>                      The language to convert to.
  -o|--output-directory                               Directory to be wiped, and used for output.
  -b|--best-effort                                     Overrides warnings about compilation issues with input, and attempts a best effort conversion anyway
  -p|--property <Platform=x64;Configuration=Release>  Set or override the specified project-level properties, where name is the property name and value is the property value. Specify each property separately, or use a semicolon or comma to separate multiple properties.

Remarks:
  Converts all projects in a solution from VB.NET to C#.
  See https://github.com/icsharpcode/CodeConverter for the source code, issues, Visual Studio extension and other info.
```
