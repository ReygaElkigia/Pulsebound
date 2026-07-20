namespace Pulsebound.Data
{
    /// <summary>
    /// Every original Pulsebound mechanic that lives on the beat grid as a node.
    /// Ranged mechanics (Gravity Shift, Speed, Invisible, Mirror, Tempo Ramp) are
    /// authored as <see cref="MechanicZone"/>s rather than point nodes.
    /// </summary>
    public enum NodeType
    {
        Standard = 0,        // one press on the beat
        Hold = 1,            // press + sustain, release on tail beat
        DoubleBeat = 2,      // two presses: beat + immediate sub-beat
        Echo = 3,            // hit, then repeat the same rhythm from memory
        ReverseRhythm = 4,   // window scoring inverts (anti-anticipation)
        TeleportGate = 5,    // Pulse jumps to a linked path segment on hit
        SplitPath = 6,       // press timing selects a branch (early/late)
        SyncChainLink = 7,   // part of an all-Perfect-or-break chain
        Resonance = 8,       // charges a bar, banks an auto-clear on the next node
        SilentDownbeat = 9   // node on a rest; audio ducks for one beat
    }

    /// <summary>Ranged section mechanics authored over a beat interval.</summary>
    public enum ZoneType
    {
        GravityShift = 0,    // rotates "down"; camera + momentum reorient
        SpeedModifier = 1,   // scales Pulse travel speed over the section
        Invisible = 2,       // fades node telegraphs; bonus multiplier
        Mirror = 3,          // horizontally mirrors path + read
        TempoRamp = 4        // BPM interpolates (handled by BpmMap ramp anchors)
    }
}
