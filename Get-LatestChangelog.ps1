Param(
    [string]$Path = 'CHANGELOG.md'
)

if (-not [System.IO.Path]::IsPathRooted($Path)) {
    $Path = Join-Path $PSScriptRoot $Path
}

if (-not (Test-Path -Path $Path)) {
    throw "Changelog not found at '$Path'."
}

$lines = Get-Content -Path $Path
$inSection = $false
$collected = New-Object System.Collections.Generic.List[string]

foreach ($line in $lines) {
    if ($line -match '^## \[') {
        if (-not $inSection) {
            if ($line -match '^## \[Unreleased\]') {
                continue
            }
            $inSection = $true
        }
        elseif ($inSection) {
            break
        }
    }

    if ($inSection) {
        $collected.Add($line) | Out-Null
    }
}

if (-not $inSection -or $collected.Count -eq 0) {
    throw "No released changelog section found in '$Path'."
}

$collected | Write-Output
