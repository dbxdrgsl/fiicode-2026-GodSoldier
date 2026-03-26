# Editor Agent Guide

- `GodSoldierVerticalSliceSetup.cs` is the canonical generator for the current game skeleton.
- When adding a generated scene object, also wire its serialized references in the generator.
- Always refresh scene-placed `NetworkObject` data before saving generated scenes.
- Keep generated mission settings at `2` max human players unless the design changes explicitly.
- If menu/lobby flow changes, update both the generator and the runtime controllers together.

