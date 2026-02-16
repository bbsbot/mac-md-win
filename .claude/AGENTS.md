# AGENTS.md — How Autonomous Agents Work in This Repo

This document defines agent roles, responsibilities, and safe collaboration rules.

## Core Principle
Work like a small product team shipping a Windows app:
- one agent “owns” architecture consistency
- others implement focused slices
- everything integrates cleanly in small increments

## Required Roles (conceptual)
These are *responsibilities*, not necessarily separate processes.

### 1) Architect / Integrator
Owns:
- solution structure (`src/`, `tools/`, `docs/`)
- tech stack consistency (.NET 8, WinUI 3)
- integration of features from other agents
Rules:
- may request refactors, but only when build is green

### 2) UI Agent
Owns:
- XAML layout
- theme resources
- navigation + selection behavior
- keyboard shortcuts and command bindings (later)
Rules:
- must keep UI testable even with placeholder data

### 3) Preview/Rendering Agent
Owns:
- Markdown -> HTML generation (Markdig)
- WebView2 hosting
- debounce updates
Rules:
- keep preview deterministic; avoid network-only dependencies where possible

### 4) Persistence Agent
Owns:
- SQLite schema
- CRUD services
- migrations (simple versioning)
Rules:
- local-first; no cloud

### 5) Localization/Tooling Agent
Owns:
- Python scripts
- JSON -> .resw generation
- keeping language codes mapping correct for Windows
Rules:
- don’t break existing Apple pipelines; add Windows output as a new target

## Collaboration Rules
- Work in small commits.
- Don’t create “mega PRs” that touch everything.
- Prefer adding new files over rewriting existing ones.
- Keep the build green at all times.
- Use `/docs/decisions/` to record major choices (ADR-style).

## Testing Gates
At the end of each milestone:
1. Agents must ensure the build succeeds.
2. Agents must provide the user with:
   - exact build command(s)
   - exact run instructions
   - what to click/test
3. STOP. Wait for user confirmation.

## What agents should write down
Agents must keep a short running log in:
- `/docs/STATUS.md` (what’s done, what’s next, what’s blocked)
- `/docs/KNOWN_ISSUES.md` (issues discovered during development)

## “Don’t Surprise the User”
If a decision changes the tech stack, packaging approach, or licensing, STOP and ask first.