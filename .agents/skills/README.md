# Repository RPG skills

Eight reviewed skill packages are committed here so a fresh Codex Cloud or GitHub Actions checkout can read them without depending on a previous host snapshot. Use the repository root as the working directory. Enumerate `*/SKILL.md`, read the complete relevant skill plus necessary references, and record actual application in SKILLS_USAGE.md each round.

The user has authorized game development. Preserve accepted design and user edits, follow repository AGENTS.md, validate actual behavior, never force push, and separate unavailable or unrun checks from passes.

| Skill | Intended use | Runtime limit |
| --- | --- | --- |
| session-handoff | Load/check context and record a resumable iteration | Run helper scripts by their repository-relative path; historical handoff status is not current code proof |
| game-design | Choose a complete improvement to explore/fight/progress/reward | Requires real project/playtest evidence |
| game-art | Preserve pixel density, silhouettes, action timing and asset organization | Metadata checks do not prove natural animation |
| game-audio | Audit/add feedback categories and deliberate mix priorities | No audio synthesis tool or audio assets are supplied by the skill |
| systematic-debugging | Reproduce, trace root cause, fix, and retest failures | The bundled npm polluter example is not a Unity test command |
| verification-before-completion | Match every completion claim to fresh evidence | Offline checks cannot substitute for licensed Unity playback |
| unity-mcp-orchestrator | Inspect/editor/compile/test workflows when MCP is connected | Guidance only; compatible plugin, server, licensed editor required; Unity 6 examples require adaptation |
| imagegen | Reference-driven raster assets and consistent character-frame production | A callable image tool must exist; CI runners do not inherit chat image generation; CLI API billing is separate and explicit opt-in |

Source revisions, licenses, checksums, upstream/local modifications and attribution are preserved in each package and docs/skills-source-manifest.json. Code and documentation licenses differ for the three game-* packages. No models, editor caches, credentials or host registration links are included. Existing same-name user files are never silently overwritten.

The cloud-environment-onboarding:setup skill remains a Codex Cloud connector capability and is not redistributed here: no redistribution license was provided, and its environment draft tools are unavailable in ordinary Actions runners.

Useful repository-relative handoff commands:

```bash
python3 .agents/skills/session-handoff/scripts/list_handoffs.py .
python3 .agents/skills/session-handoff/scripts/check_staleness.py .claude/handoffs/<existing-record>.md
python3 .agents/skills/session-handoff/scripts/create_handoff.py <round-slug>
python3 .agents/skills/session-handoff/scripts/validate_handoff.py .claude/handoffs/<completed-record>.md
```

Read the chosen handoff completely and validate current Git/scene/asset state before continuing. Never run an example merely because it appears in a reference.
