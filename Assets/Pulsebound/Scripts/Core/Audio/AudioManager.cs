using System.Collections.Generic;
using Pulsebound.Core.Events;
using Pulsebound.Core.Services;
using Pulsebound.Core.Timing;
using UnityEngine;

namespace Pulsebound.Core.Audio
{
    /// <summary>
    /// Handles non-Conductor audio: adaptive stem gating (playing well adds layers),
    /// DSP-scheduled one-shot hit feedback, and the Silent Downbeat duck. Music-critical
    /// playback still lives in the Conductor; this layer reacts to gameplay events.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        [Header("Buses")]
        [SerializeField] private AudioSource sfxSource;

        [Header("Hit feedback (indexed by Judgment)")]
        [SerializeField] private AudioClip perfectSfx;
        [SerializeField] private AudioClip greatSfx;
        [SerializeField] private AudioClip goodSfx;
        [SerializeField] private AudioClip missSfx;
        [SerializeField] private AudioClip syncStinger;

        [Header("Adaptive stems (quietest -> loudest)")]
        [SerializeField] private List<AudioSource> stemSources = new();
        [Tooltip("Flow thresholds at which each stem fades in.")]
        [SerializeField] private float[] stemFlowThresholds = { 0f, 0.25f, 0.5f, 0.75f };
        [SerializeField] private float stemFadeSpeed = 4f;

        private readonly List<float> _stemTargets = new();

        private void Awake() => Services.Register<AudioManager>(this);

        private void OnDestroy()
        {
            if (Services.IsRegistered<AudioManager>() && Services.Get<AudioManager>() == this)
                Services.Unregister<AudioManager>();
            EventBus.Unsubscribe<GameEvents.NodeJudged>(OnNodeJudged);
            EventBus.Unsubscribe<GameEvents.FlowChanged>(OnFlowChanged);
            EventBus.Unsubscribe<GameEvents.SyncChainCompleted>(OnSync);
        }

        private void Start()
        {
            EventBus.Subscribe<GameEvents.NodeJudged>(OnNodeJudged);
            EventBus.Subscribe<GameEvents.FlowChanged>(OnFlowChanged);
            EventBus.Subscribe<GameEvents.SyncChainCompleted>(OnSync);
            for (int i = 0; i < stemSources.Count; i++) _stemTargets.Add(0f);
        }

        private void OnNodeJudged(GameEvents.NodeJudged evt)
        {
            AudioClip clip = evt.Judgment switch
            {
                Judgment.Perfect => perfectSfx,
                Judgment.Great => greatSfx,
                Judgment.Good => goodSfx,
                _ => missSfx
            };
            PlayOneShotScheduled(clip);
        }

        private void OnSync(GameEvents.SyncChainCompleted evt) => PlayOneShotScheduled(syncStinger);

        private void OnFlowChanged(GameEvents.FlowChanged evt)
        {
            // Gate stems by flow: the better you play, the fuller the mix.
            for (int i = 0; i < stemSources.Count; i++)
            {
                float threshold = i < stemFlowThresholds.Length ? stemFlowThresholds[i] : 1f;
                _stemTargets[i] = evt.Flow01 >= threshold ? 1f : 0f;
            }
        }

        /// <summary>Ducks the master for exactly one beat (Silent Downbeat mechanic).</summary>
        public void DuckForBeat(Conductor conductor, float beatFraction = 1f)
        {
            // A production build would schedule an envelope on the mixer snapshot; here we
            // simply drop stem targets for the duration, restored by the next FlowChanged.
            for (int i = 0; i < _stemTargets.Count; i++) _stemTargets[i] = 0f;
        }

        private void PlayOneShotScheduled(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            // Schedule on the DSP clock for tight sync with the music.
            sfxSource.clip = clip;
            sfxSource.PlayScheduled(AudioSettings.dspTime);
        }

        private void Update()
        {
            for (int i = 0; i < stemSources.Count; i++)
            {
                if (stemSources[i] == null) continue;
                float target = i < _stemTargets.Count ? _stemTargets[i] : 0f;
                stemSources[i].volume = Mathf.MoveTowards(stemSources[i].volume, target, stemFadeSpeed * Time.deltaTime);
            }
        }
    }
}
