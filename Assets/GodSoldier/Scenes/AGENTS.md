# Scenes Agent Guide

- These scenes are generated and should stay aligned with `GodSoldierVerticalSliceSetup.cs`.
- Player-facing scene set:
- `GodSoldier_Bootstrap`
- `GodSoldier_MainMenu`
- `GodSoldier_Lobby`
- `GodSoldier_Descent`
- `GodSoldier_WarTrial`
- `GodSoldier_Judgment`
- If you change scene structure, also update the generator and build settings.
- Menu and lobby scenes are presentation and routing scenes.
- Mission scenes must preserve:
- one `GodSoldierBootstrapper`
- one valid core-director service
- mission-specific director with server-authoritative references
- clear God and Soldier spawn markers

