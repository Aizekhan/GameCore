// Assets/Scripts/Managers/UIManager/UIManager.cs
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace GameCore.Core
{
    /// <summary>
    /// Уніфікований UI Manager: реагує на події, відкриває панелі по імені, керує fade.
    /// </summary>
    public class UIManager : MonoBehaviour, IService, IInitializable
    {
        public static UIManager Instance { get; private set; }

        [Header("Fade")]
        [SerializeField] private FadeController fadeController;

        private UIPanel _currentPanel;
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 80;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
              
            }
            else
            {
                Destroy(gameObject);
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public async Task Initialize()
        {
            EventBus.Subscribe("UI/ShowPanel", OnShowPanelEvent);
            EventBus.Subscribe("UI/HideAllPanels", _ => HideAll());

            _isInitialized = true;
            CoreLogger.Log("UI", "UIManager initialized");
            await Task.CompletedTask;
        }

        private async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu")
                await ShowPanelByName("MainMenuPanel");
            else if (scene.name == "GameScene")
                await ShowPanelByName("GameplayPanel");
            else if (scene.name == "LoadingScene")
                await ShowPanelByName("LoadingPanel");
        }

        public async Task<UIPanel> ShowPanelByName(string panelName, bool withFade = true)
        {
            var factory = ServiceLocator.Instance.GetService<UIPanelFactory>();

            if (withFade && fadeController != null)
                await fadeController.FadeToBlack();

            if (_currentPanel != null)
                _currentPanel.Hide();

            var panel = factory.CreatePanel(panelName);
            _currentPanel = panel;

            panel?.Show();

            if (withFade && fadeController != null)
                await fadeController.FadeFromBlack();

            EventBus.Emit("UI/PanelChanged", panelName);
            return panel;
        }

        public void HideAll()
        {
            _currentPanel?.Hide();
            _currentPanel = null;
            EventBus.Emit("UI/AllPanelsHidden", null);
        }

        public UIPanel GetCurrentPanel() => _currentPanel;

        private void OnShowPanelEvent(object data)
        {
            if (data is string panelName)
                ShowPanelByName(panelName).ConfigureAwait(false);
        }

        public async Task FadeToBlack(float duration)
        {
            if (fadeController != null)
            {
                fadeController.fadeDuration = duration;
                await fadeController.FadeToBlack();
            }
        }
    }
}
