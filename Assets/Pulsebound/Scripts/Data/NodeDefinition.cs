using System;
using UnityEngine;

namespace Pulsebound.Data
{
    /// <summary>
    /// One authored node on the beat grid. Plain serializable data (not a MonoBehaviour)
    /// so a beatmap is pure content — editor-authorable, JSON-portable, and pool-friendly
    /// when instantiated into runtime node views.
    /// </summary>
    [Serializable]
    public struct NodeDefinition
    {
        [Tooltip("Stable id within a beatmap; used to correlate judgments and views.")]
        public int id;

        public NodeType type;

        [Tooltip("Absolute target beat (float; fractional = sub-beat).")]
        public float beat;

        [Tooltip("For Hold nodes: tail beat where the sustain should be released.")]
        public float endBeat;

        [Tooltip("For Echo nodes: beat of the remembered repeat.")]
        public float echoBeat;

        [Tooltip("For DoubleBeat: beat of the second press (usually beat + 1/2 or 1/4).")]
        public float secondBeat;

        [Tooltip("For TeleportGate/SplitPath: index of the linked path segment or branch group.")]
        public int linkIndex;

        [Tooltip("Optional per-node scoring weight override (0 = use type default).")]
        public float scoreOverride;

        public readonly bool RequiresSustain => type == NodeType.Hold;
        public readonly bool RequiresSecondInput => type == NodeType.DoubleBeat;

        /// <summary>Beat at which this node should begin spawning its telegraph.</summary>
        public readonly float FirstActiveBeat => beat;

        /// <summary>Last beat this node is relevant for judgment (tail for holds, echo for echoes).</summary>
        public readonly float LastRelevantBeat => type switch
        {
            NodeType.Hold => Mathf.Max(beat, endBeat),
            NodeType.Echo => Mathf.Max(beat, echoBeat),
            NodeType.DoubleBeat => Mathf.Max(beat, secondBeat),
            _ => beat
        };
    }

    /// <summary>A ranged section mechanic authored over [startBeat, endBeat].</summary>
    [Serializable]
    public struct MechanicZone
    {
        public ZoneType type;
        public float startBeat;
        public float endBeat;

        [Tooltip("Speed multiplier for SpeedModifier; rotation degrees for GravityShift; ignored otherwise.")]
        public float value;

        public readonly bool Contains(float beat) => beat >= startBeat && beat <= endBeat;
    }
}
