namespace Pulsebound.Core.Timing
{
    /// <summary>Accuracy tiers for a single node input, best to worst.</summary>
    public enum Judgment
    {
        Perfect = 0,
        Great = 1,
        Good = 2,
        Miss = 3
    }

    public static class JudgmentExtensions
    {
        /// <summary>Fraction of a node's base value awarded for this judgment.</summary>
        public static float ScoreWeight(this Judgment j) => j switch
        {
            Judgment.Perfect => 1.00f,
            Judgment.Great => 0.75f,
            Judgment.Good => 0.40f,
            _ => 0f
        };

        /// <summary>True if the input counted as a hit (kept the combo alive).</summary>
        public static bool IsHit(this Judgment j) => j != Judgment.Miss;

        /// <summary>Only Perfects grow the multiplier and advance Sync chains.</summary>
        public static bool GrowsMultiplier(this Judgment j) => j == Judgment.Perfect;
    }
}
