# Pulsebound — Technical Architecture

A companion to GDD §18. This documents *how the code actually fits together* in this repo.

## 1. The timing spine (why it's correct)

Everything timed reads from the **Conductor**, which anchors to `AudioSettings.dspTime`:

```
dspSongStart = dspTime at PlayScheduled()
songSeconds  = dspTime - dspSongStart + audioOffset
songBeat     = BpmMap.SecondsToBeats(songSeconds)
```

Input is timestamped **inside the input callback** (`InputRouter`), not read in `Update()`.
A press becomes a beat via `Conductor.DspToBeat(dspTimestamp)`, which also applies the
player's `inputOffset` calibration. Judgment then compares `inputBeat` to `node.targetBeat`.

**Consequence:** identical judgments at 30 FPS and 240 FPS, on any machine — the requirement
for fair global leaderboards. Frame hitches cannot move the Pulse or shift a window.

The `BpmMap` supports constant tempo, mid-song tempo changes, and **Tempo Ramps** (linear
accelerando/ritardando) with a closed-form beat↔second integral, so ramps don't accumulate
numeric drift.

## 2. Event flow of a single hit

```
InputRouter ──BindPressed(dsp)──▶ EventBus
                                    │
                       JudgmentSystem subscribes
                                    │ classify vs TimingWindow
                                    ▼
                        NodeJudged(id, judgment, deltaMs) ──▶ EventBus
              ┌───────────────┬───────────────┬───────────────┐
              ▼               ▼               ▼               ▼
        ScoreManager     AudioManager      HUDController     NodeView
     (combo/mult/flow)  (hit sfx/stems)   (popup/score)   (return to pool)
              │
              ▼
        ScoreChanged / FlowChanged ──▶ HUD, AudioManager, Achievements
```

No system holds a reference to another. Adding a new reactor (e.g. a camera shake driver) is
one `EventBus.Subscribe` call and zero edits elsewhere.

## 3. Lifecycle of a run

```
BeatmapRunner.Begin(beatmap, clip, hardcore)
  ├─ JudgmentSystem.ResetForRun(difficulty)
  ├─ ScoreManager.Configure(hardcore)
  └─ Conductor.Play(clip, bpmMap, songId, leadIn)   → SongStarted

  Update() each frame:
  ├─ stream nodes within lookaheadBeats into JudgmentSystem.RegisterActive
  └─ detect end-of-chart → Finish()

BeatmapRunner.Finish(failed)
  ├─ Conductor.Stop()                                → SongEnded
  └─ RunFinished(score, acc, combo, fullPerfect, failed)
        └─ RunResultsRecorder: persist best, Resonance XP, leaderboard, unlock eval
        └─ AchievementManager: evaluate skill achievements
```

## 4. Memory & performance

- **Object pooling** (`ObjectPool<T>`) for node views, popups, particle bursts → zero
  steady-state GC during a run.
- **Struct events** on the `EventBus` → publishing does not allocate.
- **Addressables** stream tracks/stems and unload cleanly → supports DLC & Workshop content
  without rebuilds and keeps memory bounded.
- Targets: 60–144 FPS Windows, stable 60 on Steam Deck, DSP-bound input latency.

## 5. Platform isolation

`ISteamService` is the only seam to Steam. `GameBootstrap` registers either `SteamService`
(behind the `PULSEBOUND_STEAM` define + a Steamworks binding) or `NullSteamService`. Nothing
else in the codebase references a Steam type, so the game runs identically off-Steam.

## 6. Data-driven content

| Concept | Type | Authored in |
| --- | --- | --- |
| Chart | `Beatmap` (SO) / `.pbmap` (JSON) | Editor / inspector |
| Song + audio | `SongDefinition` (SO, Addressable) | Inspector |
| Node / mechanic | `NodeDefinition` / `MechanicZone` (struct) | Editor |
| Difficulty windows | `TimingWindow` | Code defaults, per-difficulty |
| Cosmetic | `CosmeticDefinition` (SO) | Inspector |
| Achievement | `AchievementDefinition` (SO) | Inspector |

The editor and shipped content share the exact same `Beatmap`/`.pbmap` types, so *what you
build is what you play* and Workshop transport is trivial.

## 7. Determinism contract (leaderboard integrity)

- Gameplay timing derives **only** from the authored beat map + DSP clock.
- FFT/audio-reactivity drives **cosmetics only**, never judgment.
- Same inputs + same chart ⇒ same score on any hardware.
```
