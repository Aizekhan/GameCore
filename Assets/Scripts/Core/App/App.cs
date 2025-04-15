// Assets/Scripts/Core/App/App.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore.Core
{
    /// <summary>
    /// ���������� ����� ������������ ��� ������.
    /// ������������� ������ � ����� Startup.
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

        // ������ ����������, �� ���������� ������������
        private readonly List<IInitializable> _initializables = new List<IInitializable>();

        private void Awake()
        {
            CoreLogger.Log("APP", "Initializing application...");
            // �������� App �� �������
            DontDestroyOnLoad(gameObject);
            // ����������� ����� ������
            InitializeServiceLocator();
        }

        private async void Start()
        {
            // ����������� �� ������ �� ����������
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
                serviceLocatorGO.transform.SetParent(transform, false); // ������ �������� ��'����� App
                var serviceLocator = serviceLocatorGO.GetComponent<ServiceLocator>();

                if (serviceLocator == null)
                {
                    CoreLogger.LogError("APP", "ServiceLocator prefab doesn't have ServiceLocator component!");
                    return;
                }

                CoreLogger.Log("APP", "ServiceLocator initialized");
            }
        }

        private async Task InitializeServices()
        {
            // ��������� � �������� ������� ������
            await InitializeUIManager();
            await InitializeAudioManager();
            await InitializeSaveManager();
            await InitializeInputActionHandler();

            // ������� ���������� �� ����������
            _initializables.Sort((a, b) => b.InitializationPriority.CompareTo(a.InitializationPriority));

            // ����������� �� �������� ����������
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
                uiManagerRootGO.transform.SetParent(transform, false); // ������ �������� ��'����� App
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
                audioManagerGO.transform.SetParent(transform, false); // ������ �������� ��'����� App
                audioManager = audioManagerGO.GetComponent<AudioManager>();
            }

            if (audioManager != null)
            {
                // ����������, �� ��� �������������
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
                saveManagerGO.transform.SetParent(transform, false); // ������ �������� ��'����� App
                saveManager = saveManagerGO.GetComponent<SaveManager>();
            }

            if (saveManager != null)
            {
                // ����������, �� ��� �������������
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