using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using GameCore.Core;
using System.Threading.Tasks;

namespace GameCore.Core
{
    /// <summary>
    /// ������� ��� ��������� �� ������������ UI ������
    /// </summary>
    public class UIButtonFactory : MonoBehaviour, IService, IInitializable
    {
        // ���� �� ������� � Resources
        [SerializeField] private string buttonPrefabPath = "UI/Prefabs/StandardButton";

        // ������� ���������� ��� ��������� �����
        public string ButtonPrefabPath => buttonPrefabPath;

        // ��������� ���������� IInitializable
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 50;

        public async Task Initialize()
        {
            if (IsInitialized) return;

            // �������� �������� �������
            var prefab = Resources.Load<GameObject>(buttonPrefabPath);
            if (prefab == null)
            {
                CoreLogger.LogWarning($"Button prefab not found at path: {buttonPrefabPath}");
            }

            CoreLogger.Log("UIButtonFactory initialized");
            IsInitialized = true;
            await Task.CompletedTask;
        }

        // ��������� ������������ ������
        public UIButton CreateButton(
            Transform parentPanel,
            string buttonName = "New Button",
            string category = "UserCreated")
        {
            // ������������ ������� � Resources
            GameObject buttonPrefab = Resources.Load<GameObject>(buttonPrefabPath);

            if (buttonPrefab == null)
            {
                CoreLogger.LogError($"Button prefab not found at path: {buttonPrefabPath}");
                return null;
            }

            // ��������� ���� ������ � �������
            GameObject newButtonObject = Instantiate(buttonPrefab, parentPanel);
            newButtonObject.name = buttonName;

            UIButton newButton = newButtonObject.GetComponent<UIButton>();

            if (newButton == null)
            {
                CoreLogger.LogError("UIButton component not found on prefab");
                Destroy(newButtonObject);
                return null;
            }

            // ����������� ��������� � ButtonRegistry
            var registry = ServiceLocator.Instance?.GetService<UIButtonRegistry>();
            registry?.RegisterButton(newButton, category);

            // ������������ ������ ������
            var textComponent = newButtonObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            if (textComponent != null)
                textComponent.text = buttonName;

            return newButton;
        }

        // ����� ��� ���� ����� ������
        public void SetButtonPrefabPath(string path)
        {
            buttonPrefabPath = path;
        }

        // ��������� ������ � ���������� ���������� 䳺�
        public UIButton CreateButtonWithAction(
            Transform parentPanel,
            string buttonName,
            UnityAction action,
            string category = "UserCreated")
        {
            var button = CreateButton(parentPanel, buttonName, category);

            if (button != null)
            {
                button.Button.onClick.AddListener(action);
            }

            return button;
        }

        // �������� ������ ��� ��������� ��������� ������
        public UIButton CreateLinkButton(Transform parentPanel, string url)
        {
            return CreateButtonWithAction(
                parentPanel,
                "Open Link",
                () => Application.OpenURL(url),
                "Links"
            );
        }

        public UIButton CreateSystemButton(
            Transform parentPanel,
            string actionName,
            Action systemAction)
        {
            return CreateButtonWithAction(
                parentPanel,
                actionName,
                () => systemAction?.Invoke(),
                "System"
            );
        }
    }
}