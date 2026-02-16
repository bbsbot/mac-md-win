<#
.SYNOPSIS
    Fetches Claude Code skill files from curated GitHub repositories.
.DESCRIPTION
    Clones four skill repos, extracts useful content, organizes into
    .claude/skills/ subdirectories, and generates a MANIFEST.md.
.PARAMETER ProjectRoot
    Root directory of the project. Defaults to current directory.
.PARAMETER Force
    Remove existing skill directories before fetching.
#>

[CmdletBinding()]
param(
    [string]$ProjectRoot = (Get-Location).Path,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---- CONFIGURATION ----

$SkillSources = @(
    @{
        Name        = "dotnet"
        Repo        = "https://github.com/Aaronontheweb/dotnet-skills.git"
        Description = ".NET / C# / Win32 build toolchain skills"
        TargetDir   = "dotnet"
    },
    @{
        Name        = "workflow"
        Repo        = "https://github.com/levnikolaevich/claude-code-skills.git"
        Description = "Full delivery workflow - Git, documentation, epic delivery"
        TargetDir   = "workflow"
    },
    @{
        Name        = "community"
        Repo        = "https://github.com/VoltAgent/awesome-agent-skills.git"
        Description = "Community-curated collection of agent skills"
        TargetDir   = "community"
    },
    @{
        Name        = "official"
        Repo        = "https://github.com/anthropics/skills.git"
        Description = "Anthropic official reference skills"
        TargetDir   = "official"
    }
)

$IncludeExtensions = @(".md", ".yml", ".yaml", ".json", ".txt")

$ExcludeDirectories = @(
    ".git", ".github", ".vscode", "node_modules",
    "__pycache__", ".idea", "bin", "obj"
)

$ExcludeFiles = @(
    "LICENSE", "LICENSE.md", "LICENSE.txt",
    ".gitignore", ".gitattributes", ".editorconfig",
    ".prettierrc", ".eslintrc", "package.json",
    "package-lock.json", "yarn.lock", "Makefile",
    "Dockerfile", ".dockerignore", "CODEOWNERS", ".npmignore"
)

# ---- HELPER FUNCTIONS ----

function Write-Banner {
    param([string]$Text, [string]$Color = "Cyan")
    $border = "=" * 70
    Write-Host ""
    Write-Host $border -ForegroundColor $Color
    Write-Host "  $Text" -ForegroundColor $Color
    Write-Host $border -ForegroundColor $Color
    Write-Host ""
}

function Write-Step {
    param([string]$Text)
    Write-Host "  [*] " -ForegroundColor Cyan -NoNewline
    Write-Host $Text
}

function Write-Success {
    param([string]$Text)
    Write-Host "  [+] " -ForegroundColor Green -NoNewline
    Write-Host $Text -ForegroundColor Green
}

function Write-Warn {
    param([string]$Text)
    Write-Host "  [!] " -ForegroundColor Yellow -NoNewline
    Write-Host $Text -ForegroundColor Yellow
}

function Write-Failure {
    param([string]$Text)
    Write-Host "  [-] " -ForegroundColor Red -NoNewline
    Write-Host $Text -ForegroundColor Red
}

function Write-Detail {
    param([string]$Text)
    Write-Host "      $Text" -ForegroundColor DarkGray
}

function Test-GitAvailable {
    try {
        $v = & git --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Step "Git detected: $v"
            return $true
        }
    } catch { }
    return $false
}

function Get-UsefulFiles {
    param([string]$SourcePath)
    $allFiles = @()
    Get-ChildItem -Path $SourcePath -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
        $file = $_
        $rel = $file.FullName.Substring($SourcePath.Length).TrimStart('\', '/')

        $skip = $false
        foreach ($d in $ExcludeDirectories) {
            if ($rel -like "$d\*" -or $rel -like "*\$d\*") {
                $skip = $true
                break
            }
        }
        if ($skip) { return }
        if ($file.Name -in $ExcludeFiles) { return }
        if ($file.Extension -in $IncludeExtensions) {
            $allFiles += @{
                FullPath     = $file.FullName
                RelativePath = $rel
                Extension    = $file.Extension
                SizeBytes    = $file.Length
            }
        }
    }
    return $allFiles
}

function Copy-SkillFiles {
    param([string]$SourcePath, [string]$TargetPath, [array]$Files)
    $count = 0
    foreach ($f in $Files) {
        $dest = Join-Path $TargetPath $f.RelativePath
        $dir = Split-Path $dest -Parent
        if (-not (Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }
        Copy-Item -Path $f.FullPath -Destination $dest -Force
        $count++
    }
    return $count
}

function Remove-TempDirectory {
    param([string]$Path)
    if (Test-Path $Path) {
        Get-ChildItem -Path $Path -Recurse -Force -ErrorAction SilentlyContinue |
            ForEach-Object {
                if ($_.Attributes -band [System.IO.FileAttributes]::ReadOnly) {
                    $_.Attributes = $_.Attributes -bxor [System.IO.FileAttributes]::ReadOnly
                }
            }
        Remove-Item -Path $Path -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# ---- MAIN EXECUTION ----

$scriptStart = Get-Date

Write-Banner "Claude Code Skill Fetcher - Setup-Skills.ps1"

$ProjectRoot = (Resolve-Path -Path $ProjectRoot -ErrorAction Stop).Path
Write-Step "Project root: $ProjectRoot"

if (-not (Test-GitAvailable)) {
    Write-Failure "Git is not installed or not on PATH."
    Write-Failure "Install from https://git-scm.com and try again."
    exit 1
}

$SkillsBaseDir = Join-Path (Join-Path $ProjectRoot ".claude") "skills"

if ($Force -and (Test-Path $SkillsBaseDir)) {
    Write-Warn "Force flag set - removing existing skills directory."
    Remove-Item -Path $SkillsBaseDir -Recurse -Force
}

if (-not (Test-Path $SkillsBaseDir)) {
    New-Item -ItemType Directory -Path $SkillsBaseDir -Force | Out-Null
    Write-Success "Created: .claude/skills/"
} else {
    Write-Step "Skills directory already exists: .claude/skills/"
}

$TempBaseDir = Join-Path ([System.IO.Path]::GetTempPath()) ("claude-skills-" + (Get-Date -Format "yyyyMMddHHmmss"))
New-Item -ItemType Directory -Path $TempBaseDir -Force | Out-Null
Write-Detail "Temp workspace: $TempBaseDir"

$Results = @()
$totalFiles = 0
$successCount = 0

# ---- FETCH EACH REPO ----

foreach ($source in $SkillSources) {
    Write-Host ""
    Write-Banner ("Fetching: " + $source.Name + " - " + $source.Description) "White"

    $repoUrl = $source.Repo
    $targetDir = Join-Path $SkillsBaseDir $source.TargetDir
    $tempDir = Join-Path $TempBaseDir $source.Name

    $result = @{
        Name        = $source.Name
        Repo        = $repoUrl
        Description = $source.Description
        TargetDir   = $source.TargetDir
        FileCount   = 0
        Status      = "pending"
        Error       = $null
        Extensions  = @{}
    }

    try {
        Write-Step "Cloning $repoUrl (shallow, depth 1)..."
        $out = & git clone --depth 1 --quiet $repoUrl $tempDir 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Git clone failed (exit $LASTEXITCODE): $out"
        }
        Write-Success "Clone successful."

        Write-Step "Scanning for useful content files..."
        $files = Get-UsefulFiles -SourcePath $tempDir

        if ($null -eq $files -or $files.Count -eq 0) {
            Write-Warn "No useful skill files found in this repository."
            $result.Status = "empty"
        } else {
            $extBreakdown = @{}
            foreach ($f in $files) {
                $e = $f.Extension
                if ($extBreakdown.ContainsKey($e)) { $extBreakdown[$e]++ }
                else { $extBreakdown[$e] = 1 }
            }
            $result.Extensions = $extBreakdown

            if (-not (Test-Path $targetDir)) {
                New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
            }

            Write-Step ("Copying " + $files.Count + " files to .claude/skills/" + $source.TargetDir + "/...")
            $copied = Copy-SkillFiles -SourcePath $tempDir -TargetPath $targetDir -Files $files

            $result.FileCount = $copied
            $result.Status = "success"
            $totalFiles += $copied
            $successCount++

            Write-Success "Copied $copied files."
            foreach ($e in ($extBreakdown.Keys | Sort-Object)) {
                Write-Detail ($extBreakdown[$e].ToString() + " $e files")
            }
        }
    } catch {
        $result.Status = "failed"
        $result.Error = $_.Exception.Message
        Write-Failure ("Failed: " + $_.Exception.Message)
        Write-Warn "Continuing with remaining repositories..."
    } finally {
        Write-Step "Cleaning up temp clone..."
        Remove-TempDirectory -Path $tempDir
        Write-Detail "Temp directory removed."
    }

    $Results += $result
}

Remove-TempDirectory -Path $TempBaseDir

# ---- GENERATE MANIFEST ----

Write-Host ""
Write-Banner "Generating MANIFEST.md"

$manifestPath = Join-Path $SkillsBaseDir "MANIFEST.md"
$fetchDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$elapsed = (Get-Date) - $scriptStart

$lines = @()
$lines += "# Claude Code Skills - Fetch Manifest"
$lines += ""
$lines += "Auto-generated by Setup-Skills.ps1. Re-run the script to refresh."
$lines += ""
$lines += "## Fetch Details"
$lines += ""
$lines += "| Field | Value |"
$lines += "|---|---|"
$lines += "| Date Fetched | $fetchDate |"
$lines += "| Project Root | $ProjectRoot |"
$lines += "| Total Files | $totalFiles |"
$lines += ("| Repos Succeeded | $successCount / " + $SkillSources.Count + " |")
$lines += ("| Elapsed Time | " + [math]::Round($elapsed.TotalSeconds, 1) + " seconds |")
$lines += ""
$lines += "## Source Repositories"

foreach ($r in $Results) {
    $tag = $r.Status.ToUpper()
    $lines += ""
    $lines += ("### " + $r.Name + " [" + $tag + "]")
    $lines += ""
    $lines += ("Repository: " + $r.Repo)
    $lines += ("Description: " + $r.Description)
    $lines += ("Target: .claude/skills/" + $r.TargetDir + "/")
    $lines += ("Files Copied: " + $r.FileCount)
    if ($r.Extensions.Count -gt 0) {
        $lines += "Breakdown:"
        foreach ($e in ($r.Extensions.Keys | Sort-Object)) {
            $lines += ("  " + $r.Extensions[$e].ToString() + " $e files")
        }
    }
    if ($r.Error) {
        $lines += ("Error: " + $r.Error)
    }
}

$lines += ""
$lines += "## Review Before Using"
$lines += ""
$lines += "These skills were bulk-fetched. Not all will be relevant to your"
$lines += "C#/Win32 localization project. Browse each directory, cherry-pick"
$lines += "what matters, and delete the rest to keep context clean."
$lines += ""
$lines += "## Directory Structure"
$lines += ""
$lines += ".claude/skills/"
$lines += "    MANIFEST.md          <- this file"
$lines += "    dotnet/              <- Aaronontheweb/dotnet-skills"
$lines += "    workflow/            <- levnikolaevich/claude-code-skills"
$lines += "    community/           <- VoltAgent/awesome-agent-skills"
$lines += "    official/            <- anthropics/skills"

$lines -join "`r`n" | Set-Content -Path $manifestPath -Encoding UTF8
Write-Success "MANIFEST.md generated."

# ---- SUMMARY ----

Write-Host ""
Write-Host ""
$box = "+" + ("-" * 58) + "+"
Write-Host $box -ForegroundColor Cyan
Write-Host "|  SKILL FETCH COMPLETE                                    |" -ForegroundColor Cyan
Write-Host $box -ForegroundColor Cyan

foreach ($r in $Results) {
    $color = switch ($r.Status) { "success" { "Green" } "failed" { "Red" } default { "Yellow" } }
    $label = $r.Status.ToUpper().PadRight(7)
    $name = $r.Name.PadRight(12)
    $msg = "|  $label  $name  $($r.FileCount) files"
    $pad = 59 - $msg.Length
    if ($pad -lt 0) { $pad = 0 }
    $msg += (" " * $pad) + "|"
    Write-Host $msg -ForegroundColor $color
}

Write-Host $box -ForegroundColor Cyan
Write-Host ("|  Total: $totalFiles files in " + [math]::Round($elapsed.TotalSeconds,1) + "s".PadRight(40) + "|") -ForegroundColor White
Write-Host $box -ForegroundColor Cyan
Write-Host ""
Write-Host "  NEXT STEPS:" -ForegroundColor Yellow
Write-Host "  1. Review .claude/skills/MANIFEST.md" -ForegroundColor White
Write-Host "  2. Browse each skill directory" -ForegroundColor White
Write-Host "  3. Cherry-pick skills for your C#/Win32 project" -ForegroundColor White
Write-Host "  4. Delete irrelevant skills" -ForegroundColor White
Write-Host "  5. Reference chosen skills in CLAUDE.md" -ForegroundColor White
Write-Host "  6. Write custom skills for Git submodule workflows" -ForegroundColor White
Write-Host ""
