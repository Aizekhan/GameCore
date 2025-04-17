// Оновлений UIManager.cs з підтримкою пулу
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using GameCore.Core.EventSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace GameCore.Core
{
    public class UIManager : MonoBehaviour, IService, IInitializable
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Animation Settings")]
        [SerializeField] private UIPanelAnimationType defaultShowAnimation = UIPanelAnimationType.Fade;
        [SerializeField] private UIPanelAnimationType defaultHideAnimation = UIPanelAnimationType.Fade;
        [SerializeField] private float defaultAnimationDuration = 0.3f;

        [Header("UI Pooling Settings")]
        [SerializeField] private bool usePooling = true;

        public GameObject GlobalFadeOverlay { get; private set; }

        private UIPanel _currentPanel;
        private UIPanelAnimation _panelAnimation;
        private UIPanelPool _panelPool;
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
                
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private async Task EnsureCanvasExists()
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
            _panelPool = ServiceLocator.Instance.GetService<UIPanelPool>();

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
                CoreLogger.LogWarning("UI", "Спроба відобразити панель з порожнім іменем");
                return null;
            }

            UIPanel panel = null;

            if (withFade)
                await FadeToBlack();

            if (_currentPanel != null)
            {
                await HidePanel(_currentPanel);
            }

            // Спочатку перевіряємо чи є ResourceManager і пробуємо через нього
            var resourceManager = ServiceLocator.Instance?.GetService<ResourceManager>();
            if (resourceManager != null)
            {
                try
                {
                    // Отримуємо панель через ResourceManager
                    if (usePooling && _panelPool != null)
                    {
                        panel = await _panelPool.GetPanel(panelName);
                    }
                    else
                    {
                        // Використовуємо ResourceRequest для завантаження панелі
                        var request = resourceManager.CreateRequest<GameObject>(
                            ResourceManager.ResourceType.UI,
                            $"Panels/{panelName}",
                            true // instantiate
                        );

                        GameObject panelGo = await request.GetResultAsync();
                        if (panelGo != null)
                        {
                            // Забезпечуємо правильного батька для UI
                            panelGo.transform.SetParent(GameObject.Find("UICanvas_Root")?.transform);
                            panel = panelGo.GetComponent<UIPanel>();
                        }
                    }
                }
                catch (Exception ex)
                {
                    CoreLogger.LogWarning("UI", $"Помилка завантаження панелі через ResourceManager: {ex.Message}. Повертаємось до стандартного методу.");
                }
            }

            // Якщо не вдалося завантажити через ResourceManager, використовуємо старий метод
            if (panel == null)
            {
                if (usePooling && _panelPool != null)
                {
                    panel = await _panelPool.GetPanel(panelName);
                }
                else
                {
                    // Альтернативний варіант - створення через фабрику
                    var factory = ServiceLocator.Instance.GetService<UIPanelFactory>();
                    if (factory == null)
                    {
                        CoreLogger.LogError("UI", "Сервіс UIPanelFactory не знайдено");
                        return null;
                    }

                    panel = factory.CreatePanel(panelName);
                }
            }

            if (panel == null)
            {
                CoreLogger.LogError("UI", $"Не вдалося створити панель: {panelName}");
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

        public async Task HidePanel(UIPanel panel)
        {
            if (panel == null) return;

            await panel.Hide();

            // Повертаємо панель до пулу, якщо пулінг увімкнено
            if (usePooling && _panelPool != null)
            {
                _panelPool.ReturnToPool(panel);
            }
            else
            {
                // Знищуємо панель, якщо пулінг вимкнено
                if (panel != _currentPanel) // Не знищуємо поточну панель, це може статися в інших методах
                {
                    Destroy(panel.gameObject);
                }
            }

            // Якщо це була поточна панель, скидаємо посилання
            if (_currentPanel == panel)
            {
                _currentPanel = null;
            }
        }

        public async Task HideAll()
        {
            if (_currentPanel != null)
            {
                await HidePanel(_currentPanel);
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

        public void SetUsePooling(bool enabled)
        {
            usePooling = enabled;
            CoreLogger.Log("UI", $"UI Pooling is now {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Попереднє завантаження часто використовуваних панелей
        /// </summary>
        public async Task PreloadPanelsWithResourceManager(string[] panelNames, int countPerType = 1)
        {
            var resourceManager = ServiceLocator.Instance?.GetService<ResourceManager>();
            if (resourceManager == null || !usePooling || _panelPool == null)
                return;

            foreach (var panelName in panelNames)
            {
                try
                {
                    await resourceManager.PreloadAsync(
                        ResourceManager.ResourceType.UI,
                        $"Panels/{panelName}",
                        countPerType
                    );

                    CoreLogger.Log("UI", $"Попередньо завантажено {countPerType} екземплярів {panelName} через ResourceManager");
                }
                catch (Exception ex)
                {
                    CoreLogger.LogWarning("UI", $"Помилка попереднього завантаження {panelName}: {ex.Message}");
                }
            }
        }
        public async Task PreloadCommonPanels()
        {
            if (!usePooling || _panelPool == null)
                return;

            // Перевіряємо наявність ResourceManager
            var resourceManager = ServiceLocator.Instance?.GetService<ResourceManager>();
            bool useResourceManager = resourceManager != null;

            string[] commonPanels = {
        "MainMenuPanel",
        "LoadingPanel",
        "SettingsPanel",
        "ErrorPanel"
    };

            if (useResourceManager)
            {
                // Використовуємо ResourceManager для попереднього завантаження
                await PreloadPanelsWithResourceManager(commonPanels);
                CoreLogger.Log("UI", "Загальні панелі попередньо завантажені через ResourceManager");
            }
            else
            {
                // Використовуємо стандартний пул для попереднього завантаження
                await _panelPool.PreloadPanels(commonPanels);
                CoreLogger.Log("UI", "Загальні панелі попередньо завантажені через пул панелей");
            }
        }
    }
}