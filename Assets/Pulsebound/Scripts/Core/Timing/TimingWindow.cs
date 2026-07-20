using System;
using UnityEngine;

namespace Pulsebound.Core.Timing
{
    /// <summary>
    /// Per-difficulty timing windows in milliseconds. Symmetric around the target beat.
    /// These are the numbers that make Easy forgiving and Pulsemaster brutal; nothing
    /// else about the beatmap changes difficulty at this layer.
    /// </summary>
    [Serializable]
    public struct TimingWindow
    {
        [Tooltip("± ms counted as Perfect.")] public float perfectMs;
        [Tooltip("± ms counted as Great (outside Perfect).")] public float greatMs;
        [Tooltip("± ms counted as Good (outside Great).")] public float goodMs;

        public TimingWindow(float perfectMs, float greatMs, float goodMs)
        {
            this.perfectMs = perfectMs;
            this.greatMs = greatMs;
            this.goodMs = goodMs;
        }

        /// <summary>Largest timing error (ms) that still registers as a hit at all.</summary>
        public float MissThresholdMs => goodMs;

        /// <summary>Classify a signed timing delta (ms). Sign is ignored for scoring; magnitude matters.</summary>
        public readonly Judgment Classify(float deltaMs)
        {
            float m = Mathf.Abs(deltaMs);
            if (m <= perfectMs) return Judgment.Perfect;
            if (m <= greatMs) return Judgment.Great;
            if (m <= goodMs) return Judgment.Good;
            return Judgment.Miss;
        }

        // Canonical defaults per difficulty (see GDD §7).
        public static TimingWindow Easy => new(55f, 90f, 130f);
        public static TimingWindow Normal => new(45f, 80f, 120f);
        public static TimingWindow Hard => new(35f, 70f, 110f);
        public static TimingWindow Expert => new(28f, 58f, 95f);
        public static TimingWindow Pulsemaster => new(22f, 48f, 82f);

        public static TimingWindow ForDifficulty(GameDifficulty d) => d switch
        {
            GameDifficulty.Easy => Easy,
            GameDifficulty.Normal => Normal,
            GameDifficulty.Hard => Hard,
            GameDifficulty.Expert => Expert,
            GameDifficulty.Pulsemaster => Pulsemaster,
            _ => Normal
        };
    }

    public enum GameDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Expert = 3,
        Pulsemaster = 4
    }
}
