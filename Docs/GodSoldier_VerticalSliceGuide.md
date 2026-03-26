# God Soldier Playtest Guide

## Scene Flow
- `GodSoldier_Bootstrap`
- `GodSoldier_MainMenu`
- `GodSoldier_Lobby`
- `GodSoldier_Descent`
- `GodSoldier_WarTrial`
- `GodSoldier_Judgment`

## What To Expect
- Main menu with `Play`, `Settings`, and `Exit`
- Mission timeline after pressing `Play`
- Out-of-order mission warning when applicable
- Mission-specific lobby flow with `Public Match` and `Private Match`
- Role selection for `God` and `Soldier`

## Basic Test Loop
1. Open `C:\Users\dbxdr_iytiz92\God Soldier\Assets\GodSoldier\Scenes\GodSoldier_Bootstrap.unity`.
2. Press Play.
3. Enter the mission timeline from the main menu.
4. Select a mission.
5. In the lobby, choose either `Public Match` or `Private Match`.
6. Connect two players.
7. Lock one player to `God` and the other to `Soldier`.
8. Start the mission from the host.

## Multiplayer Test Options
- Best local editor path: `Window > Multiplayer Play Mode`
- Alternative smoke test: one editor instance plus one desktop build

## Expected Current Mission Outcomes
- `Descent`: revival, clue reveal, obstacle push, dream traversal, combat clear, explosive finale
- `War Trial`: scripted boss counters, wave clear, second boss counter, mission completion
- `Judgment`: chamber choices, assassination decision, ending result

## Quick Validation Checklist
- No player-facing text says `slice`
- Role buttons become usable after session connection
- Only two human players can occupy the current missions
- Host start is blocked until two unique roles are chosen
- No `GlobalObjectIdHash value 0` errors appear when mission scenes load
- No sound-system initialization collapse appears on scene load
