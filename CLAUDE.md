# CLAUDE.md — mac-md-win

## What this repo is

A curated library of agent skills for .NET development and structured
project workflows. Skills are self-contained markdown instructions
organized under `.claude/skills/`.

## Repo structure

.claude/skills/
├── community/       # Contributing guidelines and community docs
├── dotnet/          # C# / .NET coding standards, patterns, and tooling
│   ├── agents/      # Agent role definitions
│   └── skills/      # 13 skill directories (EF Core, serialization, etc.)
└── workflow/        # Multi-agent development workflow (epics → stories → tasks)
    ├── shared/      # Shared references, templates, and conventions (44 files)
    ├── docs/        # Workflow documentation
    └── ln-*/        # 15 numbered skill directories (research → decompose → execute → verify)

## How to use skills

Each skill directory contains a `skill.md` or `README.md` with complete
instructions. Read the full skill file before acting on it.

**Dotnet skills** are standalone guidance — apply them when writing or
reviewing C# code. They do not depend on each other.

**Workflow skills** are numbered and sequential. The `ln-` prefix
indicates execution order. Most depend on files in `shared/`.
Do NOT attempt to chain workflow skills autonomously — execute one
at a time and confirm results before proceeding.

## Critical rules

1. Read the entire skill file before starting work. Do not skim.
2. Follow file paths exactly. Paths in skills are relative to the
   skills repo root (`.claude/skills/`), not the working directory.
3. When a skill references `shared/`, look in `.claude/skills/workflow/shared/`.
4. Do not invent conventions. If a skill doesn't specify something, ask.
5. Commit messages follow conventional commits: `feat:`, `fix:`, `docs:`, `refactor:`.

## Inventory

See `.claude/skills/MANIFEST.md` for a generated index of all skill
groups, directories, and file counts.