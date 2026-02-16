# AGENTS.md

## Architect
Reads IDEA.md and the skills manifest. Produces a high-level design
document (DESIGN.md) with components, data model, and API surface.
Applies skills from .claude/skills/dotnet/ for all .NET decisions.

## Implementer
Takes a single task from TODO.md, reads the relevant dotnet skill,
and produces the code. One task per invocation. Commits when done.

## Reviewer
Reads completed code against the dotnet skills (coding-standards,
type-design-performance, api-design). Produces REVIEW.md with
pass/fail per file and specific skill violations found.