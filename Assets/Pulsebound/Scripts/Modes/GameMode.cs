using Pulsebound.Core.Timing;

namespace Pulsebound.Modes
{
    public enum GameModeId
    {
        Story, Endless, Hardcore, Practice, DailyChallenge, WeeklyChallenge, Speed, Zen, Community
    }

    /// <summary>
    /// Declarative modifiers a mode applies to a run. The BeatmapRunner + Score/Judgment
    /// systems read these instead of hard-coding per-mode behaviour, so adding a mode is data,
    /// not new control flow.
    /// </summary>
    public readonly struct ModeConfig
    {
        public readonly GameModeId Id;
        public readonly bool FailHard;       // Hardcore
        public readonly bool NoScoring;      // Zen
        public readonly bool NoFail;         // Practice / Zen
        public readonly float BpmScale;      // Speed (1.1–1.5), Practice (0.5–1.0)
        public readonly float WindowScale;   // Zen widens, Hardcore keeps tight
        public readonly bool AllowAssists;   // Practice section-loop / preview

        public ModeConfig(GameModeId id, bool failHard, bool noScoring, bool noFail,
                          float bpmScale, float windowScale, bool allowAssists)
        {
            Id = id; FailHard = failHard; NoScoring = noScoring; NoFail = noFail;
            BpmScale = bpmScale; WindowScale = windowScale; AllowAssists = allowAssists;
        }

        public static ModeConfig Story => new(GameModeId.Story, false, false, false, 1f, 1f, false);
        public static ModeConfig Endless => new(GameModeId.Endless, false, false, false, 1f, 1f, false);
        public static ModeConfig Hardcore => new(GameModeId.Hardcore, true, false, false, 1f, 1f, false);
        public static ModeConfig Practice(float bpmScale = 1f) => new(GameModeId.Practice, false, true, true, bpmScale, 1f, true);
        public static ModeConfig Daily => new(GameModeId.DailyChallenge, false, false, false, 1f, 1f, false);
        public static ModeConfig Weekly => new(GameModeId.WeeklyChallenge, false, false, false, 1f, 1f, false);
        public static ModeConfig Speed(float bpmScale = 1.25f) => new(GameModeId.Speed, false, false, false, bpmScale, 1f, false);
        public static ModeConfig Zen => new(GameModeId.Zen, false, true, true, 1f, 1.4f, true);

        /// <summary>Applies the mode's window scaling to a base difficulty window.</summary>
        public TimingWindow ScaleWindow(TimingWindow baseWindow)
            => new(baseWindow.perfectMs * WindowScale,
                   baseWindow.greatMs * WindowScale,
                   baseWindow.goodMs * WindowScale);
    }
}
