# Session Management Skill v1.0

This skill provides session-aware pacing and resource management for AI coding agents. Follow all instructions below throughout your entire working session.

---

## 1. Session Budget Awareness

You are operating under a finite session budget. Track your resource usage throughout the session.

### Default Budget Parameters
- **Budget Window:** 300 minutes (5 hours)
- **Estimated Max Tool Calls:** 80 per window
- **Estimated Max Tokens:** 200,000 per window

Override these defaults if a `config/session-defaults.yaml` file is present.

### Budget Tracking
After every tool call, mentally increment your internal call counter. After every milestone (completing a file, module, test suite, or logical unit of work), report your status:

```
📊 Checkpoint: ~[N] tool calls used | Budget: [ZONE] | Sprint [X] of session
```

### Budget Zones

| Zone     | Usage   | Color  | Behavior                                    |
|----------|---------|--------|---------------------------------------------|
| GREEN    | 0-50%   | 🟢     | Full speed. All operations permitted.        |
| YELLOW   | 50-75%  | 🟡     | Conserve. Batch operations, skip optional.   |
| RED      | 75-90%  | 🔴     | Planning only. Outlines, TODOs, docs.        |
| CRITICAL | 90-100% | ⛔     | Stop. Write final status and halt all work.  |

---

## 2. Work Cadence — Pomodoro Mode

Structure all work into sprint cycles to prevent budget burnout.

### Sprint Cycle
1. **SPRINT** — Work for up to 20 tool calls or ~15 minutes of activity
2. **CHECKPOINT** — Summarize progress, update tracking docs, estimate budget zone
3. **REST** — Execute synchronous sleep for 5 minutes (see Sleep Commands below)
4. **REPEAT** — Begin next sprint

After **4 consecutive sprints**, take an **extended rest of 15 minutes**.

### Sleep Commands

**Linux:**
```bash
echo "⏸️  Resting until $(date -d '+N minutes' '+%H:%M:%S')..." && sleep $((N * 60)) && echo "▶️  Resuming work."
```

**macOS:**
```bash
echo "⏸️  Resting for N minutes at $(date '+%H:%M:%S')..." && sleep $((N * 60)) && echo "▶️  Resuming work at $(date '+%H:%M:%S')."
```

**With countdown script (if available):**
```bash
bash scripts/session-timer.sh N
```

Replace `N` with the number of minutes. These commands block synchronously, preventing any other activity until the timer completes.

### Pre-Sprint Checklist
Before starting each sprint, confirm:
- [ ] What is the specific goal for this sprint?
- [ ] What is my current budget zone?
- [ ] Am I continuing previous work or starting something new?
- [ ] Are there any pending rate limit issues?

---

## 3. Rate Limit Detection and Response

Monitor for signs of rate limiting or resource exhaustion.

### Detection Signals
- HTTP 429 (Too Many Requests) or 529 (Overloaded) status codes
- API errors mentioning "rate limit", "capacity", "quota", or "throttle"
- Unusually slow responses (>30 seconds for simple operations)
- Repeated failures on the same operation (3+ consecutive failures)
- Timeout errors

### Response Protocol

**On first detection:**
1. STOP current work immediately
2. Log the event to SESSION_LOG.md (if logging enabled)
3. Execute REST for **5 minutes**
4. Retry the failed operation once

**On second detection (within same sprint):**
1. STOP and log
2. Execute REST for **10 minutes**
3. Retry once, then continue with reduced scope

**On third detection (within same sprint):**
1. STOP and log
2. Execute REST for **30 minutes**
3. Write status report to PROGRESS.md
4. Resume in YELLOW zone regardless of actual budget

### Backoff Schedule
| Attempt | Wait Time | Next Action             |
|---------|-----------|-------------------------|
| 1st     | 5 min     | Retry once              |
| 2nd     | 10 min    | Retry with reduced scope|
| 3rd     | 30 min    | Status report + resume  |
| 4th+    | 60 min    | Enter CRITICAL zone     |

---

## 4. Budget Conservation Strategies

Adapt your working style based on your current budget zone.

### GREEN Zone (0-50% used)
- Full operations: read, write, edit, test, refactor
- Multi-file changes permitted
- Exploratory work and research allowed
- Normal sprint cadence

### YELLOW Zone (50-75% used)
- Batch file reads (read multiple files in one call where possible)
- Skip optional improvements (formatting, minor refactors)
- Prefer editing over rewriting entire files
- Combine related changes into single operations
- No exploratory or speculative work
- Reduce sprint size to 15 tool calls

### RED Zone (75-90% used)
- **Planning only** — no file edits
- Write outlines, TODOs, and documentation
- Summarize remaining work with specific instructions
- Update PROGRESS.md with detailed next steps
- Prepare handoff notes for next session

### CRITICAL Zone (90-100% used)
- **Stop all work immediately**
- Write final status report including:
  - What was completed
  - What is in progress (with state)
  - What remains (prioritized list)
  - Any blockers or issues discovered
- Save to PROGRESS.md and SESSION_LOG.md
- Do not attempt any further tool calls

### Always
- Complete the current atomic unit of work before entering a lower zone
- Never leave files in a broken or half-edited state
- Commit or save before resting

---

## 5. Multi-Agent Budget Splitting

When operating in a multi-agent configuration with sub-agents:

### Budget Allocation
- **Orchestrator** reserves **20%** of total session budget for coordination
- Remaining **80%** is split equally among active sub-agents
- Example: 80 total calls → Orchestrator gets 16, each of 4 sub-agents gets 16

### Sub-Agent Requirements
Each sub-agent MUST:
- Track its own call count independently
- Report call count when returning results to orchestrator
- Respect its individual budget allocation
- Enter its own REST cycles independently

### Orchestrator Requirements
The orchestrator MUST:
- Track total calls across all agents
- Flag any sub-agent task that exceeds 15 tool calls as "expensive"
- Redistribute unused budget from completed sub-agents
- Enter YELLOW zone when aggregate usage hits 50%

### Sub-Agent Budget Exceeded Protocol
If a sub-agent exceeds its allocated budget:
1. Sub-agent completes current atomic operation
2. Sub-agent reports: "⚠️ Budget exceeded. [N] calls used of [M] allocated."
3. Orchestrator decides: reallocate from other agents OR queue remaining work
4. Sub-agent does NOT continue without orchestrator approval

---

## 6. Session Logging

Maintain a session log for debugging and optimization.

### Log File
Write entries to `SESSION_LOG.md` (or configured `log_file` path).

### Entry Format
```
## [TIMESTAMP] — [EVENT_TYPE]
- **Sprint:** [N]
- **Tool Calls:** [count] / [budget]
- **Zone:** [GREEN|YELLOW|RED|CRITICAL]
- **Event:** [description]
- **Files Touched:** [list]
```

### Event Types
- `SESSION_START` — Beginning of session with config summary
- `SPRINT_COMPLETE` — End of a sprint with summary
- `REST_START` — Entering rest period
- `REST_END` — Resuming from rest
- `RATE_LIMIT` — Rate limit detected
- `ZONE_CHANGE` — Budget zone transition
- `SESSION_END` — End of session with final summary

### End-of-Session Summary
At session end, append a summary block:
```
## Session Summary
- **Duration:** [X] minutes
- **Total Tool Calls:** [N]
- **Sprints Completed:** [N]
- **Rate Limits Hit:** [N]
- **Files Created:** [list]
- **Files Modified:** [list]
- **Key Accomplishments:** [list]
- **Remaining Work:** [list]
```

---

## 7. Platform-Specific Notes

### Claude Code
- Include this file via `CLAUDE.md`: "Read and follow SKILL.md"
- Sleep commands use `bash` tool
- Sub-agents inherit from AGENTS.md
- Permission prompts count toward cognitive load but not tool calls

### Cursor
- Include via `.cursorrules`: reference SKILL.md content
- Cursor uses background/foreground agent distinction
- Background agents should have smaller sprint sizes (10 calls)

### Aider
- Include via `.aider.conf.yml` read directive
- Aider's /run command can execute sleep scripts
- Map "tool calls" to aider edit cycles

### Windsurf
- Include via `.windsurfrules` or Cascade settings
- Windsurf Flows map to sprints naturally
- Use Windsurf's built-in terminal for sleep commands

### Cline
- Include via `.clinerules`
- Cline's auto-approve mode burns budget fast — reduce sprint size to 10
- Use Cline's terminal integration for sleep

### Generic / Custom Agents
- Paste SKILL.md content into your system prompt or agent instructions
- Adapt sleep commands to your agent's shell execution capability
- If no shell access, use the checkpoint/reporting system without automated sleep