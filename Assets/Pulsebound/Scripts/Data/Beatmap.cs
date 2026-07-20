using System;
using System.Collections.Generic;
using Pulsebound.Core.Timing;
using UnityEngine;

namespace Pulsebound.Data
{
    /// <summary>
    /// A playable chart for one song at one difficulty. Authored either as a ScriptableObject
    /// (shipped content) or deserialized from a .pbmap JSON (editor / Workshop / custom songs).
    /// </summary>
    [CreateAssetMenu(menuName = "Pulsebound/Beatmap", fileName = "NewBeatmap")]
    public sealed class Beatmap : ScriptableObject
    {
        [Header("Identity")]
        public string beatmapId = Guid.NewGuid().ToString("N");
        public string songId;
        public string title;
        public string author;

        [Header("Difficulty")]
        public GameDifficulty difficulty = GameDifficulty.Normal;
        [Range(1, 10)] public int starRating = 3;

        [Header("Tempo")]
        [Tooltip("Constant BPM used when tempoAnchors is empty.")]
        public float constantBpm = 120f;
        [Tooltip("Optional multi-section / ramp tempo anchors. Overrides constantBpm when non-empty.")]
        public List<TempoAnchor> tempoAnchors = new();

        [Tooltip("Extra seconds of silent lead-in before beat 0.")]
        public float leadInSeconds = 2f;

        [Header("Content")]
        public List<NodeDefinition> nodes = new();
        public List<MechanicZone> zones = new();

        [Header("Presentation")]
        public string backgroundThemeId = "deep-void";
        public string accentDuoId = "cyan-magenta";
        [Range(0f, 1f)] public float reactiveIntensity = 0.7f;

        /// <summary>Builds the tempo map for this chart (anchors if present, else constant BPM).</summary>
        public BpmMap BuildBpmMap()
        {
            return tempoAnchors != null && tempoAnchors.Count > 0
                ? new BpmMap(tempoAnchors)
                : new BpmMap(constantBpm);
        }

        /// <summary>Beat position of the last relevant node (used for run-end detection).</summary>
        public float LastBeat()
        {
            float last = 0f;
            foreach (var n in nodes)
                if (n.LastRelevantBeat > last) last = n.LastRelevantBeat;
            return last;
        }

        private void OnValidate()
        {
            constantBpm = Mathf.Max(1f, constantBpm);
            // Keep nodes sorted by beat so the runner can consume them as a forward stream.
            nodes?.Sort((a, b) => a.beat.CompareTo(b.beat));
        }
    }
}
