using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameCore.Core
{
    public class MainMenuPanel : UIPanel
    {
        [Header("Button References")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Optional UI Elements")]
        [SerializeField] private TextMeshProUGUI versionText;

        private MainMenuController menuController;
        private PlatformDetector platformDetector;

        protected override void Awake()
        {
            base.Awake();

            // Отримуємо посилання на контролери через ServiceLocator
            menuController = FindFirstObjectByType<MainMenuController>();
            if (menuController == null)
            {
                CoreLogger.LogWarning("MainMenu", "MainMenuController not found!");
            }
            platformDetector = ServiceLocator.Instance.GetService<PlatformDetector>();

            // Підключаємо обробники подій до кнопок
            SetupButtonListeners();

            // Встановлюємо версію гри
            UpdateVersionDisplay();
        }

        private void SetupButtonListeners()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartButtonClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        private void OnStartButtonClicked()
        {
            CoreLogger.Log("MainMenu", "Start Game button clicked");
            menuController?.OnStartGamePressed();

            // Програємо звук кліку (якщо налаштовано)
            AudioManager.Instance?.PlaySound("ButtonClick", AudioType.UI);
        }

        private void OnSettingsButtonClicked()
        {
            CoreLogger.Log("MainMenu", "Settings button clicked");

            // Показуємо панель налаштувань через EventBus
            EventBus.Emit("UI/ShowPanel", "SettingsPanel");

            // Програємо звук кліку
            AudioManager.Instance?.PlaySound("ButtonClick", AudioType.UI);
        }

        private void OnExitButtonClicked()
        {
            CoreLogger.Log("MainMenu", "Exit button clicked");
            menuController?.OnExitPressed();

            // Програємо звук кліку
            AudioManager.Instance?.PlaySound("ButtonClick", AudioType.UI);
        }

        private void UpdateVersionDisplay()
        {
            if (versionText != null)
            {
                string version = Application.version;

                // Додаємо інформацію про платформу, якщо є PlatformDetector
                if (platformDetector != null)
                {
                    version += $" ({platformDetector.CurrentPlatform})";
                }

                versionText.text = $"Версія {version}";
            }
        }

        // Перевизначаємо методи Show/Hide для додаткових ефектів
        public override void Show()
        {
            base.Show();
            CoreLogger.Log("MainMenu", "Main Menu panel shown");
        }

        public override void Hide()
        {
            base.Hide();
            CoreLogger.Log("MainMenu", "Main Menu panel hidden");
        }
    }
}