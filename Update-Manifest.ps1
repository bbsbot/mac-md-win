# Update-Manifest.ps1
# Regenerates .claude/skills/MANIFEST.md from the current directory contents.
# Run from project root.
param(
    [switch]$DryRun
)
$skillsRoot = Join-Path (Join-Path $PSScriptRoot ".claude") "skills"
$manifestPath = Join-Path $skillsRoot "MANIFEST.md"
if (-not (Test-Path $skillsRoot)) {
    Write-Host "[ERROR] Skills directory not found at: $skillsRoot" -ForegroundColor Red
    exit 1
}
Write-Host "=== Manifest Generator ===" -ForegroundColor Cyan
Write-Host "Skills root: $skillsRoot" -ForegroundColor Gray
Write-Host ""
$lines = New-Object System.Collections.ArrayList
$timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm'
[void]$lines.Add("# Skills Manifest")
[void]$lines.Add("")
[void]$lines.Add("Auto-generated inventory of curated agent skills.")
[void]$lines.Add("Generated: $timestamp")
[void]$lines.Add("")
$groups = Get-ChildItem -Path $skillsRoot -Directory | Sort-Object Name
$totalDirs = 0
$totalFiles = 0
# --- Config/support dir names to separate from skill dirs ---
$configNames = @(".claude", ".claude-plugin", "agents", "docs", "hooks", "shared", "templates")

# --- Boilerplate prefixes to skip during description extraction ---
$boilerplatePrefixes = @(
    "Use this skill",
    "This skill",
    "This document",
    "See the",
    "Refer to",
    "TODO",
    "DRAFT",
    "WIP",
    "Overview of",
    "When to use"
)

Write-Host "Found groups: $($groups.Count)" -ForegroundColor Gray
foreach ($group in $groups) {
    Write-Host "  Processing group: $($group.Name)" -ForegroundColor Gray
    $groupFiles = @(Get-ChildItem -Path $group.FullName -Recurse -File)
    $groupFileCount = $groupFiles.Count
    $totalFiles += $groupFileCount
    $heading = "## {0}/ ({1} files)" -f $group.Name, $groupFileCount
    [void]$lines.Add("---")
    [void]$lines.Add("")
    [void]$lines.Add($heading)
    [void]$lines.Add("")
    # --- Top-level files ---
    $topFiles = @(Get-ChildItem -Path $group.FullName -File -ErrorAction SilentlyContinue) | Sort-Object Name
    if ($topFiles.Count -gt 0) {
        [void]$lines.Add("### Root files")
        [void]$lines.Add("")
        foreach ($f in $topFiles) {
            $entry = "- ``{0}``" -f $f.Name
            [void]$lines.Add($entry)
        }
        [void]$lines.Add("")
    }
    # --- All subdirectories ---
    $allSubDirs = @(Get-ChildItem -Path $group.FullName -Directory) | Sort-Object Name
    # --- Config dirs ---
    $configDirs = @($allSubDirs | Where-Object { $configNames -contains $_.Name })
    if ($configDirs.Count -gt 0) {
        [void]$lines.Add("### Config / support directories")
        [void]$lines.Add("")
        foreach ($dir in $configDirs) {
            $count = @(Get-ChildItem -Path $dir.FullName -Recurse -File).Count
            $entry = "- ``{0}/`` ({1} files)" -f $dir.Name, $count
            [void]$lines.Add($entry)
        }
        [void]$lines.Add("")
    }
    # --- Skill dirs: everything not in configNames ---
    $skillDirs = @($allSubDirs | Where-Object { $configNames -notcontains $_.Name })
    # --- For dotnet: if there's a nested "skills" subfolder, expand it ---
    $finalSkillDirs = New-Object System.Collections.ArrayList
    foreach ($dir in $skillDirs) {
        if ($dir.Name -eq "skills") {
            $nested = @(Get-ChildItem -Path $dir.FullName -Directory) | Sort-Object Name
            foreach ($n in $nested) {
                [void]$finalSkillDirs.Add($n)
            }
        } else {
            [void]$finalSkillDirs.Add($dir)
        }
    }
    Write-Host "    Skill dirs: $($finalSkillDirs.Count)" -ForegroundColor Gray
    if ($finalSkillDirs.Count -gt 0) {
        $secHeading = "### Skills ({0} directories)" -f $finalSkillDirs.Count
        [void]$lines.Add($secHeading)
        [void]$lines.Add("")
        foreach ($dir in $finalSkillDirs) {
            $totalDirs++
            $count = @(Get-ChildItem -Path $dir.FullName -Recurse -File).Count
            # --- Extract description, skipping YAML front matter, blockquotes, boilerplate ---
            $desc = ""
            $readmePath = Join-Path $dir.FullName "README.md"
            $skillMdPath = Join-Path $dir.FullName "skill.md"
            $descSource = $null
            if (Test-Path $readmePath) { $descSource = $readmePath }
            elseif (Test-Path $skillMdPath) { $descSource = $skillMdPath }
            if ($descSource) {
                $inFrontMatter = $false
                $rawLines = @(Get-Content -Path $descSource -TotalCount 30)
                foreach ($l in $rawLines) {
                    $trimmed = $l.Trim()
                    if ($trimmed -eq "---") {
                        $inFrontMatter = -not $inFrontMatter
                        continue
                    }
                    if ($inFrontMatter) { continue }
                    if (-not $trimmed) { continue }
                    if ($trimmed.StartsWith("#")) { continue }
                    if ($trimmed.StartsWith(">")) { continue }
                    $isBoilerplate = $false
                    foreach ($prefix in $boilerplatePrefixes) {
                        if ($trimmed.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                            $isBoilerplate = $true
                            break
                        }
                    }
                    if ($isBoilerplate) { continue }
                    $desc = $trimmed
                    if ($desc.Length -gt 120) {
                        $desc = $desc.Substring(0, 117) + "..."
                    }
                    break
                }
            }
            if ($desc) {
                $entry = "- **{0}/** ({1} files) -- {2}" -f $dir.Name, $count, $desc
            } else {
                $entry = "- **{0}/** ({1} files)" -f $dir.Name, $count
            }
            [void]$lines.Add($entry)
        }
        [void]$lines.Add("")
    }
}
# --- Footer ---
[void]$lines.Add("---")
[void]$lines.Add("")
[void]$lines.Add("## Totals")
[void]$lines.Add("")
$entry = "- **Groups:** {0}" -f $groups.Count
[void]$lines.Add($entry)
$entry = "- **Skill directories:** {0}" -f $totalDirs
[void]$lines.Add($entry)
$entry = "- **Total files:** {0}" -f $totalFiles
[void]$lines.Add($entry)
$content = $lines -join "`r`n"
Write-Host ""
if ($DryRun) {
    Write-Host "[DRY RUN] Would write MANIFEST.md ($($content.Length) chars):" -ForegroundColor Yellow
    Write-Host ""
    Write-Host $content
} else {
    Set-Content -Path $manifestPath -Value $content -Encoding UTF8
    Write-Host "[WRITTEN] $manifestPath" -ForegroundColor Green
    Write-Host "  Groups: $($groups.Count)" -ForegroundColor White
    Write-Host "  Skill directories: $totalDirs" -ForegroundColor White
    Write-Host "  Total files: $totalFiles" -ForegroundColor White
}