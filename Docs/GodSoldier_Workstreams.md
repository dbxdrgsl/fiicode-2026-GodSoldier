# God Soldier Workstreams

## Workstream A: Menu And Match Flow
- Owns main menu presentation, mission timeline, settings, mission selection, and pre-match routing.
- Done when:
- menu uses `Play`, `Settings`, `Exit`
- mission timeline lists all current missions
- lobby supports public/private entry for the selected mission

## Workstream B: Role And Mission Runtime
- Owns role assignment, HUD, mission directors, objective text, and mission completion tracking.
- Done when:
- God/Soldier role lock works
- each mission scene has a functioning director
- story and objective HUD update correctly

## Workstream C: Networking And Session Contracts
- Owns mission-specific session settings, player caps, start conditions, and public/private match rules.
- Done when:
- every current mission caps at two humans
- host start requires two connected players with unique roles
- mission-specific settings are respected

## Workstream D: Scene Generation And Validation
- Owns `GodSoldierVerticalSliceSetup.cs`, generated scenes, build settings, and scene-placed network object validation.
- Done when:
- all six scenes regenerate cleanly
- build settings match the current game structure
- no broken generated references remain

## Workstream E: Human Polish Passes
- Human-owned by default.
- Includes:
- imported environment cleanup
- mission dressing
- shot timing
- lighting and color taste
- animation and feel polish
