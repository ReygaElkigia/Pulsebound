using System;
using Pulsebound.Core.Timing;
using UnityEngine;

namespace Pulsebound.Data
{
    /// <summary>
    /// Converts a <see cref="Beatmap"/> to/from the portable <c>.pbmap</c> JSON format used
    /// by the level editor, custom songs, and Steam Workshop transport. Kept separate from the
    /// ScriptableObject so shipped content and community content share one wire format.
    /// </summary>
    public static class BeatmapSerializer
    {
        [Serializable]
        private sealed class Dto
        {
            public int schema = 1;
            public string beatmapId;
            public string songId;
            public string title;
            public string author;
            public int difficulty;
            public int starRating;
            public float constantBpm;
            public TempoAnchor[] tempoAnchors;
            public float leadInSeconds;
            public NodeDefinition[] nodes;
            public MechanicZone[] zones;
            public string backgroundThemeId;
            public string accentDuoId;
            public float reactiveIntensity;
        }

        public static string ToJson(Beatmap map)
        {
            var dto = new Dto
            {
                beatmapId = map.beatmapId,
                songId = map.songId,
                title = map.title,
                author = map.author,
                difficulty = (int)map.difficulty,
                starRating = map.starRating,
                constantBpm = map.constantBpm,
                tempoAnchors = map.tempoAnchors?.ToArray() ?? Array.Empty<TempoAnchor>(),
                leadInSeconds = map.leadInSeconds,
                nodes = map.nodes?.ToArray() ?? Array.Empty<NodeDefinition>(),
                zones = map.zones?.ToArray() ?? Array.Empty<MechanicZone>(),
                backgroundThemeId = map.backgroundThemeId,
                accentDuoId = map.accentDuoId,
                reactiveIntensity = map.reactiveIntensity
            };
            return JsonUtility.ToJson(dto, prettyPrint: true);
        }

        public static Beatmap FromJson(string json)
        {
            var dto = JsonUtility.FromJson<Dto>(json);
            if (dto == null) throw new ArgumentException("Invalid .pbmap JSON.");

            var map = ScriptableObject.CreateInstance<Beatmap>();
            map.beatmapId = dto.beatmapId;
            map.songId = dto.songId;
            map.title = dto.title;
            map.author = dto.author;
            map.difficulty = (GameDifficulty)dto.difficulty;
            map.starRating = Mathf.Clamp(dto.starRating, 1, 10);
            map.constantBpm = Mathf.Max(1f, dto.constantBpm);
            map.tempoAnchors = new System.Collections.Generic.List<TempoAnchor>(dto.tempoAnchors ?? Array.Empty<TempoAnchor>());
            map.leadInSeconds = dto.leadInSeconds;
            map.nodes = new System.Collections.Generic.List<NodeDefinition>(dto.nodes ?? Array.Empty<NodeDefinition>());
            map.zones = new System.Collections.Generic.List<MechanicZone>(dto.zones ?? Array.Empty<MechanicZone>());
            map.backgroundThemeId = dto.backgroundThemeId;
            map.accentDuoId = dto.accentDuoId;
            map.reactiveIntensity = dto.reactiveIntensity;
            return map;
        }
    }
}
