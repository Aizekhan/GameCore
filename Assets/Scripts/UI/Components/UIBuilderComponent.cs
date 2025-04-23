using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace GameCore.Core
{
    /// <summary>
    /// Компонент для автоматизованого створення UI елементів під час виконання
    /// </summary>
    [AddComponentMenu("GameCore/UI/UI Builder Component")]
    public class UIBuilderComponent : MonoBehaviour
    {
        [Header("UI Creation Settings")]
        [SerializeField] private Transform uiRoot;
        [SerializeField] private bool createAtStart = false;
        [SerializeField] private string configPath = "UI/Config/UILayoutConfig";

        [Header("Runtime Button Creation")]
        [SerializeField] private UIButtonConfig[] runtimeButtons;

        [Header("Runtime Panel Creation")]
        [SerializeField] private UIPanelConfig[] runtimePanels;

        private UIButtonFactory _buttonFactory;
        private UIPanelFactory _panelFactory;
        private UIManager _uiManager;
        private UIPanelRegistry _panelRegistry;

        private void Start()
        {
            if (createAtStart)
            {
                CreateUI();
            }
        }

        /// <summary>
        /// Встановлює конфігурацію з ScriptableObject
        /// </summary>
        public void SetConfiguration(UILayoutConfig config)
        {
            if (config != null)
            {
                runtimeButtons = config.buttons;
                runtimePanels = config.panels;
            }
        }

        /// <summary>
        /// Створює всі налаштовані UI елементи
        /// </summary>
        public void CreateUI()
        {
            // Отримуємо необхідні сервіси
            _buttonFactory = ServiceLocator.Instance?.GetService<UIButtonFactory>();
            _panelFactory = ServiceLocator.Instance?.GetService<UIPanelFactory>();
            _uiManager = ServiceLocator.Instance?.GetService<UIManager>();
            _panelRegistry = ServiceLocator.Instance?.GetService<UIPanelRegistry>();

            if (_buttonFactory == null || _panelFactory == null || _uiManager == null)
            {
                CoreLogger.LogError("UI", "Required UI services not found. Unable to create UI.");
                return;
            }

            // Встановлюємо UI Root, якщо не вказано
            if (uiRoot == null)
            {
                uiRoot = transform;
            }

            // Завантажуємо конфігурацію, якщо вказано
            if (!string.IsNullOrEmpty(configPath))
            {
                UILayoutConfig config = Resources.Load<UILayoutConfig>(configPath);
                if (config != null)
                {
                    // Об'єднуємо конфігурації
                    runtimeButtons = CombineConfigs(runtimeButtons, config.buttons);
                    runtimePanels = CombineConfigs(runtimePanels, config.panels);
                }
            }

            // Створюємо панелі
            foreach (UIPanelConfig panelConfig in runtimePanels)
            {
                CreatePanel(panelConfig);
            }

            // Створюємо кнопки
            foreach (UIButtonConfig buttonConfig in runtimeButtons)
            {
                CreateButton(buttonConfig);
            }

            CoreLogger.Log("UI", "UI creation completed successfully");
        }

        /// <summary>
        /// Об'єднує два масиви конфігурацій
        /// </summary>
        private T[] CombineConfigs<T>(T[] array1, T[] array2)
        {
            if (array1 == null || array1.Length == 0)
                return array2;
            if (array2 == null || array2.Length == 0)
                return array1;

            return array1.Concat(array2).ToArray();
        }

        /// <summary>
        /// Створює панель за налаштуваннями
        /// </summary>
        private void CreatePanel(UIPanelConfig config)
        {
            if (string.IsNullOrEmpty(config.panelName))
            {
                CoreLogger.LogWarning("UI", "Panel name is empty. Skipping.");
                return;
            }

            // Перевіряємо, чи панель вже зареєстрована
            if (_panelRegistry != null && _panelRegistry.HasPanel(config.panelName))
            {
                CoreLogger.Log("UI", $"Panel '{config.panelName}' is already registered. Using existing panel.");
                // Тут можна додати код для отримання зареєстрованої панелі, якщо потрібно
                return;
            }

            // Створюємо панель через фабрику
            UIPanel panel = _panelFactory.CreatePanel(config.panelName);

            if (panel == null)
            {
                CoreLogger.LogWarning("UI", $"Failed to create panel: {config.panelName}");
                return;
            }

            // Налаштовуємо анімацію
            panel.SetAnimationType(config.showAnimation, config.hideAnimation);
            panel.SetAnimationDurations(config.animationDuration, config.animationDuration);

            // Показуємо/приховуємо в залежності від налаштувань
            if (config.showAtStart)
            {
                panel.Show();
            }
            else
            {
                panel.Hide();
            }

            // Реєструємо у реєстрі, якщо потрібно і не зареєстровано
            if (_panelRegistry != null && !_panelRegistry.HasPanel(config.panelName))
            {
                // Тут може бути код для реєстрації, але в runtime це зазвичай не потрібно,
                // оскільки панелі мають бути зареєстровані в редакторі або автоматично при старті
                CoreLogger.Log("UI", $"Panel '{config.panelName}' created (and would be registered at edit time)");
            }
            else
            {
                CoreLogger.Log("UI", $"Panel '{config.panelName}' created");
            }
        }

        /// <summary>
        /// Створює кнопку за налаштуваннями
        /// </summary>
        private void CreateButton(UIButtonConfig config)
        {
            if (string.IsNullOrEmpty(config.buttonText))
            {
                CoreLogger.LogWarning("UI", "Button text is empty. Skipping.");
                return;
            }

            Transform parent = uiRoot;

            // Якщо вказано батьківську панель, знаходимо її
            if (!string.IsNullOrEmpty(config.parentPanelName))
            {
                UIPanel parentPanel = FindPanel(config.parentPanelName);
                if (parentPanel != null)
                {
                    parent = parentPanel.transform;
                }
                else
                {
                    // Якщо панель зареєстрована, але не створена, створюємо її
                    if (_panelRegistry != null && _panelRegistry.HasPanel(config.parentPanelName))
                    {
                        UIPanel newPanel = _panelFactory.CreatePanel(config.parentPanelName);
                        if (newPanel != null)
                        {
                            parent = newPanel.transform;
                        }
                    }
                    else
                    {
                        CoreLogger.LogWarning("UI", $"Parent panel '{config.parentPanelName}' not found. Using default parent.");
                    }
                }
            }

            // Створюємо кнопку відповідного типу
            UIButton button;

            if (config.isBackButton)
            {
                button = _buttonFactory.CreateBackButton(parent, config.category);
            }
            else if (!string.IsNullOrEmpty(config.targetPanelName))
            {
                button = _buttonFactory.CreateOpenPanelButton(parent, config.buttonText, config.targetPanelName, config.category);
            }
            else if (!string.IsNullOrEmpty(config.actionName))
            {
                button = _buttonFactory.CreateActionButton(parent, config.buttonText, config.actionName, config.category);
            }
            else
            {
                button = _buttonFactory.CreateButton(parent, config.buttonText, null, config.category);
            }

            if (button != null)
            {
                // Позиціонуємо кнопку, якщо вказано
                if (config.position != Vector2.zero)
                {
                    RectTransform rectTransform = button.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.anchoredPosition = config.position;
                    }
                }

                CoreLogger.Log("UI", $"Button '{config.buttonText}' created");
            }
        }

        /// <summary>
        /// Знаходить панель за її назвою
        /// </summary>
        private UIPanel FindPanel(string panelName)
        {
            // Спочатку перевіряємо активні панелі в сцені
            UIPanel[] panels = FindObjectsOfType<UIPanel>();
            UIPanel panel = panels.FirstOrDefault(p => p.name == panelName || p.name == panelName + "(Clone)");

            if (panel != null)
                return panel;

            // Якщо не знайдено, перевіряємо, чи можна отримати через UIManager
            if (_uiManager != null)
            {
                // Тут може бути код для отримання панелі через UIManager, якщо він підтримує таку функціональність
                UIPanel currentPanel = _uiManager.GetCurrentPanel();
                if (currentPanel != null && (currentPanel.name == panelName || currentPanel.name == panelName + "(Clone)"))
                {
                    return currentPanel;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Конфігурація для кнопки
    /// </summary>
    [System.Serializable]
    public class UIButtonConfig
    {
        public string buttonText = "Button";
        public string category = "Default";
        public bool isBackButton = false;
        public string targetPanelName = "";
        public string actionName = "";
        public string parentPanelName = "";
        public Vector2 position = Vector2.zero;
    }

    /// <summary>
    /// Конфігурація для панелі
    /// </summary>
    [System.Serializable]
    public class UIPanelConfig
    {
        public string panelName = "Panel";
        public UIPanelAnimationType showAnimation = UIPanelAnimationType.Fade;
        public UIPanelAnimationType hideAnimation = UIPanelAnimationType.Fade;
        public float animationDuration = 0.3f;
        public bool showAtStart = false;
    }

    /// <summary>
    /// Конфігурація макету UI для завантаження з Resources
    /// </summary>
    [CreateAssetMenu(fileName = "UILayoutConfig", menuName = "GameCore/UI/Layout Config")]
    public class UILayoutConfig : ScriptableObject
    {
        public UIButtonConfig[] buttons = new UIButtonConfig[0];
        public UIPanelConfig[] panels = new UIPanelConfig[0];
    }
}