using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using GameCore.Core;
using System.Threading.Tasks;
namespace GameCore.Core
{
    /// <summary>
    /// Фабрика для створення та налаштування UI кнопок
    /// </summary>
    public class UIButtonFactory : MonoBehaviour, IService
    {
        [SerializeField] private GameObject defaultButtonPrefab; // Базовий префаб кнопки
        public async Task Initialize()
        {
            CoreLogger.Log("UIButtonFactory initialized");
            await Task.CompletedTask;
        }
        // Створення інтерактивної кнопки
        public UIButton CreateButton(
            Transform parentPanel,
            string buttonName = "New Button",
            string category = "UserCreated")
        {
            // Перевірка наявності префабу
            if (defaultButtonPrefab == null)
            {
                CoreLogger.LogWarning("No default button prefab set in UIButtonFactory");
                return null;
            }

            // Створення нової кнопки з префабу
            GameObject newButtonObject = Instantiate(defaultButtonPrefab, parentPanel);
            newButtonObject.name = buttonName;

            UIButton newButton = newButtonObject.GetComponent<UIButton>();

            // Автоматична реєстрація в ButtonRegistry
            var registry = ServiceLocator.Instance?.GetService<UIButtonRegistry>();
            registry?.RegisterButton(newButton, category);

            // Налаштування тексту кнопки
            var textComponent = newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
         

            if (textComponent != null)
                textComponent.text = buttonName;

            return newButton;
        }

        // Створення кнопки з попередньо визначеною дією
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

        // Додаткові утиліти для спрощення створення кнопок
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