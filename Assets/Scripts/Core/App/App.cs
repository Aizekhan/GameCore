// Assets/Scripts/Core/App/App.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameCore.Core.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore.Core
{
    /// <summary>
    /// Центральна точка ініціалізації всіх систем.
    /// Завантажується першим у сцені Startup.
    /// </summary>
    public class App : MonoBehaviour
    {
        [Header("Core Services")]
        [SerializeField] private GameObject serviceLocatorPrefab;

        [Header("Managers")]
        [SerializeField] private GameObject uiManagerPrefab;
        [SerializeField] private GameObject audioManagerPrefab;
        [SerializeField] private GameObject saveManagerPrefab;

        [SerializeField] private GameObject inputActionHandlerPrefab;

        [Header("Configuration")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private bool automaticallyLoadMainMenu = true;

        // Список компонентів, які потребують ініціалізації
        private readonly List<IInitializable> _initializables = new List<IInitializable>();

        private void Awake()
        {
            CoreLogger.Log("APP", "Initializing application...");
            // Зберігаємо App між сценами
            DontDestroyOnLoad(gameObject);
            // Ініціалізуємо базові сервіси
            InitializeServiceLocator();
        }

        private async void Start()
        {
            // Ініціалізуємо всі сервіси та компоненти
            await InitializeServices();

            CoreLogger.Log("APP", "Application initialized successfully!");

            if (automaticallyLoadMainMenu)
            {
                CoreLogger.Log("APP", $"Loading main menu: {mainMenuSceneName}");
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        private void InitializeServiceLocator()
        {
            if (FindFirstObjectByType<ServiceLocator>() == null)
            {
                var serviceLocatorGO = Instantiate(serviceLocatorPrefab);
                serviceLocatorGO.transform.SetParent(transform, false); // Робимо дочірнім об'єктом App
                var serviceLocator = serviceLocatorGO.GetComponent<ServiceLocator>();

                if (serviceLocator == null)
                {
                    CoreLogger.LogError("APP", "ServiceLocator prefab doesn't have ServiceLocator component!");
                    return;
                }

                CoreLogger.Log("APP", "ServiceLocator initialized");
            }
        }
        private async Task InitializePlatformService()
        {
            if (!ServiceLocator.Instance.HasService<IPlatformService>())
            {
                var platformGO = new GameObject("PlatformDetector");
                DontDestroyOnLoad(platformGO);
                var detector = platformGO.AddComponent<PlatformDetector>();
                await ServiceLocator.Instance.RegisterService<IPlatformService>(detector);
            }
        }
        private async Task InitializeServices()
        {
            // Створюємо і реєструємо основні сервіси
            await InitializePlatformService();

            await InitializeUIManager();

            await InitializeUIPanelServices();
            await InitializeUIServices();
            await InitializeUINavigationService();
           

            await InitializeAudioManager();
            await InitializeSaveManager();
            await InitializeInputSchemeManager();
            await InitializeInputActionHandler();

            // Сортуємо компоненти за пріоритетом
            _initializables.Sort((a, b) => b.InitializationPriority.CompareTo(a.InitializationPriority));

            // Ініціалізуємо всі додаткові компоненти
            foreach (var initializable in _initializables.Where(i => !i.IsInitialized))
            {
                CoreLogger.Log("APP", $"Initializing {initializable.GetType().Name}...");
                await initializable.Initialize();
            }
        }

        private async Task InitializeUIManager()
        {
            var uiManager = FindFirstObjectByType<UIManager>();

            if (uiManager == null && uiManagerPrefab != null)
            {
                CoreLogger.Log("APP", "Creating new UIManager");
                var uiManagerRootGO = Instantiate(uiManagerPrefab);
                uiManagerRootGO.transform.SetParent(transform, false); // Робимо дочірнім об'єктом App
                uiManager = uiManagerRootGO.GetComponentInChildren<UIManager>();

                if (uiManager == null)
                {
                    CoreLogger.LogError("APP", "UIManager component not found in prefab!");
                }
            }

            if (uiManager != null)
            {
                if (!ServiceLocator.Instance.HasService<UIManager>())
                {
                    await ServiceLocator.Instance.RegisterService<UIManager>(uiManager);
                    CoreLogger.Log("APP", "UIManager initialized and registered");
                }
                else
                {
                    CoreLogger.Log("APP", "UIManager already registered, skipping");
                }
            }
            else
            {
                CoreLogger.LogError("APP", "Failed to find UIManager component!");
            }
        }

        private async Task InitializeAudioManager()
        {
            var audioManager = FindFirstObjectByType<AudioManager>();

            if (audioManager == null && audioManagerPrefab != null)
            {
                var audioManagerGO = Instantiate(audioManagerPrefab);
                audioManagerGO.transform.SetParent(transform, false); // Робимо дочірнім об'єктом App
                audioManager = audioManagerGO.GetComponent<AudioManager>();
            }

            if (audioManager != null)
            {
                // Перевіряємо, чи вже зареєстрований
                if (!ServiceLocator.Instance.HasService<AudioManager>())
                {
                    await ServiceLocator.Instance.RegisterService<AudioManager>(audioManager);
                    CoreLogger.Log("APP", "AudioManager initialized");
                }
                else
                {
                    CoreLogger.Log("APP", "AudioManager already registered, skipping");
                }
            }
        }

        private async Task InitializeSaveManager()
        {
            var saveManager = Object.FindAnyObjectByType<SaveManager>();

            if (saveManager == null && saveManagerPrefab != null)
            {
                var saveManagerGO = Instantiate(saveManagerPrefab);
                saveManagerGO.transform.SetParent(transform, false); // Робимо дочірнім об'єктом App
                saveManager = saveManagerGO.GetComponent<SaveManager>();
            }

            if (saveManager != null)
            {
                // Перевіряємо, чи вже зареєстрований
                if (!ServiceLocator.Instance.HasService<SaveManager>())
                {
                    await ServiceLocator.Instance.RegisterService<SaveManager>(saveManager);
                    CoreLogger.Log("APP", "SaveManager initialized");
                }
                else
                {
                    CoreLogger.Log("APP", "SaveManager already registered, skipping");
                }
            }
        }

        private async Task InitializeInputActionHandler()
        {
            var handler = FindFirstObjectByType<InputActionHandler>();

            if (handler == null && inputActionHandlerPrefab != null)
            {
                var handlerGO = Instantiate(inputActionHandlerPrefab);
                handlerGO.transform.SetParent(transform, false);

                handler = handlerGO.GetComponent<InputActionHandler>();
            }

            if (handler != null)
            {
                if (!ServiceLocator.Instance.HasService<InputActionHandler>())
                {
                    await ServiceLocator.Instance.RegisterService(handler);
                    CoreLogger.Log("APP", "InputActionHandler initialized");
                }
                else
                {
                    CoreLogger.Log("APP", "InputActionHandler already registered");
                }

                // 🧠 Підписка на події після реєстрації
                handler.onPause.AddListener(() =>
                {
                    EventBus.Emit("UI/ShowPanel", "SettingsPanel");
                });

                handler.onCancel.AddListener(() =>
                {
                    EventBus.Emit("Input/Cancel");
                });
            }
            else
            {
                CoreLogger.LogWarning("APP", "❌ InputActionHandler not found in scene or prefab.");
            }
        }

        private async Task InitializeUINavigationService()
        {
            var navigationService = FindFirstObjectByType<UINavigationService>();

            if (navigationService == null)
            {
                var navServiceGO = new GameObject("UINavigationService");
                navServiceGO.transform.SetParent(transform, false);
                navigationService = navServiceGO.AddComponent<UINavigationService>();
            }

            if (navigationService != null)
            {
                await ServiceLocator.Instance.RegisterService<UINavigationService>(navigationService);
                CoreLogger.Log("APP", "UINavigationService initialized");
            }
        }

        private async Task InitializeUIServices()
        {
            // Створення об'єктів
            var buttonRegistry = gameObject.AddComponent<UIButtonRegistry>();
            var buttonFactory = gameObject.AddComponent<UIButtonFactory>();

            // Встановлення шляху до префабу кнопки (опційно)
            buttonFactory.SetButtonPrefabPath("UI/Prefabs/StandardButton");

            // Реєстрація сервісів
            await ServiceLocator.Instance.RegisterService<UIButtonRegistry>(buttonRegistry);
            await ServiceLocator.Instance.RegisterService<UIButtonFactory>(buttonFactory);

            // Ініціалізація сервісів
            await buttonRegistry.Initialize();
            await buttonFactory.Initialize();

            CoreLogger.Log("UI Button Services initialized");
        }
        private async Task InitializeUIPanelServices()
        {
            // 🧩 Створення об’єктів
            var panelRegistry = gameObject.AddComponent<UIPanelRegistry>();
            var panelFactory = gameObject.AddComponent<UIPanelFactory>();

            // 🔗 Встановлення залежностей
            panelFactory.SetRegistry(panelRegistry);

            // 🖼️ Пошук кореня Canvas
            var panelRoot = GameObject.Find("UICanvas_Root")?.transform;
            if (panelRoot != null)
            {
                panelFactory.SetPanelRoot(panelRoot);
            }
            else
            {
                CoreLogger.LogWarning("UI", "⚠️ UICanvas_Root not found, panels may be instantiated under wrong parent.");
            }

            // 📦 Реєстрація сервісів
            await ServiceLocator.Instance.RegisterService<UIPanelRegistry>(panelRegistry);
            await ServiceLocator.Instance.RegisterService<UIPanelFactory>(panelFactory);

            // 🚀 Ініціалізація сервісів
            await panelRegistry.Initialize();
            await panelFactory.Initialize();

            CoreLogger.Log("UI", "✅ UI Panel Services initialized");
        }


        private async Task InitializeInputSchemeManager()
        {
            var inputManager = FindFirstObjectByType<InputSchemeManager>();

            if (inputManager == null)
            {
                var go = new GameObject("InputSchemeManager");
                go.transform.SetParent(transform);
                inputManager = go.AddComponent<InputSchemeManager>();
            }

            if (inputManager != null)
            {
                await ServiceLocator.Instance.RegisterService<InputSchemeManager>(inputManager);
                CoreLogger.Log("APP", "✅ InputSchemeManager initialized");
            }
        }

       
        public void RegisterInitializable(IInitializable initializable)
        {
            if (!_initializables.Contains(initializable))
            {
                _initializables.Add(initializable);
                CoreLogger.Log("APP", $"Registered initializable: {initializable.GetType().Name}");
            }
        }
        
    }
}