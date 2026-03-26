# Runtime Agent Guide

- Keep the current game model at `2-player co-op`.
- Do not add new playable roles beyond `God` and `Soldier`.
- Preserve server authority for mission state, role state, triggers, and mission completion.
- Prefer mission-specific directors over reviving the older all-purpose gameplay controller.
- Public/private session flow should be mission-driven, not global.
- Any user-facing strings should use `Game`, `Mission`, `Story`, and `Settings`, not `slice`.

