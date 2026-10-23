# Read-only prerequisite probes shared by the entry point and fixture tests.
Set-StrictMode -Version Latest

function Get-EnvironmentDiagnostics {
    [CmdletBinding()]
    param(
        [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
        [ValidateSet('Backend', 'Mobile')]
        [string] $Profile = 'Backend',
        [scriptblock] $CommandProbe = {
            param($Command, $CommandArguments)
            if (-not (Get-Command $Command -CommandType Application -ErrorAction SilentlyContinue)) {
                return [pscustomobject]@{ ExitCode = -1; Output = '' }
            }
            # Do not print raw environment/errors: they may contain private paths or feeds.
            try {
                $output = & $Command @CommandArguments 2>&1
                return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($output -join "`n") }
            }
            catch { return [pscustomobject]@{ ExitCode = -1; Output = '' } }
        },
        [bool] $IsWindowsHost = ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT)
    )

    $root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
    $results = [Collections.Generic.List[object]]::new()
    Push-Location -LiteralPath $root
    try {
        $git = & $CommandProbe 'git' @('--version')
        $status = if ($git.ExitCode -eq 0) { 'Passed' } else { 'Blocked' }
        $results.Add([pscustomobject]@{ Id = 'git'; Status = $status; Detail = 'Git must be available for change selection. If blocked, check installation or tool permissions.' })

        $settingsPath = Join-Path $root 'global.json'
        if (-not (Test-Path -LiteralPath $settingsPath -PathType Leaf)) { throw 'RepositoryRoot must contain global.json.' }
        $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
        $sdk = & $CommandProbe 'dotnet' @('--version')
        # Let the CLI resolve global.json, including roll-forward, from the repo root.
        $sdkReady = $sdk.ExitCode -eq 0 -and $sdk.Output.Trim() -match '^\d+\.\d+\.\d+(-[\w.-]+)?$'
        if ($sdkReady) {
            $detail = "Selected SDK $($sdk.Output.Trim()); global.json requests $($settings.sdk.version) with $($settings.sdk.rollForward) roll-forward."
        }
        else {
            $detail = 'No usable SDK selected. Run dotnet --version from the repo root; distinguish SDK/global.json mismatch from tool permissions. Install a compatible SDK only after identifying the cause.'
        }
        $status = if ($sdkReady) { 'Passed' } else { 'Blocked' }
        $results.Add([pscustomobject]@{ Id = 'sdk'; Status = $status; Detail = $detail })

        if ($Profile -eq 'Mobile') {
            $status = if ($IsWindowsHost) { 'Passed' } else { 'Blocked' }
            $results.Add([pscustomobject]@{ Id = 'windows'; Status = $status; Detail = 'The Mobile validation profile requires Windows.' })
            if ($sdkReady -and $IsWindowsHost) {
                $workloads = & $CommandProbe 'dotnet' @('workload', 'list')
                $ready = $workloads.ExitCode -eq 0 -and $workloads.Output -match '(?m)^\s*maui(-windows)?\s'
                $status = if ($ready) { 'Passed' } else { 'Blocked' }
                $results.Add([pscustomobject]@{ Id = 'workload'; Status = $status; Detail = 'MAUI Windows workload must be listed by dotnet workload list. If blocked, inspect that command before considering installation.' })
            }
            else {
                $results.Add([pscustomobject]@{ Id = 'workload'; Status = 'Info'; Detail = 'Workload inspection skipped until Windows and SDK prerequisites are available.' })
            }
        }

        $projects = if ($Profile -eq 'Backend') {
            @('tests/Catchlogr.Tests', 'tests/Catchlogr.Api.IntegrationTests')
        }
        else { @('tests/Catchlogr.Mobile.Tests', 'src/Catchlogr.Mobile') }
        foreach ($project in $projects) {
            $present = Test-Path -LiteralPath (Join-Path $root "$project/obj/project.assets.json") -PathType Leaf
            $detail = if ($present) { 'Restore assets exist; presence does not prove freshness or available runtime packs.' }
                else { 'Restore assets absent; a normal validation run will restore. Network access or cached dependencies may be needed.' }
            $results.Add([pscustomobject]@{ Id = $project; Status = 'Info'; Detail = $detail })
        }
        $results.Add([pscustomobject]@{ Id = 'limits'; Status = 'Info'; Detail = 'No restore, build, tests, network probes, repairs, installs, or cache deletion performed. NuGet access, runtime packs, generated-import correctness, and build/test permissions remain unverified. See docs/VALIDATION.md for recovery guidance.' })
    }
    finally { Pop-Location }
    return $results.ToArray()
}

Export-ModuleMember -Function Get-EnvironmentDiagnostics
