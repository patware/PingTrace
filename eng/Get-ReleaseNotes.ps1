#requires -Version 7.0
[CmdletBinding()]
param(
    [string] $Version,
    [string] $ChangelogPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = (Get-Content (Join-Path $root 'VERSION') -Raw).Trim()
}

if ([string]::IsNullOrWhiteSpace($ChangelogPath)) {
    $ChangelogPath = Join-Path $root 'CHANGELOG.md'
}

$content = Get-Content -LiteralPath $ChangelogPath -Raw
$escapedVersion = [regex]::Escape($Version)

# Accept:
# ## 0.0.0
# ## 0.0.0 - Pending publication
# ## 0.0.0 - 0000-00-00
#
# Stop at the next level-two heading, preserving level-three sections.
$pattern = '(?ms)^##[ \t]+' + $escapedVersion +
    '(?:[ \t]+-[^\r\n]+)?[ \t]*\r?\n' +
    '(?<body>.*?)(?=^##[ \t]+|\z)'

$sections = @([regex]::Matches($content, $pattern))

if ($sections.Count -ne 1) {
    throw "Expected exactly one changelog section for $Version; found $($sections.Count)."
}

if ([string]::IsNullOrWhiteSpace($sections[0].Groups['body'].Value)) {
    throw "The changelog section for $Version is empty."
}

Write-Output $sections[0].Value.Trim()
