# Subagent Spawning Guide

> **Purpose**: Comprehensive guide for AI agents on when and how to spawn subagents during Claude Code runs.

---

## Quick Reference

```
Spawn subagent when:
  - Task generates intermediate noise â†’ YES
  - Independent investigation â†’ YES
  - Verification/testing â†’ YES
  - Needs user clarification â†’ NO
  - Has dependencies on other tasks â†’ MAYBE (sequential)
  - Judgment call for main thread â†’ NO
```

---

## 1. When to Spawn Subagents

### Decision Heuristic

Before starting any investigative or verification task, ask:

**"Will this generate intermediate noise that pollutes my main context?"**

If YES, spawn a subagent.

### Always Delegate

| Task Type | Examples | Why Delegate |
|-----------|----------|--------------|
| **Codebase Exploration** | "How is X implemented?", "Find all usages of Y", "Trace data flow" | Generates search results, file contents, intermediate analysis |
| **Verification Tasks** | Running tests, type-checking, linting, compilation checks | Output noise, error traces, pass/fail details |
| **Investigation** | Reading docs, understanding APIs, summarizing file responsibilities | Exploratory reading, dead ends, context building |
| **Search** | Pattern matching, file location, dependency tracing | Many results to filter through |
| **Data Queries** | Schema exploration, data validation, count verification | Query results, JSON dumps, analysis |

### Never Delegate

| Task Type | Why Keep in Main Thread |
|-----------|-------------------------|
| **User Clarification Needed** | Subagents cannot prompt user for input |
| **Multi-step Interdependent Operations** | Main thread tracks state across steps |
| **Judgment Calls** | Decisions should surface to conversation |
| **Irreversible Actions** | Deployments, migrations need user visibility |
| **Final Deliverables** | User expects to see final work in main thread |

### Cost-Benefit Analysis

```
Spawning Cost:
  - ~5 seconds overhead
  - Prompt construction time
  - Result reading time

Context Pollution Cost:
  - Search results: 500-5000 tokens
  - File contents: 200-2000 tokens per file
  - Error traces: 100-1000 tokens
  - Investigation dead ends: 500-3000 tokens

Rule: If task might generate >500 tokens of intermediate output â†’ SPAWN
```

### Examples

**Good Subagent Usage:**
```
Main Task: "Fix the database connection timeout bug"

Subagent 1: "Search codebase for all timeout configurations"
  â†’ Returns: FINDINGS.md with 3 relevant files identified

Subagent 2: "Trace the connection lifecycle from pool creation to query"
  â†’ Returns: FINDINGS.md with data flow diagram

Main Thread: Reads findings, implements fix with user visibility
```

**Bad Subagent Usage:**
```
Main Task: "What color should the button be?"

Subagent: [WRONG - this is a judgment call needing user input]

Correct: Keep in main thread, ask user directly
```

---

## 2. Subagent Working Directories

### Location

Subagent directories live within the parent run's directory:

```
runs/CLAUDE-RUNS/<RUN-ID>-<slug>/
â”œâ”€â”€ TASK_LOG.md           # Parent run's log
â”œâ”€â”€ SPEC_v1.md            # Parent run's spec
â””â”€â”€ subagents/            # All subagent work
    â”œâ”€â”€ YYYYMMDD-HHMM-<descriptive-slug>/
    â”‚   â”œâ”€â”€ FINDINGS.md   # Primary deliverable (REQUIRED)
    â”‚   â””â”€â”€ [other files] # Supporting materials
    â””â”€â”€ YYYYMMDD-HHMM-<another-slug>/
        â””â”€â”€ FINDINGS.md
```

### Naming Convention

```
subagents/YYYYMMDD-HHMM-<descriptive-slug>/
```

**Components:**
- `YYYYMMDD`: Date in year-month-day format
- `HHMM`: Time in 24-hour format
- `<descriptive-slug>`: Lowercase, hyphen-separated task description

**Examples:**
```
subagents/20251227-1430-verify-schema/
subagents/20251227-1445-trace-connection-lifecycle/
subagents/20251227-1500-audit-test-coverage/
```

### Required Files

**FINDINGS.md** - The primary deliverable (REQUIRED)

Every subagent MUST produce a `FINDINGS.md` with this structure:

```markdown
# [Task Description] - Findings

**Agent ID:** YYYYMMDD-HHMM-<slug>
**Created:** YYYY-MM-DD HH:MM zzz
**Parent Task:** RUN-YYYYMMDD-HHMM-<parent-slug>
**Status:** Complete | In Progress | Blocked

---

## Summary

[2-3 sentence executive summary of findings]

---

## Findings

### [Finding Category 1]

[Details, evidence, code snippets]

### [Finding Category 2]

[Details, evidence, code snippets]

---

## Recommendations

- [Actionable recommendation 1]
- [Actionable recommendation 2]

---

## Files Examined

- `path/to/file1.py` - [why examined]
- `path/to/file2.py` - [why examined]
```

### Optional Files

| File | When to Include |
|------|-----------------|
| `README.md` | Complex investigations with multiple output files |
| `SUMMARY.md` | When FINDINGS.md is very long (>200 lines) |
| `TASK_LOG.md` | Multi-hour investigations needing progress tracking |
| `*_REPORT.md` | Specialized detailed reports (e.g., `SECURITY_REPORT.md`) |
| `*.json` | Raw data outputs for later processing |
| `*.sql` | Query files for reproducibility |

---

## 3. Subagent Prompt Template

When spawning a subagent, use this prompt structure:

```markdown
You are Subagent [Letter]: [Brief Role Description].

**Your Working Directory:**
`runs/CLAUDE-RUNS/[PARENT-RUN-ID]/subagents/YYYYMMDD-HHMM-<slug>/`

Save all work to this directory. Your primary deliverable is `FINDINGS.md`.

## Task

[Clear, specific task description - 2-4 sentences]

## Context

[Background information the subagent needs to understand the task]
[Reference parent task if relevant]

## Scope

**In Scope:**
- [Explicit item 1]
- [Explicit item 2]

**Out of Scope:**
- [What NOT to investigate]
- [Boundaries to respect]

## Constraints

- [Time/resource constraints]
- [Files to focus on or avoid]

## Deliverables

1. **Primary:** `FINDINGS.md` - [What it should contain]
2. **Optional:** `[other-file.md]` - [When to create]

## Success Criteria

- [ ] [Measurable criterion 1]
- [ ] [Measurable criterion 2]
```

### Example Prompt

```markdown
You are Subagent A: TEI/Markdown Pipeline Validator.

**Your Working Directory:**
`runs/CLAUDE-RUNS/RUN-YYYYMMDD-HHMM-<slug>/subagents/YYYYMMDD-HHMM-validate-segment-mapping/`

Save all work to this directory. Your primary deliverable is `FINDINGS.md`.

## Task

Verify that the `users` table in the project database contains all required columns
for the registration flow query.

## Context

The parent task is debugging why new user registrations fail. We need to confirm
the schema matches expectations before investigating the query logic.

## Scope

**In Scope:**
- Verify table exists
- Check column names and types
- Confirm nullable constraints

**Out of Scope:**
- Query performance
- Data quality/content
- Other tables

## Constraints

- Do not modify any data
- Focus only on the users table

## Deliverables

1. **Primary:** `FINDINGS.md` - Schema validation results with column matrix
2. **Optional:** `table_schema.md` - Full schema output if useful

## Success Criteria

- [ ] Confirmed table exists (yes/no)
- [ ] Listed all columns with types
- [ ] Identified any missing columns vs expected
```


---

## 4. Parallel vs Sequential Subagents

### When to Run in Parallel

**Parallel** when tasks are:
- Independent (no shared state)
- Reading different parts of codebase
- Querying different data
- Producing separate deliverables

```
Main Thread spawns simultaneously:
â”œâ”€â”€ Subagent A: Trace auth flow        [independent]
â”œâ”€â”€ Subagent B: Audit test coverage    [independent]
â””â”€â”€ Subagent C: Check DB schema        [independent]

All write to separate directories â†’ No conflicts
```

### When to Run Sequentially

**Sequential** when:
- Subagent B needs Subagent A's findings
- One investigation informs the next
- Shared resources (same file, same DB transaction)

```
Main Thread spawns:
1. Subagent A: Identify all config files
   â†’ Produces: list of 5 config files

2. [Main thread reads A's findings]

3. Subagent B: Analyze each config file for security issues
   â†’ Uses: A's file list as input
```

### Coordination Patterns

**Pattern 1: Fan-Out (Parallel)**
```
Main Thread
    â”œâ”€â”€â†’ Subagent A â”€â”€â†’ FINDINGS.md
    â”œâ”€â”€â†’ Subagent B â”€â”€â†’ FINDINGS.md
    â””â”€â”€â†’ Subagent C â”€â”€â†’ FINDINGS.md

Main Thread reads all three, synthesizes
```

**Pattern 2: Pipeline (Sequential)**
```
Main Thread
    â””â”€â”€â†’ Subagent A â”€â”€â†’ FINDINGS.md
                            â†“
Main Thread reads, passes to:
    â””â”€â”€â†’ Subagent B â”€â”€â†’ FINDINGS.md (uses A's output)
```

**Pattern 3: Hybrid**
```
Phase 1 (parallel):
    â”œâ”€â”€â†’ Subagent A: Explore codebase
    â””â”€â”€â†’ Subagent B: Query database

Phase 2 (sequential, uses phase 1):
    â””â”€â”€â†’ Subagent C: Correlate code + data findings
```

### Maximum Parallelism

**Recommended:** 2-3 concurrent subagents
**Maximum:** 5 concurrent subagents

Beyond 5 subagents:
- Result integration becomes complex
- Risk of conflicting recommendations
- User loses track of investigation threads

---

## 5. Collecting Results

### Reading Subagent Findings

After spawning a subagent, read its deliverables back:

```python
# Conceptual workflow (not actual code)

1. Spawn subagent with prompt
2. Wait for completion signal
3. Read: subagents/YYYYMMDD-HHMM-<slug>/FINDINGS.md
4. Extract key findings into main context
5. Continue main thread work
```

### Integration Checklist

After each subagent completes:

- [ ] Read `FINDINGS.md` in full
- [ ] Extract actionable items to main thread
- [ ] Note any follow-up investigations needed
- [ ] Update parent `TASK_LOG.md` with subagent summary
- [ ] If findings change scope, consider new `SPEC_vN.md`

### Result Summary Pattern

In the parent `TASK_LOG.md`, log subagent results:

```markdown
### YYYY-MM-DD HH:MM zzz - Subagent Results Integration

**Subagent A (YYYYMMDD-HHMM-verify-schema):**
- Status: Complete
- Key Finding: All 7 required columns present
- Action: Proceed with query investigation (schema confirmed)
- Full report: subagents/YYYYMMDD-HHMM-verify-schema/FINDINGS.md

**Subagent B (YYYYMMDD-HHMM-trace-query-flow):**
- Status: Complete
- Key Finding: row_limit=1 causing truncation
- Action: Fix identified in hydration.py line 304
- Full report: subagents/YYYYMMDD-HHMM-trace-query-flow/FINDINGS.md
```

### Handling Subagent Failures

If a subagent's investigation fails or is inconclusive:

1. **Document the failure** in parent TASK_LOG.md
2. **Note what was tried** (avoid re-trying same approach)
3. **Consider alternative approach** with new subagent
4. **Escalate to user** if blocked

```markdown
### YYYY-MM-DD HH:MM zzz - Subagent Blocked

**Subagent C (YYYYMMDD-HHMM-inspect-remote-db):**
- Status: BLOCKED
- Issue: Cannot connect to production database (credentials expired)
- Action: Escalated to user - need refreshed credentials
- Partial findings: subagents/YYYYMMDD-HHMM-inspect-remote-db/FINDINGS.md
```

---

**Last Updated:** 2026-03-07


