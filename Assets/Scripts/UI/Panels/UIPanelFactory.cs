// ✅ Оновлений UIPanelFactory.cs з кешуванням створених інстанцій панелей
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GameCore.Core.Interfaces;
using GameCore.Core.EventSystem;

namespace GameCore.Core
{
    public class UIPanelFactory : MonoBehaviour, IService, IInitializable
    {
        [SerializeField] private Transform panelRoot;
        private UIPanelRegistry _registry;
        private Dictionary<string, UIPanel> _createdPanels = new();

        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 60;

        public void SetRegistry(UIPanelRegistry registry)
        {
            _registry = registry;
        }

        public void SetPanelRoot(Transform root)
        {
            panelRoot = root;
        }

        public UIPanel CreatePanel(string panelName)
        {
            if (_createdPanels.TryGetValue(panelName, out var existingPanel) && existingPanel != null)
            {
                CoreLogger.Log("UI", $"♻️ Reusing existing panel: {panelName}");
                return existingPanel;
            }

            var prefab = _registry.GetPanelPrefab(panelName);
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>($"UI/Panels/{panelName}");
                if (prefab != null)
                {
                    _registry.RegisterPanel(panelName, prefab);
                    CoreLogger.Log("UI", $"🔄 Lazy-registered panel: {panelName}");
                }
                else
                {
                    CoreLogger.LogWarning("UI", $"⚠️ Panel '{panelName}' not found in Resources.");
                    return null;
                }
            }

            var instance = Instantiate(prefab, panelRoot);
            instance.name = prefab.name;

            var panel = instance.GetComponent<UIPanel>();
            _createdPanels[panelName] = panel;

            return panel;
        }

        public async Task Initialize()
        {
            if (_registry == null)
            {
                CoreLogger.LogError("UI", "❗ UIPanelRegistry is not set for UIPanelFactory.");
                return;
            }

            if (panelRoot == null)
            {
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    CoreLogger.LogError("UI", "❗ Canvas not found in scene. Please set panel root manually.");
                    return;
                }
                panelRoot = canvas.transform;
            }

            CoreLogger.Log("UI", "✅ UIPanelFactory initialized");
            IsInitialized = true;
            await Task.CompletedTask;
        }
    }
}
