<#
.SYNOPSIS
Previews relevant checks for local changes and optionally runs them.
.DESCRIPTION
Includes staged, unstaged, and untracked files. BaseRef also includes committed
changes since the merge base. Preview does not restore, build, test, or fetch.
.EXAMPLE
./scripts/validate-change.ps1 -BaseRef origin/dev
.EXAMPLE
./scripts/validate-change.ps1 -Run
#>
[CmdletBinding()]
param(
    [string] $BaseRef,
    [switch] $Run,
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
try {
    Import-Module (Join-Path $PSScriptRoot 'Validation.psm1') -Force
    $plan = Get-ValidationPlan -RepositoryRoot $RepositoryRoot -BaseRef $BaseRef
    if ($plan.Paths.Count -eq 0) { Write-Host 'No changed files; no checks selected.'; exit 0 }
    Write-Host 'Changed files:'
    $plan.Paths | ForEach-Object { Write-Host "  $_" }
    Write-Host 'Selected checks (normal incremental restore/build):'
    foreach ($check in $plan.Checks) {
        Write-Host ("  {0}: {1} {2}" -f $check.Id, $check.Command, ($check.Arguments -join ' '))
        Write-Host ("    Because: {0}" -f ($check.Reasons -join ', '))
    }
    foreach ($item in $plan.Prerequisites) { Write-Host "Prerequisite: $item" }
    foreach ($item in $plan.Manual) { Write-Host "Manual review required: $item" }
    if (-not $Run) { Write-Host 'Preview only. Use -Run to execute automated checks.'; exit 0 }
    $results = @(Invoke-ValidationPlan -Plan $plan)
    foreach ($result in $results) {
        Write-Host ("{0}: {1} ({2}s) {3}" -f $result.Id, $result.Status, $result.Seconds, $result.Detail)
    }
    if (@($results | Where-Object Status -ne 'Passed').Count -gt 0) { exit 1 }
    if ($plan.Manual.Count -gt 0) { Write-Host 'Automated checks passed; manual review is still required.' }
    else { Write-Host 'Selected automated checks passed; review behavior and broaden checks where needed.' }
    exit 0
}
catch {
    Write-Error $_ -ErrorAction Continue
    exit 1
}
