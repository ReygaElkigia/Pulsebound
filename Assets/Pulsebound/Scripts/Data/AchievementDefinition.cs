using UnityEngine;

namespace Pulsebound.Data
{
    public enum AchievementCategory { Skill, Breadth, Endurance, Mastery, Creation, Hidden }

    /// <summary>
    /// An achievement definition. Progress is tracked locally and mirrored to Steam Stats.
    /// May grant a cosmetic and/or a title on completion.
    /// </summary>
    [CreateAssetMenu(menuName = "Pulsebound/Achievement", fileName = "NewAchievement")]
    public sealed class AchievementDefinition : ScriptableObject
    {
        public string achievementId;
        [Tooltip("Must match the Steamworks achievement API name for mirroring.")]
        public string steamApiName;

        public string displayName;
        [TextArea] public string description;
        public AchievementCategory category;
        public bool hidden;

        [Tooltip("Target value for progress-based achievements (e.g. 10000 lifetime Perfects). 1 = boolean.")]
        public long targetValue = 1;

        [Tooltip("Optional cosmetic granted on completion.")]
        public CosmeticDefinition rewardCosmetic;
        [Tooltip("Optional title granted on completion.")]
        public string rewardTitle;

        public Sprite icon;
    }
}
