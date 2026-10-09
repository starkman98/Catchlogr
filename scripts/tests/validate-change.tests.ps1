<#
.SYNOPSIS
Dependency-free behavioral checks for validation selection and execution.
.DESCRIPTION
Uses disposable Git repositories and fake runners, never real application tests.
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path (Split-Path -Parent $PSScriptRoot) 'Validation.psm1') -Force
$fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) ('catchlogr-validation-' + [guid]::NewGuid().ToString('N'))
$assertions = 0
$entryDiagnostics = ''

function Assert-True {
    param([bool] $Condition, [string] $Message)
    if (-not $Condition) { throw "$Message`n$script:entryDiagnostics" }
    $script:assertions++
}
function Invoke-FixtureGit {
    param([string[]] $GitArguments)
    & git -C $fixtureRoot @GitArguments | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Fixture Git failed: $GitArguments" }
}
function Set-FixtureFile {
    param([string] $Path, [string] $Content = 'fixture')
    $target = Join-Path $fixtureRoot $Path
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    Set-Content -LiteralPath $target -Value $Content -Encoding utf8
}
function Get-Ids {
    param($Plan)
    return (@($Plan.Checks | ForEach-Object Id) -join ',')
}

function Invoke-FixtureEntry {
    param([switch] $Run, [string] $BaseRef)
    # Capture native stderr directly: PS 5.1's 2>&1 can throw before exit-code assertions.
    $command = "& '" + $entry.Replace("'", "''") + "' -RepositoryRoot '" + $fixtureRoot.Replace("'", "''") + "'"
    if ($Run) { $command += ' -Run' }
    if ($BaseRef) { $command += " -BaseRef '" + $BaseRef.Replace("'", "''") + "'" }
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = (Get-Process -Id $PID).Path
    $start.Arguments = '-NoProfile -EncodedCommand ' + [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($command))
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    try {
        [void] $process.Start()
        $errors = $process.StandardError.ReadToEndAsync()
        $output = $process.StandardOutput.ReadToEnd()
        $process.WaitForExit()
        $errorText = $errors.GetAwaiter().GetResult()
        $script:entryDiagnostics = "Fixture stdout:`n$output`nFixture stderr:`n$errorText"
        return [pscustomobject]@{ ExitCode = $process.ExitCode; Output = $output; Error = $errorText }
    }
    finally { $process.Dispose() }
}

try {
    New-Item -ItemType Directory -Path $fixtureRoot | Out-Null
    Invoke-FixtureGit @('init', '--quiet')
    Invoke-FixtureGit @('config', 'user.name', 'Validation fixture')
    Invoke-FixtureGit @('config', 'user.email', 'validation@example.invalid')
    # Avoid signing/hooks from a user's global Git config in disposable fixtures.
    Invoke-FixtureGit @('config', 'commit.gpgsign', 'false')
    Invoke-FixtureGit @('config', 'core.autocrlf', 'false')
    Invoke-FixtureGit @('config', 'core.hooksPath', (Join-Path $fixtureRoot 'no-hooks'))
    Set-FixtureFile '.gitignore' 'ignored/'
    Set-FixtureFile 'README.md'
    Set-FixtureFile 'src/Catchlogr.Api/Old name.cs'
    Set-FixtureFile 'src/Catchlogr.Sync/Services/Example.cs'
    Set-FixtureFile 'docs/existing.md'
    Invoke-FixtureGit @('add', '--all')
    Invoke-FixtureGit @('commit', '--quiet', '-m', 'Fixture baseline')
    Invoke-FixtureGit @('branch', 'fixture-base')
    $plan = Get-ValidationPlan -RepositoryRoot $fixtureRoot
    Assert-True ($plan.Paths.Count -eq 0 -and $plan.Checks.Count -eq 0) 'Clean repository selected checks.'

    Set-FixtureFile 'docs/existing.md' 'staged'
    Invoke-FixtureGit @('add', 'docs/existing.md')
    Set-FixtureFile 'docs/existing.md' 'unstaged'
    Set-FixtureFile 'docs/new file.md'
    Set-FixtureFile 'ignored/not selected.cs'
    $plan = Get-ValidationPlan -RepositoryRoot (Join-Path $fixtureRoot 'docs')
    Assert-True ((Get-Ids $plan) -eq 'docs') 'Documentation-only changes selected application checks.'
    Assert-True ($plan.Paths.Count -eq 2) 'Staged/unstaged union, spaces, or ignored-file handling failed.'
    Assert-True ($plan.DocumentationReview.Count -eq 0) 'Documentation-only edits suggested unrelated guides.'
    Invoke-FixtureGit @('add', 'docs')
    Invoke-FixtureGit @('commit', '--quiet', '-m', 'Fixture documentation')
    $plan = Get-ValidationPlan -RepositoryRoot $fixtureRoot -BaseRef 'fixture-base'
    Assert-True ((Get-Ids $plan) -eq 'docs' -and $plan.Paths.Count -eq 2) 'Committed branch changes were missed.'

    New-Item -ItemType Directory -Path (Join-Path $fixtureRoot 'src/Catchlogr.Mobile') | Out-Null
    Invoke-FixtureGit @('mv', 'src/Catchlogr.Api/Old name.cs', 'src/Catchlogr.Mobile/New name.cs')
    Remove-Item -LiteralPath (Join-Path $fixtureRoot 'src/Catchlogr.Sync/Services/Example.cs')
    $plan = Get-ValidationPlan -RepositoryRoot $fixtureRoot
    Assert-True ((Get-Ids $plan) -eq 'unit,api,mobile') 'Cross-project rename/deletion selection failed.'
    Assert-True ($plan.Paths -contains 'src/Catchlogr.Api/Old name.cs') 'Rename source was missed.'
    Assert-True ($plan.Paths -contains 'src/Catchlogr.Mobile/New name.cs') 'Rename destination was missed.'
    Assert-True ($plan.Paths -contains 'src/Catchlogr.Sync/Services/Example.cs') 'Deletion was missed.'
    Assert-True (@($plan.DocumentationReview | Where-Object Guide -eq 'docs/SYNC_STRATEGY.md').Count -eq 1) 'Deleted source lost its documentation reminder.'
    Invoke-FixtureGit @('add', '--all')
    Invoke-FixtureGit @('commit', '--quiet', '-m', 'Fixture boundaries')

    Set-FixtureFile 'src/Catchlogr.Sync/Services/Reminder.cs'
    Set-FixtureFile 'src/Catchlogr.Sync/Services/Another.cs'
    $plan = Get-ValidationPlan -RepositoryRoot $fixtureRoot
    Assert-True ((Get-Ids $plan) -eq 'unit') 'Documentation reminders expanded automated checks.'
    Assert-True ($plan.DocumentationReview.Count -eq 1 -and $plan.DocumentationReview[0].Guide -eq 'docs/SYNC_STRATEGY.md') 'Sync guide was not suggested exactly once.'
    Assert-True ($plan.DocumentationReview[0].Reasons.Count -eq 2 -and -not $plan.DocumentationReview[0].Changed) 'Guide reasons or unchanged state were lost.'
    Set-FixtureFile 'docs/SYNC_STRATEGY.md'
    $plan = Get-ValidationPlan -RepositoryRoot $fixtureRoot
    Assert-True ($plan.DocumentationReview[0].Changed) 'Changed guide was not identified.'
    Remove-Item -LiteralPath (Join-Path $fixtureRoot 'src/Catchlogr.Sync/Services/Reminder.cs'), (Join-Path $fixtureRoot 'src/Catchlogr.Sync/Services/Another.cs'), (Join-Path $fixtureRoot 'docs/SYNC_STRATEGY.md')

    foreach ($case in @(
        @{ Path = 'src/Catchlogr.Api/Endpoints/New.cs'; Guide = 'README.md'; Ids = 'unit,api' },
        @{ Path = 'src/Catchlogr.Mobile/Configuration/New.cs'; Guide = 'docs/MOBILE_CONFIGURATION.md'; Ids = 'mobile' },
        @{ Path = 'src/Catchlogr.Infrastructure/Weather/New.cs'; Guide = 'docs/WEATHER_INTEGRATION.md'; Ids = 'unit,api' },
        @{ Path = '.github/workflows/ownership.yml'; Guide = 'docs/CI-CD.md'; Ids = 'docs,unit,api,mobile' }
    )) {
        Set-FixtureFile $case.Path
        Invoke-FixtureGit @('add', '--all')
        Invoke-FixtureGit @('commit', '--quiet', '-m', 'Fixture guide ownership')
        $plan = Get-ValidationPlan -RepositoryRoot $fixtureRoot -BaseRef 'HEAD~1'
        Assert-True ((Get-Ids $plan) -eq $case.Ids) "Unexpected branch checks for $($case.Path)"
        Assert-True (@($plan.DocumentationReview | Where-Object Guide -eq $case.Guide).Count -eq 1) "Missing branch guide reminder for $($case.Path)"
    }

    foreach ($case in @(
        @{ Path = 'src/Catchlogr.Mobile/AGENTS.md'; Ids = 'docs' },
        @{ Path = 'src/Catchlogr.Sync/Services/New.cs'; Ids = 'unit' },
        @{ Path = 'src/Catchlogr.Application/Interfaces/INew.cs'; Ids = 'unit,api,mobile' },
        @{ Path = 'src/Catchlogr.Contracts/New.cs'; Ids = 'unit,api,mobile' },
        @{ Path = 'Directory.Build.props'; Ids = 'unit,api,mobile' },
        @{ Path = 'src/Catchlogr.Mobile/Pages/New.xaml'; Ids = 'mobile,mobile-build' },
        @{ Path = 'src/Catchlogr.Mobile/Pages/New.xaml.cs'; Ids = 'mobile,mobile-build' },
        @{ Path = 'src/Catchlogr.Mobile/Resources/Images/new.svg'; Ids = 'mobile,mobile-build' },
        @{ Path = 'src/Catchlogr.Infrastructure/Persistence/New.cs'; Ids = 'unit,api' },
        @{ Path = 'src/Catchlogr.Mobile/Data/New.cs'; Ids = 'mobile' },
        @{ Path = 'scripts/Validation.psm1'; Ids = 'docs,helper' },
        @{ Path = '.github/workflows/new.yml'; Ids = 'docs,unit,api,mobile' },
        @{ Path = 'unknown configuration.json'; Ids = 'docs,helper,unit,api,mobile' }
    )) {
        Set-FixtureFile $case.Path
        $plan = Get-ValidationPlan -RepositoryRoot $fixtureRoot
        Assert-True ((Get-Ids $plan) -eq $case.Ids) ("Unexpected checks for $($case.Path): $(Get-Ids $plan)")
        if ($case.Path -match 'Persistence|/Data/|unknown|\.xaml$') {
            Assert-True ($plan.Manual.Count -gt 0) "Missing manual requirement for $($case.Path)"
        }
        Remove-Item -LiteralPath (Join-Path $fixtureRoot $case.Path)
    }
    $failed = $false
    try { Get-ValidationPlan -RepositoryRoot $fixtureRoot -BaseRef 'missing-base' | Out-Null }
    catch { $failed = $true }
    Assert-True $failed 'Invalid base reference did not fail.'

    Set-FixtureFile 'src/Catchlogr.Api/One.cs'
    Set-FixtureFile 'src/Catchlogr.Api/Two.cs'
    $plan = Get-ValidationPlan -RepositoryRoot $fixtureRoot
    Assert-True ($plan.Checks.Count -eq 2) 'Checks were duplicated.'
    $results = @(Invoke-ValidationPlan $plan -PrerequisiteChecker { $null } -CommandRunner { 0 } 6>$null)
    Assert-True (@($results | Where-Object Status -eq 'Passed').Count -eq 2) 'Successful commands were not recorded.'
    $results = @(Invoke-ValidationPlan $plan -PrerequisiteChecker { $null } -CommandRunner { 7 } 6>$null)
    Assert-True (@($results | Where-Object Status -eq 'Failed').Count -eq 2) 'Failed exit codes were lost.'
    $results = @(Invoke-ValidationPlan $plan -PrerequisiteChecker { 'Missing prerequisite' } -CommandRunner { throw 'Must not execute' })
    Assert-True (@($results | Where-Object Status -eq 'Blocked').Count -eq 2) 'Blocked checks were executed or reported as passing.'
    $results = @(Invoke-ValidationPlan $plan -PrerequisiteChecker { $null } -CommandRunner { throw 'Execution failed' } 6>$null)
    Assert-True (@($results | Where-Object Status -eq 'Failed').Count -eq 2) 'Thrown command errors were lost.'

    Remove-Item -LiteralPath (Join-Path $fixtureRoot 'src/Catchlogr.Api/One.cs'), (Join-Path $fixtureRoot 'src/Catchlogr.Api/Two.cs')
    $unicodePath = 'docs/' + [char] 0x00e5 + ' file.md'
    Set-FixtureFile $unicodePath
    $plan = Get-ValidationPlan -RepositoryRoot $fixtureRoot
    Assert-True ($plan.Paths -contains $unicodePath) 'UTF-8 Git filename decoding failed.'
    Remove-Item -LiteralPath (Join-Path $fixtureRoot $unicodePath)

    # Exercise the public entry point in a fresh shell so its exit cannot end tests.
    Set-FixtureFile 'scripts/validate-docs.ps1' "Set-Content -LiteralPath marker.txt -Value executed`nexit 7"
    Set-FixtureFile 'docs/preview.md'
    $entry = Join-Path (Split-Path -Parent $PSScriptRoot) 'validate-change.ps1'
    $result = Invoke-FixtureEntry
    Assert-True ($result.ExitCode -eq 0) 'Preview entry point failed.'
    Assert-True ($result.Output -match 'Documentation review suggested: docs/VALIDATION.md') 'Preview omitted guide reminders.'
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $fixtureRoot 'marker.txt'))) 'Preview executed a validation command.'
    $result = Invoke-FixtureEntry -Run
    Assert-True ($result.ExitCode -eq 1) 'Entry point did not propagate command failure.'
    Assert-True ($result.Output -match 'Command exit code: 7') 'Actual command failure details were hidden.'
    Assert-True (Test-Path -LiteralPath (Join-Path $fixtureRoot 'marker.txt')) 'Run entry point did not execute its selected check.'
    Remove-Item -LiteralPath (Join-Path $fixtureRoot 'marker.txt')
    Set-FixtureFile 'scripts/validate-docs.ps1' "Write-Host 'Fixture validation passed'"
    $result = Invoke-FixtureEntry -Run
    Assert-True ($result.ExitCode -eq 0) 'Successful script retained a stale native exit code.'
    $result = Invoke-FixtureEntry -BaseRef 'missing-base'
    Assert-True ($result.ExitCode -eq 1) 'Entry point did not propagate invalid base reference.'
    Assert-True ($result.Error -match 'Git failed') 'Actual Git failure details were hidden.'
    Write-Host "Validation helper checks passed: $assertions assertions."
}
finally {
    # Verify the exact disposable target before recursive cleanup.
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    if (-not $resolvedFixture.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolvedFixture) -notmatch '^catchlogr-validation-[a-f0-9]{32}$') {
        throw "Unsafe fixture cleanup path: $resolvedFixture"
    }
    if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
}
exit 0
