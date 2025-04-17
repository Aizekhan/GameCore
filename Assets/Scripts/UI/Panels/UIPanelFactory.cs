using UnityEngine;
using System.Threading.Tasks;

namespace GameCore.Core
{
    /// <summary>
    /// Створює панелі на основі зареєстрованих у реєстрі префабів.
    /// </summary>
    public class UIPanelFactory : MonoBehaviour, IService, IInitializable
    {
       
        [SerializeField] private Transform panelRoot; // основний root
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 60;
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
                _registry = ServiceLocator.Instance.GetService<UIPanelRegistry>();

                if (_registry == null)
                {
                    CoreLogger.LogError("UI", "❗ UIPanelRegistry not found. Trying to create one...");

                    // Спроба створити реєстр
                    var registryGO = new GameObject("UIPanelRegistry");
                    registryGO.transform.SetParent(transform.parent);
                    _registry = registryGO.AddComponent<UIPanelRegistry>();

                    // Реєструємо його в ServiceLocator
                    await ServiceLocator.Instance.RegisterService(_registry);

                    if (_registry is IInitializable initializable)
                        await initializable.Initialize();
                }
            }

            if (panelRoot == null)
            {
                var canvas = Object.FindFirstObjectByType<Canvas>();
                panelRoot = canvas != null ? canvas.transform : this.transform;
            }

            CoreLogger.Log("UI", "✅ UIPanelFactory initialized.");
            IsInitialized = true;
        }

        public UIPanel CreatePanel(string panelName)
        {
            var prefab = _registry.GetPanelPrefab(panelName);

            if (prefab == null)
            {
                // ❗ Спроба завантажити вручну, якщо панель не була зареєстрована
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

            var instance = Object.Instantiate(prefab, panelRoot);
            instance.name = prefab.name;
            return instance.GetComponent<UIPanel>();
        }

    }
}
