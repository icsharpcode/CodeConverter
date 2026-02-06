Param([Switch] $Release, $InitialArgs = @('--help'))

$subFolder = 'Debug'
if ($Release) { $subFolder = 'Release' }
Push-Location "$PSScriptRoot\bin\$subFolder"

dotnet new tool-manifest --force
dotnet tool install ICSharpCode.CodeConverter.CodeConv --add-source .

$InitialArgs = ,'codeconv' + $InitialArgs
& dotnet $InitialArgs