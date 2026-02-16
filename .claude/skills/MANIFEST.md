# Skills Manifest

Auto-generated inventory of curated agent skills.
Generated: 2026-02-16 00:19

---

## community/ (2 files)

### Root files

- `CONTRIBUTING.md`
- `README.md`

---

## dotnet/ (24 files)

### Root files

- `AGENTS.md`
- `CLAUDE.md`
- `README.md`
- `RELEASE_NOTES.md`

### Config / support directories

- `.claude-plugin/` (2 files)
- `agents/` (5 files)

### Skills (13 directories)

- **csharp-api-design/** (1 files) -- - Designing public APIs for NuGet packages or libraries
- **csharp-coding-standards/** (1 files) -- - Writing new C# code or refactoring existing code
- **csharp-concurrency-patterns/** (1 files) -- - Deciding how to handle concurrent operations in .NET
- **csharp-type-design-performance/** (1 files) -- - Designing new types and APIs
- **database-performance/** (1 files) -- - Designing data access layers
- **efcore-patterns/** (1 files) -- - Setting up EF Core in a new project
- **local-tools/** (1 files) -- - Setting up consistent tooling across a development team
- **microsoft-extensions-configuration/** (1 files) -- - Binding configuration from appsettings.json to strongly-typed classes
- **microsoft-extensions-dependency-injection/** (1 files) -- - Organizing service registrations in ASP.NET Core applications
- **package-management/** (1 files) -- - Adding, removing, or updating NuGet packages
- **project-structure/** (1 files) -- - Setting up a new .NET solution with modern best practices
- **serialization/** (1 files) -- - Choosing a serialization format for APIs, messaging, or persistence
- **skills-index-snippets/** (1 files) -- - Adding, removing, or renaming any skills or agents in this repository

---

## workflow/ (78 files)

### Root files

- `CHANGELOG.md`
- `CLAUDE.md`
- `CONTRIBUTING.md`
- `README.md`

### Config / support directories

- `.claude/` (2 files)
- `.claude-plugin/` (1 files)
- `docs/` (6 files)
- `hooks/` (2 files)
- `shared/` (44 files)

### Skills (15 directories)

- **ln-001-standards-researcher/** (2 files) -- Research industry standards, RFCs, and architectural patterns for a given Epic/Story domain. Produce a Standards Rese...
- **ln-002-best-practices-researcher/** (1 files) -- Research industry standards and create project documentation in one workflow.
- **ln-003-push-all/** (1 files) -- **Type:** Standalone Utility
- **ln-004-agent-sync/** (1 files) -- **Type:** Standalone Utility
- **ln-200-scope-decomposer/** (1 files) -- Top-level orchestrator for complete initiative decomposition from scope to User Stories through Epic and Story coordi...
- **ln-300-task-coordinator/** (1 files) -- Coordinates creation or replanning of implementation tasks for a Story. Builds the ideal plan first, then routes to w...
- **ln-301-task-creator/** (1 files) -- Worker that generates task documents and creates Linear issues for implementation, refactoring, or test tasks as inst...
- **ln-400-story-executor/** (1 files) -- Executes a Story end-to-end by looping through its tasks in priority order. Sets Story to **To Review** when all task...
- **ln-401-task-executor/** (1 files) -- Executes a single implementation (or refactor) task from Todo to To Review using the task description and linked guides.
- **ln-500-story-quality-gate/** (1 files) -- Thin orchestrator that coordinates quality checks and test planning, then determines final Story verdict.
- **ln-510-quality-coordinator/** (3 files) -- Sequential coordinator for code quality pipeline. Invokes 4 workers in index order (511 -> 512 -> 513 -> 514) and ret...
- **ln-511-code-quality-checker/** (2 files) -- Analyzes Done implementation tasks with quantitative Code Quality Score based on metrics, MCP Ref validation, and iss...
- **ln-780-bootstrap-verifier/** (1 files) -- **Type:** L2 Domain Coordinator
- **ln-781-build-verifier/** (1 files) -- **Type:** L3 Worker
- **ln-782-test-runner/** (1 files) -- **Type:** L3 Worker

---

## Totals

- **Groups:** 3
- **Skill directories:** 28
- **Total files:** 104
