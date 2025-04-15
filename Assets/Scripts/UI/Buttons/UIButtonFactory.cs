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
    public class UIButtonFactory : MonoBehaviour, IService, IInitializable
    {
        // Шлях до префабу в Resources
        [SerializeField] private string buttonPrefabPath = "UI/Prefabs/StandardButton";

        // Публічна властивість для отримання шляху
        public string ButtonPrefabPath => buttonPrefabPath;

        // Реалізація інтерфейсу IInitializable
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 50;

        public async Task Initialize()
        {
            if (IsInitialized) return;

            // Перевірка наявності префабу
            var prefab = Resources.Load<GameObject>(buttonPrefabPath);
            if (prefab == null)
            {
                CoreLogger.LogWarning($"Button prefab not found at path: {buttonPrefabPath}");
            }

            CoreLogger.Log("UIButtonFactory initialized");
            IsInitialized = true;
            await Task.CompletedTask;
        }

        // Створення інтерактивної кнопки
        public UIButton CreateButton(
            Transform parentPanel,
            string buttonName = "New Button",
            string category = "UserCreated")
        {
            // Завантаження префабу з Resources
            GameObject buttonPrefab = Resources.Load<GameObject>(buttonPrefabPath);

            if (buttonPrefab == null)
            {
                CoreLogger.LogError($"Button prefab not found at path: {buttonPrefabPath}");
                return null;
            }

            // Створення нової кнопки з префабу
            GameObject newButtonObject = Instantiate(buttonPrefab, parentPanel);
            newButtonObject.name = buttonName;

            UIButton newButton = newButtonObject.GetComponent<UIButton>();

            if (newButton == null)
            {
                CoreLogger.LogError("UIButton component not found on prefab");
                Destroy(newButtonObject);
                return null;
            }

            // Автоматична реєстрація в ButtonRegistry
            var registry = ServiceLocator.Instance?.GetService<UIButtonRegistry>();
            registry?.RegisterButton(newButton, category);

            // Налаштування тексту кнопки
            var textComponent = newButtonObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            if (textComponent != null)
                textComponent.text = buttonName;

            return newButton;
        }

        // Метод для зміни шляху кнопки
        public void SetButtonPrefabPath(string path)
        {
            buttonPrefabPath = path;
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