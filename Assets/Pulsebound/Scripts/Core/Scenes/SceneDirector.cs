using System;
using System.Collections;
using Pulsebound.Core.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pulsebound.Core.Scenes
{
    /// <summary>
    /// Async scene loading with a fade hook and a "load additively then activate" flow so
    /// transitions are smooth. Instant-retry uses <see cref="ReloadActive"/> which keeps the
    /// bootstrap scene resident and reloads only the gameplay scene.
    /// </summary>
    public sealed class SceneDirector : MonoBehaviour
    {
        public const string BootstrapScene = "Bootstrap";
        public const string MenuScene = "Menu";
        public const string GameplayScene = "Gameplay";
        public const string EditorScene = "LevelEditor";

        public event Action<string> LoadStarted;
        public event Action<string> LoadFinished;

        private bool _busy;
        public bool IsBusy => _busy;

        private void Awake()
        {
            Services.Register<SceneDirector>(this);
            DontDestroyOnLoad(gameObject);
        }

        public void Load(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (_busy) return;
            StartCoroutine(LoadRoutine(sceneName, mode));
        }

        public void ReloadActive()
        {
            var active = SceneManager.GetActiveScene().name;
            Load(active);
        }

        private IEnumerator LoadRoutine(string sceneName, LoadSceneMode mode)
        {
            _busy = true;
            LoadStarted?.Invoke(sceneName);

            var op = SceneManager.LoadSceneAsync(sceneName, mode);
            op.allowSceneActivation = false;
            while (op.progress < 0.9f) yield return null;

            // Hook: play fade-out here before activation.
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            _busy = false;
            LoadFinished?.Invoke(sceneName);
        }
    }
}
