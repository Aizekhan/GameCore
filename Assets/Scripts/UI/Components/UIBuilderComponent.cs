using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace GameCore.Core
{
    /// <summary>
    /// ��������� ��� ���������������� ��������� UI �������� �� ��� ���������
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
        /// ���������� ������������ � ScriptableObject
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
        /// ������� �� ���������� UI ��������
        /// </summary>
        public void CreateUI()
        {
            // �������� �������� ������
            _buttonFactory = ServiceLocator.Instance?.GetService<UIButtonFactory>();
            _panelFactory = ServiceLocator.Instance?.GetService<UIPanelFactory>();
            _uiManager = ServiceLocator.Instance?.GetService<UIManager>();
            _panelRegistry = ServiceLocator.Instance?.GetService<UIPanelRegistry>();

            if (_buttonFactory == null || _panelFactory == null || _uiManager == null)
            {
                CoreLogger.LogError("UI", "Required UI services not found. Unable to create UI.");
                return;
            }

            // ������������ UI Root, ���� �� �������
            if (uiRoot == null)
            {
                uiRoot = transform;
            }

            // ����������� ������������, ���� �������
            if (!string.IsNullOrEmpty(configPath))
            {
                UILayoutConfig config = Resources.Load<UILayoutConfig>(configPath);
                if (config != null)
                {
                    // ��'������ ������������
                    runtimeButtons = CombineConfigs(runtimeButtons, config.buttons);
                    runtimePanels = CombineConfigs(runtimePanels, config.panels);
                }
            }

            // ��������� �����
            foreach (UIPanelConfig panelConfig in runtimePanels)
            {
                CreatePanel(panelConfig);
            }

            // ��������� ������
            foreach (UIButtonConfig buttonConfig in runtimeButtons)
            {
                CreateButton(buttonConfig);
            }

            CoreLogger.Log("UI", "UI creation completed successfully");
        }

        /// <summary>
        /// ��'���� ��� ������ ������������
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
        /// ������� ������ �� ��������������
        /// </summary>
        private void CreatePanel(UIPanelConfig config)
        {
            if (string.IsNullOrEmpty(config.panelName))
            {
                CoreLogger.LogWarning("UI", "Panel name is empty. Skipping.");
                return;
            }

            // ����������, �� ������ ��� ������������
            if (_panelRegistry != null && _panelRegistry.HasPanel(config.panelName))
            {
                CoreLogger.Log("UI", $"Panel '{config.panelName}' is already registered. Using existing panel.");
                // ��� ����� ������ ��� ��� ��������� ������������ �����, ���� �������
                return;
            }

            // ��������� ������ ����� �������
            UIPanel panel = _panelFactory.CreatePanel(config.panelName);

            if (panel == null)
            {
                CoreLogger.LogWarning("UI", $"Failed to create panel: {config.panelName}");
                return;
            }

            // ����������� �������
            panel.SetAnimationType(config.showAnimation, config.hideAnimation);
            panel.SetAnimationDurations(config.animationDuration, config.animationDuration);

            // ��������/��������� � ��������� �� �����������
            if (config.showAtStart)
            {
                panel.Show();
            }
            else
            {
                panel.Hide();
            }

            // �������� � �����, ���� ������� � �� ������������
            if (_panelRegistry != null && !_panelRegistry.HasPanel(config.panelName))
            {
                // ��� ���� ���� ��� ��� ���������, ��� � runtime �� �������� �� �������,
                // ������� ����� ����� ���� ����������� � �������� ��� ����������� ��� �����
                CoreLogger.Log("UI", $"Panel '{config.panelName}' created (and would be registered at edit time)");
            }
            else
            {
                CoreLogger.Log("UI", $"Panel '{config.panelName}' created");
            }
        }

        /// <summary>
        /// ������� ������ �� ��������������
        /// </summary>
        private void CreateButton(UIButtonConfig config)
        {
            if (string.IsNullOrEmpty(config.buttonText))
            {
                CoreLogger.LogWarning("UI", "Button text is empty. Skipping.");
                return;
            }

            Transform parent = uiRoot;

            // ���� ������� ���������� ������, ��������� ��
            if (!string.IsNullOrEmpty(config.parentPanelName))
            {
                UIPanel parentPanel = FindPanel(config.parentPanelName);
                if (parentPanel != null)
                {
                    parent = parentPanel.transform;
                }
                else
                {
                    // ���� ������ ������������, ��� �� ��������, ��������� ��
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

            // ��������� ������ ���������� ����
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
                // ����������� ������, ���� �������
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
        /// ��������� ������ �� �� ������
        /// </summary>
        private UIPanel FindPanel(string panelName)
        {
            // �������� ���������� ������ ����� � ����
            UIPanel[] panels = FindObjectsOfType<UIPanel>();
            UIPanel panel = panels.FirstOrDefault(p => p.name == panelName || p.name == panelName + "(Clone)");

            if (panel != null)
                return panel;

            // ���� �� ��������, ����������, �� ����� �������� ����� UIManager
            if (_uiManager != null)
            {
                // ��� ���� ���� ��� ��� ��������� ����� ����� UIManager, ���� �� ������� ���� ���������������
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
    /// ������������ ��� ������
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
    /// ������������ ��� �����
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
    /// ������������ ������ UI ��� ������������ � Resources
    /// </summary>
    [CreateAssetMenu(fileName = "UILayoutConfig", menuName = "GameCore/UI/Layout Config")]
    public class UILayoutConfig : ScriptableObject
    {
        public UIButtonConfig[] buttons = new UIButtonConfig[0];
        public UIPanelConfig[] panels = new UIPanelConfig[0];
    }
}