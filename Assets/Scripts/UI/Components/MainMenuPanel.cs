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

            // �������� ��������� �� ���������� ����� ServiceLocator
            menuController = FindFirstObjectByType<MainMenuController>();
            if (menuController == null)
            {
                CoreLogger.LogWarning("MainMenu", "MainMenuController not found!");
            }
            platformDetector = ServiceLocator.Instance.GetService<PlatformDetector>();

            // ϳ�������� ��������� ���� �� ������
            SetupButtonListeners();

            // ������������ ����� ���
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

            // �������� ���� ���� (���� �����������)
            AudioManager.Instance?.PlaySound("ButtonClick", AudioType.UI);
        }

        private void OnSettingsButtonClicked()
        {
            CoreLogger.Log("MainMenu", "Settings button clicked");

            // �������� ������ ����������� ����� EventBus
            EventBus.Emit("UI/ShowPanel", "SettingsPanel");

            // �������� ���� ����
            AudioManager.Instance?.PlaySound("ButtonClick", AudioType.UI);
        }

        private void OnExitButtonClicked()
        {
            CoreLogger.Log("MainMenu", "Exit button clicked");
            menuController?.OnExitPressed();

            // �������� ���� ����
            AudioManager.Instance?.PlaySound("ButtonClick", AudioType.UI);
        }

        private void UpdateVersionDisplay()
        {
            if (versionText != null)
            {
                string version = Application.version;

                // ������ ���������� ��� ���������, ���� � PlatformDetector
                if (platformDetector != null)
                {
                    version += $" ({platformDetector.CurrentPlatform})";
                }

                versionText.text = $"����� {version}";
            }
        }

        // ������������� ������ Show/Hide ��� ���������� ������
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