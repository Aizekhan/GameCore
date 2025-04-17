using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GameCore.Core
{
    /// <summary>
    /// Зберігає мапу імен панелей → відповідні префаби.
    /// </summary>
    public class UIPanelRegistry : MonoBehaviour, IService, IInitializable
    {
        [SerializeField] private List<GameObject> panelPrefabs;

        private Dictionary<string, GameObject> _panelMap;
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 65; // встановіть потрібний пріоритет

        public async Task Initialize()
        {
            if (IsInitialized)
            {
                return; // Prevent multiple initializations
            }

            _panelMap = new Dictionary<string, GameObject>();
            var panelPrefabs = Resources.LoadAll<GameObject>("UI/Panels");

            foreach (var prefab in panelPrefabs)
            {
                if (prefab != null)
                {
                    RegisterPanel(prefab.name, prefab);
                }
            }

            IsInitialized = true;
            CoreLogger.Log("UI", "✅ UIPanelRegistry initialized");
            await Task.CompletedTask;
        }

        public void RegisterPanel(string name, GameObject prefab)
        {
            if (prefab == null)
            {
                CoreLogger.LogWarning("UI", $"⚠️ Attempt to register null prefab for panel '{name}'");
                return;
            }

            if (!_panelMap.ContainsKey(name))
            {
                _panelMap[name] = prefab;
                CoreLogger.Log("UI", $"🧩 Registered panel: {name}");
            }
            else
            {
                CoreLogger.LogWarning("UI", $"⚠️ Panel '{name}' already registered. Ignoring duplicate registration.");
            }
        }

        public GameObject GetPanelPrefab(string panelName)
        {
            if (_panelMap == null)
            {
                CoreLogger.LogError("UI", "Panel map not initialized");
                return null;
            }

            if (_panelMap.TryGetValue(panelName, out var prefab))
                return prefab;

            // Спробуємо динамічно завантажити
            prefab = Resources.Load<GameObject>($"UI/Panels/{panelName}");
            if (prefab != null)
            {
                RegisterPanel(panelName, prefab);
                return prefab;
            }

            CoreLogger.LogWarning("UI", $"❌ Panel with name '{panelName}' not found.");
            return null;
        }

        public bool HasPanel(string panelName)
        {
            return _panelMap != null && _panelMap.ContainsKey(panelName);
        }

        public void ClearRegistry()
        {
            if (_panelMap != null)
            {
                _panelMap.Clear();
                CoreLogger.Log("UI", "Panel registry cleared");
            }
        }
    }
}