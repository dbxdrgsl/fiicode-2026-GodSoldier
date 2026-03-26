# God Soldier Agent Guide

## Project Vision
- `God Soldier` is currently a `2-player co-op game`.
- The playable bond is asymmetric:
- `God` guides, reveals, supports, crafts, and manipulates spiritual space.
- `Soldier` moves, fights, carries, pushes, and resolves physical problems.
- The early campaign is structured as three missions:
- `Descent`
- `War Trial`
- `Judgment`

## Terminology
- Use `Game`, `Mission`, `Story`, `Settings`, `Public Match`, and `Private Match` in player-facing work.
- Do not introduce `slice` in UI copy, menu copy, subtitles, or docs meant for players.
- Legacy internal class names that still contain `Slice` may remain until they are safely refactored.

## Folder Ownership
- Primary gameplay write area: `C:\Users\dbxdr_iytiz92\God Soldier\Assets\GodSoldier`
- Shared gameplay/framework code: `C:\Users\dbxdr_iytiz92\God Soldier\Assets\Core`
- Session and sample integration code: `C:\Users\dbxdr_iytiz92\God Soldier\Assets\Blocks`
- Edit `Assets/Blocks` only when the God Soldier game flow cannot be achieved from `Assets/GodSoldier` alone.

## Current Mission Contracts
- Every current mission is capped at `2 human players`.
- Role set stays fixed at `God` and `Soldier`.
- Mission 1 `Descent`: intro/revival, clue room, obstacle push, dream traversal, first combat, explosive finale.
- Mission 2 `War Trial`: scripted co-op boss mission against a false God and a corrupted Soldier, with filler waves between boss phases.
- Mission 3 `Judgment`: narrative choice mission with an assassination climax and branching endings.

## Networking Rules
- Mission-specific session settings drive public/private flow and player cap.
- Public flow should expose public lobbies only.
- Private flow should support code joining.
- Do not reintroduce global `4-player` assumptions into the current game.

## Unity Workflow
- Use `God Soldier > Regenerate Game Skeleton` after changing generated scenes, mission-catalog structure, or mission-setting assets.
- Bootstrap scene: `C:\Users\dbxdr_iytiz92\God Soldier\Assets\GodSoldier\Scenes\GodSoldier_Bootstrap.unity`
- Menu scene: `C:\Users\dbxdr_iytiz92\God Soldier\Assets\GodSoldier\Scenes\GodSoldier_MainMenu.unity`
- Lobby scene: `C:\Users\dbxdr_iytiz92\God Soldier\Assets\GodSoldier\Scenes\GodSoldier_Lobby.unity`
- Mission scenes:
- `C:\Users\dbxdr_iytiz92\God Soldier\Assets\GodSoldier\Scenes\GodSoldier_Descent.unity`
- `C:\Users\dbxdr_iytiz92\God Soldier\Assets\GodSoldier\Scenes\GodSoldier_WarTrial.unity`
- `C:\Users\dbxdr_iytiz92\God Soldier\Assets\GodSoldier\Scenes\GodSoldier_Judgment.unity`

## Validation Expectations
- No compile errors.
- No player-facing `slice` language in menu/lobby/story docs.
- Session creation/joining respects the selected mission’s public/private settings.
- Role lock and mission start require exactly two connected players with unique roles.
- Generated scenes must not produce `GlobalObjectIdHash value 0` errors.
- Core services must exist so `CoreDirector` and sound setup do not collapse on scene load.

## Human vs Agent Work Split
- Agents are best for:
- C#, UXML, USS, ScriptableObjects, scene generators, glue code, validators, and documentation.
- Humans are best for:
- environment composition, imported package cleanup, final cinematics, shot timing, animation feel, and aesthetic taste decisions.
