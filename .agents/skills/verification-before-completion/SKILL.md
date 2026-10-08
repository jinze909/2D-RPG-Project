---
name: verification-before-completion
description: Use when about to claim work is complete, fixed, or passing, before committing or creating PRs - requires running verification commands and confirming output before making any success claims; evidence before assertions always
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

# Verification Before Completion

## Overview

**Core principle:** Evidence before claims, always.

**Violating the letter of this rule is violating the spirit of this rule.**

## The Iron Law

```
NO COMPLETION CLAIMS WITHOUT FRESH VERIFICATION EVIDENCE
```

If you haven't run the verification command in this message, you cannot claim it passes.

## The Gate Function

```
BEFORE claiming any status or expressing satisfaction:

1. IDENTIFY: What command proves this claim?
2. RUN: Execute the FULL command (fresh, complete)
3. READ: Full output, check exit code, count failures
4. VERIFY: Does output confirm the claim?
   - If NO: State actual status with evidence
   - If YES: State claim WITH evidence
5. ONLY THEN: Make the claim

Skip any step = lying, not verifying
```

## Common Failures

| Claim | Requires | Not Sufficient |
|-------|----------|----------------|
| Tests pass | Test command output: 0 failures | Previous run, "should pass" |
| Linter clean | Linter output: 0 errors | Partial check, extrapolation |
| Build succeeds | Build command: exit 0 | Linter passing, logs look good |
| Bug fixed | Test original symptom: passes | Code changed, assumed fixed |
| Regression test works | Red-green cycle verified | Test passes once |
| Agent completed | VCS diff shows changes | Agent reports "success" |
| Requirements met | Line-by-line checklist | Tests passing |

## Red Flags - STOP

- Using "should", "probably", "seems to"
- Expressing satisfaction before verification ("Great!", "Perfect!", "Done!", etc.)
- About to commit/push/PR without verification
- Trusting agent success reports
- Relying on partial verification
- Thinking "just this once"
- Tired and wanting work over
- **ANY wording implying success without having run verification**

## Rationalization Prevention

| Excuse | Reality |
|--------|---------|
| "Should work now" | RUN the verification |
| "I'm confident" | Confidence ≠ evidence |
| "Just this once" | No exceptions |
| "Linter passed" | Linter ≠ compiler |
| "Agent said success" | Verify independently |
| "I'm tired" | Exhaustion ≠ excuse |
| "Partial check is enough" | Partial proves nothing |
| "Different words so rule doesn't apply" | Spirit over letter |

## Key Patterns

**Tests:**
```
✅ [Run test command] [See: 34/34 pass] "All tests pass"
❌ "Should pass now" / "Looks correct"
```

**Regression tests (TDD Red-Green):**
```
✅ Write → Run (pass) → Revert fix → Run (MUST FAIL) → Restore → Run (pass)
❌ "I've written a regression test" (without red-green verification)
```

**Build:**
```
✅ [Run build] [See: exit 0] "Build passes"
❌ "Linter passed" (linter doesn't check compilation)
```

**Requirements:**
```
✅ Re-read plan → Create checklist → Verify each → Report gaps or completion
❌ "Tests pass, phase complete"
```

**Agent delegation:**
```
✅ Agent reports success → Check VCS diff → Verify changes → Report actual state
❌ Trust agent report
```

## When To Apply

**ALWAYS before:**
- ANY variation of success/completion claims
- ANY expression of satisfaction
- ANY positive statement about work state
- Committing, PR creation, task completion
- Moving to next task
- Delegating to agents

**Rule applies to:**
- Exact phrases
- Paraphrases and synonyms
- Implications of success
- ANY communication suggesting completion/correctness
