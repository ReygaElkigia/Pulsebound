using Pulsebound.Core.Timing;

namespace Pulsebound.Core.Events
{
    /// <summary>
    /// Every gameplay event is a small readonly struct so publishing is allocation-free.
    /// Grouped here for discoverability; each is published on its own <see cref="EventBus"/> channel.
    /// </summary>
    public static class GameEvents
    {
        // ---- Conductor / timing ----
        public readonly struct SongStarted
        {
            public readonly string SongId;
            public readonly float Bpm;
            public SongStarted(string songId, float bpm) { SongId = songId; Bpm = bpm; }
        }

        public readonly struct BeatTick
        {
            public readonly int BeatIndex;   // whole beats since song start
            public readonly float SongTime;  // seconds
            public BeatTick(int beatIndex, float songTime) { BeatIndex = beatIndex; SongTime = songTime; }
        }

        public readonly struct SongEnded
        {
            public readonly string SongId;
            public readonly bool Completed; // false if aborted
            public SongEnded(string songId, bool completed) { SongId = songId; Completed = completed; }
        }

        // ---- Input ----
        public readonly struct BindPressed
        {
            public readonly double DspTime;
            public readonly bool IsAlt;
            public BindPressed(double dspTime, bool isAlt) { DspTime = dspTime; IsAlt = isAlt; }
        }

        public readonly struct BindReleased
        {
            public readonly double DspTime;
            public BindReleased(double dspTime) { DspTime = dspTime; }
        }

        // ---- Judgment / scoring ----
        public readonly struct NodeJudged
        {
            public readonly int NodeId;
            public readonly Judgment Judgment;
            public readonly float DeltaMs;   // signed: negative = early, positive = late
            public readonly float TargetBeat;
            public NodeJudged(int nodeId, Judgment judgment, float deltaMs, float targetBeat)
            {
                NodeId = nodeId; Judgment = judgment; DeltaMs = deltaMs; TargetBeat = targetBeat;
            }
        }

        public readonly struct ScoreChanged
        {
            public readonly long Score;
            public readonly int Combo;
            public readonly int MultiplierTier;
            public readonly float MultiplierValue;
            public readonly float Accuracy; // 0..1
            public ScoreChanged(long score, int combo, int multiplierTier, float multiplierValue, float accuracy)
            {
                Score = score; Combo = combo; MultiplierTier = multiplierTier;
                MultiplierValue = multiplierValue; Accuracy = accuracy;
            }
        }

        public readonly struct ComboBroken
        {
            public readonly int LostCombo;
            public ComboBroken(int lostCombo) { LostCombo = lostCombo; }
        }

        public readonly struct FlowChanged
        {
            public readonly float Flow01;    // 0..1
            public readonly bool Depleted;
            public FlowChanged(float flow01, bool depleted) { Flow01 = flow01; Depleted = depleted; }
        }

        public readonly struct SyncChainCompleted
        {
            public readonly int ChainLength;
            public SyncChainCompleted(int chainLength) { ChainLength = chainLength; }
        }

        // ---- Run lifecycle ----
        public readonly struct RunFinished
        {
            public readonly long Score;
            public readonly float Accuracy;
            public readonly int MaxCombo;
            public readonly bool FullPerfect;
            public readonly bool Failed;
            public RunFinished(long score, float accuracy, int maxCombo, bool fullPerfect, bool failed)
            {
                Score = score; Accuracy = accuracy; MaxCombo = maxCombo;
                FullPerfect = fullPerfect; Failed = failed;
            }
        }
    }
}
