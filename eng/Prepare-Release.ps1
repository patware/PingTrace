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

# TODO: Implement release preparation steps, such as updating the version number, generating release notes, and creating a release branch.
