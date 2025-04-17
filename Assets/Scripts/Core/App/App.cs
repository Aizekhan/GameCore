// App.cs — з реєстрацією UIPanelAnimation
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private bool automaticallyLoadMainMenu = true;
        [SerializeField] private InputActionAsset inputAsset;

        private readonly List<IInitializable> _initializables = new();

        private AppStateManager _stateManager;
        private ServiceLocator _serviceLocator;
        private PlayerInput _playerInput;
        private InputSchemeManager _inputManager;
        private AudioManager _audioManager;
        private SceneLoader _sceneLoader;
        private UINavigationService _navigationService;
        private UIPanelFactory _panelFactory;
        private UIPanelRegistry _panelRegistry;
        private UIManager _uiManager;
        private UIPanelAnimation _panelAnimation;

        private void Awake()
        {
            CoreLogger.Log("APP", "Initializing application...");
            DontDestroyOnLoad(gameObject);

            _serviceLocator = GetComponent<ServiceLocator>();
            _stateManager = GetComponent<AppStateManager>();
            _inputManager = GetComponent<InputSchemeManager>();
            _audioManager = GetComponent<AudioManager>();
            _sceneLoader = GetComponent<SceneLoader>();
            _panelAnimation = GetComponent<UIPanelAnimation>();
            _navigationService = GetComponent<UINavigationService>();
            _panelFactory = GetComponent<UIPanelFactory>();
            _panelRegistry = GetComponent<UIPanelRegistry>();
            _uiManager = GetComponent<UIManager>();
           

            if (_serviceLocator == null || _stateManager == null || _inputManager == null ||
                _audioManager == null || _sceneLoader == null || _navigationService == null ||
                _panelFactory == null || _panelRegistry == null || _uiManager == null || _panelAnimation == null)
            {
                Debug.LogError("❌ Missing required components on App prefab");
                return;
            }

            InitializeEventSystem();
            InitializeUICanvas();
        }

        private async void Start()
        {
            _stateManager.ChangeState(AppStateManager.AppState.Initializing, false);
            await InitializeAllServices();
            CoreLogger.Log("APP", "✅ Application initialized successfully!");
            EventBus.Emit("App/Ready");
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
            await _serviceLocator.RegisterService(_stateManager);
            await _serviceLocator.RegisterService(_inputManager);
            await _serviceLocator.RegisterService(_audioManager);
            await _serviceLocator.RegisterService(_sceneLoader);
            await _serviceLocator.RegisterService(_panelRegistry);

            _panelFactory.SetRegistry(_panelRegistry);
            _panelFactory.SetPanelRoot(GameObject.Find("UICanvas_Root")?.transform);

            await _serviceLocator.RegisterService(_panelFactory);
            await _serviceLocator.RegisterService(_uiManager);
            await _serviceLocator.RegisterService(_panelAnimation);

            RegisterInitializable(_stateManager);
            RegisterInitializable(_inputManager);
            RegisterInitializable(_audioManager);
            RegisterInitializable(_sceneLoader);
            RegisterInitializable(_panelRegistry);
            RegisterInitializable(_panelFactory);
            RegisterInitializable(_uiManager);
            RegisterInitializable(_panelAnimation);

            await InitializePlatformService();
            await InitializePlayerInput();

            await _serviceLocator.RegisterService(_navigationService);
            RegisterInitializable(_navigationService);

            _initializables.Sort((a, b) => b.InitializationPriority.CompareTo(a.InitializationPriority));
            foreach (var init in _initializables.Where(i => !i.IsInitialized))
            {
                CoreLogger.Log("APP", $"Initializing {init.GetType().Name}...");
                await init.Initialize();
            }
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
            var inputGO = new GameObject("PlayerInput");
            inputGO.transform.SetParent(transform, false);
            _playerInput = inputGO.AddComponent<PlayerInput>();
            _playerInput.actions = inputAsset;
            _playerInput.defaultControlScheme = "Keyboard&Mouse";
            _inputManager.SetPlayerInput(_playerInput);
            await Task.CompletedTask;
        }

        public void RegisterInitializable(IInitializable initializable)
        {
            if (initializable != null && !_initializables.Contains(initializable))
            {
                _initializables.Add(initializable);
                CoreLogger.Log("APP", $"✅ Registered: {initializable.GetType().Name}");
            }
        }
    }
}