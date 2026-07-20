using System;
using System.IO;
using Pulsebound.Core.Services;
using UnityEngine;

namespace Pulsebound.Core.Save
{
    /// <summary>
    /// Versioned JSON save with atomic write + backup. Writes go to a temp file then
    /// atomically replace the live file, so a crash mid-write can never corrupt the save.
    /// A migration hook upgrades old versions on load.
    /// </summary>
    public sealed class SaveSystem : MonoBehaviour
    {
        [SerializeField] private string fileName = "pulsebound.save.json";

        private string Path => System.IO.Path.Combine(Application.persistentDataPath, fileName);
        private string TempPath => Path + ".tmp";
        private string BackupPath => Path + ".bak";

        public SaveData Data { get; private set; } = new();
        public event Action<SaveData> Saved;
        public event Action<SaveData> Loaded;

        private void Awake()
        {
            Services.Register<SaveSystem>(this);
            Load();
        }

        private void OnDestroy()
        {
            if (Services.IsRegistered<SaveSystem>() && Services.Get<SaveSystem>() == this)
                Services.Unregister<SaveSystem>();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(Path))
                {
                    string json = File.ReadAllText(Path);
                    Data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
                }
                else if (File.Exists(BackupPath))
                {
                    Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(BackupPath)) ?? new SaveData();
                }
                else
                {
                    Data = new SaveData();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Load failed ({e.Message}); starting fresh.");
                Data = new SaveData();
            }

            Migrate(Data);
            Loaded?.Invoke(Data);
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(Data, prettyPrint: true);
                File.WriteAllText(TempPath, json);

                if (File.Exists(Path))
                {
                    // Keep a backup, then atomically swap temp -> live.
                    if (File.Exists(BackupPath)) File.Delete(BackupPath);
                    File.Replace(TempPath, Path, BackupPath);
                }
                else
                {
                    File.Move(TempPath, Path);
                }
                Saved?.Invoke(Data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
            }
        }

        /// <summary>Upgrade older save versions in place. Add cases as the schema evolves.</summary>
        private static void Migrate(SaveData data)
        {
            if (data.version < SaveData.CurrentVersion)
            {
                // switch (data.version) { case 0: ... ; goto case 1; ... }
                data.version = SaveData.CurrentVersion;
            }
        }

        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() => Save();
    }
}
