using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Pulsebound.Data
{
    /// <summary>
    /// Metadata + audio references for one song. Audio loads through Addressables so
    /// tracks (and DLC/Workshop content) stream on demand and unload cleanly.
    /// Stems drive the adaptive-audio system (see AudioManager): playing well adds layers.
    /// </summary>
    [CreateAssetMenu(menuName = "Pulsebound/Song", fileName = "NewSong")]
    public sealed class SongDefinition : ScriptableObject
    {
        public string songId = Guid.NewGuid().ToString("N");
        public string title;
        public string artist;

        [Tooltip("Full mixed track (used when stems are unavailable, e.g. custom songs).")]
        public AssetReferenceT<AudioClip> mixedClip;

        [Tooltip("Layered stems, quietest→loudest gating order. Empty = use mixedClip only.")]
        public List<AssetReferenceT<AudioClip>> stems = new();

        public Sprite cover;
        [TextArea] public string credits;

        [Tooltip("Beatmaps available for this song, one per difficulty.")]
        public List<Beatmap> beatmaps = new();

        public bool HasStems => stems != null && stems.Count > 0;
    }
}
