using Pulsebound.Core.Audio;
using Pulsebound.Core.Events;
using Pulsebound.Core.Services;
using Pulsebound.Data;
using UnityEngine;

namespace Pulsebound.Gameplay.Nodes
{
    /// <summary>
    /// The pooled visual for a single node. Purely presentational: it reads its target beat
    /// from the Conductor to animate its telegraph (the ring "closing on the beat") and
    /// listens for its own <see cref="GameEvents.NodeJudged"/> to play a hit/miss reaction,
    /// then returns itself to the pool.
    ///
    /// One component covers every node type by switching telegraph behaviour, so the pool
    /// stays homogeneous and the editor spawns them uniformly.
    /// </summary>
    public sealed class NodeView : MonoBehaviour
    {
        [SerializeField] private Transform ring;      // scales down to the beat
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private float approachBeats = 2f;
        [SerializeField] private float maxRingScale = 3f;

        private Conductor _conductor;
        private NodeDefinition _def;
        private bool _resolved;
        private System.Action<NodeView> _returnToPool;

        public int NodeId => _def.id;

        public void Init(in NodeDefinition def, System.Action<NodeView> returnToPool)
        {
            _def = def;
            _resolved = false;
            _returnToPool = returnToPool;
            _conductor ??= Services.Get<Conductor>();
            ApplyTypeStyle(def.type);
            EventBus.Subscribe<GameEvents.NodeJudged>(OnJudged);
        }

        private void OnDisable() => EventBus.Unsubscribe<GameEvents.NodeJudged>(OnJudged);

        private void ApplyTypeStyle(NodeType type)
        {
            if (body == null) return;
            // Distinct silhouettes/tints per mechanic keep the read instantaneous.
            body.color = type switch
            {
                NodeType.Standard => Color.white,
                NodeType.Hold => new Color(0.5f, 0.9f, 1f),
                NodeType.DoubleBeat => new Color(1f, 0.85f, 0.4f),
                NodeType.Echo => new Color(0.7f, 0.7f, 0.7f),
                NodeType.ReverseRhythm => new Color(1f, 0.4f, 0.7f),
                NodeType.SyncChainLink => new Color(0.6f, 1f, 0.7f),
                NodeType.SilentDownbeat => new Color(0.8f, 0.8f, 1f),
                NodeType.Resonance => new Color(1f, 0.7f, 1f),
                _ => Color.white
            };
        }

        private void Update()
        {
            if (_conductor == null || ring == null) return;
            float beat = (float)_conductor.SongBeat;
            float beatsUntil = _def.beat - beat;

            // Ring closes from maxRingScale toward 1 as the beat approaches (Reverse opens outward).
            float t = Mathf.Clamp01(beatsUntil / approachBeats);
            float scale = _def.type == NodeType.ReverseRhythm
                ? Mathf.Lerp(1f, maxRingScale, 1f - t)   // opens outward
                : Mathf.Lerp(1f, maxRingScale, t);        // closes inward
            ring.localScale = Vector3.one * scale;
        }

        private void OnJudged(GameEvents.NodeJudged evt)
        {
            if (_resolved || evt.NodeId != _def.id) return;
            _resolved = true;
            // A production build spawns a pooled hit-burst here tinted by judgment.
            _returnToPool?.Invoke(this);
        }
    }
}
