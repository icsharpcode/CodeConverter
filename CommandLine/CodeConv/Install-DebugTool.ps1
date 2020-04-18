Param($args = $null)

Push-Location "$PSScriptRoot\bin\Debug"

dotnet new tool-manifest --force
dotnet tool install ICSharpCode.CodeConverter.CodeConv --add-source .

dotnet codeconv $args