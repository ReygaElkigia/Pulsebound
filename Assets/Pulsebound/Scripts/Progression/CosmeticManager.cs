using System.Collections.Generic;
using Pulsebound.Core.Save;
using Pulsebound.Core.Services;
using Pulsebound.Data;
using UnityEngine;

namespace Pulsebound.Progression
{
    /// <summary>
    /// Owns cosmetic ownership + equipping. Grants are cosmetic-only; this manager has no
    /// path to modify any gameplay value. Accessibility cosmetics are treated as always owned.
    /// </summary>
    public sealed class CosmeticManager : MonoBehaviour
    {
        [SerializeField] private List<CosmeticDefinition> catalog = new();

        private SaveSystem _save;
        private readonly Dictionary<string, CosmeticDefinition> _byId = new();

        private void Awake()
        {
            Services.Register<CosmeticManager>(this);
            foreach (var c in catalog)
                if (c != null && !string.IsNullOrEmpty(c.cosmeticId))
                    _byId[c.cosmeticId] = c;
        }

        private void OnDestroy()
        {
            if (Services.IsRegistered<CosmeticManager>() && Services.Get<CosmeticManager>() == this)
                Services.Unregister<CosmeticManager>();
        }

        private void Start() => _save = Services.Get<SaveSystem>();

        public bool IsUnlocked(string cosmeticId)
        {
            if (_byId.TryGetValue(cosmeticId, out var c) && c.isAccessibility) return true;
            return _save.Data.unlockedCosmetics.Contains(cosmeticId);
        }

        public bool Unlock(string cosmeticId)
        {
            if (!_byId.ContainsKey(cosmeticId) || IsUnlocked(cosmeticId)) return false;
            _save.Data.unlockedCosmetics.Add(cosmeticId);
            _save.Save();
            return true;
        }

        public bool Equip(string cosmeticId)
        {
            if (!IsUnlocked(cosmeticId) || !_byId.TryGetValue(cosmeticId, out var c)) return false;

            // One equipped item per category.
            var equipped = _save.Data.equippedCosmetics;
            equipped.RemoveAll(id => _byId.TryGetValue(id, out var e) && e.category == c.category);
            equipped.Add(cosmeticId);
            _save.Save();
            return true;
        }

        public string GetEquipped(CosmeticCategory category)
        {
            foreach (var id in _save.Data.equippedCosmetics)
                if (_byId.TryGetValue(id, out var c) && c.category == category) return id;
            return null;
        }

        /// <summary>Re-evaluate resonance/grade-based unlocks after a run or level-up.</summary>
        public void EvaluateUnlocks(int resonanceLevel)
        {
            foreach (var c in catalog)
            {
                if (c == null || IsUnlocked(c.cosmeticId)) continue;
                if (c.source == UnlockSource.ResonanceLevel && resonanceLevel >= c.unlockThreshold)
                    Unlock(c.cosmeticId);
                else if (c.source == UnlockSource.AlwaysAvailable)
                    Unlock(c.cosmeticId);
            }
        }
    }
}
