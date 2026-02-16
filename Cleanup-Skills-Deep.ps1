# Cleanup-Skills-Deep.ps1
# Second-pass curation: removes irrelevant skill subdirectories
# from dotnet/ and workflow/, keeping only what's relevant to
# a C#/.NET 8 / WinUI 3 / SQLite desktop app.
# Run from project root.

param(
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$skillsRoot = Join-Path (Join-Path $PSScriptRoot ".claude") "skills"

if (-not (Test-Path $skillsRoot)) {
    Write-Error "Skills directory not found at: $skillsRoot"
    exit 1
}

Write-Host "=== Deep Skills Curation ===" -ForegroundColor Cyan
Write-Host "Skills root: $skillsRoot" -ForegroundColor Gray
Write-Host ""

$totalDeleted = 0

function Remove-SkillDirs {
    param(
        [string]$ParentPath,
        [string[]]$RemoveList,
        [string]$Label
    )

    Write-Host "--- $Label ---" -ForegroundColor Cyan

    foreach ($name in $RemoveList) {
        $path = Join-Path $ParentPath $name
        if (Test-Path $path) {
            $count = (Get-ChildItem -Path $path -Recurse -File).Count
            if ($DryRun) {
                Write-Host "  [DRY RUN] Would delete: $name/ ($count files)" -ForegroundColor Yellow
            } else {
                Remove-Item -Path $path -Recurse -Force
                Write-Host "  [DELETED] $name/ ($count files)" -ForegroundColor Green
            }
            $script:totalDeleted += $count
        } else {
            Write-Host "  [SKIP] $name/ not found" -ForegroundColor DarkGray
        }
    }
    Write-Host ""
}

# ═══════════════════════════════════════════════════════
# 1. DOTNET SKILLS — remove irrelevant subdirectories
# ═══════════════════════════════════════════════════════
#
# KEEPING (13 dirs):
#   csharp-api-design, csharp-coding-standards,
#   csharp-concurrency-patterns, csharp-type-design-performance,
#   database-performance, efcore-patterns, local-tools,
#   microsoft-extensions-configuration,
#   microsoft-extensions-dependency-injection,
#   package-management, project-structure, serialization,
#   skills-index-snippets

$dotnetSkills = Join-Path (Join-Path $skillsRoot "dotnet") "skills"

$dotnetRemove = @(
    "akka-aspire-configuration"
    "akka-best-practices"
    "akka-hosting-actor-patterns"
    "akka-management"
    "akka-testing-patterns"
    "aspire-configuration"
    "aspire-integration-testing"
    "aspire-mailpit-integration"
    "aspire-service-defaults"
    "crap-analysis"
    "dotnet-devcert-trust"
    "ilspy-decompile"
    "marketplace-publishing"
    "mjml-email-templates"
    "playwright-blazor"
    "playwright-ci-caching"
    "slopwatch"
    "snapshot-testing"
    "testcontainers"
    "verify-email-snapshots"
)

Remove-SkillDirs -ParentPath $dotnetSkills -RemoveList $dotnetRemove -Label "Dotnet Skills Cleanup (20 dirs)"

# ═══════════════════════════════════════════════════════
# 2. WORKFLOW — remove irrelevant agent definitions
# ═══════════════════════════════════════════════════════
#
# KEEPING (core workflow, 15 dirs + config):
#   .claude/, .claude-plugin, docs, hooks, shared
#   ln-001-standards-researcher
#   ln-002-best-practices-researcher
#   ln-003-push-all
#   ln-004-agent-sync
#   ln-200-scope-decomposer
#   ln-300-task-coordinator
#   ln-301-task-creator
#   ln-400-story-executor
#   ln-401-task-executor
#   ln-500-story-quality-gate
#   ln-510-quality-coordinator
#   ln-511-code-quality-checker
#   ln-780-bootstrap-verifier
#   ln-781-build-verifier
#   ln-782-test-runner

$workflowRoot = Join-Path $skillsRoot "workflow"

$workflowRemove = @(
    # Document pipeline (too elaborate)
    "ln-100-documents-pipeline"
    "ln-1000-pipeline-orchestrator"
    "ln-110-project-docs-coordinator"
    "ln-111-root-docs-creator"
    "ln-112-project-core-creator"
    "ln-113-backend-docs-creator"
    "ln-114-frontend-docs-creator"
    "ln-115-devops-docs-creator"
    "ln-120-reference-docs-creator"
    "ln-130-tasks-docs-creator"
    "ln-140-test-docs-creator"
    "ln-150-presentation-creator"

    # Epic/story planning (overkill for this project)
    "ln-201-opportunity-discoverer"
    "ln-210-epic-coordinator"
    "ln-220-story-coordinator"
    "ln-221-story-creator"
    "ln-222-story-replanner"
    "ln-230-story-prioritizer"

    # Task review/rework extras
    "ln-302-task-replanner"
    "ln-310-story-validator"
    "ln-311-agent-reviewer"
    "ln-402-task-reviewer"
    "ln-403-task-rework"
    "ln-404-test-executor"

    # Quality sub-agents (keep coordinator + code quality only)
    "ln-512-tech-debt-cleaner"
    "ln-513-agent-reviewer"
    "ln-514-regression-checker"
    "ln-520-test-planner"
    "ln-521-test-researcher"
    "ln-522-manual-tester"
    "ln-523-auto-test-planner"

    # Auditors (massive, mostly web/backend specific)
    "ln-600-docs-auditor"
    "ln-601-semantic-content-auditor"
    "ln-610-code-comments-auditor"
    "ln-620-codebase-auditor"
    "ln-621-security-auditor"
    "ln-622-build-auditor"
    "ln-623-code-principles-auditor"
    "ln-624-code-quality-auditor"
    "ln-625-dependencies-auditor"
    "ln-626-dead-code-auditor"
    "ln-627-observability-auditor"
    "ln-628-concurrency-auditor"
    "ln-629-lifecycle-auditor"
    "ln-630-test-auditor"
    "ln-631-test-business-logic-auditor"
    "ln-632-test-e2e-priority-auditor"
    "ln-633-test-value-auditor"
    "ln-634-test-coverage-auditor"
    "ln-635-test-isolation-auditor"
    "ln-640-pattern-evolution-auditor"
    "ln-641-pattern-analyzer"
    "ln-642-layer-boundary-auditor"
    "ln-643-api-contract-auditor"
    "ln-644-dependency-graph-auditor"
    "ln-650-persistence-performance-auditor"
    "ln-651-query-efficiency-auditor"
    "ln-652-transaction-correctness-auditor"
    "ln-653-runtime-performance-auditor"

    # Bootstrap/infrastructure (web/devops focused)
    "ln-700-project-bootstrap"
    "ln-710-dependency-upgrader"
    "ln-711-npm-upgrader"
    "ln-712-nuget-upgrader"
    "ln-713-pip-upgrader"
    "ln-720-structure-migrator"
    "ln-721-frontend-restructure"
    "ln-722-backend-generator"
    "ln-723-seed-data-generator"
    "ln-724-artifact-cleaner"
    "ln-730-devops-setup"
    "ln-731-docker-generator"
    "ln-732-cicd-generator"
    "ln-733-env-configurator"
    "ln-740-quality-setup"
    "ln-741-linter-configurator"
    "ln-742-precommit-setup"
    "ln-743-test-infrastructure"
    "ln-750-commands-generator"
    "ln-751-command-templates"
    "ln-760-security-setup"
    "ln-761-secret-scanner"
    "ln-770-crosscutting-setup"
    "ln-771-logging-configurator"
    "ln-772-error-handler-setup"
    "ln-773-cors-configurator"
    "ln-774-healthcheck-setup"
    "ln-775-api-docs-generator"
    "ln-783-container-launcher"
)

Remove-SkillDirs -ParentPath $workflowRoot -RemoveList $workflowRemove -Label "Workflow Skills Cleanup (83 dirs)"

# ═══════════════════════════════════════════════════════
# 3. SUMMARY
# ═══════════════════════════════════════════════════════
Write-Host "=== Summary ===" -ForegroundColor Cyan
$remaining = (Get-ChildItem -Path $skillsRoot -Recurse -File).Count
Write-Host "Files removed (or would be): $totalDeleted" -ForegroundColor White
Write-Host "Total files remaining in skills/: $remaining" -ForegroundColor White

if ($DryRun) {
    Write-Host ""
    Write-Host "This was a dry run. Re-run without -DryRun to apply changes." -ForegroundColor Yellow
}