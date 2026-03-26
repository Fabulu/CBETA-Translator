# ðŸ§  Memory & Context Optimization

> **Purpose**: Strategies for efficient context management when working with AI coding agents.
>
> **Project-specific mappings in this file are customized for CBETA-Translator.**

---

## ðŸŽ¯ Context Loading Strategy

### Progressive Disclosure Principle

**Don't load everything at once. Load based on task type.**

```
Task Type                â†’ Load These Documents
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Quick command/fix        â†’ CLAUDE.md only
Debugging error          â†’ runs/CLAUDE-RUNS/<RUN-ID>-<slug>/TASK_LOG.md + error logs
Adding new feature       â†’ README.md + CLAUDE.md (Architecture + Translation Pipeline)
Understanding arch       â†’ CLAUDE.md (Architecture + Translation Pipeline) â†’ CLAUDE.md (Architecture section)
First time contributor   â†’ BUILD.md and BUILD_SELFCONTAINED.md â†’ README.md
```


---

## ðŸ“š Document Loading Order

### Tier 1: Always Available (Cache These)

**Load at conversation start, keep in working memory:**

1. **CLAUDE.md** â€” Navigation hub and essential quick reference
2. **Key constants** â€” Your project's port numbers, import patterns, critical rules
3. **Core architectural rule(s)** â€” The 1-2 rules that can't be violated

**Tier 1 constants for this repo:**`n```text`n- Runtime: .NET 8, C#, Avalonia 11 desktop app`n- Key directories: xml-p5/ (source TEI), md-p5t/ (editable), xml-p5t/ (generated output)`n- Build commands: .\\eng\\bootstrap.ps1, .\\eng\\build.ps1`n- Rule: Source TEI XML is never edited directly; markdown is the editable source of truth`n- Rule: XML output is regenerated from markdown just-in-time`n```

### Tier 2: Task-Specific (Load on Demand)

**Load only when task requires:**

| Document (Approx Size) | When to Load |
|------------------------|--------------|
| README.md (~8 KB) | Implementing or changing user-facing behavior |
| CLAUDE.md (~4 KB) | Architecture, service boundaries, critical rules |
| BUILD.md + BUILD_SELFCONTAINED.md (~7 KB combined) | Build/release setup and packaging |
| runs/CLAUDE-RUNS/<RUN-ID>/TASK_LOG.md (varies) | Debugging chronology within an active run |
| docs/coding_agents/SUBAGENT_GUIDE.md (~12 KB) | Subagent planning, parallelization, deliverable format |

### Tier 3: Deep Dives (Load Sparingly)

**Only load for complex architectural work:**

| Document Category | When to Load |
|-------------------|--------------|
| Architecture context doc | Major refactoring |
| Service/module README files | Service-specific work |
| API/endpoint references | API changes |

---

## ðŸ”„ Context Refresh Strategy

### When to Refresh Context

**Indicators you need to reload:**
- âŒ Suggesting patterns that don't exist in current codebase
- âŒ Using import paths that were refactored away
- âŒ Referencing endpoints that were deprecated
- âŒ Assuming file structure that changed

**Refresh triggers:**
- Major codebase changes (file moves, deletions)
- After prolonged conversation (>50 messages)
- When user corrects architectural assumptions
- After significant upstream changes

### Selective Refresh Pattern

```
# Don't refresh everything - refresh what changed

# Scenario: User says "we removed ClassX"
Refresh: examples doc (has ClassX usage)
Keep:    diagrams doc (unchanged)
Keep:    quick reference (unchanged)

# Scenario: User says "we changed port from A to B"
Refresh: quick reference (has port matrix)
Refresh: CLAUDE.md (has port numbers)
Keep:    examples doc (no hardcoded ports)
```

---

## ðŸŽ¯ Task-Based Loading Patterns

### Pattern 1: Quick Fix Task

**Example**: "Fix the typo in config.py line 96"

**Load Strategy**:
```
1. Read target file only (config.py)
2. Fix typo
3. Done

No docs needed - file path is explicit
```

**Token Usage**: ~500 tokens (file only)

### Pattern 2: Implement New Feature

**Example**: "Add a new API endpoint for weekly reports"

**Load Strategy**:
```
1. Examples doc â†’ Find "endpoint pattern" section
2. Diagrams doc â†’ Understand request flow
3. One existing implementation as reference
4. Implement following pattern exactly
```

**Token Usage**: ~30,000 tokens (2 docs + 1 file)

### Pattern 3: Debug Production Error

**Example**: "Getting 'permission denied' error on table access"

**Load Strategy**:
```
1. Troubleshooting doc â†’ Find relevant section
2. Quick reference â†’ Get debug commands
3. Diagnose: Check permissions configuration
4. Fix: Update access configuration
```

**Token Usage**: ~15,000 tokens (2 docs + minimal code)

### Pattern 4: Understand Architecture for Refactoring

**Example**: "Should we consolidate these two database clients?"

**Load Strategy**:
```
1. Diagrams doc â†’ Understand current architecture
2. Architecture doc â†’ Read design rationale
3. Examples doc â†’ See current usage patterns
4. Trace actual usage in codebase
5. Make informed decision
```

**Token Usage**: ~50,000 tokens (3 docs + code exploration)

---

## ðŸ’¾ Caching Strategies

### What to Cache Between Tasks

**Cache for entire conversation:**
- Port/service mappings (rarely change)
- Import path patterns
- Core architectural rules (which DB for what, never-mix rules)
- Security validation sequences

**Cache for current task only:**
- Specific file contents
- Endpoint implementation details
- Schema details

**Never cache:**
- Dynamic data (query results)
- User-specific configurations
- Temporary error states

### Pattern: Incremental Knowledge Building

```
Message 1: User: "Add new endpoint"
â†’ Load: examples doc (endpoint patterns)
â†’ Cache: Endpoint pattern structure

Message 2: User: "It should query inventory data"
â†’ Load: examples doc (database patterns) [already loaded, use cached]
â†’ Cache: DB executor usage pattern

Message 3: User: "Add error handling"
â†’ Load: examples doc (error handling section) [already loaded, use cached]
â†’ Use cached endpoint + database patterns
â†’ Implement complete solution

Token savings: ~32,000 tokens (didn't reload doc 3 times)
```

---

## ðŸ” Smart File Reading

### When to Read Full Files vs Targeted Sections

**Read full file when:**
- File is <300 lines
- Need to understand overall structure
- Making changes that could affect multiple areas
- First time encountering this file

**Read targeted sections when:**
- File is >500 lines
- Know exact function/class needed
- Making isolated change
- Have seen file structure before

### Pattern: Lazy Loading Imports

```python
# Don't read every imported file - trace only what you need

# User: "Fix error in database.py endpoint"
# database.py imports: ConfigBuilder, DBConnection, SecurityValidator

Step 1: Read database.py (find error location)
Step 2: Error is in ConfigBuilder.build_from_config() call
Step 3: NOW read config_builder.py (only because error is there)
Step 4: Don't read DBConnection or SecurityValidator (not involved)

Token savings: ~4,000 tokens (didn't read 2 unnecessary files)
```

---

## ðŸ“Š Token Budget Guidelines

### Typical Task Token Usage

| Task Type | Typical Token Cost | Budget Allocation |
|-----------|-------------------|-------------------|
| Quick fix | 500-2,000 | 1% of context window |
| Add simple endpoint | 15,000-30,000 | 15% of context window |
| Debug complex error | 30,000-50,000 | 25% of context window |
| Major refactoring | 80,000-120,000 | 60% of context window |

### Token Optimization Techniques

**1. Summarize Before Storing**

```
Instead of caching entire file contents:
Cache: "runs.py has execute_run() at line 42 that calls ConfigBuilder"

Token savings: 90% (500 tokens vs 5,000 tokens)
```

**2. Extract Only Relevant Sections**

```
Full file: 800 lines = ~16,000 tokens
Relevant function: 50 lines = ~1,000 tokens

Read full file first, extract function, cache extraction
Token savings: 94% for repeated access
```

**3. Progressive Disclosure in Code Reading**

```
Step 1: Read function signature only (1 line)
Step 2: If needed, read docstring (5 lines)
Step 3: If needed, read full implementation (50 lines)

Don't jump to Step 3 unless Steps 1-2 prove insufficient
```

---

## ðŸš€ Optimized Workflow Examples

### Workflow 1: Add New Component (Optimized)

```
âŒ Suboptimal (100,000 tokens):
1. Load architecture doc (12,000 tokens)
2. Load examples doc (16,000 tokens)
3. Read all existing components (40,000 tokens)
4. Read related utilities (5,000 tokens)
5. Read security module (5,000 tokens)
6. Implement component
Total: 78,000 tokens before implementation

âœ… Optimized (9,000 tokens):
1. Load examples doc â†’ "Creating a Custom Component" section only (3,000 tokens)
2. Read one existing component as reference (5,000 tokens)
3. Read relevant utility â†’ just the function needed (1,000 tokens)
4. Implement component
Total: 9,000 tokens before implementation

Savings: 69,000 tokens (76% reduction)
```

### Workflow 2: Debug Database Connection (Optimized)

```
âŒ Suboptimal (80,000 tokens):
1. Load architecture doc (12,000 tokens)
2. Load dev workflows doc (10,000 tokens)
3. Load troubleshooting doc (8,000 tokens)
4. Read db_connection.py (5,000 tokens)
5. Read config_builder.py (6,000 tokens)
6. Read all .env files (2,000 tokens)
Total: 43,000 tokens

âœ… Optimized (5,000 tokens):
1. Load troubleshooting doc â†’ "Database Connection Problems" section (2,000 tokens)
2. Run suggested diagnostic command (0 tokens, just execute)
3. Based on error, read relevant file section only (3,000 tokens)
Total: 5,000 tokens

Savings: 38,000 tokens (88% reduction)
```

---

## ðŸŽ¯ Decision Trees for Context Loading

### Decision Tree: Do I Need to Load Docs?

```
Is the file path explicit in user request?
â”œâ”€ YES: Read file directly, no docs needed
â””â”€ NO: Continue...

Do I know the exact pattern needed?
â”œâ”€ YES: Load examples doc â†’ Find pattern â†’ Implement
â””â”€ NO: Continue...

Is this architectural/design question?
â”œâ”€ YES: Load diagrams doc â†’ Understand â†’ Decide
â””â”€ NO: Continue...

Is this a debugging task?
â”œâ”€ YES: Load troubleshooting doc â†’ Find solution
â””â”€ NO: Load quick reference for commands
```

### Decision Tree: Which Doc to Load?

```
Task involves...

â”œâ”€ Commands/quick lookup? â†’ CLAUDE.md
â”œâ”€ Error message? â†’ runs/CLAUDE-RUNS/<RUN-ID>/TASK_LOG.md
â”œâ”€ "How do I..." question? â†’ README.md
â”œâ”€ "How does X work?" question? â†’ CLAUDE.md (Architecture + pipeline sections)
â”œâ”€ "Why is X designed this way?" â†’ CLAUDE.md (Architecture section)
â”œâ”€ New developer setup? â†’ BUILD.md + BUILD_SELFCONTAINED.md
â””â”€ Simple code change? â†’ No docs, read code directly
```

---

## ðŸ“ˆ Measuring Optimization Success

### Key Metrics

**1. Token Efficiency Ratio**
```
Formula: (Tokens used for implementation) / (Total tokens loaded)
Target: >0.5 (spent more tokens implementing than loading context)

Good: 10,000 tokens context, 15,000 tokens implementation = 0.6
Bad:  50,000 tokens context, 10,000 tokens implementation = 0.2
```

**2. First-Try Success Rate**
```
Formula: (Tasks completed without reloading docs) / (Total tasks)
Target: >0.7 (most tasks succeed with initial context)

Indicates: Loaded right context the first time
```

**3. Context Reuse Rate**
```
Formula: (Context used in multiple responses) / (Total context loaded)
Target: >2.0 (each loaded doc used at least twice)

Indicates: Efficient caching between related tasks
```

---

## ðŸ› ï¸ Practical Tips

### Tip 1: Use CLAUDE.md as Your Compass

**Always start here.** It's the navigation hub that tells you where to find specific information.

```
Bad workflow:  Load all docs â†’ Search for answer
Good workflow: CLAUDE.md "When to Use Each Guide" â†’ Load specific doc
```

### Tip 2: Cache Constants and Patterns

**These rarely change and are referenced frequently:**

- Runtime and UI stack: .NET 8 + Avalonia 11 + AvaloniaEdit.`n- Pipeline invariant: xml-p5 (read-only source) -> md-p5t (editable) -> xml-p5t (generated output).`n- Build baseline: use .\\eng\\bootstrap.ps1 for prerequisites, then .\\eng\\build.ps1.`n- Search changes must keep SearchIndexService and BloomSearchIndexService synchronized.`n- Git operations should follow GitRepoService/GitBinaryLocator patterns in Services/.

### Tip 3: Lazy Load Everything Else

**Only load when you have concrete evidence you need it:**
- Implementation details (load when implementing)
- Endpoint internals (load when modifying)
- Schema details (load when writing queries)

### Tip 4: Summarize and Compress

**After loading large doc, extract and cache key points:**

```
Instead of keeping 16,000 tokens of an examples doc in memory:

Extract and cache:
- "Endpoint pattern: Hydrate config, execute agent, return structured response"
- "Tool pattern: Inherit BaseTool, accept context, return dict"
- "Security pattern: Validate before execute, parameterize always"

Reduced to: ~500 tokens
```

### Tip 5: Know When to Stop Loading

**Stop loading context when you can answer:**
1. What file(s) do I need to modify?
2. What pattern should I follow?
3. What are the critical constraints (security, validation, etc.)?

**If you can answer these, start implementing. Load more only if blocked.**

---

**Last Updated:** 2026-03-07


