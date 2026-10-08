---
name: game-design
description: "Game design principles. GDD structure, balancing, player psychology, progression."
risk: none
source: community
date_added: "2026-02-27"
---

## Repository adaptation for continuous RPG development

Modified on 2026-10-08 by the project development agent for portable Codex Cloud and GitHub Actions use. Earlier host notes were replaced; upstream workflow and license notices are retained. The current user instruction and repository AGENTS.md take precedence over examples.

- Work from the current Git checkout root; resolve all project paths relative to that root. Target international Unity 2022.3.53f1. Preserve existing user work, GUIDs, accepted character identity, and mature features. Do not upgrade to Unity 6 merely because an example names it.
- Read DEVELOPMENT_PROGRESS.md, KNOWN_ISSUES.md, NEXT_ITERATION.md, SKILLS_USAGE.md and relevant current design/handoff records when present. Historical reports describe their own checkpoint, not proof that their code or art exists in this checkout. Inspect actual scenes and entry points before choosing Animator or OnGUI integration.
- The user has authorized continuous scoped development, commits, ordinary pushes to master, packaging and scheduled iteration. Apply that authority without repeating design approval requests. Preserve dirty work; isolate changes that cannot receive the required validation. Never force push.
- Inspect credential names/presence only. Never dump environment values or credential files, and never write secrets, Unity license contents or tokens into prompts, logs, assets or handoffs.
- Use the real runtime tool list. A downloaded skill is guidance, not an installed MCP connection, external service, model, Unity license, or image generation entitlement. GitHub Actions must detect its own capabilities anew.
- Preserve command exit status and fresh evidence. Distinguish static checks, offline C# harnesses, native Unity compilation, EditMode, PlayMode, graphics/animation inspection and gameplay/audio acceptance. Do not call an unrun or zero-test suite passed.
- Apply only relevant guidance. Do not add unnecessary generated art, third-party services, audio or tests just to claim every skill was used. Record the actual application or why a skill was not applicable each round.

# Game Design Principles

> Design thinking for engaging games.

---

## 1. Core Loop Design

### The 30-Second Test

```
Every game needs a fun 30-second loop:
1. ACTION → Player does something
2. FEEDBACK → Game responds
3. REWARD → Player feels good
4. REPEAT
```

### Loop Examples

| Genre | Core Loop |
|-------|-----------|
| Platformer | Run → Jump → Land → Collect |
| Shooter | Aim → Shoot → Kill → Loot |
| Puzzle | Observe → Think → Solve → Advance |
| RPG | Explore → Fight → Level → Gear |

---

## 2. Game Design Document (GDD)

### Essential Sections

| Section | Content |
|---------|---------|
| **Pitch** | One-sentence description |
| **Core Loop** | 30-second gameplay |
| **Mechanics** | How systems work |
| **Progression** | How player advances |
| **Art Style** | Visual direction |
| **Audio** | Sound direction |

### Principles

- Keep it living (update regularly)
- Visuals help communicate
- Less is more (start small)

---

## 3. Player Psychology

### Motivation Types

| Type | Driven By |
|------|-----------|
| **Achiever** | Goals, completion |
| **Explorer** | Discovery, secrets |
| **Socializer** | Interaction, community |
| **Killer** | Competition, dominance |

### Reward Schedules

| Schedule | Effect | Use |
|----------|--------|-----|
| **Fixed** | Predictable | Milestone rewards |
| **Variable** | Addictive | Loot drops |
| **Ratio** | Effort-based | Grind games |

---

## 4. Difficulty Balancing

### Flow State

```
Too Hard → Frustration → Quit
Too Easy → Boredom → Quit
Just Right → Flow → Engagement
```

### Balancing Strategies

| Strategy | How |
|----------|-----|
| **Dynamic** | Adjust to player skill |
| **Selection** | Let player choose |
| **Accessibility** | Options for all |

---

## 5. Progression Design

### Progression Types

| Type | Example |
|------|---------|
| **Skill** | Player gets better |
| **Power** | Character gets stronger |
| **Content** | New areas unlock |
| **Story** | Narrative advances |

### Pacing Principles

- Early wins (hook quickly)
- Gradually increase challenge
- Rest beats between intensity
- Meaningful choices

---

## 6. Anti-Patterns

| ❌ Don't | ✅ Do |
|----------|-------|
| Design in isolation | Playtest constantly |
| Polish before fun | Prototype first |
| Force one way to play | Allow player expression |
| Punish excessively | Reward progress |

---

> **Remember:** Fun is discovered through iteration, not designed on paper.

## When to Use
This skill is applicable to execute the workflow or actions described in the overview.

## Limitations
- Use this skill only when the task clearly matches the scope described above.
- Do not treat the output as a substitute for environment-specific validation, testing, or expert review.
- Stop and ask for clarification if required inputs, permissions, safety boundaries, or success criteria are missing.
