Param([Switch] $major, [Switch] $minor)
$incrementPart = 3
if ($minor) { $incrementPart = 2 }
if ($major) { $incrementPart = 1 }

# https://stackoverflow.com/a/9121679/1128762
function Get-FileEncoding($filePath) {  
    $sr = New-Object System.IO.StreamReader($filePath, (New-Object System.Text.UTF8Encoding $false), $true)
    [char[]] $buffer = new-object char[] 5
    $sr.Read($buffer, 0, 5) | Out-Null
    $encoding = [System.Text.Encoding] $sr.CurrentEncoding
    $sr.Close()
    return $encoding
}

function Get-VersionParts($version) {
    if ($version.Revision -ne -1) { $versionParts = 4; }
    elseif ($version.Build -ne -1) { $versionParts = 3; }
    else { $versionParts = 2; }
    return $versionParts;
}

function Create-Version([Version] $version, $versionMajorNew = $version.Major, $versionMinorNew = $version.Minor, $versionBuildNew = $version.Build, $versionRevisionNew = $version.Revision) {
    $versionParts = Get-VersionParts $version
    switch($versionParts){
       4 { return [Version]::new($versionMajorNew, $versionMinorNew, $versionBuildNew, $versionRevisionNew); }
       3 { return [Version]::new($versionMajorNew, $versionMinorNew, $versionBuildNew); }
       2 { return [Version]::new($versionMajorNew, $versionMinorNew); }
       1 { return [Version]::new($versionMajorNew); }
    }
}

function WriteAllText-PreservingEncoding($filePath, $contents) {
    [System.IO.File]::WriteAllText($filePath, $contents, (Get-FileEncoding $filePath))
}

function Increment-Version($version) {
    if ($incrementPart -eq 1 -Or $version.Minor -ge 9) { return Create-Version $version ($version.Major + 1) 0 0 0 }
    if ($incrementPart -eq 2 -Or $version.Build -ge 9) { return Create-Version $version $version.Major ($version.Minor + 1) 0 0}
    return Create-Version $version $version.Major $version.Minor ($version.Build + 1) 0
}

#Regex must contain 3 groups. 1: Text preceding version to replace, 2: Version number part to replace, 3: Text after version to replace
function Increment-VersionInFile($filePath, $find, $allowNoMatch=$false) {
    if (-not [System.IO.Path]::IsPathRooted($filePath)) { $filePath = Join-Path $PSScriptRoot $filePath }
    $contents = [System.IO.File]::ReadAllText($filePath)
    $fileName = [System.IO.Path]::GetFileName($filePath)

    $match = [RegEx]::Match($contents, $find)
    
    if (-not $match.Success -and $allowNoMatch) {
        Write-Host "Nothing to update for $fileName using $find"
        return;
    }
    
    $version = [Version] $match.groups["2"].Value
    $newVersion = Increment-Version $version

    Write-Host "Updating $fileName`: Replacing $version with $newVersion using $find"

    $contents = [RegEx]::Replace($contents, $find, ("`${1}" + $newVersion.ToString() + "`${3}"))
    WriteAllText-PreservingEncoding $filePath $contents
    return $newVersion
}

function Update-Changelog($filePath, $newVersion) {

    if (-not [System.IO.Path]::IsPathRooted($filePath)) { $filePath = Join-Path $PSScriptRoot $filePath }
    $date = get-date -format 'yyyy-MM-dd'
    $newReleaseFragment =
@"
## [Unreleased]


### Vsix


### VB -> C#


### C# -> VB


## [$newVersion] - $date
"@

    $contents = [System.IO.File]::ReadAllText($filePath)
    $contents = $contents.Replace('## [Unreleased]',$newReleaseFragment)
    WriteAllText-PreservingEncoding $filePath $contents
}

$newVersion = Increment-VersionInFile 'azure-pipelines.yml' '(buildVersion: .)(\d+\.\d+\.\d+)(\.\$)'
Increment-VersionInFile 'Vsix\source.extension.vsixmanifest' '(7e2a69d6-193b-4cdf-878d-3370d5931942" Version=")(\d+\.\d+\.\d+)(\.)' | Out-Null
Get-ChildItem -Recurse '*.csproj' | Where { -not $_.FullName.Contains("TestData")} | % {
    Increment-VersionInFile $_ '(\n    <Version>)(\d+\.\d+\.\d+)(<)' $true | Out-Null
    Increment-VersionInFile $_ '(\n    <FileVersion>)(\d+\.\d+\.\d+)(\.)'  $true | Out-Null
    Increment-VersionInFile $_ '(\n    <AssemblyVersion>)(\d+\.\d+\.\d+)(\.)' $true | Out-Null
}
$newVersionString = $newVersion.ToString(3)
Update-Changelog 'CHANGELOG.md' $newVersionString