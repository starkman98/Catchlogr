<#
.SYNOPSIS
Checks local backend or Windows Mobile prerequisites without attempting repairs.
.EXAMPLE
./scripts/diagnose-environment.ps1 -Profile Mobile
#>
[CmdletBinding()]
param(
    [ValidateSet('Backend', 'Mobile')]
    [string] $Profile = 'Backend',
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)
$ErrorActionPreference = 'Stop'
try {
    Import-Module (Join-Path $PSScriptRoot 'Environment.psm1') -Force
    Write-Host "Environment diagnostics: $Profile"
    $results = @(Get-EnvironmentDiagnostics -RepositoryRoot $RepositoryRoot -Profile $Profile)
    foreach ($result in $results) { Write-Host ("{0}: {1} - {2}" -f $result.Id, $result.Status, $result.Detail) }
    if (@($results | Where-Object Status -eq 'Blocked').Count -gt 0) { exit 1 }
    Write-Host 'Inspected prerequisites passed; build/test readiness remains unverified.'
    exit 0
}
catch {
    Write-Error $_ -ErrorAction Continue
    exit 1
}
