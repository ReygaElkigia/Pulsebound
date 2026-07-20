# Pulsebound

> A minimalist precision **rhythm game** built in **Unity 6 (C#)**. Guide a glowing **Pulse**
> through a living musical world where every input is locked to the soundtrack. Easy to learn,
> difficult to master, built for score-chasing and community levels.

**100% original.** Pulsebound's mechanics, systems, identity, and content are designed from
scratch. It copies no mechanics, maps, UI, music, or assets from any existing product.

---

## What's in this repository

This repo contains the **Game Design Document** and a **working, modular code foundation** for
Pulsebound — the systems layer a full production builds on top of.

### Documentation (`docs/`)
- **[Game Design Document](docs/GameDesignDocument.md)** — the complete 22-section GDD
  (vision, mechanics, modes, progression, editor, Steam, architecture, roadmap).
- **[Technical Architecture](docs/TechnicalArchitecture.md)** — how the code fits together.
- **[Folder Structure](docs/FolderStructure.md)** — annotated project tree.
- **[Roadmap & MVP](docs/Roadmap.md)** — milestones and the MVP checklist.

### Code (`Assets/Pulsebound/Scripts/`)
A Unity 6 project with an event-driven, ScriptableObject-data-driven, pooled architecture:

| System | Highlights |
| --- | --- |
| **Conductor** | DSP-clock timing (`AudioSettings.dspTime`), frame-rate-independent judgment, full BPM map with tempo ramps |
| **Judgment** | Perfect/Great/Good/Miss vs. per-difficulty timing windows; auto-miss on elapsed windows |
| **Scoring** | Combo, tiered multiplier, Flow meter (fail-soft / Hardcore fail-hard), accuracy, grades |
| **Event bus** | Type-safe, allocation-free struct events; systems never reference each other |
| **Input** | New Input System, inputs timestamped in-callback; legacy fallback so it always compiles |
| **Save/Settings** | Versioned atomic JSON save with backup + migration; calibration offsets |
| **Audio** | Adaptive stem gating (play well → fuller mix), DSP-scheduled hit feedback |
| **Content** | `Beatmap` SO + portable `.pbmap` JSON; 15 original mechanics as data |
| **Gameplay** | `BeatmapRunner` orchestration, spline-based `PulseController`, pooled `NodeView` |
| **Progression** | Cosmetic-only unlocks, achievements, Resonance XP — no pay-to-win |
| **Level Editor** | Runtime editor core: place/snap/instant-playtest/validate/export/publish |
| **Steam** | `ISteamService` seam with `NullSteamService` + guarded Steamworks adapter |

The design pillars — **one input / infinite depth**, **the music is the level**,
**readable chaos**, **fair failure**, **made by everyone** — drive every system above.

---

## Requirements

- **Unity 6** (`6000.0.x`) — see [`ProjectSettings/ProjectVersion.txt`](ProjectSettings/ProjectVersion.txt)
- Packages (auto-restored from [`Packages/manifest.json`](Packages/manifest.json)): URP,
  Input System, Addressables, Splines, TextMeshPro, Cinemachine.

## Getting started

1. Open the project folder in Unity 6. Unity restores packages on first open.
2. Explore `Assets/Pulsebound/Scripts/` — every system self-registers via the `Services`
   locator; `GameBootstrap` is the composition root.
3. The remaining MVP work (scenes, prefabs, original tracks, VFX) is tracked in
   [`docs/Roadmap.md`](docs/Roadmap.md). The code layer here is what those assets plug into.

## Design principles (non-negotiable)

- **Cosmetic-only progression.** Nothing unlockable changes timing, hitboxes, or difficulty.
- **Deterministic timing.** Judgment derives only from the authored beat map + DSP clock;
  audio-reactivity is cosmetic. Same inputs + same chart ⇒ same score, on any hardware.
- **Accessibility first.** Single-button playability, full rebinding, reduced-motion,
  colorblind-safe palettes, and High-Contrast UI are never gated.

---

*Pulsebound — fast, satisfying, visually striking, and completely original.*
