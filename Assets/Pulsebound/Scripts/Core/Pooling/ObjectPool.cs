using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pulsebound.Core.Pooling
{
    /// <summary>
    /// Minimal generic pool for Components. Node views, judgment popups and particle
    /// bursts are pooled so a run produces zero steady-state GC allocations.
    /// </summary>
    public sealed class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _idle = new();
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;

        public int CountIdle => _idle.Count;
        public int CountActive { get; private set; }

        public ObjectPool(T prefab, Transform parent = null, int prewarm = 0,
                          Action<T> onGet = null, Action<T> onRelease = null)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _parent = parent;
            _onGet = onGet;
            _onRelease = onRelease;
            for (int i = 0; i < prewarm; i++)
            {
                var inst = CreateNew();
                inst.gameObject.SetActive(false);
                _idle.Push(inst);
            }
        }

        public T Get()
        {
            T inst = _idle.Count > 0 ? _idle.Pop() : CreateNew();
            inst.gameObject.SetActive(true);
            CountActive++;
            _onGet?.Invoke(inst);
            return inst;
        }

        public void Release(T inst)
        {
            if (inst == null) return;
            _onRelease?.Invoke(inst);
            inst.gameObject.SetActive(false);
            _idle.Push(inst);
            CountActive = Mathf.Max(0, CountActive - 1);
        }

        private T CreateNew()
        {
            var inst = UnityEngine.Object.Instantiate(_prefab, _parent);
            return inst;
        }
    }
}
