// Assets/Scripts/Core/App/App.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        [Header("Configuration")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private bool automaticallyLoadMainMenu = true;

        // Список компонентів, які потребують ініціалізації
        private readonly List<IInitializable> _initializables = new List<IInitializable>();

        private void Awake()
        {
            CoreLogger.Log("APP", "Initializing application...");

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
            // Створюємо і реєструємо основні сервіси
            await InitializeUIManager();
            await InitializeAudioManager();
            await InitializeSaveManager();

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
                var uiManagerRootGO = Instantiate(uiManagerPrefab);
                uiManager = uiManagerRootGO.GetComponentInChildren<UIManager>();
            }

            if (uiManager != null)
            {
                await ServiceLocator.Instance.RegisterService<UIManager>(uiManager);
                CoreLogger.Log("APP", "UIManager initialized");
            }
            else
            {
                Debug.LogError("Failed to find UIManager component!");
            }
        }

        private async Task InitializeAudioManager()
        {
            var audioManager = FindFirstObjectByType<AudioManager>();

            if (audioManager == null && audioManagerPrefab != null)
            {
                var audioManagerGO = Instantiate(audioManagerPrefab);
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