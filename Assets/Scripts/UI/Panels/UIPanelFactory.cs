using UnityEngine;
using System.Threading.Tasks;

namespace GameCore.Core
{
    /// <summary>
    /// Створює панелі на основі зареєстрованих у реєстрі префабів.
    /// </summary>
    public class UIPanelFactory : MonoBehaviour, IService
    {
        [SerializeField] private string panelPrefabsPath = "UI/Panels";
        [SerializeField] private Transform panelRoot; // основний root

        private UIPanelRegistry _registry;

        public void SetRegistry(UIPanelRegistry registry)
        {
            _registry = registry;
        }

        public void SetPanelRoot(Transform root)
        {
            panelRoot = root;
        }

        public async Task Initialize()
        {
            if (_registry == null)
            {
                CoreLogger.LogError("UI", "❗ UIPanelRegistry is not assigned to UIPanelFactory.");
                return;
            }

            if (panelRoot == null)
            {
                var canvas = Object.FindFirstObjectByType<Canvas>();
                panelRoot = canvas != null ? canvas.transform : this.transform;
            }

            var panelPrefabs = Resources.LoadAll<GameObject>(panelPrefabsPath);

            foreach (var prefab in panelPrefabs)
            {
                if (prefab != null)
                {
                    _registry.RegisterPanel(prefab.name, prefab);
                }
            }

            CoreLogger.Log("UI", $"✅ UIPanelFactory initialized with {panelPrefabs.Length} panels.");
            await Task.CompletedTask;
        }

        public UIPanel CreatePanel(string panelName)
        {
            var prefab = _registry.GetPanelPrefab(panelName);
            if (prefab == null)
            {
                CoreLogger.LogWarning("UI", $"⚠️ Panel '{panelName}' not found.");
                return null;
            }

            var instance = Instantiate(prefab, panelRoot);
            instance.name = prefab.name;
            return instance.GetComponent<UIPanel>();
        }
    }
}
