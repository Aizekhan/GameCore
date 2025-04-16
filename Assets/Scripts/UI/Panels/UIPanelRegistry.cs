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
            _panelMap = new Dictionary<string, GameObject>();
            var panelPrefabs = Resources.LoadAll<GameObject>("UI/Panels");

            foreach (var prefab in panelPrefabs)
            {
                if (prefab != null)
                {
                    RegisterPanel(prefab.name, prefab); // використовуємо існуючу логіку
                }
            }

            await Task.CompletedTask;
        }

        public void RegisterPanel(string name, GameObject prefab)
        {
            if (!_panelMap.ContainsKey(name))
            {
                _panelMap[name] = prefab;
                CoreLogger.Log("UI", $"🧩 Registered (factory): {name}");
            }
            else
            {
                CoreLogger.LogWarning("UI", $"⚠️ Panel '{name}' already registered.");
            }
        }

        public GameObject GetPanelPrefab(string panelName)
        {
            if (_panelMap.TryGetValue(panelName, out var prefab))
                return prefab;

            CoreLogger.LogWarning("UIPanel", $"❌ Panel with name '{panelName}' not found.");
            return null;
        }
    }
}
