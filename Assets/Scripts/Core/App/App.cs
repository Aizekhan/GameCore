using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GameCore.Core.Interfaces;
using GameCore.Core.EventSystem;
using UnityEngine.InputSystem;

namespace GameCore.Core
{
    public class App : MonoBehaviour
    {
        [Header("Core Configuration")]
        [SerializeField] private bool automaticallyLoadMainMenu = true;
        [SerializeField] private InputActionAsset inputAsset;

        [Header("Service Configuration")]
        [SerializeField] private bool autoCreateServices = true;
        [SerializeField] private List<ServiceDescriptor> manualServices = new List<ServiceDescriptor>();

        private UnityEngine.InputSystem.PlayerInput _playerInput;
        private readonly List<IInitializable> _initializables = new();
        private ServiceLocator _serviceLocator;

        // Словник типів сервісів та їх фабричних методів
        private Dictionary<Type, Func<IService>> _serviceFactories = new Dictionary<Type, Func<IService>>();

        // Відстеження зареєстрованих сервісів для уникнення дублювання
        private HashSet<Type> _registeredServiceTypes = new HashSet<Type>();

        [Serializable]
        public class ServiceDescriptor
        {
            public string serviceName;
            public GameObject serviceReference;
            public int priority = 50;
        }

        private void Awake()
        {
            CoreLogger.Log("APP", "Initializing application...");
            DontDestroyOnLoad(gameObject);

            // Створюємо лише ServiceLocator вручну
            _serviceLocator = gameObject.AddComponent<ServiceLocator>();

            // Реєструємо всі фабрики сервісів
            RegisterServiceFactories();

            // Створюємо базову інфраструктуру
            InitializeEventSystem();
            InitializeUICanvas();
        }

        private async void Start()
        {
            await InitializeAllServices();
            CoreLogger.Log("APP", "✅ Application initialized successfully!");
            EventBus.Emit("App/Ready");

            // Змінюємо стан після ініціалізації всіх сервісів через вже налаштований AppStateManager
            var stateManager = _serviceLocator.GetService<AppStateManager>();
            if (stateManager != null)
            {
                stateManager.ChangeState(AppStateManager.AppState.Initializing, false);

                if (automaticallyLoadMainMenu)
                {
                    // Перехід до головного меню через подію App/Ready
                    // Підписка робиться в самому AppStateManager
                }
            }
            else
            {
                CoreLogger.LogWarning("APP", "AppStateManager not found. Cannot change application state.");
            }
        }

        private void RegisterServiceFactories()
        {
            // Фабричні методи для створення сервісів
            _serviceFactories[typeof(AppStateManager)] = () => CreateService<AppStateManager>();
            _serviceFactories[typeof(InputSchemeManager)] = () => CreateService<InputSchemeManager>();
            _serviceFactories[typeof(AudioManager)] = () => CreateService<AudioManager>();
            _serviceFactories[typeof(SceneLoader)] = () => CreateService<SceneLoader>();
            _serviceFactories[typeof(UINavigationService)] = () => CreateService<UINavigationService>();
            _serviceFactories[typeof(UIPanelAnimation)] = () => CreateService<UIPanelAnimation>();
            _serviceFactories[typeof(UIPanelRegistry)] = () => CreateService<UIPanelRegistry>();
            _serviceFactories[typeof(UIPanelFactory)] = () => CreateService<UIPanelFactory>();
            _serviceFactories[typeof(UIManager)] = () => CreateService<UIManager>();
            _serviceFactories[typeof(UIPanelPool)] = () => CreateService<UIPanelPool>();
            _serviceFactories[typeof(UIButtonRegistry)] = () => CreateService<UIButtonRegistry>();
            _serviceFactories[typeof(UIButtonFactory)] = () => CreateService<UIButtonFactory>();

            // Додаємо ResourceManager і ResourceBundleManager до фабрик
            _serviceFactories[typeof(ResourceManager)] = () => CreateService<ResourceManager>();
            _serviceFactories[typeof(ResourceBundleManager)] = () => CreateService<ResourceBundleManager>();

            // Додати інші сервіси по необхідності
        }

        private void InitializeEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.transform.SetParent(transform, false);
                var eventSystem = eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                var inputModule = eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                inputModule.actionsAsset = inputAsset;

                CoreLogger.Log("APP", "🆕 EventSystem created and linked");
            }
        }

        private void InitializeUICanvas()
        {
            if (GameObject.Find("UICanvas_Root") == null)
            {
                var canvasGO = new GameObject("UICanvas_Root");
                canvasGO.transform.SetParent(transform, false);
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1;

                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasGO.AddComponent<GraphicRaycaster>();

                CoreLogger.Log("APP", "🆕 UICanvas_Root created");
            }
        }

        private async Task InitializeAllServices()
        {
            CoreLogger.Log("APP", "Starting service initialization");

            // Створюємо і реєструємо критичні сервіси в правильному порядку
            await RegisterCriticalServices();

            // Створюємо решту сервісів
            List<IService> createdServices = new List<IService>();

            if (autoCreateServices)
            {
                // Пропускаємо вже зареєстровані критичні сервіси
                var criticalTypes = new Type[] {
                    typeof(UIPanelRegistry),
                    typeof(UIPanelFactory),
                    typeof(UIPanelPool),
                    typeof(UIManager),
                    typeof(UIButtonRegistry),
                    typeof(UIButtonFactory),
                    typeof(UIPanelAnimation),
                    typeof(UINavigationService),
                    typeof(ResourceManager),
                    typeof(ResourceBundleManager)
                };

                foreach (var factory in _serviceFactories.Where(keyValuePair => !criticalTypes.Contains(keyValuePair.Key)))
                {
                    // Перевіряємо, чи сервіс уже зареєстрований
                    if (_registeredServiceTypes.Contains(factory.Key))
                        continue;

                    var service = factory.Value();
                    if (service != null)
                    {
                        createdServices.Add(service);
                    }
                }
            }

            // Додаємо ручно вказані сервіси (з інспектора)
            foreach (var descriptor in manualServices)
            {
                if (descriptor.serviceReference != null)
                {
                    var service = descriptor.serviceReference.GetComponent<IService>();
                    Type serviceType = service?.GetType();

                    // Перевіряємо, чи не зареєстрований вже цей тип сервісу
                    if (service != null && serviceType != null && !_registeredServiceTypes.Contains(serviceType))
                    {
                        createdServices.Add(service);
                    }
                }
            }

            // Реєструємо створені сервіси
            foreach (var service in createdServices)
            {
                Type serviceType = service.GetType();

                // Пропускаємо вже зареєстровані сервіси
                if (_registeredServiceTypes.Contains(serviceType))
                    continue;

                await _serviceLocator.RegisterService(service);
                _registeredServiceTypes.Add(serviceType);

                if (service is IInitializable initializable)
                {
                    RegisterInitializable(initializable);
                }
            }

            // Створюємо платформозалежні сервіси
            await InitializePlatformService();

            // Ініціалізуємо системи вводу
            await InitializePlayerInput();

            // Запускаємо ініціалізацію всіх сервісів у правильному порядку
            _initializables.Sort((a, b) => b.InitializationPriority.CompareTo(a.InitializationPriority));

            CoreLogger.Log("APP", $"Initializing {_initializables.Count} services...");

            foreach (var init in _initializables.Where(i => !i.IsInitialized))
            {
                try
                {
                    CoreLogger.Log("APP", $"Initializing {init.GetType().Name}...");
                    await init.Initialize();
                }
                catch (Exception ex)
                {
                    CoreLogger.LogError("APP", $"Failed to initialize {init.GetType().Name}: {ex.Message}");
                }
            }

            // Попереднє завантаження панелей (після ініціалізації всіх сервісів)
            var uiManager = _serviceLocator.GetService<UIManager>();
            if (uiManager != null)
            {
                await uiManager.PreloadCommonPanels();
            }
        }

        // Новий метод для реєстрації критичних UI сервісів у правильному порядку
        private async Task RegisterCriticalServices()
        {
            // Спочатку ResourceManager, оскільки інші сервіси можуть від нього залежати
            if (!_serviceLocator.HasService<ResourceManager>())
            {
                var resourceManager = CreateService<ResourceManager>();
                await _serviceLocator.RegisterService(resourceManager);
                _registeredServiceTypes.Add(typeof(ResourceManager));
                RegisterInitializable(resourceManager);

                CoreLogger.Log("APP", "✅ ResourceManager registered");
            }

            // Потім ResourceBundleManager, він залежить від ResourceManager
            if (!_serviceLocator.HasService<ResourceBundleManager>())
            {
                var bundleManager = CreateService<ResourceBundleManager>();
                await _serviceLocator.RegisterService(bundleManager);
                _registeredServiceTypes.Add(typeof(ResourceBundleManager));
                RegisterInitializable(bundleManager);

                CoreLogger.Log("APP", "✅ ResourceBundleManager registered");
            }

            if (!_serviceLocator.HasService<UIPanelRegistry>())
            {
                var panelRegistry = CreateService<UIPanelRegistry>();
                await _serviceLocator.RegisterService(panelRegistry);
                _registeredServiceTypes.Add(typeof(UIPanelRegistry));
                RegisterInitializable(panelRegistry);
            }

            if (!_serviceLocator.HasService<UIPanelFactory>())
            {
                var panelFactory = CreateService<UIPanelFactory>();
                panelFactory.SetRegistry(_serviceLocator.GetService<UIPanelRegistry>());
                panelFactory.SetPanelRoot(GameObject.Find("UICanvas_Root")?.transform);
                await _serviceLocator.RegisterService(panelFactory);
                _registeredServiceTypes.Add(typeof(UIPanelFactory));
                RegisterInitializable(panelFactory);
            }

            if (!_serviceLocator.HasService<UIButtonRegistry>())
            {
                var buttonRegistry = CreateService<UIButtonRegistry>();
                await _serviceLocator.RegisterService(buttonRegistry);
                _registeredServiceTypes.Add(typeof(UIButtonRegistry));
                RegisterInitializable(buttonRegistry);
            }

            if (!_serviceLocator.HasService<UIButtonFactory>())
            {
                var buttonFactory = CreateService<UIButtonFactory>();
                buttonFactory.SetButtonPrefabPath("UI/Prefabs/StandardButton");
                await _serviceLocator.RegisterService(buttonFactory);
                _registeredServiceTypes.Add(typeof(UIButtonFactory));
                RegisterInitializable(buttonFactory);
            }

            if (!_serviceLocator.HasService<UIPanelPool>())
            {
                var panelPool = CreateService<UIPanelPool>();
                await _serviceLocator.RegisterService(panelPool);
                _registeredServiceTypes.Add(typeof(UIPanelPool));
                RegisterInitializable(panelPool);
            }

            if (!_serviceLocator.HasService<UIPanelAnimation>())
            {
                var panelAnimation = CreateService<UIPanelAnimation>();
                await _serviceLocator.RegisterService(panelAnimation);
                _registeredServiceTypes.Add(typeof(UIPanelAnimation));
                RegisterInitializable(panelAnimation);
            }

            if (!_serviceLocator.HasService<UIManager>())
            {
                var uiManager = CreateService<UIManager>();
                await _serviceLocator.RegisterService(uiManager);
                _registeredServiceTypes.Add(typeof(UIManager));
                RegisterInitializable(uiManager);
            }

            if (!_serviceLocator.HasService<UINavigationService>())
            {
                var navigationService = CreateService<UINavigationService>();
                await _serviceLocator.RegisterService(navigationService);
                _registeredServiceTypes.Add(typeof(UINavigationService));
                RegisterInitializable(navigationService);
            }

            // --- Додані 3 критичні сервіси нижче ---

            if (!_serviceLocator.HasService<AppStateManager>())
            {
                var appStateManager = CreateService<AppStateManager>();
                await _serviceLocator.RegisterService(appStateManager);
                _registeredServiceTypes.Add(typeof(AppStateManager));
                RegisterInitializable(appStateManager);
            }

            if (!_serviceLocator.HasService<InputSchemeManager>())
            {
                var inputManager = CreateService<InputSchemeManager>();
                await _serviceLocator.RegisterService(inputManager);
                _registeredServiceTypes.Add(typeof(InputSchemeManager));
                RegisterInitializable(inputManager);
            }

            if (!_serviceLocator.HasService<SceneLoader>())
            {
                var sceneLoader = CreateService<SceneLoader>();
                await _serviceLocator.RegisterService(sceneLoader);
                _registeredServiceTypes.Add(typeof(SceneLoader));
                RegisterInitializable(sceneLoader);
            }

            if (!_serviceLocator.HasService<AudioManager>())
            {
                var audioManager = CreateService<AudioManager>();
                await _serviceLocator.RegisterService(audioManager);
                _registeredServiceTypes.Add(typeof(AudioManager));
                RegisterInitializable(audioManager);
            }
        }

        private async Task InitializeUIPanelPool()
        {
            // Перевіряємо, чи є пул у ServiceLocator
            var panelPool = _serviceLocator.GetService<UIPanelPool>();

            // Якщо немає, створюємо новий
            if (panelPool == null && autoCreateServices)
            {
                panelPool = CreateService<UIPanelPool>();
                await _serviceLocator.RegisterService(panelPool);
                _registeredServiceTypes.Add(typeof(UIPanelPool));
                RegisterInitializable(panelPool);
            }
        }

        private void ConfigureSpecialServices()
        {
            // Налаштування UIPanelFactory
            var panelFactory = _serviceLocator.GetService<UIPanelFactory>();
            var panelRegistry = _serviceLocator.GetService<UIPanelRegistry>();

            if (panelFactory != null && panelRegistry != null)
            {
                panelFactory.SetRegistry(panelRegistry);
                panelFactory.SetPanelRoot(GameObject.Find("UICanvas_Root")?.transform);
            }

            // Інші спеціальні налаштування для інших сервісів
        }

        private async Task InitializePlatformService()
        {
            if (!_serviceLocator.HasService<IPlatformService>())
            {
                var platformDetector = FindFirstObjectByType<PlatformDetector>() ?? CreatePlatformDetector();
                await _serviceLocator.RegisterService<IPlatformService>(platformDetector);
                _registeredServiceTypes.Add(typeof(IPlatformService));
                RegisterInitializable(platformDetector);
            }
        }

        private PlatformDetector CreatePlatformDetector()
        {
            var go = new GameObject("PlatformDetector");
            go.transform.SetParent(transform, false);
            return go.AddComponent<PlatformDetector>();
        }

        private async Task InitializePlayerInput()
        {
            // Створюємо GameObject і додаємо PlayerInput, якщо ще не існує
            if (_playerInput == null)
            {
                var go = new GameObject("PlayerInput");
                go.transform.SetParent(transform);

                _playerInput = go.AddComponent<UnityEngine.InputSystem.PlayerInput>();
            }

            // Завантажуємо InputActionAsset з Resources, якщо не задано
            if (_playerInput.actions == null)
            {
                var inputAsset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("Input/UIInputActions");

                if (inputAsset == null)
                {
                    CoreLogger.LogError("INPUT", "❌ UIInputActions not found in Resources/Input/");
                    return;
                }

                _playerInput.actions = inputAsset;
            }

            // Реєструємо сервіс InputSchemeManager, якщо ще не зареєстровано
            if (!_serviceLocator.HasService<InputSchemeManager>())
            {
                var inputSchemeManager = CreateService<InputSchemeManager>();
                inputSchemeManager.SetPlayerInput(_playerInput);

                await _serviceLocator.RegisterService(inputSchemeManager);
                _registeredServiceTypes.Add(typeof(InputSchemeManager));
                RegisterInitializable(inputSchemeManager);
            }
        }

        private T CreateService<T>() where T : MonoBehaviour, IService
        {
            // Створюємо новий GameObject для сервісу
            string serviceName = typeof(T).Name;
            var serviceGO = new GameObject(serviceName);
            serviceGO.transform.SetParent(transform);

            // Додаємо компонент і повертаємо його
            return serviceGO.AddComponent<T>();
        }

        public void RegisterInitializable(IInitializable initializable)
        {
            if (initializable != null && !_initializables.Contains(initializable))
            {
                _initializables.Add(initializable);
                CoreLogger.Log("APP", $"✅ Registered: {initializable.GetType().Name}");
            }
        }

        // Метод для отримання сервісу за типом
        public T GetService<T>() where T : class, IService
        {
            return _serviceLocator.GetService<T>();
        }
    }
}