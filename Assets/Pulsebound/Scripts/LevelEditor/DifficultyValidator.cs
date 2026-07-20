using System.Collections.Generic;
using Pulsebound.Core.Timing;
using Pulsebound.Data;
using UnityEngine;

namespace Pulsebound.LevelEditor
{
    /// <summary>
    /// The editor's difficulty validation: computes a star rating from the same weighted
    /// axes the design doc describes (§7/§16) and flags un-hittable spacing and off-grid
    /// nodes. Runs identically in the live editor and in a headless CI check for shipped maps.
    /// </summary>
    public static class DifficultyValidator
    {
        public struct Report
        {
            public int suggestedStars;      // 1..10
            public float peakNodesPerBar;
            public float avgNodesPerBar;
            public int mechanicVariety;
            public List<string> warnings;
            public bool IsPlayable;         // false if any hard-fail warning present
        }

        public static Report Validate(Beatmap map)
        {
            var report = new Report { warnings = new List<string>(), IsPlayable = true };
            if (map == null || map.nodes == null || map.nodes.Count == 0)
            {
                report.warnings.Add("Beatmap has no nodes.");
                report.IsPlayable = false;
                return report;
            }

            var bpm = map.BuildBpmMap();
            var window = TimingWindow.ForDifficulty(map.difficulty);

            // Minimum spacing: two nodes closer than the Perfect window are physically un-hittable
            // as distinct presses.
            float minGapBeats = 0.05f;
            var byBeat = new List<NodeDefinition>(map.nodes);
            byBeat.Sort((a, b) => a.beat.CompareTo(b.beat));

            float lastBeat = byBeat[0].beat;
            float firstBeat = byBeat[0].beat;
            var mechanics = new HashSet<NodeType>();

            for (int i = 0; i < byBeat.Count; i++)
            {
                mechanics.Add(byBeat[i].type);
                if (i > 0)
                {
                    float gap = byBeat[i].beat - byBeat[i - 1].beat;
                    float gapMs = gap * (float)bpm.BeatToSeconds(1) * 1000f; // approx at song tempo
                    if (gap < minGapBeats)
                    {
                        report.warnings.Add($"Nodes {byBeat[i - 1].id}/{byBeat[i].id} are {gap:F3} beats apart — un-hittable.");
                        report.IsPlayable = false;
                    }
                }
                lastBeat = byBeat[i].beat;
            }

            // Off-grid check: warn on nodes far from any 1/24 subdivision.
            foreach (var n in byBeat)
            {
                float sub = n.beat * 24f;
                if (Mathf.Abs(sub - Mathf.Round(sub)) > 0.15f)
                    report.warnings.Add($"Node {n.id} at beat {n.beat:F3} is off the 1/24 grid.");
            }

            float bars = Mathf.Max(1f, (lastBeat - firstBeat) / 4f);
            report.avgNodesPerBar = byBeat.Count / bars;
            report.peakNodesPerBar = ComputePeakDensity(byBeat);
            report.mechanicVariety = mechanics.Count;

            // Weighted star formula: density + window tightness + mechanic variety.
            float densityScore = Mathf.Clamp01(report.peakNodesPerBar / 12f);
            float windowScore = 1f - Mathf.Clamp01(window.perfectMs / 55f);
            float varietyScore = Mathf.Clamp01(mechanics.Count / 10f);
            float raw = densityScore * 0.5f + windowScore * 0.3f + varietyScore * 0.2f;
            report.suggestedStars = Mathf.Clamp(Mathf.RoundToInt(raw * 10f), 1, 10);

            return report;
        }

        private static float ComputePeakDensity(List<NodeDefinition> nodes)
        {
            // Slide a 4-beat (one bar) window and find the max node count.
            int peak = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                int count = 0;
                for (int j = i; j < nodes.Count && nodes[j].beat - nodes[i].beat < 4f; j++) count++;
                peak = Mathf.Max(peak, count);
            }
            return peak;
        }
    }
}
