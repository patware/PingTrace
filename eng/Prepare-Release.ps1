#requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Git {
    $output = & git @args

    if ($LASTEXITCODE -ne 0) {
        throw "git failed with exit code $LASTEXITCODE."
    }

    return $output
}

$root = Split-Path $PSScriptRoot -Parent

Push-Location $root

try {
    $status = @(Invoke-Git status --porcelain --untracked-files=all)

    if ($status.Count -ne 0) {
        throw 'Release preparation requires a clean working tree, including no untracked files. Commit your changes first.'
    }

    $commit = (Invoke-Git rev-parse HEAD).Trim()
    $version = (Get-Content 'VERSION' -Raw).Trim()

    # Fail before building if the release notes are not ready.
    $notes = & (Join-Path $PSScriptRoot 'Get-ReleaseNotes.ps1') `
        -Version $version

    $sdkVersion = & dotnet --version

    if ($LASTEXITCODE -ne 0) {
        throw 'Cannot determine the selected .NET SDK.'
    }

    $attempt = [Guid]::NewGuid().ToString('N')
    $releaseDirectory = Join-Path $root "artifacts/releases/v$version/$attempt"
    $packageDirectory = Join-Path $releaseDirectory 'packages'

    New-Item -ItemType Directory -Path $releaseDirectory | Out-Null

    $builtDirectory = & (Join-Path $root 'Build.ps1') `
        -OutputDirectory $packageDirectory

    $validation = & (Join-Path $PSScriptRoot 'Test-Packages.ps1') `
        -PackageDirectory $builtDirectory `
        -ExpectedCommit $commit

    # Ensure the recorded source identity still describes this attempt.
    $finalCommit = (Invoke-Git rev-parse HEAD).Trim()
    $finalStatus = @(Invoke-Git status --porcelain --untracked-files=all)

    if ($finalCommit -ne $commit -or $finalStatus.Count -ne 0) {
        throw 'The source checkout changed during release preparation. Start a new attempt.'
    }

    $notesPath = Join-Path $releaseDirectory 'release-notes.md'
    Set-Content -LiteralPath $notesPath -Value $notes -Encoding utf8

    $packageFiles = @(
        Get-ChildItem -LiteralPath $packageDirectory -File |
            Where-Object Extension -In @('.nupkg', '.snupkg') |
            Sort-Object Name
    )

    $files = @(
        foreach ($file in $packageFiles) {
            [ordered]@{
                path   = "packages/$($file.Name)"
                bytes  = $file.Length
                sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
            }
        }
    )

    $manifest = [ordered]@{
        schemaVersion = 1
        version       = $version
        tag           = "v$version"
        commit        = $commit
        sdk           = $sdkVersion.Trim()
        preparedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        validation    = [ordered]@{
            libraryTests  = 'passed'
            packageChecks = 'passed'
            consumerSmoke = 'passed'
            packageCount  = $validation.PackageCount
            symbolCount   = $validation.SymbolCount
        }
        releaseNotes = [ordered]@{
            path   = 'release-notes.md'
            sha256 = (Get-FileHash -LiteralPath $notesPath -Algorithm SHA256).Hash
        }
        files = $files
    }

    # Written last: absence means this attempt did not complete.
    $manifestPath = Join-Path $releaseDirectory 'manifest.json'

    $manifest |
        ConvertTo-Json -Depth 8 |
        Set-Content -LiteralPath $manifestPath -Encoding utf8

    Write-Host "`nRelease candidate prepared successfully."
    Write-Host "Commit:   $commit"
    Write-Host "Manifest: $manifestPath"

    Write-Output $releaseDirectory
}
finally {
    Pop-Location
}
