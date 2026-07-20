using System.IO;
using Pulsebound.Core.Services;
using Pulsebound.Data;
using Pulsebound.Gameplay;
using Pulsebound.Steam;
using UnityEngine;

namespace Pulsebound.LevelEditor
{
    /// <summary>
    /// Runtime (in-game) level editor core. This is the headline feature's backbone: it edits
    /// a live <see cref="Beatmap"/>, snaps placements to the beat grid, round-trips to instant
    /// playtest through the SAME <see cref="BeatmapRunner"/> the game uses, validates difficulty,
    /// and imports/exports the portable <c>.pbmap</c> format for Workshop sharing.
    ///
    /// UI wiring (timeline, drag-drop, waveform) lives in the editor UI scene; this class is the
    /// model + operations those views call.
    /// </summary>
    public sealed class LevelEditorController : MonoBehaviour
    {
        [SerializeField] private BeatmapRunner runner;

        [Tooltip("Grid snap denominator: 4 = 1/4 beat, 24 = 1/24 (triplet/expert).")]
        [SerializeField] private int snapDenominator = 4;

        public Beatmap Current { get; private set; }
        private int _nextId = 1;

        public void NewBeatmap(string songId, float bpm)
        {
            Current = ScriptableObject.CreateInstance<Beatmap>();
            Current.songId = songId;
            Current.constantBpm = bpm;
            _nextId = 1;
        }

        /// <summary>Places a node at the snapped beat nearest <paramref name="rawBeat"/>.</summary>
        public NodeDefinition PlaceNode(NodeType type, float rawBeat)
        {
            float snapped = Snap(rawBeat);
            var node = new NodeDefinition { id = _nextId++, type = type, beat = snapped };
            Current.nodes.Add(node);
            Current.nodes.Sort((a, b) => a.beat.CompareTo(b.beat));
            return node;
        }

        public void RemoveNode(int id) => Current.nodes.RemoveAll(n => n.id == id);

        public void AddZone(ZoneType type, float startBeat, float endBeat, float value)
            => Current.zones.Add(new MechanicZone { type = type, startBeat = Snap(startBeat), endBeat = Snap(endBeat), value = value });

        public float Snap(float beat)
        {
            float grid = 1f / Mathf.Max(1, snapDenominator);
            return Mathf.Round(beat / grid) * grid;
        }

        public void SetSnap(int denominator) => snapDenominator = Mathf.Max(1, denominator);

        /// <summary>One-key jump into a live run from a beat cursor (sub-second round-trip).</summary>
        public void Playtest(AudioClip clip, bool hardcore = false)
        {
            if (runner == null || Current == null) return;
            runner.Begin(Current, clip, hardcore);
        }

        public void StopPlaytest() => runner?.Abort();

        public DifficultyValidator.Report Validate() => DifficultyValidator.Validate(Current);

        // --- .pbmap I/O ---

        public void Export(string path) => File.WriteAllText(path, BeatmapSerializer.ToJson(Current));

        public void Import(string path)
        {
            Current = BeatmapSerializer.FromJson(File.ReadAllText(path));
            _nextId = 1;
            foreach (var n in Current.nodes) _nextId = Mathf.Max(_nextId, n.id + 1);
        }

        /// <summary>Publish the current map to Steam Workshop (or the null fallback).</summary>
        public void PublishToWorkshop(string contentFolder)
        {
            var validation = Validate();
            if (!validation.IsPlayable)
            {
                Debug.LogWarning("[Editor] Cannot publish: beatmap failed playability validation.");
                return;
            }

            Directory.CreateDirectory(contentFolder);
            Export(Path.Combine(contentFolder, "chart.pbmap"));

            if (Services.TryGet<ISteamService>(out var steam))
            {
                var item = new WorkshopItem
                {
                    Title = Current.title,
                    Description = $"{Current.difficulty} • {validation.suggestedStars}★",
                    ContentPath = contentFolder
                };
                item.Tags.Add(Current.difficulty.ToString());
                steam.PublishWorkshopItem(item, id => Debug.Log($"[Editor] Published Workshop item {id}"));
            }
        }
    }
}
