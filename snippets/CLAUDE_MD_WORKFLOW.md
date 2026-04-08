---

## 🎯 Core Task Execution Protocol

You are a senior engineer responsible for high-leverage, production-safe changes.
Follow this workflow **without exception**:

### 1. Clarify Scope First

- Initialize a new run: `cd runs/CLAUDE-RUNS && .\init-run.ps1 <slug>`
- Add entry to [Active Tasks](#-active-tasks) section
- Map out your approach before writing code
- Confirm your interpretation with the user
- Fill in `SPEC_v1.md` with scope and constraints

### 2. Locate Exact Code Insertion Point

- Identify precise file(s) and line(s)
- Never make sweeping edits across unrelated files
- Justify each file modification explicitly

### 3. Minimal, Contained Changes

- Only write code directly required for the task
- No speculative changes or "while we're here" edits
- Isolate logic to avoid breaking existing flows

### 4. Double Check Everything

- Review for correctness and side effects
- Align with existing codebase patterns

### 5. Deliver Clearly

- Summarize what changed and why
- List every file modified
- Flag assumptions or risks

---

## 🔄 Agent Task Tracking Protocol (Self-Updating System)

### Overview

This section enables Claude Code instances to track their work across sessions and
parallel workstreams by **dynamically updating this CLAUDE.md file**.

### Protocol Rules (MANDATORY)

#### 1. Starting ANY Task

When beginning work (bug fix, feature, analysis, etc.):

1. **Initialize Run Directory:**

   ```powershell
   cd runs/CLAUDE-RUNS; .\init-run.ps1 <slug>
   # Example: .\init-run.ps1 fix-auth-bug
   # Creates: RUN-YYYYMMDD-HHMM-fix-auth-bug/ with templated files
   ```

2. **Read Subagent Guide (for investigation/verification tasks):**

   [`docs/coding_agents/SUBAGENT_GUIDE.md`](docs/coding_agents/SUBAGENT_GUIDE.md)

   **Key pattern:** Subagents write to their own `subagents/YYYYMMDD-HHMM-slug/` directory.
   Main thread reads `FINDINGS.md` files afterward (file-based, not context-based).

3. **Update "Active Tasks" Section Below:**

   - Add new entry with Run ID, status, context
   - Mark as "In Progress"

4. **Begin Work:**

   - Update `TASK_LOG.md` continuously with detailed progress
   - Update `SPEC_v1.md` with scope, decisions, and what's been ruled out

#### 2. During Task Execution

- **Update `TASK_LOG.md`** (in working directory) with:

  - ✅ Completed steps (detailed)
  - 🔄 Current action (with timestamps)
  - ⏳ Pending steps
  - 📁 Files created/modified (with paths)
  - ⚠️ Blockers or questions
  - 🔍 Key findings or decisions

- **Create new `SPEC_vN.md` file** when state changes materially:

  - Scope boundaries shift → new version
  - General approach fails (add to "Don't Retry") → new version
  - User clarifies/changes requirements → new version
  - Minor clarification only → note in TASK_LOG, no new SPEC version
  - **Blocker:** Do not create a new SPEC version without first confirming with the user
  - **Proactive:** Suggest proactively creating new SPEC versions if applicable

SPEC_vN.md captures the contract — what success looks like, what's out of scope,
what's been decided, what failed and shouldn't be retried.
TASK_LOG.md captures the narrative — what actually happened chronologically.

After compaction or instance swap, re-read the current SPEC version to recover where you are.

**Immutable versioning:**

- Never edit an existing SPEC file
- Scope/constraint/failure-knowledge change → create `SPEC_v2.md`, `SPEC_v3.md`, etc.
- Each new version notes what changed from prior version and links back

#### 3. Task Completion Protocol (CRITICAL)

When you believe a task is complete:

**❌ DO NOT automatically remove the task from CLAUDE.md**

Instead:

1. **Update Task Status:**

   ```markdown
   **Status:** ✅ READY FOR REVIEW - Awaiting User Approval
   ```

2. **Summarize in TASK_LOG.md:**

   - What was accomplished
   - Files created/modified
   - Any follow-up needed

3. **Validate Docstrings (MANDATORY):**

   - Create `docstring_validation.md` in the run directory
   - Audit ALL files modified during this run
   - Format: See `docs/templates/docstring_validation_template.md`
   - **BLOCKING:** Resolve all discrepancies before proceeding

   **Validation Scope:**
   - ✅ Files you created (new files)
   - ✅ Files you modified (changed code)
   - ❌ Files you only read

4. **Ask User Permission:**

   ```
   "Task RUN-YYYYMMDD-HHMM appears complete.

   Summary:
   - [Brief outcome]
   - Files modified: [count]
   - Docstring validation: [✅ All accurate | ⚠️ X issues found and resolved]
   - Files in: runs/CLAUDE-RUNS/<RUN-ID>-<slug>/

   May I archive this task and remove it from Active Tasks in CLAUDE.md?"
   ```

5. **If User Approves:**

   - Remove task entry from "Active Tasks" in CLAUDE.md
   - Run `.\eng\build.ps1` (or `dotnet build .\ReadZen.App.csproj -c Debug`) as a post-change quality gate
   - Add entry to `runs/CLAUDE-RUNS/ARCHIVE.md` (see template in that file)
   - Keep working directory intact (never auto-delete)

6. **If User Rejects:**

   - Mark status back to "In Progress"
   - Continue work based on user feedback

#### 4. Parallel Instance Disambiguation

If running multiple Claude Code instances:

- **Declare Your Instance:**

  ```markdown
  **Agent Instance:** Terminal 1 (PowerShell)
  **Agent Instance:** VS Code Terminal 2
  ```

- **Resume Detection:**

  - If user mentions a specific Run ID → Resume that task
  - If ambiguous → Ask user which task they're continuing

- **Context Recovery:**

  - After compaction or instance swap → Re-read `SPEC_vN.md` (latest version)
  - Check "Don't Retry" section before attempting any approach

#### 5. Task Log Format

Generated from template at
[`docs/coding_agents/claude_run_templates/TASK_LOG/TASK_LOG.md`](docs/coding_agents/claude_run_templates/TASK_LOG/TASK_LOG.md).
Sections: Objective, Progress Timeline (timestamped), Subagent Spawns, Files Created,
Decisions Made, Next Steps.

#### 6. SPEC Header Format

```markdown
# SPEC v1: [Task Description]

**Run ID:** RUN-YYYYMMDD-HHMM
**Created:** YYYY-MM-DD HH:MM zzz
**Status:** Active | Superseded by vN
**Previous Version:** N/A (or SPEC_v{N-1}.md)

---

[Body at agent's discretion based on task needs]
```

### Maintenance Rules

1. **Active Tasks Limit:** Maximum 5 active tasks. If starting a 6th, ask if any can be archived.
2. **Completion Confirmation:** ALWAYS ask user permission before removing from Active Tasks.
3. **Archive Process:**

   - Completed tasks removed from CLAUDE.md upon user approval
   - Working directories remain in `runs/CLAUDE-RUNS/<RUN-ID>-<slug>/` indefinitely
   - Add entry to TOP of `runs/CLAUDE-RUNS/ARCHIVE.md` (newest first)
   - Never delete working directories without explicit user permission

4. **Error Recovery:**

   - If agent crashes mid-task, Run ID, TASK_LOG.md, and SPEC_vN.md enable resume
   - User can reference Run ID to continue: "Resume RUN-20251107-1423"

---

## 🤝 Subagent Usage

> **Complete Guide:** [`docs/coding_agents/SUBAGENT_GUIDE.md`](docs/coding_agents/SUBAGENT_GUIDE.md)

Use subagents PROACTIVELY. The cost of spawning is low; the cost of context pollution is high.

**Always delegate:**

- Codebase exploration: "How is X implemented?", "Find all usages of Y", "Trace data flow"
- Verification tasks: Running tests, type-checking, linting, compilation checks
- Investigation: Reading docs, understanding APIs, summarizing file responsibilities
- Search: Pattern matching, file location, dependency tracing

**Delegation heuristic:** Before any investigative or verification task, ask:
"Will this generate intermediate noise that pollutes my main context?" If yes → subagent.

**Do NOT delegate:**

- Tasks requiring iterative user clarification
- Multi-step operations with interdependencies
- Judgment calls that should surface to main conversation

### Spawning Subagents (Main Thread Responsibility)

1. **Create a subdirectory** for the subagent in your current run:

   ```
   runs/CLAUDE-RUNS/RUN-YYYYMMDD-HHMM-<your-task>/subagents/YYYYMMDD-HHMM-<subagent-slug>/
   ```

2. **Tell the subagent its directory path** in the spawn prompt:

   ```
   Your working directory: runs/CLAUDE-RUNS/RUN-20251228-1400-fix-bug/subagents/20251228-1430-trace-auth/
   Write your FINDINGS.md and any helper files there.
   ```

3. **Read `FINDINGS.md`** after the subagent completes.

> **Backup:** Hook `.claude/hooks/subagent-directory-protocol.py` reinforces these
> instructions to subagents automatically.

### Codex as Implementation Subagent

Use **Codex CLI** for long, focused implementation tasks.

**Launch command:**
```powershell
echo "Read <relative/path/to/SPEC.md> and follow the instructions." | \
  codex exec --model gpt-5.2-codex --dangerously-bypass-approvals-and-sandbox \
  -C "/path/to/your/repo" - 2>&1
```

Run with `run_in_background: true`, check with `TaskOutput`.

**Key flags:**
- `exec` — Non-interactive mode (REQUIRED)
- `-` — Read prompt from stdin (avoids quoting issues)
- `-C <dir>` — Set working directory
- `--dangerously-bypass-approvals-and-sandbox` — Full file access

### Cursor Agent as Subagent

**Prerequisite:** `~/bin/agent` in PATH (see SETUP.md for one-time setup)

**Invocation:**
```powershell
agent -p "your full task here" --force --model auto --output-format stream-json 2>&1
```

Run with `run_in_background: true`, check with `TaskOutput`.

`--output-format stream-json` streams NDJSON events in real-time. To peek without
blowing context — use `head -n 50` on the output file, **never read the full stream.**

**⚠️ DO NOT:**
- Use `agent.cmd` directly (Windows CMD shim truncates multi-line prompts)
- Read the full stream-json output (can be hundreds of KB)

---

## ⚙️ Background Process Guidelines

- **Never Auto-Check Output:** Don't call TaskOutput just because the system reports
  new output available. Only check when you need specific information.
- **Synchronous by Default:** For short commands (<30 seconds), run synchronously.
- **Long commands:** Run in background, check output ONCE when ready — not on every notification.
- **Record the task ID** for background processes.
- **Kill processes when done** to prevent lingering jobs.

---

## 📋 Active Tasks

| Run ID | Description | Status | Working Directory |
|--------|-------------|--------|-------------------|

<!-- Active task rows are maintained dynamically during work. -->

---

## ⏰ Timestamps

AI agents do NOT have access to real-time clocks. When timestamps are needed:

1. **Run `Get-Date` in PowerShell** to get accurate system time
2. **Never hallucinate/guess timestamps** — always verify via command
3. **Format:** `yyyy-MM-dd HH:mm zzz` for documentation, `yyyyMMdd-HHmm` for file/directory names
