// Assets/Scripts/UI/Buttons/UIButtonFactory.cs
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameCore.Core.EventSystem;

namespace GameCore.Core
{
    /// <summary>
    /// Фабрика для створення UI кнопок
    /// </summary>
    public class UIButtonFactory : MonoBehaviour, IService, IInitializable
    {
        [SerializeField] private string buttonPrefabPath = "UI/Prefabs/StandardButton";

        private GameObject _buttonPrefab;
        private UIButtonRegistry _buttonRegistry;
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 60;
        public void SetButtonPrefabPath(string path)
        {
            buttonPrefabPath = path;
        }

        public async Task Initialize()
        {
            // Завантажуємо префаб кнопки
            _buttonPrefab = Resources.Load<GameObject>(buttonPrefabPath);

            if (_buttonPrefab == null)
            {
                CoreLogger.LogWarning("UI", $"⚠️ Button prefab not found at path: {buttonPrefabPath}");
            }

            // Отримуємо реєстр кнопок
            _buttonRegistry = ServiceLocator.Instance.GetService<UIButtonRegistry>();

            if (_buttonRegistry == null)
            {
                CoreLogger.LogWarning("UI", "⚠️ UIButtonRegistry not found in ServiceLocator");
            }

            CoreLogger.Log("UI", "✅ UIButtonFactory initialized");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Створює нову кнопку на вказаному трансформі
        /// </summary>
        public UIButton CreateButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick = null, string category = "Default")
        {
            if (_buttonPrefab == null)
            {
                _buttonPrefab = Resources.Load<GameObject>(buttonPrefabPath);

                if (_buttonPrefab == null)
                {
                    CoreLogger.LogError("UI", $"❌ Button prefab not found at path: {buttonPrefabPath}");
                    return null;
                }
            }

            // Створюємо кнопку
            GameObject buttonObj = Instantiate(_buttonPrefab, parent);
            buttonObj.name = $"Button_{text.Replace(" ", "")}";

            // Встановлюємо текст
            TextMeshProUGUI tmpText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = text;
            }
            else
            {
                Text legacyText = buttonObj.GetComponentInChildren<Text>();
                if (legacyText != null)
                {
                    legacyText.text = text;
                }
            }

            // Отримуємо компонент UIButton
            UIButton uiButton = buttonObj.GetComponent<UIButton>();

            if (uiButton == null)
            {
                uiButton = buttonObj.AddComponent<UIButton>();
            }

            // Додаємо слухача
            if (onClick != null)
            {
                uiButton.Button.onClick.AddListener(onClick);
            }

            // Реєструємо в реєстрі
            if (_buttonRegistry != null)
            {
                _buttonRegistry.RegisterButton(uiButton, category);
            }

            return uiButton;
        }

        /// <summary>
        /// Створює кнопку, що відкриває панель
        /// </summary>
        public UIButton CreateOpenPanelButton(Transform parent, string text, string panelToOpen, string category = "Navigation")
        {
            UIButton button = CreateButton(parent, text, null, category);

            if (button != null)
            {
                button.showPanelName = panelToOpen;
            }

            return button;
        }

        /// <summary>
        /// Створює кнопку "назад"
        /// </summary>
        public UIButton CreateBackButton(Transform parent, string category = "Navigation")
        {
            UIButton button = CreateButton(parent, "Назад", null, category);

            if (button != null)
            {
                // Встановлюємо як кнопку "назад"
                button.GetType().GetField("isBackButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(button, true);
            }

            return button;
        }

        /// <summary>
        /// Створює кнопку для заданої дії
        /// </summary>
        public UIButton CreateActionButton(Transform parent, string text, string actionName, string category = "Actions")
        {
            UIButton button = CreateButton(parent, text, () => {
                // Відправляємо подію через EventBus
                EventBus.Emit($"UI/Action/{actionName}", text);
            }, category);

            return button;
        }
    }
}