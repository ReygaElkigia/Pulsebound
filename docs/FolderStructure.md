# Pulsebound — Folder Structure

Annotated tree of the Unity project. The `Scripts/` layout maps 1:1 to the layered
architecture in the GDD (§18): dependencies point downward only
(Presentation → Gameplay → Core → Data → Platform).

```
Pulsebound/
├── docs/                                  # Design & engineering documentation
│   ├── GameDesignDocument.md              # The full 22-section GDD
│   ├── FolderStructure.md                 # This file
│   ├── TechnicalArchitecture.md           # System diagrams + data flow
│   └── Roadmap.md                         # Milestones & MVP checklist
│
├── Packages/
│   └── manifest.json                      # URP, Input System, Addressables, Splines, TMP, Cinemachine
│
├── ProjectSettings/
│   └── ProjectVersion.txt                 # Unity 6 (6000.0.x)
│
└── Assets/Pulsebound/
    ├── Scripts/
    │   ├── Pulsebound.asmdef              # Runtime assembly (references InputSystem/Addressables/Splines/TMP/UI)
    │   │
    │   ├── Core/                          # Engine-agnostic services (no gameplay knowledge)
    │   │   ├── GameBootstrap.cs           #   Composition root: picks Steam vs Null
    │   │   ├── Events/                    #   EventBus (typed, alloc-free) + GameEvents structs
    │   │   ├── Services/                  #   Services locator
    │   │   ├── Audio/                     #   Conductor (DSP-clock timing) + AudioManager (stems/sfx)
    │   │   ├── Timing/                    #   BpmMap, TimingWindow, Judgment, JudgmentSystem
    │   │   ├── Scoring/                   #   ScoreManager (combo/multiplier/flow)
    │   │   ├── Input/                     #   InputRouter (New Input System, DSP-timestamped)
    │   │   ├── Pooling/                   #   ObjectPool<T>
    │   │   ├── Save/                      #   SaveSystem (atomic JSON) + SaveData
    │   │   ├── Settings/                  #   SettingsManager (volumes, offsets, accessibility)
    │   │   └── Scenes/                    #   SceneDirector (async load, instant retry)
    │   │
    │   ├── Data/                          # ScriptableObject + serializable content (no logic)
    │   │   ├── NodeType.cs                #   Node + Zone enums (the mechanic vocabulary)
    │   │   ├── NodeDefinition.cs          #   One authored node / mechanic zone
    │   │   ├── Beatmap.cs                 #   A playable chart (SO)
    │   │   ├── BeatmapSerializer.cs       #   .pbmap JSON <-> Beatmap
    │   │   ├── SongDefinition.cs          #   Song metadata + Addressable audio/stems
    │   │   ├── CosmeticDefinition.cs      #   Cosmetic reward (never affects gameplay)
    │   │   └── AchievementDefinition.cs   #   Achievement + reward mapping
    │   │
    │   ├── Gameplay/                      # The run itself
    │   │   ├── BeatmapRunner.cs           #   Orchestrates a run; streams nodes; grades
    │   │   ├── PulseController.cs         #   Moves the Pulse along the spline by beat
    │   │   └── Nodes/NodeView.cs          #   Pooled node visual/telegraph
    │   │
    │   ├── Modes/                         # Game-mode modifiers
    │   │   └── GameMode.cs                #   ModeConfig (Story/Endless/Hardcore/Practice/...)
    │   │
    │   ├── Progression/                   # Meta layer
    │   │   ├── CosmeticManager.cs         #   Own/equip cosmetics
    │   │   ├── AchievementManager.cs      #   Track + mirror achievements
    │   │   └── RunResultsRecorder.cs      #   Persist scores, Resonance XP, leaderboard push
    │   │
    │   ├── Steam/                         # Platform abstraction
    │   │   ├── ISteamService.cs           #   Contract (achievements/boards/cloud/workshop/presence)
    │   │   ├── NullSteamService.cs        #   Steam-free fallback (local disk cloud)
    │   │   └── SteamService.cs            #   Steamworks adapter (behind PULSEBOUND_STEAM define)
    │   │
    │   ├── LevelEditor/                   # In-game (runtime) editor — headline feature
    │   │   ├── LevelEditorController.cs   #   Model + ops: place/snap/playtest/export/publish
    │   │   └── DifficultyValidator.cs     #   Star rating + playability validation
    │   │
    │   └── UI/
    │       └── HUDController.cs           #   In-run HUD (event-driven, render-only)
    │
    ├── Beatmaps/                          # Shipped & sample charts
    │   └── first-light.easy.pbmap        #   Example chart in portable format
    │
    ├── Art/        Audio/       Prefabs/   Scenes/     Settings/
    └── (neon materials) (stems) (nodes/pulse) (Bootstrap/Menu/Gameplay/Editor) (URP + input assets)
```

## Assembly strategy

The runtime code currently ships as a **single assembly** (`Pulsebound.asmdef`). This is a
deliberate, valid choice for the current stage: the design layering is enforced by
namespace convention, and a single assembly sidesteps assembly-level dependency cycles
while the API surface is still settling.

The GDD's target end-state is per-module assemblies
(`Pulsebound.Core` → `Pulsebound.Data` → `Pulsebound.Game`). The one code move required to
split cleanly is relocating `JudgmentSystem` (which depends on `Data`) and `GameBootstrap`
(which depends on `Steam`) into the upper `Game` assembly — the timing *primitives*
(`BpmMap`, `TimingWindow`, `Judgment`) stay in `Core` with no `Data` dependency. Splitting is
tracked as a P1 task in the roadmap.

> **Note on the `LevelEditor/` folder name:** it is intentionally *not* called `Editor`.
> Unity reserves the folder name `Editor` for editor-only assemblies that are stripped from
> player builds. Pulsebound's level editor is a **runtime** feature, so it lives in
> `LevelEditor/`. Genuine Unity-editor tooling (custom inspectors, build scripts) would go in
> a separate `Editor/` folder with its own editor-only asmdef.
```
