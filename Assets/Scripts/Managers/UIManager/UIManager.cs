// Updated UIManager.cs to remove dependency on FindObjectOfType<Canvas>() and fix CS4014 warning
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using GameCore.Core.EventSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GameCore.Core
{
    public class UIManager : MonoBehaviour, IService, IInitializable
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Animation Settings")]
        [SerializeField] private UIPanelAnimationType defaultShowAnimation = UIPanelAnimationType.Fade;
        [SerializeField] private UIPanelAnimationType defaultHideAnimation = UIPanelAnimationType.Fade;
        [SerializeField] private float defaultAnimationDuration = 0.3f;

        public GameObject GlobalFadeOverlay { get; private set; }

        private UIPanel _currentPanel;
        private UIPanelAnimation _panelAnimation;
        private CanvasGroup _fadeCanvasGroup;
        private RectTransform _fadeRectTransform;
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 70;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private async  Task EnsureCanvasExists()
        {
            if (GameObject.Find("UICanvas_Root") == null)
            {
                var canvasGO = new GameObject("UICanvas_Root");
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                DontDestroyOnLoad(canvasGO);
                CoreLogger.Log("UI", "[UIManager] Canvas created from UIManager");
            }
            // Додайте цей рядок, щоб прибрати попередження
            await Task.CompletedTask;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public async Task Initialize()
        {
            EventBus.Subscribe("UI/ShowPanel", OnShowPanelEvent);
            EventBus.Subscribe("UI/HideAllPanels", async _ => await HideAll());
            EventBus.Subscribe("UI/FadeScreen", OnFadeScreenEvent);

            _panelAnimation = ServiceLocator.Instance.GetService<UIPanelAnimation>();

            if (_panelAnimation == null)
            {
                CoreLogger.LogWarning("UI", "UIPanelAnimation service not found. Creating one...");
                GameObject animationGO = new GameObject("UIPanelAnimation");
                animationGO.transform.SetParent(transform);
                _panelAnimation = animationGO.AddComponent<UIPanelAnimation>();
                await ServiceLocator.Instance.RegisterService<UIPanelAnimation>(_panelAnimation);
            }

            await EnsureCanvasExists();

            if (GlobalFadeOverlay == null)
                InitializeFadeOverlay();

            _isInitialized = true;
            CoreLogger.Log("UI", "✅ UIManager initialized");
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

        public async Task<UIPanel> ShowPanelByName(string panelName, bool withFade = true,
            UIPanelAnimationType showAnimationType = UIPanelAnimationType.Default)
        {
            if (string.IsNullOrEmpty(panelName))
            {
                CoreLogger.LogWarning("UI", "Attempt to show panel with empty name");
                return null;
            }

            var factory = ServiceLocator.Instance.GetService<UIPanelFactory>();
            if (factory == null)
            {
                CoreLogger.LogError("UI", "UIPanelFactory service not found");
                return null;
            }

            if (withFade)
                await FadeToBlack();

            if (_currentPanel != null)
                await _currentPanel.Hide();

            var panel = factory.CreatePanel(panelName);
            if (panel == null)
            {
                CoreLogger.LogError("UI", $"Failed to create panel: {panelName}");
                return null;
            }

            _currentPanel = panel;

            if (_panelAnimation != null)
            {
                UIPanelAnimationType animType = showAnimationType != UIPanelAnimationType.Default ?
                    showAnimationType : defaultShowAnimation;

                panel.SetAnimationType(animType, defaultHideAnimation);
                panel.SetAnimationDurations(defaultAnimationDuration, defaultAnimationDuration);
            }

            await panel.Show();

            if (withFade)
                await FadeFromBlack();

            EventBus.Emit("UI/PanelChanged", panelName);
            return panel;
        }

        public async Task HideAll()
        {
            if (_currentPanel != null)
            {
                await _currentPanel.Hide();
                _currentPanel = null;
            }

            await Task.Run(() => EventBus.Emit("UI/AllPanelsHidden", null));
        }

        public UIPanel GetCurrentPanel() => _currentPanel;

        private void OnShowPanelEvent(object data)
        {
            if (data is string panelName)
            {
                _ = ShowPanelByName(panelName);
            }
            else if (data is object[] args && args.Length >= 1 && args[0] is string name)
            {
                UIPanelAnimationType animType = UIPanelAnimationType.Default;
                if (args.Length >= 2 && args[1] is string animName &&
                    System.Enum.TryParse<UIPanelAnimationType>(animName, out UIPanelAnimationType parsedType))
                {
                    animType = parsedType;
                }

                _ = ShowPanelByName(name, true, animType);
            }
        }

        private void OnFadeScreenEvent(object data)
        {
            if (data is bool fadeIn && fadeIn)
                _ = FadeToBlack();
            else
                _ = FadeFromBlack();
        }

        public async Task FadeToBlack(float duration = -1)
        {
            if (duration < 0)
                duration = defaultAnimationDuration;

            if (GlobalFadeOverlay == null || _fadeCanvasGroup == null || _fadeRectTransform == null)
            {
                InitializeFadeOverlay();

                if (_fadeCanvasGroup == null)
                {
                    CoreLogger.LogError("UI", "Failed to create fade overlay");
                    return;
                }
            }

            _fadeCanvasGroup.blocksRaycasts = true;

            if (_panelAnimation != null)
            {
                await _panelAnimation.AnimateShow(_fadeRectTransform, _fadeCanvasGroup,
                    UIPanelAnimationType.Fade, duration, LeanTweenType.easeInOutQuad);
            }
            else
            {
                _fadeCanvasGroup.alpha = 1f;
            }
        }

        public async Task FadeFromBlack(float duration = -1)
        {
            if (duration < 0)
                duration = defaultAnimationDuration;

            if (_fadeCanvasGroup == null || _fadeRectTransform == null)
                return;

            if (_panelAnimation != null)
            {
                await _panelAnimation.AnimateHide(_fadeRectTransform, _fadeCanvasGroup,
                    UIPanelAnimationType.Fade, duration, LeanTweenType.easeInOutQuad);
            }
            else
            {
                _fadeCanvasGroup.alpha = 0f;
            }

            _fadeCanvasGroup.blocksRaycasts = false;
        }

        private void InitializeFadeOverlay()
        {
            var canvasGO = GameObject.Find("UICanvas_Root");
            if (canvasGO == null)
            {
                CoreLogger.LogWarning("UI", "UICanvas_Root not found. FadeOverlay will not be created.");
                return;
            }

            GlobalFadeOverlay = new GameObject("GlobalFadeOverlay");
            GlobalFadeOverlay.transform.SetParent(canvasGO.transform, false);

            _fadeRectTransform = GlobalFadeOverlay.AddComponent<RectTransform>();
            _fadeRectTransform.anchorMin = Vector2.zero;
            _fadeRectTransform.anchorMax = Vector2.one;
            _fadeRectTransform.sizeDelta = Vector2.zero;
            _fadeRectTransform.localScale = Vector3.one;

            _fadeCanvasGroup = GlobalFadeOverlay.AddComponent<CanvasGroup>();
            _fadeCanvasGroup.alpha = 0;
            _fadeCanvasGroup.blocksRaycasts = false;

            var image = GlobalFadeOverlay.AddComponent<Image>();
            image.color = Color.black;

            GlobalFadeOverlay.transform.SetAsLastSibling();
        }
    }
}