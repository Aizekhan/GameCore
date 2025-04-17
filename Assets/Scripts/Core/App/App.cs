// Переосмислений App.cs — з автогенерацією сервісів
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
            CoreLogger.Log("InitializeAllServices started!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
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
                    typeof(UINavigationService)
                };

                foreach (var factory in _serviceFactories.Where(f => !criticalTypes.Contains(f.Key)))
                {
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
                    if (service != null && !createdServices.Contains(service))
                    {
                        createdServices.Add(service);
                    }
                }
            }

            // Реєструємо створені сервіси
            foreach (var service in createdServices)
            {
                await _serviceLocator.RegisterService(service);

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
            foreach (var init in _initializables.Where(i => !i.IsInitialized))
            {
                CoreLogger.Log("APP", $"Initializing {init.GetType().Name}...");
                await init.Initialize();
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
            if (!_serviceLocator.HasService<UIPanelRegistry>())
            {
                var panelRegistry = CreateService<UIPanelRegistry>();
                await _serviceLocator.RegisterService(panelRegistry);
                RegisterInitializable(panelRegistry);
            }

            if (!_serviceLocator.HasService<UIPanelFactory>())
            {
                var panelFactory = CreateService<UIPanelFactory>();
                panelFactory.SetRegistry(_serviceLocator.GetService<UIPanelRegistry>());
                panelFactory.SetPanelRoot(GameObject.Find("UICanvas_Root")?.transform);
                await _serviceLocator.RegisterService(panelFactory);
                RegisterInitializable(panelFactory);
            }

            if (!_serviceLocator.HasService<UIButtonRegistry>())
            {
                var buttonRegistry = CreateService<UIButtonRegistry>();
                await _serviceLocator.RegisterService(buttonRegistry);
                RegisterInitializable(buttonRegistry);
            }

            if (!_serviceLocator.HasService<UIButtonFactory>())
            {
                var buttonFactory = CreateService<UIButtonFactory>();
                buttonFactory.SetButtonPrefabPath("UI/Prefabs/StandardButton");
                await _serviceLocator.RegisterService(buttonFactory);
                RegisterInitializable(buttonFactory);
            }

            if (!_serviceLocator.HasService<UIPanelPool>())
            {
                var panelPool = CreateService<UIPanelPool>();
                await _serviceLocator.RegisterService(panelPool);
                RegisterInitializable(panelPool);
            }

            if (!_serviceLocator.HasService<UIPanelAnimation>())
            {
                var panelAnimation = CreateService<UIPanelAnimation>();
                await _serviceLocator.RegisterService(panelAnimation);
                RegisterInitializable(panelAnimation);
            }

            if (!_serviceLocator.HasService<UIManager>())
            {
                var uiManager = CreateService<UIManager>();
                await _serviceLocator.RegisterService(uiManager);
                RegisterInitializable(uiManager);
            }

            if (!_serviceLocator.HasService<UINavigationService>())
            {
                var navigationService = CreateService<UINavigationService>();
                await _serviceLocator.RegisterService(navigationService);
                RegisterInitializable(navigationService);
            }

            // --- Додані 3 критичні сервіси нижче ---

            if (!_serviceLocator.HasService<AppStateManager>())
            {
                var appStateManager = CreateService<AppStateManager>();
                await _serviceLocator.RegisterService(appStateManager);
                RegisterInitializable(appStateManager);
            }

            if (!_serviceLocator.HasService<InputSchemeManager>())
            {
                var inputManager = CreateService<InputSchemeManager>();
                await _serviceLocator.RegisterService(inputManager);
                RegisterInitializable(inputManager);
            }

            if (!_serviceLocator.HasService<SceneLoader>())
            {
                var sceneLoader = CreateService<SceneLoader>();
                await _serviceLocator.RegisterService(sceneLoader);
                RegisterInitializable(sceneLoader);
            }

            if (!_serviceLocator.HasService<AudioManager>())
            {
                var audioManager = CreateService<AudioManager>();
                await _serviceLocator.RegisterService(audioManager);
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