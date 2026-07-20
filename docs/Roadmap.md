# Pulsebound — Roadmap & MVP Checklist

Condensed from GDD §20–21. Milestones gate on **feel and stability**, not feature counts.

## Phases

| Phase | Goal | Exit criteria |
| --- | --- | --- |
| **P0 Vertical Slice** | Prove the feel | Conductor + Standard/Hold/Double + judgment + scoring + 1 track feels *perfect* |
| **P1 Core Systems** | Production core | Save/Settings/Audio/Pooling/Scenes/EventBus done; 3 tracks; results + calibration; split assemblies |
| **P2 Mechanics** | Full vocabulary | All 15 mechanics implemented, teachable, validated |
| **P3 Editor** | Headline editor | Drag-drop, timeline, instant playtest, difficulty validation, `.pbmap` I/O |
| **P4 Modes & Meta** | Content loop | All modes, progression, cosmetics, achievements |
| **P5 Steam** | Platform | Achievements, leaderboards, cloud, Workshop, presence, Deck verified |
| **P6 Polish & Launch** | Ship | Perf targets, accessibility, localization, QA, demo |

## MVP checklist (demo-shippable)

### Core (implemented in this repo as the foundation)
- [x] Conductor with constant + multi-section + ramp BPM map (DSP timing)
- [x] Event bus (alloc-free) + service locator
- [x] Judgment system (Perfect/Great/Good/Miss) + per-difficulty windows
- [x] Score/combo/multiplier + Flow meter (fail-soft / fail-hard)
- [x] Object pooling
- [x] Save (atomic JSON, versioned, migration hook) + Settings + calibration offsets
- [x] Audio manager (adaptive stems + DSP-scheduled hit sfx)
- [x] Scene director (async + instant retry)
- [x] Input router (New Input System, DSP-timestamped, legacy fallback)
- [x] Beatmap data + `.pbmap` serializer + sample chart
- [x] Node types: Standard, Hold, Double, Echo, Reverse, TeleportGate, Split, SyncChain, Resonance, SilentDownbeat (data + judgment paths)
- [x] Mechanic zones: Gravity Shift, Speed, Invisible, Mirror, Tempo Ramp (data + Pulse motion)
- [x] Pulse movement bound to beat + zones
- [x] HUD (event-driven)
- [x] Progression: cosmetics, achievements, run-results recording (cosmetic-only)
- [x] Steam abstraction (interface + Null + guarded adapter)
- [x] Level editor core: place/snap/playtest/validate/export/publish
- [x] Difficulty validator (star rating + playability warnings)

### Remaining to reach a playable MVP build (needs the Unity Editor + assets)
- [ ] Author 3–5 handcrafted tracks with original music + stems
- [ ] Build Bootstrap/Menu/Gameplay/Editor scenes + node/pulse prefabs
- [ ] URP bloom look, reactive background, pooled hit-burst VFX
- [ ] Track select + Results screens (UI Toolkit / uGUI)
- [ ] Input Action asset + rebinding UI + Steam Input manifest
- [ ] Calibration minigame UI
- [ ] Story Act I level set + Practice/Zen wiring

### Fast-follow (post-MVP → 1.0)
- [ ] Full editor UI (waveform timeline, drag-drop, phrase stamps)
- [ ] Endless procedural phrase sequencer
- [ ] Daily/Weekly challenge seeding + boards
- [ ] Steam: real Steamworks binding wired into `SteamService`
- [ ] Split into `Pulsebound.Core` / `.Data` / `.Game` assemblies
- [ ] Localization + full accessibility pass
```
