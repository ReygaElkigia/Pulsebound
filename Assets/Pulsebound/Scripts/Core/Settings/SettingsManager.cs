using Pulsebound.Core.Audio;
using Pulsebound.Core.Save;
using Pulsebound.Core.Services;
using UnityEngine;
using UnityEngine.Audio;

namespace Pulsebound.Core.Settings
{
    /// <summary>
    /// Applies persisted <see cref="SettingsData"/> to the running game: audio bus volumes,
    /// frame-rate cap, vsync, accessibility toggles, and Conductor calibration offsets.
    /// Editing settings mutates the SaveSystem's data and re-applies immediately.
    /// </summary>
    public sealed class SettingsManager : MonoBehaviour
    {
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private string masterParam = "MasterVol";
        [SerializeField] private string musicParam = "MusicVol";
        [SerializeField] private string sfxParam = "SfxVol";
        [SerializeField] private string uiParam = "UiVol";

        private SaveSystem _save;

        public SettingsData Settings => _save != null ? _save.Data.settings : new SettingsData();

        private void Awake() => Services.Register<SettingsManager>(this);

        private void OnDestroy()
        {
            if (Services.IsRegistered<SettingsManager>() && Services.Get<SettingsManager>() == this)
                Services.Unregister<SettingsManager>();
        }

        private void Start()
        {
            _save = Services.Get<SaveSystem>();
            Apply();
        }

        public void Apply()
        {
            var s = Settings;

            SetMixer(masterParam, s.masterVolume);
            SetMixer(musicParam, s.musicVolume);
            SetMixer(sfxParam, s.sfxVolume);
            SetMixer(uiParam, s.uiVolume);

            Application.targetFrameRate = s.targetFrameRate;
            QualitySettings.vSyncCount = s.vsync ? 1 : 0;

            if (Services.TryGet<Conductor>(out var conductor))
            {
                conductor.AudioOffset = s.audioOffsetSeconds;
                conductor.InputOffset = s.inputOffsetSeconds;
            }
        }

        public void SetAudioOffset(float seconds) { Settings.audioOffsetSeconds = seconds; Apply(); Save(); }
        public void SetInputOffset(float seconds) { Settings.inputOffsetSeconds = seconds; Apply(); Save(); }
        public void SetReducedMotion(bool on) { Settings.reducedMotion = on; Save(); }
        public void SetColorblindSafe(bool on) { Settings.colorblindSafe = on; Save(); }

        private void SetMixer(string param, float linear01)
        {
            if (mixer == null || string.IsNullOrEmpty(param)) return;
            // Convert 0..1 to dB; guard against log(0).
            float db = linear01 <= 0.0001f ? -80f : Mathf.Log10(linear01) * 20f;
            mixer.SetFloat(param, db);
        }

        private void Save() => _save?.Save();
    }
}
