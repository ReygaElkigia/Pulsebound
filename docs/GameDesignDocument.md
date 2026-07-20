# Pulsebound — Game Design Document

> **Version:** 1.0 &nbsp;•&nbsp; **Engine:** Unity 6 (C#) &nbsp;•&nbsp; **Genre:** Minimalist Precision Rhythm &nbsp;•&nbsp; **Platforms:** Windows (Steam), Steam Deck

> **Originality statement:** Pulsebound is an entirely original work. All mechanics, systems, visual identity, music, level content, and terminology described here are designed from scratch for this project. Nothing in this document copies mechanics, maps, UI, code, music, or assets from any existing product.

---

## Table of Contents

1. [Game Overview](#1-game-overview)
2. [Gameplay Loop](#2-gameplay-loop)
3. [Core Mechanics](#3-core-mechanics)
4. [Original Mechanics](#4-original-mechanics)
5. [Controls](#5-controls)
6. [Level Design Philosophy](#6-level-design-philosophy)
7. [Difficulty Progression](#7-difficulty-progression)
8. [Music Synchronization System](#8-music-synchronization-system)
9. [Art Direction](#9-art-direction)
10. [UI/UX Design](#10-uiux-design)
11. [Audio Design](#11-audio-design)
12. [Progression System](#12-progression-system)
13. [Unlockable Cosmetics](#13-unlockable-cosmetics)
14. [Game Modes](#14-game-modes)
15. [Achievement System](#15-achievement-system)
16. [Level Editor Design](#16-level-editor-design)
17. [Steam Integration](#17-steam-integration)
18. [Technical Architecture](#18-technical-architecture)
19. [Folder Structure](#19-folder-structure)
20. [Development Roadmap](#20-development-roadmap)
21. [MVP Feature List](#21-mvp-feature-list)
22. [Future DLC and Content Updates](#22-future-dlc-and-content-updates)

---

## 1. Game Overview

**Pulsebound** is a minimalist precision rhythm game where the player guides a single glowing orb — the **Pulse** — through a living musical world. The Pulse is always in motion, tracing handcrafted paths that breathe with the soundtrack. The player provides one thing: **perfectly timed intent**. Everything else — movement, color, camera, the world itself — is a reaction to the music and to the player's accuracy.

The design pillar is **flow through friction**. The controls are trivially simple (one primary action) but mastery is deep, because the *timing* of that single action carries all of the game's difficulty, expression, and reward. When a run is played well, music, light, and motion collapse into a single unified sensation. When it is played poorly, the world visibly and audibly destabilizes — never punishing the player with a wall of text, only with the loss of the flow state they were chasing.

### Design Pillars

| Pillar | Meaning | How it shows up |
| --- | --- | --- |
| **One Input, Infinite Depth** | A single action, mastered through timing | Node types re-contextualize the *same* press |
| **The Music Is The Level** | Levels are authored *to* the track, never against it | Every node lands on a musically meaningful moment |
| **Readable Chaos** | Maximal spectacle, zero ambiguity | VFX never obscures the next actionable beat |
| **Fair Failure** | Losing teaches, never confuses | Deterministic timing, visible windows, instant retry |
| **Made By Everyone** | The community authors the long tail | First-class level editor + Workshop |

### Target Audience

- Rhythm and score-chase players who value precision and leaderboards.
- Players who enjoy "one more try" loops with sub-second retries.
- Creators who want an approachable but expressive level-authoring toolset.

### Unique Selling Points

1. **The Pulse is continuous, not discrete.** You are not hitting falling notes — you are *steering a moving light* and its momentum is part of the read.
2. **Reactive world state.** Combo, accuracy, and multiplier physically reshape the environment in real time.
3. **A level editor strong enough to be a headline feature**, with instant playtest, difficulty validation, and Workshop publishing.
4. **Cosmetic-only progression.** Nothing you unlock changes timing, hitboxes, or difficulty. Skill is the only currency that matters to the leaderboard.

---

## 2. Gameplay Loop

### Micro Loop (seconds)
```
Anticipate next node  →  Read its type/telegraph  →  Time the input  →  Receive judgment (Perfect/Great/Good/Miss)
        ↑                                                                             │
        └──────────────────── world + combo react, feed anticipation ────────────────┘
```

### Session Loop (minutes)
```
Pick track/mode  →  Play run  →  Results (score, accuracy, grade, combo)  →  Compare to PB/leaderboard  →  Retry or advance
```

### Meta Loop (hours → weeks)
```
Earn Resonance (soft XP)  →  Unlock cosmetics  →  Chase grades/achievements  →  Climb leaderboards  →  Play + create community levels  →  Daily/Weekly challenges refresh
```

The three loops are nested and each feeds the next. The micro loop must feel perfect *before anything else is built* — it is the game.

---

## 3. Core Mechanics

### The Pulse
A glowing orb that travels along a **Path** at a music-driven speed. The Pulse always moves; the player never controls position directly. The player controls **timing** — when to fire the **Bind** action.

### Nodes
A **Node** is a point on the path anchored to a specific beat or sub-beat. As the Pulse approaches a node, the player must press **Bind** within the node's timing window. Node *types* change what a correct press means, but the input is always the same button.

### The Bind Window
Every node exposes a symmetric timing window centered on its beat. The press is judged against the delta between input time and target time:

| Judgment | Default window (± ms) | Combo effect | Score weight |
| --- | --- | --- | --- |
| **Perfect** | 0–35 ms | +1, keeps multiplier growth | 1.00 |
| **Great** | 36–70 ms | +1, slower multiplier growth | 0.75 |
| **Good** | 71–110 ms | +1, no multiplier growth | 0.40 |
| **Miss** | >110 ms or wrong action | resets combo | 0.00 |

Windows are **data-driven per difficulty** (see §7) and can be widened in Practice/Zen or tightened in Hardcore.

### Combo & Multiplier
Consecutive non-miss judgments build **Combo**. The **Multiplier** rises in tiers as combo grows and as Perfect ratio stays high. A Miss resets combo to 0 and drops the multiplier one tier (not to zero) — punishing but recoverable.

### Health / Flow
Pulsebound uses a **Flow Meter** rather than lives. Perfects fill it, Misses drain it. In most modes a depleted Flow Meter *dims the world and mutes layers of the track* but does not end the run (fail-soft). In Hardcore, empty Flow ends the run (fail-hard).

---

## 4. Original Mechanics

All mechanics below are original to Pulsebound. Each is implemented as a **Node type or Zone** driven by data, so the editor exposes them uniformly.

| # | Mechanic | Player experience | Read/telegraph |
| --- | --- | --- | --- |
| 1 | **Standard Node** | One press, one beat. | Solid ring that closes on the beat. |
| 2 | **Hold Node** | Press and *sustain* across a sustained note; release on the tail beat. | Ring elongates into a "tether" the Pulse rides. |
| 3 | **Double Beat Node** | Two presses in rapid succession (beat + immediate sub-beat). | Ring with a nested second ring. |
| 4 | **Echo Node** | Hit once, then repeat the *same rhythm* an interval later "from memory" — the echo has a faded telegraph. | First node bright, echo is a ghost. |
| 5 | **Reverse Rhythm Node** | The window scoring flips — you must press *late-biased* deliberately, teaching anti-anticipation. | Ring closes *outward* instead of inward. |
| 6 | **Teleport Gate** | The Pulse jumps to a new path segment on hit; missing the gate desyncs the camera. | Paired portal glyphs; a beam links entry/exit. |
| 7 | **Split Path Decision** | Two branches appear; the *timing* of the press selects the branch (early = top, late = bottom). Branches rejoin. | Fork glyph with two lit arcs. |
| 8 | **Gravity Shift Section** | The path's "down" rotates; camera and Pulse momentum re-orient, changing your spatial read of upcoming nodes. | Zone tint + rotating horizon line. |
| 9 | **Speed Modifier** | The Pulse accelerates/decelerates for a stretch; note spacing looks the same but *feels* faster/slower. | Chevrons on the path; speed value pip. |
| 10 | **Invisible Rhythm Section** | Nodes fade out; you play by internalized timing, rewarded with a bonus multiplier. | Fade-out warning, faint beat pips only. |
| 11 | **Pulse Synchronization Chain** | A run of nodes must *all* be Perfect to trigger a "Sync" — a burst multiplier and a musical stinger; one non-Perfect breaks the chain. | Chain-linked rings that light sequentially. |
| 12 | **Resonance Node** *(new)* | Charges on the beat, releases a shockwave that auto-clears the *next* node if timed within Great — a risk/reward "banked" hit. | Pulsing halo that fills over 1 bar. |
| 13 | **Silent Downbeat** *(new)* | A node on a *rest* — the music drops out for one beat and you must hit into the silence. | Full audio duck + single visual metronome tick. |
| 14 | **Mirror Section** *(new)* | The path and controls mirror horizontally; visual read is inverted while timing stays true. | Screen-space mirror sweep transition. |
| 15 | **Tempo Ramp** *(new)* | BPM smoothly interpolates across a section (accelerando/ritardando); the Conductor tracks a curve, not a constant. | Animated BPM readout + path density shift. |

**Design rule:** every mechanic must be teachable in a single unlabeled encounter and must never require reading text mid-run. Telegraphs are visual + audio, never tutorialized on screen during play.

---

## 5. Controls

| Action | Keyboard | Controller | Steam Deck |
| --- | --- | --- | --- |
| **Bind** (primary) | Space / Left-Click / J | A / RT / any face button | A / R2 |
| **Alt-Bind** (Double/Split assist) | K / F | X / LT | X / L2 |
| Pause | Esc | Start | Menu |
| Restart run | R | Y (hold) | Y (hold) |
| Navigate menus | Arrows / WASD | D-pad / Stick | D-pad / Stick |
| Editor: place node | Left-Click | A | A / touch |
| Editor: scrub timeline | Mouse drag / , . | Triggers | Trackpad |

**Latency:** Input is polled and timestamped against the audio DSP clock, not frame time (see §8), so judgments are frame-rate independent. A per-user **audio + input offset calibration** is available in Settings and Practice.

**Accessibility:** full rebinding via the New Input System, single-button playability for every non-editor mode, colorblind-safe judgment palette, screen-shake/flash toggles, and a "reduced motion" option that disables Gravity Shift/Mirror camera rotation while preserving timing.

---

## 6. Level Design Philosophy

1. **Author to the track, not over it.** Every node maps to something audible — a kick, a snare, a vocal chop, a synth stab. If a player closes their eyes, the rhythm they'd tap should match the nodes.
2. **Introduce, isolate, combine.** A new mechanic appears alone in a calm passage (introduce), is drilled a few times (isolate), then is layered with prior mechanics (combine). This is the "teach without words" contract.
3. **Density follows dynamics.** Node density tracks the song's energy. Breakdowns thin out; drops spike. The level *is* a visualization of the arrangement.
4. **Peaks and rests.** Never sustain maximum density. Rest bars let the player breathe and reset anticipation, making the next peak land harder.
5. **The last 20% is the signature.** Each level ends on a memorable "signature phrase" — usually a Sync Chain or a Silent Downbeat run — that defines that level's identity.
6. **One idea per level.** Story levels each foreground a single mechanic as their "theme" so the mechanic vocabulary grows legibly.

---

## 7. Difficulty Progression

Difficulty is expressed along independent axes so designers (and the validator) can reason about it:

| Axis | Easy | Normal | Hard | Expert | Pulsemaster |
| --- | --- | --- | --- | --- | --- |
| Perfect window (±ms) | 55 | 45 | 35 | 28 | 22 |
| Node density (nodes/bar avg) | 1–2 | 2–4 | 4–6 | 6–9 | 8–12 |
| Mechanics in rotation | 1–3 | 1–6 | all | all | all + variants |
| Sub-beat resolution | 1/4 | 1/8 | 1/16 | 1/16+triplets | 1/24 |
| Invisible/Silent sections | none | rare | occasional | frequent | signature |
| Flow drain per miss | low | medium | high | high | fail-hard option |

**Star rating (1–10★)** is a *computed* value from a weighted formula over the axes plus peak density and mechanic-transition frequency (see §16 Difficulty Validation). This is what the editor validates and what sorts community levels.

**Onboarding curve:** Story Journey Act I never exceeds 3★; the game front-loads *feel* over challenge, then ramps.

---

## 8. Music Synchronization System

The synchronization core is the **Conductor**. It is the single source of truth for musical time and everything (nodes, camera, VFX, background) reads from it — never from `Time.time`.

### Timing model
- The Conductor reads `AudioSettings.dspTime` at the moment playback starts and stores `dspSongStart`.
- **Song position (seconds)** = `AudioSettings.dspTime - dspSongStart - userOffset`.
- **Song position (beats)** = `secondsToBeats(songPositionSeconds)` using the active BPM map.
- BPM is a **map**, not a scalar: a list of `(beat, bpm)` anchors supporting constant BPM, and **Tempo Ramp** sections via interpolation curves. `beatsToSeconds`/`secondsToBeats` integrate across the map.
- **Sub-beats:** nodes are authored in beats as floats (e.g. `12.5` = the "and" of beat 12). Resolution goes to 1/24 for triplet/expert content.

### Judgment pipeline
```
Input event (DSP-timestamped)
      → find nearest active node on the path
      → delta = inputBeat - node.targetBeat  (converted to ms via local BPM)
      → classify against difficulty TimingWindow
      → raise NodeJudged(node, judgment, deltaMs)
```
Because judgment uses DSP time, it is **frame-rate independent** (correct at 30 or 240 FPS) and robust to hitches.

### Calibration
- **Audio offset:** compensates output-device latency.
- **Input offset:** compensates the player's own bias (systematically early/late).
- A guided calibration minigame in Settings/Practice measures both and writes them to the Settings save.

### Audio-reactive gameplay
A lightweight FFT (`AudioSource.GetSpectrumData`) drives *cosmetic* reactivity (background bloom, particle emission). **Gameplay-critical timing never depends on FFT** — only on the authored beat map — to guarantee determinism and fair leaderboards.

### Custom songs & beatmaps
Beatmaps are `ScriptableObject`-backed data (`Beatmap`) but also serialize to/from a plain-text `.pbmap` JSON for the editor, custom songs, and Workshop transport. See §16.

---

## 9. Art Direction

**Theme:** *Luminous minimalism.* A dark, deep-space canvas where the only bright things are the Pulse, the path, the nodes, and the reactions they cause. Neon on void.

- **Palette:** near-black backgrounds (`#05060A`) with a per-level **accent duo** (e.g. cyan `#28E5FF` + magenta `#FF3DAE`). Accent duos are theme-swappable cosmetics.
- **Geometry:** abstract, low-poly and line-based. Paths are glowing splines; nodes are rings/glyphs; environments are parallaxed wireframe structures that pulse on the downbeat.
- **Lighting:** bloom + additive emission carry the look. The Pulse is the brightest object on screen and casts a soft light that the path catches as it passes.
- **Particles:** node hits emit accuracy-tinted bursts (Perfect = accent, Great = softened, Good = white, Miss = desaturated shard). Trails are pooled ribbon meshes.
- **Reactive background:** low-frequency energy drives horizon bloom and structure scale; the world literally breathes on the kick.
- **Camera:** smooth follow with anticipatory lead (looks slightly *ahead* of the Pulse), subtle beat-synced dolly, and full re-orientation for Gravity Shift/Mirror. All camera motion is on a "reduced motion" toggle.
- **Readability contract:** the actionable next node is always the highest-contrast element in the frame. VFX intensity is clamped near active nodes so spectacle never eats the read.

---

## 10. UI/UX Design

**Philosophy:** minimum chrome, maximum information density where it matters, and *diegetic feedback first* (the world tells you how you're doing before any number does).

### In-run HUD (top-to-bottom priority)
- **Combo** (large, center-top, scales with combo tier).
- **Multiplier** (beside combo).
- **Accuracy %** and **Score** (corner, small).
- **Flow Meter** (thin ring around the Pulse or a slim top bar).
- **Judgment popups** (Perfect/Great/Good/Miss) at the hit location, brief and non-blocking.

### Menus
- **Track select:** carousel with cover art, difficulty stars, best grade, and leaderboard rank.
- **Results:** grade (S+/S/A/B/C/D), accuracy breakdown (Perfect/Great/Good/Miss counts), max combo, score delta vs PB, and one-tap Retry / Next.
- **Everything is controller- and Deck-first:** no menu requires a mouse; focus navigation and hold-to-confirm on destructive actions.

### UX principles
- **Instant retry** (< 300 ms) — the single most important menu action.
- **No modal walls mid-flow.** Pausing dims, never hard-cuts, the audio.
- **Consistent input glyphs** that swap between keyboard/Xbox/PlayStation/Deck automatically.

---

## 11. Audio Design

- **Adaptive stems:** each track ships as layered stems (drums, bass, lead, pad, fx). The **Flow Meter** and combo tier gate stems in/out — playing well *adds* layers; struggling strips them. This makes performance audible.
- **Hit feedback:** each judgment has a short, pitched confirmation blip mixed *under* the music, tuned to the track's key so it never sounds wrong. Perfect uses the brightest timbre.
- **Silent Downbeat / ducking:** the Silent Downbeat mechanic ducks the master bus for exactly one beat via a scheduled envelope, not a naive volume lerp, so it's sample-accurate.
- **Sync Chain stinger:** completing a Pulse Synchronization Chain triggers a musical stinger layered on the current bar.
- **Mixing:** master → music bus (stems) + sfx bus + ui bus, with per-bus volume in Settings. All one-shots are scheduled via `PlayScheduled` against DSP time for tight sync.
- **Original music only.** All tracks are composed for Pulsebound; the beatmap authoring assumes stems exist for every shipped track.

---

## 12. Progression System

Progression is **100% cosmetic and skill-gated. No mechanic, window, hitbox, or timing is ever purchasable or unlockable.** Leaderboards are pure skill.

- **Resonance (soft XP):** earned from any run, scaled by accuracy and difficulty (not by grind time). Fills a per-profile level track that hands out cosmetics.
- **Grade goals:** each track/difficulty tracks your best grade; hitting A/S/S+ unlocks themed cosmetics tied to that track.
- **Mastery tokens:** full-Perfect ("Pulse Perfect") clears award rare tokens spent on prestige cosmetics.
- **Challenge streaks:** Daily/Weekly participation builds streaks that unlock exclusive seasonal cosmetics.

There is **no energy, no stamina, no loot boxes, no paid power.** Optional cosmetic DLC packs (see §22) never touch gameplay.

---

## 13. Unlockable Cosmetics

| Category | Examples | Source |
| --- | --- | --- |
| **Pulse Skins** | Orb, Prism, Comet, Ember, Glitch, Origami | Resonance levels, grade goals |
| **Energy Trails** | Ribbon, Sparkstream, Ink, Aurora, Wireframe | Resonance, mastery tokens |
| **Hit Effects** | Bloom Burst, Shatter, Petal, Pixel Pop | Grade goals, achievements |
| **Background Themes** | Deep Void, Neon Grid, Solar, Monochrome, Sakura | Resonance, seasonal |
| **UI Themes** | Default, Terminal, Glass, Vapor, High-Contrast | Resonance, accessibility (High-Contrast always free) |
| **Profile Icons** | Node glyphs, grade badges, seasonal marks | Achievements, challenges |
| **Titles** | "Perfectionist", "Metronome", "Ghost Reader" | Achievements |
| **Accent Duos** | Cyan/Magenta, Lime/Violet, Amber/Teal, Mono | Resonance, grade goals |

All cosmetics are defined as `CosmeticDefinition` ScriptableObjects and equipped via the `CosmeticManager`, persisted in the save. High-Contrast UI and colorblind palettes are **never gated** (accessibility first).

---

## 14. Game Modes

| Mode | Description | Failure | Leaderboard |
| --- | --- | --- | --- |
| **Story Journey** | Curated acts that teach the mechanic vocabulary and tell an abstract story through music and world state. | Fail-soft | Per-level |
| **Endless** | Procedurally sequenced authored *phrases* stitched to a continuous mix; goes until you drop. | Fail-soft, ramps | Distance/score |
| **Hardcore** | Any track, tightest windows, fail-hard on empty Flow, no mid-run assists. | Fail-hard | Separate board |
| **Practice** | Any track with section looping, speed scaling (50–100%), window preview, and free calibration. | None | None |
| **Daily Challenge** | One seeded track+mods for everyone, 24h, one ranked attempt + free practice. | Fail-soft | Daily board |
| **Weekly Challenge** | A harder seeded gauntlet of 3 tracks, 7 days, cumulative score. | Fail-soft | Weekly board |
| **Speed Mode** | Any track played at 110–150% BPM with scaled scoring for the risk. | Fail-soft | Per-speed board |
| **Zen** | No scoring, no fail, widened windows, maxed reactive visuals — pure flow. | None | None |
| **Community Levels** | Play, rate, and favorite Workshop levels; sortable by validated difficulty. | Per-level | Per-level |

---

## 15. Achievement System

Achievements are `AchievementDefinition` ScriptableObjects with progress tracking, mirrored to Steam. Categories:

- **Skill:** first S grade, first Pulse Perfect, 500-combo, all-Perfect Sync Chain, clear a 9★+ level.
- **Breadth:** clear all Story Act I, play every mechanic, clear one level in every mode.
- **Endurance:** 10k lifetime Perfects, Endless past a distance threshold, 7-day Daily streak.
- **Mastery:** Pulse Perfect a Hardcore Expert track, S+ a Speed 150% run.
- **Creation:** publish a level, get 100 Workshop subscribers, get a 4.5★+ community rating.
- **Hidden:** signature-phrase Easter eggs (e.g. hit every Silent Downbeat in a level).

Each achievement can grant a cosmetic and/or title. Progress is local-authoritative and synced to Steam Stats when online.

---

## 16. Level Editor Design

The editor is a **headline feature**, not a bolt-on. It runs on the same runtime systems the game uses, so *what you build is exactly what you play*.

### Core capabilities
- **Drag-and-drop node placement** onto a beat-snapped timeline and onto the path.
- **BPM editing** with the full BPM map (constant, multi-section, Tempo Ramp curves) and tap-tempo.
- **Timeline editing:** waveform view, beat/sub-beat grid (snap 1/4 → 1/24), multi-select, copy/paste, phrase stamps, and mechanic zones (Gravity Shift, Speed, Invisible, Mirror) as ranged objects.
- **Instant playtest:** one key to jump into a live run from the cursor; one key back to edit at the same position. Sub-second round-trip.
- **Custom backgrounds & VFX:** pick theme, accent duo, reactive intensity, and per-section overrides.
- **Difficulty validation:** the editor runs the same **star-rating formula** the game uses, plus warnings for un-hittable spacing, off-grid nodes, unreachable Splits, and "unfair" invisible sections. It computes a suggested star rating and flags anything above a hard-playability threshold.
- **Auto-metadata:** estimated length, peak density, mechanic list, and a spectrogram thumbnail for the browser card.

### Data & sharing
- Beatmaps serialize to `.pbmap` (JSON) for portability and to a `Beatmap` ScriptableObject for in-engine authoring of shipped content.
- **Steam Workshop integration:** publish, update, subscribe, tag, and browse from inside the editor/menus. Ratings and play counts feed sort order.
- **Safety:** custom audio is validated for length/format; text fields are sanitized; Workshop items are sandboxed data only (no executable content).

---

## 17. Steam Integration

Wrapped behind an `ISteamService` interface so the game runs identically without Steam (e.g. dev, itch build) via a `NullSteamService`.

- **Achievements & Stats:** mirrored from the local achievement/stat store.
- **Leaderboards:** per track+difficulty+mode; upload best score, download friends/global/around-me.
- **Cloud Saves:** save file + settings + custom levels synced via Steam Cloud.
- **Workshop:** publish/subscribe/browse custom levels; auto-download subscribed items.
- **Rich Presence:** "Playing *Track* — S rank attempt", "Editing a level", "Endless: 4,120m".
- **Full Controller Support:** Steam Input action manifest; every mode fully playable on Deck.

Implementation uses a Steamworks binding (e.g. Steamworks.NET / Facepunch) behind the interface; the rest of the game never references Steam types directly.

---

## 18. Technical Architecture

**Engine:** Unity 6, C#, URP (for bloom/post), New Input System, Addressables.

**Principles:** modular, event-driven, ScriptableObject-data-driven, pooled, service-locator-mediated.

### Layered design
```
┌─────────────────────────────────────────────────────────────┐
│  Presentation:   HUD, Menus, VFX, Camera, Reactive BG        │
├─────────────────────────────────────────────────────────────┤
│  Gameplay:       PulseController, BeatmapRunner, Node types,  │
│                  Mechanic zones                               │
├─────────────────────────────────────────────────────────────┤
│  Core services:  Conductor, Judgment, Scoring, Input,         │
│                  Pooling, Save, Settings, Scenes, Audio       │
├─────────────────────────────────────────────────────────────┤
│  Data (SO/JSON): Beatmap, SongDefinition, NodeDefinition,     │
│                  Cosmetic/Achievement definitions             │
├─────────────────────────────────────────────────────────────┤
│  Platform:       ISteamService (Steam / Null)                 │
└─────────────────────────────────────────────────────────────┘
```

### Key patterns
- **Event bus** (`EventBus` + typed `GameEvents` structs): systems communicate by publishing/subscribing, not by hard references. The Judgment system raises `NodeJudged`; Scoring, HUD, VFX, and Audio all react independently.
- **Service locator** (`Services`): a thin registry so systems resolve `Conductor`, `SaveSystem`, etc., without singletons leaking everywhere. Registration is explicit and testable.
- **ScriptableObject data:** all content (beatmaps, nodes, cosmetics, achievements, difficulty windows) is data, not code — editor-authorable and hot-swappable.
- **Object pooling:** nodes, judgment popups, and particle bursts are pooled (`ObjectPool<T>`) — zero per-node GC during a run.
- **Addressables:** tracks, audio stems, and level content load on demand and unload cleanly; supports DLC/Workshop without rebuilds.
- **DSP-clock timing:** the Conductor drives all timing from `AudioSettings.dspTime` for frame-rate-independent, deterministic judgment.
- **Save system:** versioned JSON save (profile, unlocks, best scores, settings) with atomic write + backup and a migration hook.

### Performance targets
- 60–144 FPS on Windows; stable 60 on Steam Deck.
- Zero steady-state GC allocations during a run (pooling + struct events).
- Input-to-judgment latency bounded by DSP polling, independent of frame time.

---

## 19. Folder Structure

See [`docs/FolderStructure.md`](./FolderStructure.md) for the annotated tree. Summary:

```
Assets/Pulsebound/
├── Scripts/
│   ├── Core/         # Events, Audio, Timing, Scoring, Input, Pooling, Save, Settings, Scenes, Services
│   ├── Data/         # ScriptableObject definitions (Beatmap, Song, Node, Cosmetic, Achievement, Difficulty)
│   ├── Gameplay/     # PulseController, BeatmapRunner, Nodes/, Mechanics/
│   ├── Modes/        # Mode controllers (Story, Endless, Hardcore, ...)
│   ├── Progression/  # Cosmetic + Achievement managers
│   ├── Steam/        # ISteamService + implementations
│   ├── Editor/       # In-game level editor (runtime) + Unity editor tooling
│   └── UI/           # HUD + menu controllers
├── Art/  Audio/  Prefabs/  Scenes/  Settings/  Beatmaps/
```

Each `Scripts/*` module has its own `.asmdef` to enforce dependency direction (Presentation → Gameplay → Core → Data → Platform) and to keep compile times low.

---

## 20. Development Roadmap

| Phase | Duration | Goal | Exit criteria |
| --- | --- | --- | --- |
| **P0 – Vertical Slice** | 4–6 wks | Prove the feel | Conductor + Standard/Hold/Double nodes + judgment + scoring + one hand-made track feels *perfect* |
| **P1 – Core Systems** | 6–8 wks | Production-ready core | Save/Settings/Audio/Pooling/Scenes/Event bus complete; 3 tracks; results + calibration |
| **P2 – Mechanics** | 6–8 wks | Full mechanic vocabulary | All 15 mechanics implemented, teachable, validated |
| **P3 – Editor** | 8–10 wks | Headline editor | Drag-drop, timeline, instant playtest, difficulty validation, `.pbmap` I/O |
| **P4 – Modes & Meta** | 6–8 wks | Content loop | All modes, progression, cosmetics, achievements |
| **P5 – Steam** | 4–6 wks | Platform | Achievements, leaderboards, cloud, Workshop, rich presence, Deck verified |
| **P6 – Polish & Launch** | 6–8 wks | Ship | Perf targets, accessibility, localization, QA, store page, demo |

Milestones gate on *feel and stability*, not feature counts. P0 will not exit until the micro loop is fun on its own.

---

## 21. MVP Feature List

The smallest build that is genuinely fun and shippable as a demo:

**Must-have (MVP)**
- [ ] Conductor with constant + multi-section BPM map (DSP-clock timing)
- [ ] Pulse movement along a spline path
- [ ] Node types: Standard, Hold, Double Beat
- [ ] Judgment (Perfect/Great/Good/Miss) with per-difficulty windows + calibration
- [ ] Scoring: combo, multiplier tiers, Flow meter (fail-soft)
- [ ] Event bus + service locator + object pooling
- [ ] Save/Settings (versioned JSON) + audio/input offset
- [ ] Audio manager with stem gating and scheduled hit SFX
- [ ] 3–5 handcrafted tracks with beatmaps (Easy→Hard)
- [ ] Track select + Results + instant retry
- [ ] Story Journey Act I + Practice + Zen modes
- [ ] Basic cosmetics (a few Pulse skins + accent duos)
- [ ] URP bloom look, reactive background, node/hit VFX (pooled)
- [ ] Full keyboard + controller support

**Fast-follow (post-MVP, pre-1.0)**
- Remaining mechanics, remaining modes, full progression/achievements, level editor, Steam integration.

---

## 22. Future DLC and Content Updates

All post-launch content keeps the **cosmetic-only, no-pay-to-win** promise.

- **Music Packs (paid, optional):** themed sets of original tracks with hand-made beatmaps and matching background/accent themes. Never affect balance.
- **Cosmetic Packs (paid, optional):** Pulse skins, trails, hit effects, UI/background themes.
- **Seasonal Events (free):** rotating Daily/Weekly themes, event-exclusive cosmetics, leaderboards.
- **Editor updates (free):** new mechanics exposed to creators, richer validation, quality-of-life.
- **Community spotlights (free):** curated Workshop collections promoted in-game.
- **Endless expansions (free):** new authored phrase pools for fresh procedural sequencing.
- **Accessibility & localization updates (free):** ongoing, always free.

**Roadmap intent:** treat Pulsebound as a living rhythm platform where the community's levels are the infinite content, and first-party updates keep the toolbox and cosmetics fresh.

---

*End of Game Design Document — Pulsebound v1.0*
