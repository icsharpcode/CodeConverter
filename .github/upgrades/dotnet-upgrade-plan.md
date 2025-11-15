# .NET 10 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade CSharpNetStandardLib/CSharpNetStandardLib.csproj
4. Upgrade ConsoleApp2/CSharpConsoleApp.csproj
5. Upgrade ConsoleApp1/VisualBasicConsoleApp.vbproj

## Settings

### Project upgrade details

#### CSharpNetStandardLib/CSharpNetStandardLib.csproj modifications

Project properties changes:
  - Target framework should be changed from `netstandard2.0` to `net10.0`

#### ConsoleApp2/CSharpConsoleApp.csproj modifications

Project file needs to be converted to SDK-style format.

Project properties changes:
  - Target framework should be changed from `.NETFramework,Version=v4.8` to `net10.0`

#### ConsoleApp1/VisualBasicConsoleApp.vbproj modifications

Project file needs to be converted to SDK-style format.

Project properties changes:
  - Target framework should be changed from `.NETFramework,Version=v4.8` to `net10.0`
