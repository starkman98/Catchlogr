# Shared implementation for validate-change.ps1 and its dependency-free checks.
Set-StrictMode -Version Latest

function Invoke-ValidationGit {
    param([string] $RepositoryRoot, [string[]] $Arguments)

    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = 'git'
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.StandardOutputEncoding = [Text.Encoding]::UTF8
    $start.StandardErrorEncoding = [Text.Encoding]::UTF8
    # ProcessStartInfo.ArgumentList is unavailable in Windows PowerShell 5.1.
    $quoted = foreach ($argument in (@('-C', $RepositoryRoot) + $Arguments)) {
        '"' + ($argument -replace '(\\*)"', '$1$1\"' -replace '(\\+)$', '$1$1') + '"'
    }
    $start.Arguments = $quoted -join ' '
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    try {
        [void] $process.Start()
        $errors = $process.StandardError.ReadToEndAsync()
        $output = $process.StandardOutput.ReadToEnd()
        $process.WaitForExit()
        $errorText = $errors.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0) {
            throw "Git failed ($($process.ExitCode)): $errorText"
        }
        return $output
    }
    finally { $process.Dispose() }
}

function Get-ValidationPlan {
    [CmdletBinding()]
    param(
        [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
        [string] $BaseRef,
        [ValidateSet('Changed', 'Backend', 'Mobile')]
        [string] $Profile = 'Changed'
    )

    $root = (Invoke-ValidationGit $RepositoryRoot @('rev-parse', '--show-toplevel')).Trim()
    $paths = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $queries = @(
        ,@('diff', '--no-ext-diff', '--no-renames', '--name-only', '-z', '--')
        ,@('diff', '--cached', '--no-ext-diff', '--no-renames', '--name-only', '-z', '--')
        ,@('ls-files', '--others', '--exclude-standard', '-z')
    )
    if ($BaseRef) {
        $commit = (Invoke-ValidationGit $root @('rev-parse', '--verify', '--end-of-options', "$BaseRef^{commit}")).Trim()
        $mergeBase = (Invoke-ValidationGit $root @('merge-base', 'HEAD', $commit)).Trim()
        $queries += ,@('diff', '--no-ext-diff', '--no-renames', '--name-only', '-z', $mergeBase, 'HEAD', '--')
    }
    foreach ($query in $queries) {
        $output = Invoke-ValidationGit $root $query
        foreach ($path in $output.Split([char] 0)) {
            if ($path.Length -gt 0) { [void] $paths.Add($path) }
        }
    }

    # This is the executable path-to-check mapping. The guide owns its rationale.
    $rules = @(
        @{ Pattern = '\.md$'; Checks = @('docs') },
        @{ Pattern = '(^|/)(global\.json|NuGet\.Config|Directory\.[^/]+|packages\.lock\.json)$|\.(csproj|props|targets|sln|slnx)$'; Checks = @('unit', 'api', 'mobile'); Manual = 'Review dependency/build effects across project boundaries.' },
        @{ Pattern = '^src/Catchlogr\.(Contracts|Domain)/'; Checks = @('unit', 'api', 'mobile'); Manual = 'Review server, sync, and Mobile mappings and persisted-field compatibility.' },
        @{ Pattern = '^src/Catchlogr\.Application/'; Checks = @('unit', 'api') },
        @{ Pattern = '^src/Catchlogr\.Application/Interfaces/'; Checks = @('mobile'); Manual = 'Review consumers of the changed Application abstraction.' },
        @{ Pattern = '^src/Catchlogr\.Infrastructure/'; Checks = @('unit', 'api') },
        @{ Pattern = '^src/Catchlogr\.Infrastructure/Persistence/'; Checks = @(); Manual = 'Review migrations/snapshot and data preservation; validate schema changes on disposable local PostgreSQL.' },
        @{ Pattern = '^src/Catchlogr\.Api/'; Checks = @('unit', 'api') },
        @{ Pattern = '^src/Catchlogr\.Sync/'; Checks = @('unit'); Manual = 'Review Mobile/API mappings and retry, cursor, ordering, and photo-state effects; broaden checks if those boundaries change.' },
        @{ Pattern = '^src/Catchlogr\.Sync/(Entities|Abstractions)/'; Checks = @('mobile'); Manual = 'Verify local persisted-field upgrades and Mobile repository/client compatibility where affected.' },
        @{ Pattern = '^src/Catchlogr\.Web/'; Checks = @('unit') },
        @{ Pattern = '^src/Catchlogr\.Mobile/'; Checks = @('mobile') },
        @{ Pattern = '^src/Catchlogr\.Mobile/.*\.xaml(\.cs)?$|^src/Catchlogr\.Mobile/(Platforms|Resources)/'; Checks = @('mobile-build'); Manual = 'Build the affected platform target and manually exercise the changed UI/device flow; automated build covers Windows only.' },
        @{ Pattern = '^src/Catchlogr\.Mobile/Data/'; Checks = @(); Manual = 'For SQLite schema changes, exercise an upgrade from an existing local database; mocks do not prove compatibility.' },
        @{ Pattern = '^tests/Catchlogr\.Tests/'; Checks = @('unit') },
        @{ Pattern = '^tests/Catchlogr\.Api\.IntegrationTests/'; Checks = @('api') },
        @{ Pattern = '^tests/Catchlogr\.Mobile\.Tests/'; Checks = @('mobile') },
        @{ Pattern = '^scripts/(validate-change\.ps1|diagnose-environment\.ps1|Environment\.psm1|Validation\.psm1|tests/)'; Checks = @('helper', 'docs') },
        @{ Pattern = '^scripts/validate-docs\.ps1$'; Checks = @('docs') },
        @{ Pattern = '^\.github/workflows/|^deploy/|(^|/)(Dockerfile|compose[^/]*\.ya?ml)$'; Checks = @('unit', 'api', 'mobile', 'docs'); Manual = 'Review workflow triggers, required checks, deployment/service configuration, and docs/CI-CD.md; tests do not establish deployment correctness.' }
    )
    # Advisory ownership hints, independent of automated check selection.
    $guideRules = @(
        @{ Pattern = '^src/Catchlogr\.Sync/|^src/Catchlogr\.Mobile/(Data|Services/.*Sync)'; Guides = @('docs/SYNC_STRATEGY.md') },
        @{ Pattern = '^src/Catchlogr\.Mobile/'; Guides = @('docs/MOBILE_ARCHITECTURE.md') },
        @{ Pattern = '^src/Catchlogr\.Mobile/Configuration/|^src/Catchlogr\.Mobile/.*appsettings|^src/Catchlogr\.Mobile/Platforms/'; Guides = @('docs/MOBILE_CONFIGURATION.md') },
        @{ Pattern = '^src/.*(Weather|Moon)|^src/Catchlogr\.Application/Services/FishingTripService\.cs$'; Guides = @('docs/WEATHER_INTEGRATION.md') },
        @{ Pattern = '^src/.*(LocationSearch|LocationIq|LocationEndpoints)'; Guides = @('docs/LOCATION_SEARCH_INTEGRATION.md') },
        @{ Pattern = '^src/Catchlogr\.Web/|^src/.*(Identity|Email)|^src/Catchlogr\.Api/Program\.cs$'; Guides = @('docs/IDENTITY_EMAIL.md') },
        @{ Pattern = '^src/Catchlogr\.(Api|Contracts|Domain)/|^src/Catchlogr\.Infrastructure/Persistence/|(^|/)(global\.json|NuGet\.Config|Directory\.[^/]+)$|\.(csproj|sln|slnx)$'; Guides = @('README.md') },
        @{ Pattern = '^scripts/(validate-|diagnose-environment\.ps1|Environment\.psm1|Validation\.psm1|tests/)|^tests/'; Guides = @('docs/VALIDATION.md') },
        @{ Pattern = '^\.github/workflows/|^deploy/|(^|/)(Dockerfile|compose[^/]*\.ya?ml)$'; Guides = @('docs/CI-CD.md') }
    )
    $guideReasons = @{}
    foreach ($path in ($paths | Sort-Object)) {
        if ($path -match '\.md$') { continue }
        foreach ($rule in $guideRules) {
            if ($path -notmatch $rule.Pattern) { continue }
            foreach ($guide in $rule.Guides) {
                if (-not $guideReasons.ContainsKey($guide)) { $guideReasons[$guide] = [Collections.Generic.List[string]]::new() }
                if (-not $guideReasons[$guide].Contains($path)) { $guideReasons[$guide].Add($path) }
            }
        }
    }
    $documentationReview = foreach ($guide in ($guideReasons.Keys | Sort-Object)) {
        [pscustomobject]@{ Guide = $guide; Reasons = @($guideReasons[$guide].ToArray()); Changed = $paths.Contains($guide) }
    }
    $reasons = @{}
    $manual = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($path in ($paths | Sort-Object)) {
        $matched = $false
        foreach ($rule in $rules) {
            # Instruction/prose changes should never select application builds.
            if ($path -match '\.md$' -and $rule -ne $rules[0]) { continue }
            if ($path -match $rule.Pattern) {
                $matched = $true
                foreach ($id in $rule.Checks) {
                    if (-not $reasons.ContainsKey($id)) { $reasons[$id] = [Collections.Generic.List[string]]::new() }
                    if (-not $reasons[$id].Contains($path)) { $reasons[$id].Add($path) }
                }
                if ($rule.ContainsKey('Manual')) { [void] $manual.Add($rule.Manual) }
            }
        }
        if (-not $matched) {
            foreach ($id in @('docs', 'helper', 'unit', 'api', 'mobile')) {
                if (-not $reasons.ContainsKey($id)) { $reasons[$id] = [Collections.Generic.List[string]]::new() }
                $reasons[$id].Add($path)
            }
            [void] $manual.Add("Unmapped path '$path': conservative checks selected; review its effects and extend the mapping if needed.")
        }
    }
    # Profiles add minimum coverage; changed-file checks and manual items survive.
    $profileIds = switch ($Profile) {
        'Backend' { 'unit'; 'api' }
        'Mobile' { 'mobile'; 'mobile-build' }
    }
    foreach ($id in $profileIds) {
        if (-not $reasons.ContainsKey($id)) { $reasons[$id] = [Collections.Generic.List[string]]::new() }
        $reasons[$id].Add("$Profile profile")
    }
    if ($Profile -eq 'Mobile') {
        [void] $manual.Add('Windows build does not validate Android/iOS or device UI; exercise affected platform flows manually.')
    }
    $definitions = @(
        @{ Id = 'docs'; Command = (Join-Path $root 'scripts/validate-docs.ps1'); Arguments = @() },
        @{ Id = 'helper'; Command = (Join-Path $root 'scripts/tests/validate-change.tests.ps1'); Arguments = @() },
        @{ Id = 'unit'; Command = 'dotnet'; Arguments = @('test', 'tests/Catchlogr.Tests/Catchlogr.Tests.csproj') },
        @{ Id = 'api'; Command = 'dotnet'; Arguments = @('test', 'tests/Catchlogr.Api.IntegrationTests/Catchlogr.Api.IntegrationTests.csproj') },
        @{ Id = 'mobile'; Command = 'dotnet'; Arguments = @('test', 'tests/Catchlogr.Mobile.Tests/Catchlogr.Mobile.Tests.csproj') },
        @{ Id = 'mobile-build'; Command = 'dotnet'; Arguments = @('build', 'src/Catchlogr.Mobile/Catchlogr.Mobile.csproj', '-f', 'net10.0-windows10.0.19041.0') }
    )
    $checks = foreach ($definition in $definitions) {
        if ($reasons.ContainsKey($definition.Id)) {
            [pscustomobject]@{
                Id = $definition.Id
                Command = $definition.Command
                Arguments = $definition.Arguments
                Reasons = @($reasons[$definition.Id].ToArray())
            }
        }
    }
    $prerequisites = [Collections.Generic.List[string]]::new()
    if (@($checks | Where-Object Command -eq 'dotnet').Count -gt 0 -and -not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        $prerequisites.Add('Missing dotnet SDK; install the version compatible with global.json.')
    }
    if ($reasons.ContainsKey('mobile') -or $reasons.ContainsKey('mobile-build')) {
        if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { $prerequisites.Add('Mobile checks require Windows.') }
        $prerequisites.Add('Mobile requires the MAUI Windows workload; verified only when running checks.')
    }
    [pscustomobject]@{
        RepositoryRoot = $root
        BaseRef = $BaseRef
        Profile = $Profile
        Paths = @($paths | Sort-Object)
        Checks = @($checks)
        Manual = @($manual | Sort-Object)
        DocumentationReview = @($documentationReview)
        Prerequisites = @($prerequisites.ToArray())
    }
}

function Invoke-ValidationPlan {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Plan,
        # Injectable runner allows failure/prerequisite tests without real builds.
        [scriptblock] $CommandRunner = {
            param($Command, $CommandArguments)
            $global:LASTEXITCODE = 0
            & $Command @CommandArguments | Out-Host
            $succeeded = $?
            if ($global:LASTEXITCODE -ne 0) { return $global:LASTEXITCODE }
            if (-not $succeeded) { return 1 }
            return $global:LASTEXITCODE
        },
        [scriptblock] $PrerequisiteChecker = {
            param($Check)
            if ($Check.Command -ne 'dotnet') { return $null }
            if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { return 'dotnet SDK is missing.' }
            if ($Check.Id -in @('mobile', 'mobile-build')) {
                if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { return 'Mobile checks require Windows.' }
                $workloads = & dotnet workload list 2>&1
                if ($LASTEXITCODE -ne 0) { return 'Could not inspect installed MAUI workloads.' }
                if (($workloads -join "`n") -notmatch '(?m)^\s*maui(-windows)?\s') { return 'MAUI Windows workload is missing.' }
            }
            return $null
        }
    )
    $results = [Collections.Generic.List[object]]::new()
    Push-Location -LiteralPath $Plan.RepositoryRoot
    try {
        foreach ($check in $Plan.Checks) {
            $timer = [Diagnostics.Stopwatch]::StartNew()
            $status = 'Failed'
            $detail = ''
            try {
                $blocked = & $PrerequisiteChecker $check
                if ($blocked) { $status = 'Blocked'; $detail = $blocked -join '; ' }
                else {
                    Write-Host ("Running {0}: {1} {2}" -f $check.Id, $check.Command, ($check.Arguments -join ' '))
                    $code = & $CommandRunner $check.Command $check.Arguments
                    if ($code -eq 0) { $status = 'Passed' }
                    else { $detail = "Command exit code: $code" }
                }
            }
            catch { $detail = $_.Exception.Message }
            $timer.Stop()
            $results.Add([pscustomobject]@{ Id = $check.Id; Status = $status; Seconds = [math]::Round($timer.Elapsed.TotalSeconds, 2); Detail = $detail })
        }
    }
    finally { Pop-Location }
    return $results.ToArray()
}

Export-ModuleMember -Function Get-ValidationPlan, Invoke-ValidationPlan
