using UnityEngine;

namespace Pulsebound.Data
{
    public enum CosmeticCategory
    {
        PulseSkin, EnergyTrail, HitEffect, BackgroundTheme, UiTheme, ProfileIcon, Title, AccentDuo
    }

    public enum UnlockSource
    {
        ResonanceLevel, GradeGoal, Achievement, MasteryToken, Challenge, AlwaysAvailable
    }

    /// <summary>
    /// A single cosmetic reward. Cosmetics NEVER affect timing, hitboxes, or difficulty —
    /// this is enforced by design: nothing here exposes a gameplay parameter.
    /// </summary>
    [CreateAssetMenu(menuName = "Pulsebound/Cosmetic", fileName = "NewCosmetic")]
    public sealed class CosmeticDefinition : ScriptableObject
    {
        public string cosmeticId;
        public string displayName;
        [TextArea] public string description;
        public CosmeticCategory category;

        public UnlockSource source;
        [Tooltip("Resonance level, star goal, or token cost depending on source.")]
        public int unlockThreshold;
        [Tooltip("Beatmap/achievement id this unlock is tied to, when applicable.")]
        public string unlockRefId;

        public Sprite icon;

        [Tooltip("Accessibility cosmetics (High-Contrast UI, colorblind palettes) are never gated.")]
        public bool isAccessibility;
    }
}
