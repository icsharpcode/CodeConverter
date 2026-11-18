param()

$ErrorActionPreference = 'Stop'

Write-Host "Creating global.json to enforce .NET 8 for MSBuild"
$globalJson = '{"sdk":{"version":"8.0.100","rollForward":"latestFeature"}}'
[System.IO.File]::WriteAllText('global.json', $globalJson, [System.Text.Encoding]::UTF8)

Write-Host "Searching for project files under Tests/TestData..."
$projFiles = Get-ChildItem -Path Tests/TestData -Recurse -Include *.csproj,*.vbproj,*.fsproj -File -ErrorAction SilentlyContinue

if (-not $projFiles) {
    Write-Host "No project files found under Tests/TestData"
    exit 0
}

$changed = $false
foreach ($f in $projFiles) {
    $path = $f.FullName
    Write-Host "Processing: $path"

    # Use StreamReader to detect encoding and preserve it when writing back
    $sr = [System.IO.StreamReader]::new($path, $true)
    try {
        $content = $sr.ReadToEnd()
        $encoding = $sr.CurrentEncoding
    } finally {
        $sr.Close()
    }

    # Replace net10.0 and net10.0-windows with net8.0 / net8.0-windows
    $updated = [System.Text.RegularExpressions.Regex]::Replace($content, '<TargetFramework>net10\.0(-windows)?</TargetFramework>', '<TargetFramework>net8.0$1</TargetFramework>')

    if ($updated -ne $content) {
        Write-Host "Updating TargetFramework in: $path"
        # Write back preserving detected encoding and internal newlines
        [System.IO.File]::WriteAllText($path, $updated, $encoding)
        $changed = $true
    }
}

if ($changed) {
    Write-Host "Changes detected — committing to local repo so working tree is clean for tests"
    git config user.name "github-actions[bot]"
    if ($env:GITHUB_ACTOR) {
        git config user.email "$($env:GITHUB_ACTOR)@users.noreply.github.com"
    } else {
        git config user.email "actions@github.com"
    }
    git add -A
    git commit -m "CI: Update Tests/TestData TargetFramework -> net8.0 for .NET 8 run" || Write-Host "No commit created (maybe no staged changes)"
    Write-Host "Committed changes locally."
} else {
    Write-Host "No TargetFramework updates required."
}

Write-Host "Done."
