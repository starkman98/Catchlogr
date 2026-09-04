<#
.SYNOPSIS
Validates Catchlogr's local Markdown links, heading anchors, and code fences.

.DESCRIPTION
Scans README.md and docs/**/*.md. External URLs are intentionally skipped so
temporary network or third-party failures cannot make pull-request CI flaky.

.EXAMPLE
pwsh ./scripts/validate-docs.ps1
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$resolvedRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$documentationRoot = Join-Path $resolvedRoot 'docs'
$pathComparison = if ([System.IO.Path]::DirectorySeparatorChar -eq '\') {
    [StringComparison]::OrdinalIgnoreCase
}
else {
    [StringComparison]::Ordinal
}
$issues = [System.Collections.Generic.List[object]]::new()
$anchorCache = @{}

function Get-RepositoryRelativePath {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    if ($Path.StartsWith($resolvedRoot, $pathComparison)) {
        return $Path.Substring($resolvedRoot.Length).TrimStart(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar)
    }

    return $Path
}

function Add-ValidationIssue {
    param(
        [Parameter(Mandatory)]
        [System.IO.FileInfo] $Document,

        [Parameter(Mandatory)]
        [int] $Line,

        [Parameter(Mandatory)]
        [string] $Message
    )

    $issues.Add([pscustomobject]@{
        File = Get-RepositoryRelativePath -Path $Document.FullName
        Line = $Line
        Message = $Message
    })
}

function ConvertTo-GitHubAnchor {
    param(
        [Parameter(Mandatory)]
        [string] $Heading
    )

    $value = [regex]::Replace($Heading, '<[^>]+>', '')
    $value = [regex]::Replace($value, '!?\[([^\]]+)\]\([^)]+\)', '$1')
    $value = $value.Replace([char] 96, '').Replace('*', '')
    $value = $value.ToLowerInvariant()
    $value = [regex]::Replace($value, '[^\p{L}\p{M}\p{Nd}\s_-]', '')
    return [regex]::Replace($value.Trim(), '\s+', '-')
}

function Get-MarkdownAnchors {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    $cacheKey = [System.IO.Path]::GetFullPath($Path)
    if ($anchorCache.ContainsKey($cacheKey)) {
        return $anchorCache[$cacheKey]
    }

    $anchors = [System.Collections.Generic.HashSet[string]]::new(
        [StringComparer]::OrdinalIgnoreCase)
    $duplicates = @{}
    $insideFence = $false
    $fenceCharacter = $null
    $fenceLength = 0

    foreach ($line in Get-Content -LiteralPath $cacheKey) {
        $fenceMatch = [regex]::Match(
            $line,
            '^\s*(?<marker>(?:\x60){3,}|~{3,})')
        if ($fenceMatch.Success) {
            $marker = $fenceMatch.Groups['marker'].Value
            if (-not $insideFence) {
                $insideFence = $true
                $fenceCharacter = $marker[0]
                $fenceLength = $marker.Length
            }
            elseif ($marker[0] -eq $fenceCharacter -and
                $marker.Length -ge $fenceLength) {
                $insideFence = $false
            }

            continue
        }

        if ($insideFence) {
            continue
        }

        $headingMatch = [regex]::Match(
            $line,
            '^\s{0,3}#{1,6}\s+(?<heading>.+?)\s*#*\s*$')
        if (-not $headingMatch.Success) {
            continue
        }

        $baseAnchor = ConvertTo-GitHubAnchor -Heading (
            $headingMatch.Groups['heading'].Value)
        if ([string]::IsNullOrWhiteSpace($baseAnchor)) {
            continue
        }

        if ($duplicates.ContainsKey($baseAnchor)) {
            $duplicates[$baseAnchor]++
            $anchor = '{0}-{1}' -f $baseAnchor, $duplicates[$baseAnchor]
        }
        else {
            $duplicates[$baseAnchor] = 0
            $anchor = $baseAnchor
        }

        [void] $anchors.Add($anchor)
    }

    $anchorCache[$cacheKey] = $anchors
    return $anchors
}

function Test-MarkdownTarget {
    param(
        [Parameter(Mandatory)]
        [System.IO.FileInfo] $Document,

        [Parameter(Mandatory)]
        [int] $Line,

        [Parameter(Mandatory)]
        [string] $RawTarget
    )

    $target = $RawTarget.Trim()
    if ($target.StartsWith('<') -and $target.EndsWith('>')) {
        $target = $target.Substring(1, $target.Length - 2)
    }
    else {
        $target = ($target -split '\s+', 2)[0]
    }

    if ([string]::IsNullOrWhiteSpace($target) -or
        $target.StartsWith('//') -or
        $target -match '^[A-Za-z][A-Za-z0-9+.-]*:') {
        return
    }

    $fragmentIndex = $target.IndexOf('#')
    if ($fragmentIndex -ge 0) {
        $fragment = $target.Substring($fragmentIndex + 1)
        $pathPart = $target.Substring(0, $fragmentIndex)
    }
    else {
        $fragment = ''
        $pathPart = $target
    }

    $queryIndex = $pathPart.IndexOf('?')
    if ($queryIndex -ge 0) {
        $pathPart = $pathPart.Substring(0, $queryIndex)
    }

    $pathPart = [Uri]::UnescapeDataString($pathPart)
    $fragment = [Uri]::UnescapeDataString($fragment)

    if ([string]::IsNullOrWhiteSpace($pathPart)) {
        $resolvedTarget = $Document.FullName
    }
    elseif ($pathPart.StartsWith('/') -or $pathPart.StartsWith('\')) {
        $repositoryPath = $pathPart.TrimStart(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar)
        $resolvedTarget = [System.IO.Path]::GetFullPath(
            (Join-Path $resolvedRoot $repositoryPath))
    }
    else {
        $resolvedTarget = [System.IO.Path]::GetFullPath(
            (Join-Path $Document.DirectoryName $pathPart))
    }

    $rootPrefix = $resolvedRoot.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) +
        [System.IO.Path]::DirectorySeparatorChar

    if ($resolvedTarget -ne $resolvedRoot -and
        -not $resolvedTarget.StartsWith($rootPrefix, $pathComparison)) {
        Add-ValidationIssue -Document $Document -Line $Line -Message (
            "Local link escapes the repository: $RawTarget")
        return
    }

    if (-not (Test-Path -LiteralPath $resolvedTarget)) {
        Add-ValidationIssue -Document $Document -Line $Line -Message (
            "Local link target does not exist: $RawTarget")
        return
    }

    if ([string]::IsNullOrWhiteSpace($fragment)) {
        return
    }

    if ((Get-Item -LiteralPath $resolvedTarget) -isnot [System.IO.FileInfo] -or
        [System.IO.Path]::GetExtension($resolvedTarget) -ine '.md') {
        return
    }

    $anchors = Get-MarkdownAnchors -Path $resolvedTarget
    if (-not $anchors.Contains($fragment)) {
        Add-ValidationIssue -Document $Document -Line $Line -Message (
            "Markdown heading anchor does not exist: $RawTarget")
    }
}

$documents = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
$rootReadme = Join-Path $resolvedRoot 'README.md'
if (Test-Path -LiteralPath $rootReadme -PathType Leaf) {
    $documents.Add((Get-Item -LiteralPath $rootReadme))
}

if (Test-Path -LiteralPath $documentationRoot -PathType Container) {
    $additionalDocuments = Get-ChildItem -LiteralPath $documentationRoot -Filter '*.md' -File -Recurse
    foreach ($document in $additionalDocuments) {
        $documents.Add($document)
    }
}

if ($documents.Count -eq 0) {
    Write-Host 'No Markdown documentation files were found.'
    exit 1
}

foreach ($document in $documents) {
    $lines = Get-Content -LiteralPath $document.FullName
    $insideFence = $false
    $fenceCharacter = $null
    $fenceLength = 0
    $openingFenceLine = 0

    for ($lineIndex = 0; $lineIndex -lt $lines.Count; $lineIndex++) {
        $lineNumber = $lineIndex + 1
        $line = $lines[$lineIndex]
        $fenceMatch = [regex]::Match(
            $line,
            '^\s*(?<marker>(?:\x60){3,}|~{3,})')

        if ($fenceMatch.Success) {
            $marker = $fenceMatch.Groups['marker'].Value
            if (-not $insideFence) {
                $insideFence = $true
                $fenceCharacter = $marker[0]
                $fenceLength = $marker.Length
                $openingFenceLine = $lineNumber
            }
            elseif ($marker[0] -eq $fenceCharacter -and
                $marker.Length -ge $fenceLength) {
                $insideFence = $false
            }

            continue
        }

        if ($insideFence) {
            continue
        }

        foreach ($match in [regex]::Matches(
            $line,
            '!?\[[^\]]*\]\((?<target><[^>]+>|[^)]+)\)')) {
            Test-MarkdownTarget -Document $document -Line $lineNumber -RawTarget (
                $match.Groups['target'].Value)
        }

        $definitionMatch = [regex]::Match(
            $line,
            '^\s*\[[^\]]+\]:\s*(?<target><[^>]+>|\S+)')
        if ($definitionMatch.Success) {
            Test-MarkdownTarget -Document $document -Line $lineNumber -RawTarget (
                $definitionMatch.Groups['target'].Value)
        }
    }

    if ($insideFence) {
        Add-ValidationIssue -Document $document -Line $openingFenceLine -Message (
            'Markdown code fence is not closed.')
    }
}

if ($issues.Count -gt 0) {
    foreach ($issue in $issues | Sort-Object File, Line, Message) {
        if ($env:GITHUB_ACTIONS -eq 'true') {
            $message = $issue.Message.Replace('%', '%25')
            $message = $message.Replace(([char] 13).ToString(), '%0D')
            $message = $message.Replace(([char] 10).ToString(), '%0A')
            Write-Host (
                '::error file={0},line={1}::{2}' -f
                $issue.File.Replace('\', '/'),
                $issue.Line,
                $message)
        }
        else {
            Write-Host (
                '{0}:{1}: {2}' -f
                $issue.File,
                $issue.Line,
                $issue.Message)
        }
    }

    Write-Host (
        'Documentation validation failed with {0} issue(s).' -f
        $issues.Count)
    exit 1
}

Write-Host (
    'Documentation validation passed for {0} Markdown file(s).' -f
    $documents.Count)
