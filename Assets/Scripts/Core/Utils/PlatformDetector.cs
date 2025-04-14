// Assets/Scripts/Core/Utils/PlatformDetector.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

namespace GameCore.Core
{
    /// <summary>
    /// Визначає платформу, на якій запущено гру, та встановлює відповідні налаштування
    /// </summary>
    public class PlatformDetector : MonoBehaviour, IService, IInitializable
    {
        [Tooltip("Назва сцени, на яку перейти після визначення платформи")]
        public string mainMenuSceneName = "MainMenu";

        [Header("Quality Settings")]
        [SerializeField] private int androidQualityLevel = 1; // Low
        [SerializeField] private int iosQualityLevel = 1; // Low
        [SerializeField] private int webGLQualityLevel = 2; // Medium
        [SerializeField] private int windowsQualityLevel = 3; // High
        [SerializeField] private int consoleQualityLevel = 4; // Ultra

        // IInitializable implementation
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 100; // High priority - execute early

        public enum PlatformType
        {
            Windows,
            MacOS,
            Linux,
            Android,
            iOS,
            WebGL,
            PlayStation,
            Xbox,
            Unknown
        }

        public PlatformType CurrentPlatform { get; private set; }

        private void Awake()
        {
            if (ServiceLocator.Instance != null)
            {
                // Реєструємо себе як сервіс, ініціалізація буде викликана через ServiceLocator
                ServiceLocator.Instance.RegisterService<PlatformDetector>(this).ConfigureAwait(false);
            }
            else
            {
                // Якщо ServiceLocator ще не ініціалізований, робимо DontDestroyOnLoad
                DontDestroyOnLoad(gameObject);
                // І викликаємо ініціалізацію напряму
                Initialize().ConfigureAwait(false);
            }
        }

        public async Task Initialize()
        {
            if (IsInitialized) return;

            // Визначення платформи
            DetectPlatform();

            // Встановлення відповідних налаштувань якості
            ApplyQualitySettings();

            // Інші налаштування, специфічні для платформи...

            IsInitialized = true;

            // Завершення ініціалізації
            CoreLogger.Log("PLATFORM", $"Platform detection and initialization complete: {CurrentPlatform}");

            await Task.CompletedTask; // Для асинхронного інтерфейсу
        }

        private void DetectPlatform()
        {
#if UNITY_XBOXONE || UNITY_GAMECORE
            CurrentPlatform = PlatformType.Xbox;
            CoreLogger.Log("PLATFORM", "🎮 Платформа: Xbox");
#elif UNITY_PS4 || UNITY_PS5
            CurrentPlatform = PlatformType.PlayStation;
            CoreLogger.Log("PLATFORM", "🕹 Платформа: PlayStation");
#elif UNITY_STANDALONE_WIN
            CurrentPlatform = PlatformType.Windows;
            CoreLogger.Log("PLATFORM", "🖥 Платформа: Windows");
#elif UNITY_STANDALONE_OSX
            CurrentPlatform = PlatformType.MacOS;
            CoreLogger.Log("PLATFORM", "🖥 Платформа: MacOS");
#elif UNITY_STANDALONE_LINUX
            CurrentPlatform = PlatformType.Linux;
            CoreLogger.Log("PLATFORM", "🖥 Платформа: Linux");
#elif UNITY_ANDROID
            CurrentPlatform = PlatformType.Android;
            CoreLogger.Log("PLATFORM", "📱 Платформа: Android");
#elif UNITY_IOS
            CurrentPlatform = PlatformType.iOS;
            CoreLogger.Log("PLATFORM", "🍏 Платформа: iOS");
#elif UNITY_WEBGL
            CurrentPlatform = PlatformType.WebGL;
            CoreLogger.Log("PLATFORM", "🌐 Платформа: WebGL (браузер)");
#else
            CurrentPlatform = PlatformType.Unknown;
            CoreLogger.Log("PLATFORM", "❓ Невідома платформа: " + Application.platform);
#endif

            // Відправляємо подію про визначення платформи
            EventBus.Emit("Platform/Detected", CurrentPlatform);
        }

        private void ApplyQualitySettings()
        {
            int qualityLevel = 2; // Default: Medium

            switch (CurrentPlatform)
            {
                case PlatformType.Android:
                    qualityLevel = androidQualityLevel;
                    break;
                case PlatformType.iOS:
                    qualityLevel = iosQualityLevel;
                    break;
                case PlatformType.WebGL:
                    qualityLevel = webGLQualityLevel;
                    break;
                case PlatformType.Windows:
                case PlatformType.MacOS:
                case PlatformType.Linux:
                    qualityLevel = windowsQualityLevel;
                    break;
                case PlatformType.PlayStation:
                case PlatformType.Xbox:
                    qualityLevel = consoleQualityLevel;
                    break;
            }

            // Обмежуємо значення якості доступними рівнями
            qualityLevel = Mathf.Clamp(qualityLevel, 0, QualitySettings.names.Length - 1);

            // Встановлюємо рівень якості
            QualitySettings.SetQualityLevel(qualityLevel, true);

            CoreLogger.Log("PLATFORM", $"Quality level set to: {QualitySettings.names[qualityLevel]}");
        }

        // Додаткові методи для налаштування специфічних для платформи параметрів

        public bool IsMobilePlatform()
        {
            return CurrentPlatform == PlatformType.Android || CurrentPlatform == PlatformType.iOS;
        }

        public bool IsConsolePlatform()
        {
            return CurrentPlatform == PlatformType.PlayStation || CurrentPlatform == PlatformType.Xbox;
        }

        public bool IsDesktopPlatform()
        {
            return CurrentPlatform == PlatformType.Windows ||
                   CurrentPlatform == PlatformType.MacOS ||
                   CurrentPlatform == PlatformType.Linux;
        }
    }
}