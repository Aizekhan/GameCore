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
    public class UIButtonFactory : MonoBehaviour, IService
    {
        [SerializeField] private GameObject defaultButtonPrefab; // ������� ������ ������
        public async Task Initialize()
        {
            CoreLogger.Log("UIButtonFactory initialized");
            await Task.CompletedTask;
        }
        // ��������� ������������ ������
        public UIButton CreateButton(
            Transform parentPanel,
            string buttonName = "New Button",
            string category = "UserCreated")
        {
            // �������� �������� �������
            if (defaultButtonPrefab == null)
            {
                CoreLogger.LogWarning("No default button prefab set in UIButtonFactory");
                return null;
            }

            // ��������� ���� ������ � �������
            GameObject newButtonObject = Instantiate(defaultButtonPrefab, parentPanel);
            newButtonObject.name = buttonName;

            UIButton newButton = newButtonObject.GetComponent<UIButton>();

            // ����������� ��������� � ButtonRegistry
            var registry = ServiceLocator.Instance?.GetService<UIButtonRegistry>();
            registry?.RegisterButton(newButton, category);

            // ������������ ������ ������
            var textComponent = newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
         

            if (textComponent != null)
                textComponent.text = buttonName;

            return newButton;
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