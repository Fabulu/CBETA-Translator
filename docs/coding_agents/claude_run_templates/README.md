# Claude Run Templates

Templates for standardizing Claude Code run directories. Used by `runs/CLAUDE-RUNS/init-run.ps1`.

## Available Templates

| Template | Purpose | Used By |
|----------|---------|---------|
| TASK_LOG/ | Run progress timeline | init-run.ps1 (auto-generated) |
| SPEC/ | Externalized task state | init-run.ps1 (auto-generated) |
| FINDINGS/ | Subagent deliverables | Subagents (manual copy) |

## Usage

### Automatic (via init-run.ps1)

```powershell
cd runs/CLAUDE-RUNS
.\init-run.ps1 fix-auth-bug
# Creates RUN-YYYYMMDD-HHMM-fix-auth-bug/ with TASK_LOG.md and SPEC_v1.md
```

### Manual (for subagent work)

Subagents create their own directory under `subagents/` and write their findings there:

```
runs/CLAUDE-RUNS/RUN-YYYYMMDD-HHMM-<slug>/
â”œâ”€â”€ TASK_LOG.md
â”œâ”€â”€ SPEC_v1.md
â””â”€â”€ subagents/
    â””â”€â”€ 20251228-1430-trace-auth-flow/   # Subagent's working directory
        â”œâ”€â”€ FINDINGS.md                   # Required deliverable
        â”œâ”€â”€ test_queries.sql              # Optional: helper files
        â””â”€â”€ debug_output.json             # Optional: intermediate data
```

## Template Variables

Templates use `{{VARIABLE}}` placeholders:

- `{{RUN_ID}}` - YYYYMMDD-HHMM format
- `{{SLUG}}` - Task slug (lowercase, hyphen-separated)
- `{{TIMESTAMP}}` - YYYY-MM-DD HH:MM zzz (local timezone) format
- `{{DESCRIPTION}}` - Task description (defaults to placeholder)

## Subagent Async Pattern

**Key Concept**: Subagents are *file-based*, not *context-based*.

1. **Main thread spawns** subagent with explicit working directory in prompt
2. **Subagent works** independently, writes all findings to its directory
3. **Main thread reads** `FINDINGS.md` after subagent completes
4. **No context pollution** - intermediate work stays in subagent's directory

## Related Documentation

- [SUBAGENT_GUIDE.md](../SUBAGENT_GUIDE.md) - Complete subagent spawning reference
- [CLAUDE.md](../../CLAUDE.md) - Main agent instructions

