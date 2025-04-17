// Assets/Scripts/Managers/UIManager/UINavigationService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameCore.Core.EventSystem;

namespace GameCore.Core
{
    /// <summary>
    /// Сервіс навігації для UI панелей з підтримкою анімацій та історії переходів
    /// </summary>
    public class UINavigationService : MonoBehaviour, IService, IInitializable
    {
        [Header("Navigation Settings")]
        [SerializeField] private bool useAnimationsForNavigation = true;
        [SerializeField] private UIPanelAnimationType forwardTransitionType = UIPanelAnimationType.SlideFromRight;
        [SerializeField] private UIPanelAnimationType backwardTransitionType = UIPanelAnimationType.SlideFromLeft;
        [SerializeField] private float transitionDuration = 0.3f;

        private Stack<NavigationEntry> _panelHistory = new Stack<NavigationEntry>();
        private UIPanelFactory _panelFactory;
        private UIPanelPool _panelPool;
        private UIPanelAnimation _panelAnimation;
        private UIManager _uiManager;

        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 45;

        // Структура для зберігання історії навігації з додатковими параметрами
        private struct NavigationEntry
        {
            public string PanelName { get; set; }
            public UIPanelAnimationType AnimationType { get; set; }
            public object NavigationData { get; set; }

            public NavigationEntry(string panelName, UIPanelAnimationType animationType = UIPanelAnimationType.Default, object data = null)
            {
                PanelName = panelName;
                AnimationType = animationType;
                NavigationData = data;
            }
        }

        public async Task Initialize()
        {
            // Першочергово очікуємо UIManager
            _uiManager = ServiceLocator.Instance.GetService<UIManager>();
            while (_uiManager == null)
            {
                await Task.Delay(50);
                _uiManager = ServiceLocator.Instance.GetService<UIManager>();
            }

            // Потім завантажуємо інші сервіси
            _panelFactory = ServiceLocator.Instance.GetService<UIPanelFactory>();
            _panelPool = ServiceLocator.Instance.GetService<UIPanelPool>();
            _panelAnimation = ServiceLocator.Instance.GetService<UIPanelAnimation>();

            // Підписуємось на події
            EventBus.Subscribe("UI/PanelChanged", OnPanelChanged);
            EventBus.Subscribe("Input/Cancel", OnCancelPressed);
            EventBus.Subscribe("UI/Navigate", OnNavigateEvent);

            CoreLogger.Log("UI", "🧭 UINavigationService initialized");
            IsInitialized = true;
        }

        /// <summary>
        /// Обробляє події зміни панелі
        /// </summary>
        private void OnPanelChanged(object data)
        {
            if (data is string panelName)
            {
                // Не додаємо панель до історії, якщо вона вже на вершині стеку
                if (_panelHistory.Count > 0 && _panelHistory.Peek().PanelName == panelName)
                    return;

                // Додаємо до історії
                _panelHistory.Push(new NavigationEntry(panelName, forwardTransitionType));

                // Переключаємо схему введення в залежності від типу панелі
                if (ServiceLocator.Instance.HasService<InputSchemeManager>())
                {
                    var inputManager = ServiceLocator.Instance.GetService<InputSchemeManager>();
                    if (panelName != "GameplayPanel")
                        inputManager.SwitchToUI();
                    else
                        inputManager.SwitchToGameplay();
                }
            }
        }

        /// <summary>
        /// Обробляє події навігації
        /// </summary>
        private void OnNavigateEvent(object data)
        {
            if (data is string panelName)
            {
                NavigateTo(panelName).ConfigureAwait(false);
            }
            else if (data is Dictionary<string, object> navData &&
                     navData.TryGetValue("panel", out object panelObj) &&
                     panelObj is string targetPanel)
            {
                // Перевіряємо, чи передано тип анімації
                UIPanelAnimationType animType = forwardTransitionType;
                if (navData.TryGetValue("animation", out object animObj) &&
                    animObj is string animName &&
                    System.Enum.TryParse<UIPanelAnimationType>(animName, out UIPanelAnimationType parsedType))
                {
                    animType = parsedType;
                }

                // Перевіряємо, чи передано додаткові дані
                object extraData = null;
                navData.TryGetValue("data", out extraData);

                NavigateTo(targetPanel, animType, extraData).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Обробляє натискання кнопки "Назад"
        /// </summary>
        private async void OnCancelPressed(object _)
        {
            await GoBack();
        }

        /// <summary>
        /// Переходить на вказану панель
        /// </summary>
        public async Task NavigateTo(string panelName, UIPanelAnimationType animationType = UIPanelAnimationType.Default, object navigationData = null)
        {
            if (_uiManager == null)
                return;

            if (animationType == UIPanelAnimationType.Default)
                animationType = forwardTransitionType;

            // Отримуємо поточну панель
            UIPanel currentPanel = _uiManager.GetCurrentPanel();

            // Отримуємо нову панель (з пулу або створюємо нову)
            UIPanel targetPanel;

            if (_panelPool != null)
            {
                // Використовуємо пул, якщо він доступний
                targetPanel = await _panelPool.GetPanel(panelName);
            }
            else if (_panelFactory != null)
            {
                // Інакше створюємо через фабрику
                targetPanel = _panelFactory.CreatePanel(panelName);
            }
            else
            {
                CoreLogger.LogError("UI", "No way to create panels - both UIPanelPool and UIPanelFactory are not available");
                return;
            }

            if (targetPanel == null)
            {
                CoreLogger.LogError("UI", $"Failed to create panel: {panelName}");
                return;
            }

            // Встановлюємо анімацію для нової панелі
            if (useAnimationsForNavigation && _panelAnimation != null)
            {
                targetPanel.SetAnimationType(animationType, backwardTransitionType);
                if (transitionDuration > 0)
                    targetPanel.SetAnimationDurations(transitionDuration, transitionDuration);
            }

            // Передаємо дані панелі, якщо вона підтримує IDataReceiver
            if (navigationData != null && targetPanel is IDataReceiver dataReceiver)
            {
                dataReceiver.ReceiveData(navigationData);
            }

            // Приховуємо поточну панель
            if (currentPanel != null)
            {
                await _uiManager.HidePanel(currentPanel);
            }

            // Показуємо нову панель
            await targetPanel.Show();

            // Оновлюємо поточну панель в UIManager
            typeof(UIManager).GetField("_currentPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_uiManager, targetPanel);

            // Додаємо цей перехід до історії (подія UI/PanelChanged відбудеться автоматично)
            EventBus.Emit("UI/PanelChanged", panelName);
        }

        /// <summary>
        /// Повертається на попередню панель
        /// </summary>
        public async Task GoBack()
        {
            // Видаляємо поточну панель з історії
            if (_panelHistory.Count > 0)
                _panelHistory.Pop();

            // Немає історії для повернення
            if (_panelHistory.Count == 0)
            {
                if (SceneManager.GetActiveScene().name == "GameScene")
                {
                    // Показуємо діалог підтвердження виходу
                    EventBus.Emit("UI/ShowPanel", "ExitConfirmationPanel");
                    return;
                }

                EventBus.Emit("UI/HideAllPanels", null);
                return;
            }

            // Отримуємо попередню панель з історії
            NavigationEntry previousEntry = _panelHistory.Peek();

            // Перевіряємо, чи потрібно змінити сцену
            if (previousEntry.PanelName == "MainMenuPanel" && SceneManager.GetActiveScene().name != "MainMenu")
            {
                // Очищаємо історію при зміні сцени
                ClearHistory();

                // Завантажуємо сцену головного меню
                if (ServiceLocator.Instance.HasService<SceneLoader>())
                {
                    var sceneLoader = ServiceLocator.Instance.GetService<SceneLoader>();
                    await sceneLoader.LoadSceneAsync("MainMenu");
                }
                else
                {
                    await SceneLoader.Instance.LoadSceneAsync("MainMenu");
                }
                return;
            }

            // Переходимо до попередньої панелі з анімацією "назад"
            await NavigateTo(previousEntry.PanelName, backwardTransitionType, previousEntry.NavigationData);

            // Видаляємо дублікат з історії, який був доданий при NavigateTo
            if (_panelHistory.Count > 0)
                _panelHistory.Pop();
        }

        /// <summary>
        /// Очищає історію навігації
        /// </summary>
        public void ClearHistory()
        {
            _panelHistory.Clear();
            CoreLogger.Log("UI", "Navigation history cleared");
        }

        /// <summary>
        /// Отримує список всіх панелей в історії навігації
        /// </summary>
        public string[] GetNavigationHistory()
        {
            var historyArray = new string[_panelHistory.Count];
            int index = 0;

            foreach (var entry in _panelHistory)
            {
                historyArray[index++] = entry.PanelName;
            }

            return historyArray;
        }
    }

    /// <summary>
    /// Інтерфейс для панелей, які можуть отримувати дані при навігації
    /// </summary>
    public interface IDataReceiver
    {
        void ReceiveData(object data);
    }
}