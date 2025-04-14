using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

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

        private PlatformDetector platformDetector;

        protected override void Awake()
        {
            base.Awake();

            platformDetector = ServiceLocator.Instance?.GetService<PlatformDetector>();

            SetupButtonListeners();
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

            SceneLoader.sceneToLoad = "GameScene";
            SceneManager.LoadScene("LoadingScene");

            AudioManager.Instance?.PlaySound("ButtonClick", AudioType.UI);
        }

        private async  void OnSettingsButtonClicked()
        {
            CoreLogger.Log("MainMenu", "Settings button clicked");

            // Було так EventBus.Emit("UI/ShowPanel", "SettingsPanel");
            AudioManager.Instance?.PlaySound("ButtonClick", AudioType.UI);
            await UIManager.Instance.ShowPanelByName("SettingsPanel");

        }

        private void OnExitButtonClicked()
        {
            CoreLogger.Log("MainMenu", "Exit button clicked");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            AudioManager.Instance?.PlaySound("ButtonClick", AudioType.UI);
        }

        private void UpdateVersionDisplay()
        {
            if (versionText != null)
            {
                string version = Application.version;
                if (platformDetector != null)
                    version += $" ({platformDetector.CurrentPlatform})";

                versionText.text = $"Версія {version}";
            }
        }

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
