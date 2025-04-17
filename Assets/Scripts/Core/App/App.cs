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
            // Крок 1: Створюємо всі сервіси
            List<IService> createdServices = new List<IService>();

            if (autoCreateServices)
            {
                // Автоматично створюємо всі зареєстровані сервіси
                foreach (var factory in _serviceFactories)
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

            // Крок 2: Реєструємо всі сервіси
            foreach (var service in createdServices)
            {
                await _serviceLocator.RegisterService(service);

                // Якщо сервіс ініціалізований, додаємо його до списку
                if (service is IInitializable initializable)
                {
                    RegisterInitializable(initializable);
                }
            }

            // Крок 3: Спеціальна обробка для сервісів, які потребують додаткової конфігурації
            ConfigureSpecialServices();

            // Крок 4: Створюємо платформозалежні сервіси
            await InitializePlatformService();

            // Крок 5: Ініціалізуємо системи вводу
            await InitializePlayerInput();

            // Крок 6: Ініціалізуємо UI-пул (якщо є)
            await InitializeUIPanelPool();

            // Крок 7: Запускаємо ініціалізацію всіх сервісів у правильному порядку
            _initializables.Sort((a, b) => b.InitializationPriority.CompareTo(a.InitializationPriority));
            foreach (var init in _initializables.Where(i => !i.IsInitialized))
            {
                CoreLogger.Log("APP", $"Initializing {init.GetType().Name}...");
                await init.Initialize();
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
            // Отримуємо InputSchemeManager
            var inputManager = _serviceLocator.GetService<InputSchemeManager>();
            if (inputManager == null) return;

            // Створюємо PlayerInput
            var inputGO = new GameObject("PlayerInput");
            inputGO.transform.SetParent(transform, false);
            var playerInput = inputGO.AddComponent<PlayerInput>();
            playerInput.actions = inputAsset;
            playerInput.defaultControlScheme = "Keyboard&Mouse";
            inputManager.SetPlayerInput(playerInput);

            await Task.CompletedTask;
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