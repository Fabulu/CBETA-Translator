# Skills

Self-contained knowledge packages that teach Claude (Desktop or Code) how to perform specialized tasks. Each skill directory is designed to be zipped and uploaded to Claude Desktop as a Project, or referenced by Claude Code via CLAUDE.md.

## Available Skills

| Skill | Directory | Purpose |
|-------|-----------|---------|
| *(none yet)* | — | Add your first skill here |

## Skill Structure

Each skill directory follows this layout:

```
skill-name/
  SKILL.md              <- Main guide. Claude reads this first.
  references/           <- Supporting docs (field maps, design rules, glossaries)
  templates/            <- Template files (PPTX, DOCX, etc.)
  scripts/              <- Reference implementations (helpers, not rigid pipelines)
```

## How to Use

### Claude Desktop
Zip the skill directory and upload it as a Project Knowledge file. Claude will read SKILL.md and the references to understand how to perform the task.

### Claude Code
Reference the skill directory path in CLAUDE.md or point Claude to `skills/<name>/SKILL.md` when starting a relevant task.
