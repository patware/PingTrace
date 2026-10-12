#requires -Version 7.0
[CmdletBinding()]
param(
  [string] $PackageDirectory,
  [string] $ExpectedCommit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-DotNet {
    & dotnet @args | Out-Host

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet failed with exit code $LASTEXITCODE."
    }
}

function Read-ZipText {
    param(
        [IO.Compression.ZipArchive] $Archive,
        [string] $Name
    )

    $entry = $Archive.GetEntry($Name)

    if ($null -eq $entry -or $entry.Length -eq 0) {
        throw "Missing or empty package entry: $Name"
    }

    $reader = [IO.StreamReader]::new($entry.Open())

    try {
        return $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
}

function Assert-ZipEntry {
    param(
        [IO.Compression.ZipArchive] $Archive,
        [string] $Name
    )

    $entry = $Archive.GetEntry($Name)

    if ($null -eq $entry -or $entry.Length -eq 0) {
        throw "Missing or empty package entry: $Name"
    }
}

# TODO: Add a test that the package can be consumed by a new project.
