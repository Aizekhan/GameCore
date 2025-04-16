// Assets/Scripts/Core/App/App.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameCore.Core.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameCore.Core.EventSystem;
namespace GameCore.Core
{
    /// <summary>
    /// Центральна точка ініціалізації всіх систем.
    /// Завантажується першим у сцені Startup.
    /// </summary>
    public class App : MonoBehaviour
    {
        [Header("Core Configuration")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private bool automaticallyLoadMainMenu = true;

        // Список компонентів, які потребують ініціалізації
        private readonly List<IInitializable> _initializables = new List<IInitializable>();
        private AppStateManager _stateManager;

        private void Awake()
        {
            CoreLogger.Log("APP", "Initializing application...");
            // Зберігаємо App між сценами
            DontDestroyOnLoad(gameObject);

            // Створюємо компонент ServiceLocator, якщо його немає
            InitializeServiceLocator();

            // Створюємо AppStateManager
            InitializeAppStateManager();
        }

        private async void Start()
        {
            // Встановлюємо початковий стан
            _stateManager.ChangeState(AppStateManager.AppState.Initializing, false);

            // Ініціалізуємо всі сервіси та компоненти
            await InitializeServices();

            CoreLogger.Log("APP", "Application initialized successfully!");

            if (automaticallyLoadMainMenu)
            {
                // Змінюємо стан на MainMenu замість прямого завантаження сцени
                _stateManager.ChangeState(AppStateManager.AppState.MainMenu);
            }
        }

        private void InitializeServiceLocator()
        {
            if (FindFirstObjectByType<ServiceLocator>() == null)
            {
                var serviceLocatorGO = new GameObject("ServiceLocator");
                serviceLocatorGO.transform.SetParent(transform, false);
                serviceLocatorGO.AddComponent<ServiceLocator>();
                CoreLogger.Log("APP", "ServiceLocator initialized");
            }
        }

        private void InitializeAppStateManager()
        {
            var stateManagerGO = new GameObject("AppStateManager");
            stateManagerGO.transform.SetParent(transform, false);
            _stateManager = stateManagerGO.AddComponent<AppStateManager>();
            RegisterInitializable(_stateManager);
        }

        private async Task InitializeServices()
        {
            // Реєструємо AppStateManager як сервіс
            await ServiceLocator.Instance.RegisterService<AppStateManager>(_stateManager);

            // Ініціалізуємо базові сервіси
            await InitializeCoreSystems();

            // Ініціалізуємо UI системи
            await InitializeUISystems();

            // Ініціалізуємо менеджери
            await InitializeManagers();

            // Сортуємо компоненти за пріоритетом
            _initializables.Sort((a, b) => b.InitializationPriority.CompareTo(a.InitializationPriority));

            // Ініціалізуємо всі додаткові компоненти
            foreach (var initializable in _initializables.Where(i => !i.IsInitialized))
            {
                CoreLogger.Log("APP", $"Initializing {initializable.GetType().Name}...");
                await initializable.Initialize();
            }
        }

        private async Task InitializeCoreSystems()
        {
            // Платформо-залежний сервіс
            if (!ServiceLocator.Instance.HasService<IPlatformService>())
            {
                var platformGO = new GameObject("PlatformDetector");
                DontDestroyOnLoad(platformGO);
                var detector = platformGO.AddComponent<PlatformDetector>();
                await ServiceLocator.Instance.RegisterService<IPlatformService>(detector);
                RegisterInitializable(detector);
            }

            // Тут можна ініціалізувати інші базові сервіси
        }

        private async Task InitializeUISystems()
        {
            // UI Manager - знаходимо існуючий або створюємо новий
            var uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager == null)
            {
                var uiManagerGO = new GameObject("UIManager");
                uiManagerGO.transform.SetParent(transform, false);
                uiManager = uiManagerGO.AddComponent<UIManager>();

                // Додаємо FadeController, якщо потрібно
                var fadeGO = new GameObject("FadeController");
                fadeGO.transform.SetParent(uiManagerGO.transform, false);
                var fadeController = fadeGO.AddComponent<FadeController>();
                // Тут можна налаштувати fadeController, якщо потрібно
            }

            if (uiManager != null && !ServiceLocator.Instance.HasService<UIManager>())
            {
                await ServiceLocator.Instance.RegisterService<UIManager>(uiManager);
                RegisterInitializable(uiManager);
            }

            // UI Panel Services
            var panelRegistry = gameObject.AddComponent<UIPanelRegistry>();
            var panelFactory = gameObject.AddComponent<UIPanelFactory>();
            panelFactory.SetRegistry(panelRegistry);

            var panelRoot = GameObject.Find("UICanvas_Root")?.transform;
            if (panelRoot != null)
            {
                panelFactory.SetPanelRoot(panelRoot);
            }

            await ServiceLocator.Instance.RegisterService<UIPanelRegistry>(panelRegistry);
            await ServiceLocator.Instance.RegisterService<UIPanelFactory>(panelFactory);

            RegisterInitializable(panelRegistry);
            RegisterInitializable(panelFactory);

            // UI Button Services
            var buttonRegistry = gameObject.AddComponent<UIButtonRegistry>();
            var buttonFactory = gameObject.AddComponent<UIButtonFactory>();
            buttonFactory.SetButtonPrefabPath("UI/Prefabs/StandardButton");

            await ServiceLocator.Instance.RegisterService<UIButtonRegistry>(buttonRegistry);
            await ServiceLocator.Instance.RegisterService<UIButtonFactory>(buttonFactory);

            RegisterInitializable(buttonRegistry);
            RegisterInitializable(buttonFactory);

            // UI Navigation Service
            var navigationService = FindFirstObjectByType<UINavigationService>();
            if (navigationService == null)
            {
                var navServiceGO = new GameObject("UINavigationService");
                navServiceGO.transform.SetParent(transform, false);
                navigationService = navServiceGO.AddComponent<UINavigationService>();
            }

            if (navigationService != null && !ServiceLocator.Instance.HasService<UINavigationService>())
            {
                await ServiceLocator.Instance.RegisterService<UINavigationService>(navigationService);
                RegisterInitializable(navigationService);
            }
        }

        private async Task InitializeManagers()
        {
            // Audio Manager
            var audioManager = FindFirstObjectByType<AudioManager>();
            if (audioManager == null)
            {
                var audioManagerGO = new GameObject("AudioManager");
                audioManagerGO.transform.SetParent(transform, false);
                audioManager = audioManagerGO.AddComponent<AudioManager>();
            }

            if (audioManager != null && !ServiceLocator.Instance.HasService<AudioManager>())
            {
                await ServiceLocator.Instance.RegisterService<AudioManager>(audioManager);
                RegisterInitializable(audioManager);
            }

            // Save Manager
            var saveManager = FindFirstObjectByType<SaveManager>();
            if (saveManager == null)
            {
                var saveManagerGO = new GameObject("SaveManager");
                saveManagerGO.transform.SetParent(transform, false);
                saveManager = saveManagerGO.AddComponent<SaveManager>();
            }

            if (saveManager != null && !ServiceLocator.Instance.HasService<SaveManager>())
            {
                await ServiceLocator.Instance.RegisterService<SaveManager>(saveManager);
                RegisterInitializable(saveManager);
            }

            // Input Scheme Manager
            var inputManager = FindFirstObjectByType<InputSchemeManager>();
            if (inputManager == null)
            {
                var inputManagerGO = new GameObject("InputSchemeManager");
                inputManagerGO.transform.SetParent(transform, false);
                inputManager = inputManagerGO.AddComponent<InputSchemeManager>();
            }

            if (inputManager != null && !ServiceLocator.Instance.HasService<InputSchemeManager>())
            {
                await ServiceLocator.Instance.RegisterService<InputSchemeManager>(inputManager);
                RegisterInitializable(inputManager);
            }

            // Input Action Handler
            var inputHandler = FindFirstObjectByType<InputActionHandler>();
            if (inputHandler == null)
            {
                var handlerGO = new GameObject("InputActionHandler");
                handlerGO.transform.SetParent(transform, false);
                inputHandler = handlerGO.AddComponent<InputActionHandler>();
            }

            if (inputHandler != null && !ServiceLocator.Instance.HasService<InputActionHandler>())
            {
                await ServiceLocator.Instance.RegisterService<InputActionHandler>(inputHandler);
                RegisterInitializable(inputHandler);

                // Підписка на події
                inputHandler.onPause.AddListener(() => {
                    EventBus.Emit("UI/ShowPanel", "SettingsPanel");
                });

                inputHandler.onCancel.AddListener(() => {
                    EventBus.Emit("Input/Cancel");
                });
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