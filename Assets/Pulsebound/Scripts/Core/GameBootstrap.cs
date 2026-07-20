using Pulsebound.Core.Services;
using Pulsebound.Steam;
using UnityEngine;

namespace Pulsebound.Core
{
    /// <summary>
    /// Composition root. Lives in the persistent Bootstrap scene and wires the platform
    /// service before any gameplay scene loads. Every other system self-registers in its own
    /// Awake, so this stays tiny — its whole job is to pick Steam vs. Null and mark the core
    /// object persistent.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Force the Steam-free service even in a Steam build (useful for local dev).")]
        [SerializeField] private bool forceNullSteam;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            ISteamService steam = CreateSteamService();
            Services.Register(steam);
            Debug.Log($"[Bootstrap] Platform service: {steam.GetType().Name} (available={steam.IsAvailable})");
        }

        private ISteamService CreateSteamService()
        {
#if PULSEBOUND_STEAM
            if (!forceNullSteam)
            {
                var svc = new SteamService();
                if (svc.Initialize()) return svc;
            }
#endif
            return new NullSteamService();
        }

        private void Update()
        {
            if (Services.TryGet<ISteamService>(out var steam) && steam.IsAvailable)
                steam.RunCallbacks();
        }
    }
}
