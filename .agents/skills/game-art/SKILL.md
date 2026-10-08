---
name: game-art
description: "Game art principles. Visual style selection, asset pipeline, animation workflow."
risk: none
source: community
date_added: "2026-02-27"
---

## Repository adaptation for RPG development

Modified on 2026-10-08 by the project development agent for portable Codex Cloud and GitHub Actions use. Earlier host notes were replaced; upstream workflow and license notices are retained. The current user instruction and repository AGENTS.md take precedence over examples.

- Work from the current Git checkout root; resolve all project paths relative to that root. Target international Unity 2022.3.53f1. Preserve existing user work, GUIDs, accepted character identity, and mature features. Do not upgrade to Unity 6 merely because an example names it.
- Read DEVELOPMENT_PROGRESS.md, KNOWN_ISSUES.md, NEXT_ITERATION.md, SKILLS_USAGE.md and relevant current design/handoff records when present. Historical reports describe their own checkpoint, not proof that their code or art exists in this checkout. Inspect actual scenes and entry points before choosing Animator or OnGUI integration.
- The user has authorized scoped game development, commits, ordinary pushes to master and packaging. Apply that authority without repeating design approval requests. Preserve dirty work; isolate changes that cannot receive the required validation. Never force push.
- Inspect credential names/presence only. Never dump environment values or credential files, and never write secrets, Unity license contents or tokens into prompts, logs, assets or handoffs.
- Use the real runtime tool list. A downloaded skill is guidance, not an installed MCP connection, external service, model, Unity license, or image generation entitlement. GitHub Actions must detect its own capabilities anew.
- Preserve command exit status and fresh evidence. Distinguish static checks, offline C# harnesses, native Unity compilation, EditMode, PlayMode, graphics/animation inspection and gameplay/audio acceptance. Do not call an unrun or zero-test suite passed.
- Apply only relevant guidance. Do not add unnecessary generated art, third-party services, audio or tests just to claim every skill was used. Record the actual application or why a skill was not applicable each round.

# Game Art Principles

> Visual design thinking for games - style selection, asset pipelines, and art direction.

---

## 1. Art Style Selection

### Decision Tree

```
What feeling should the game evoke?
│
├── Nostalgic / Retro
│   ├── Limited palette? → Pixel Art
│   └── Hand-drawn feel? → Vector / Flash style
│
├── Realistic / Immersive
│   ├── High budget? → PBR 3D
│   └── Stylized realism? → Hand-painted textures
│
├── Approachable / Casual
│   ├── Clean shapes? → Flat / Minimalist
│   └── Soft feel? → Gradient / Soft shadows
│
└── Unique / Experimental
    └── Define custom style guide
```

### Style Comparison Matrix

| Style | Production Speed | Skill Floor | Scalability | Best For |
|-------|------------------|-------------|-------------|----------|
| **Pixel Art** | Medium | Medium | Hard to hire | Indie, retro |
| **Vector/Flat** | Fast | Low | Easy | Mobile, casual |
| **Hand-painted** | Slow | High | Medium | Fantasy, stylized |
| **PBR 3D** | Slow | High | AAA pipeline | Realistic games |
| **Low-poly** | Fast | Medium | Easy | Indie 3D |
| **Cel-shaded** | Medium | Medium | Medium | Anime, cartoon |

---

## 2. Asset Pipeline Decisions

### 2D Pipeline

| Phase | Tool Options | Output |
|-------|--------------|--------|
| **Concept** | Paper, Procreate, Photoshop | Reference sheet |
| **Creation** | Aseprite, Photoshop, Krita | Individual sprites |
| **Atlas** | TexturePacker, Aseprite | Spritesheet |
| **Animation** | Spine, DragonBones, Frame-by-frame | Animation data |
| **Integration** | Engine import | Game-ready assets |

### 3D Pipeline

| Phase | Tool Options | Output |
|-------|--------------|--------|
| **Concept** | 2D art, Blockout | Reference |
| **Modeling** | Blender, Maya, 3ds Max | High-poly mesh |
| **Retopology** | Blender, ZBrush | Game-ready mesh |
| **UV/Texturing** | Substance Painter, Blender | Texture maps |
| **Rigging** | Blender, Maya | Skeletal rig |
| **Animation** | Blender, Maya, Mixamo | Animation clips |
| **Export** | FBX, glTF | Engine-ready |

---

## 3. Color Theory Decisions

### Palette Selection

| Goal | Strategy | Example |
|------|----------|---------|
| **Harmony** | Complementary or analogous | Nature games |
| **Contrast** | High saturation differences | Action games |
| **Mood** | Warm/cool temperature | Horror, cozy |
| **Readability** | Value contrast over hue | Gameplay clarity |

### Color Principles

- **Hierarchy:** Important elements should pop
- **Consistency:** Same object = same color family
- **Context:** Colors read differently on backgrounds
- **Accessibility:** Don't rely only on color

---

## 4. Animation Principles

### The 12 Principles (Applied to Games)

| Principle | Game Application |
|-----------|------------------|
| **Squash & Stretch** | Jump arcs, impacts |
| **Anticipation** | Wind-up before attack |
| **Staging** | Clear silhouettes |
| **Follow-through** | Hair, capes after movement |
| **Slow in/out** | Easing on transitions |
| **Arcs** | Natural movement paths |
| **Secondary Action** | Breathing, blinking |
| **Timing** | Frame count = weight/speed |
| **Exaggeration** | Readable from distance |
| **Appeal** | Memorable design |

### Frame Count Guidelines

| Action Type | Typical Frames | Feel |
|-------------|----------------|------|
| Idle breathing | 4-8 | Subtle |
| Walk cycle | 6-12 | Smooth |
| Run cycle | 4-8 | Energetic |
| Attack | 3-6 | Snappy |
| Death | 8-16 | Dramatic |

---

## 5. Resolution & Scale Decisions

### 2D Resolution by Platform

| Platform | Base Resolution | Sprite Scale |
|----------|-----------------|--------------|
| Mobile | 1080p | 64-128px characters |
| Desktop | 1080p-4K | 128-256px characters |
| Pixel art | 320x180 to 640x360 | 16-32px characters |

### Consistency Rule

Choose a base unit and stick to it:
- Pixel art: Work at 1x, scale up (never down)
- HD art: Define DPI, maintain ratio
- 3D: 1 unit = 1 meter (industry standard)

---

## 6. Asset Organization

### Naming Convention

```
[type]_[object]_[variant]_[state].[ext]

Examples:
spr_player_idle_01.png
tex_stone_wall_normal.png
mesh_tree_oak_lod2.fbx
```

### Folder Structure Principle

```
assets/
├── characters/
│   ├── player/
│   └── enemies/
├── environment/
│   ├── props/
│   └── tiles/
├── ui/
├── effects/
└── audio/
```

---

## 7. Anti-Patterns

| Don't | Do |
|-------|-----|
| Mix art styles randomly | Define and follow style guide |
| Work at final resolution only | Create at source resolution |
| Ignore silhouette readability | Test at gameplay distance |
| Over-detail background | Focus detail on player area |
| Skip color testing | Test on target display |

---

> **Remember:** Art serves gameplay. If it doesn't help the player, it's decoration.

## When to Use
This skill is applicable to execute the workflow or actions described in the overview.

## Limitations
- Use this skill only when the task clearly matches the scope described above.
- Do not treat the output as a substitute for environment-specific validation, testing, or expert review.
- Stop and ask for clarification if required inputs, permissions, safety boundaries, or success criteria are missing.
