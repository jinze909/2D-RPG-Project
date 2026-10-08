# Source and local modifications

Skill: `session-handoff`
Source: https://github.com/softaworks/agent-toolkit
Upstream revision: `3027f20f3181758385a1bb8c022d4041dfb4de84`
Upstream path: `skills/session-handoff`
Repository adaptation date: 2026-10-08.

This is a redistributed and adapted instruction package, not an installed external service. Original authors and copyright notices remain in the included license file. Earlier source and local changes are preserved in INSTALLATION.json.

License: MIT. No endorsement by upstream authors is implied.

Local changes: repository-relative paths and runtime capability checks replace historical host state; confidential diagnostics use names/presence only; current user authority takes precedence over workflow examples. Handoff helper scripts operate on the current checkout; Unity MCP tools remain conditional; image generation is not implied by installation.
