# God Soldier

God Soldier is a 2-player asymmetric co-op game built in Unity. One player takes the `God` role and guides, reveals, supports, and crafts through spiritual space. The other takes the `Soldier` role and handles movement, combat, carrying, pushing, and the physical half of each objective.

The current early campaign is organized into three missions:

- `Descent`
- `War Trial`
- `Judgment`

## Current Scope

- `2 human players` only
- Fixed roles: `God` and `Soldier`
- Public flow uses `Public Match` lobbies only
- Private flow supports `Private Match` code joining
- Mission start requires exactly two connected players with unique roles

## Unity Version

- `6000.3.9f1`

## Core Stack

- Universal Render Pipeline
- Input System
- Cinemachine
- Netcode for GameObjects
- Unity Transport
- Unity Services Multiplayer

## Main Scene Flow

The build settings currently include these scenes in order:

1. `Assets/GodSoldier/Scenes/GodSoldier_Bootstrap.unity`
2. `Assets/GodSoldier/Scenes/GodSoldier_MainMenu.unity`
3. `Assets/GodSoldier/Scenes/GodSoldier_Lobby.unity`
4. `Assets/GodSoldier/Scenes/GodSoldier_Descent.unity`
5. `Assets/GodSoldier/Scenes/GodSoldier_WarTrial.unity`
6. `Assets/GodSoldier/Scenes/GodSoldier_Judgment.unity`

Use the bootstrap scene as the main entry point when validating the full game flow.

## Mission Outline

### Descent

- Revival intro
- Clue room investigation
- Co-op obstacle push
- Dream traversal
- First combat encounter
- Explosive finale

### War Trial

- Scripted co-op boss mission
- Enemy pair: false God and corrupted Soldier
- Filler waves between boss phases

### Judgment

- Story-led mission
- Choice-driven progression around `Order vs Agency`
- Assassination climax with branching endings

## Project Layout

- `Assets/GodSoldier`
  Primary gameplay write area for the God Soldier-specific game flow, scenes, mission logic, settings, and UI.

- `Assets/Core`
  Shared gameplay and framework code used across the project.

- `Assets/Blocks`
  Session and sample integration code. Prefer editing this only when the God Soldier flow cannot be achieved from `Assets/GodSoldier` alone.

- `Docs`
  Internal design and planning notes for the game direction and production work.

## Working In The Project

1. Open the project in Unity Hub with `6000.3.9f1`.
2. Let Unity restore packages and rebuild `Library`.
3. Open `Assets/GodSoldier/Scenes/GodSoldier_Bootstrap.unity` to validate the main loop.
4. Use the main menu and lobby scenes to test mission selection, match flow, and role lock behavior.

## Workflow Notes

- After changing generated scenes, mission catalog structure, or mission-setting assets, run `God Soldier > Regenerate Game Skeleton`.
- Keep player-facing terminology aligned with `Game`, `Mission`, `Story`, `Settings`, `Public Match`, and `Private Match`.
- Do not reintroduce global `4-player` assumptions into the current game.
- Legacy internal class names may still contain `Slice`, but that wording should not appear in player-facing copy.

## Validation Expectations

- No compile errors
- No player-facing `slice` language in menu, lobby, or story-facing content
- Session creation and joining respect each mission's public/private settings
- Role lock and mission start require exactly two connected players with unique roles
- Generated scenes should not produce `GlobalObjectIdHash value 0` errors
- Core services should exist so `CoreDirector` and sound setup do not fail on load

## Supporting Docs

- `Docs/GodSoldier_GDD.md`
- `Docs/GodSoldier_Roadmap.md`
- `Docs/GodSoldier_Workstreams.md`
