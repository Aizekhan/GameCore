// Assets/Scripts/UI/Buttons/UIButtonRegistry.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GameCore.Core
{
    /// <summary>
    /// Реєстр кнопок UI для їх централізованого управління
    /// </summary>
    public class UIButtonRegistry : MonoBehaviour, IService, IInitializable
    {
        private Dictionary<string, List<UIButton>> _buttonsByCategory;
        private Dictionary<string, UIButton> _buttonsById;
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 55;
        public async Task Initialize()
        {
            _buttonsByCategory = new Dictionary<string, List<UIButton>>();
            _buttonsById = new Dictionary<string, UIButton>();

            CoreLogger.Log("UI", "✅ UIButtonRegistry initialized");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Реєструє кнопку в системі
        /// </summary>
        public void RegisterButton(UIButton button, string category = "Default")
        {
            if (button == null) return;

            string buttonId = button.gameObject.name;

            // Додаємо до категорії
            if (!_buttonsByCategory.ContainsKey(category))
            {
                _buttonsByCategory[category] = new List<UIButton>();
            }

            if (!_buttonsByCategory[category].Contains(button))
            {
                _buttonsByCategory[category].Add(button);
            }

            // Додаємо до словнику за ID
            if (!_buttonsById.ContainsKey(buttonId))
            {
                _buttonsById[buttonId] = button;
            }
            else
            {
                // Якщо кнопка з таким ID вже є, оновлюємо посилання
                _buttonsById[buttonId] = button;
            }
        }

        /// <summary>
        /// Знаходить кнопку за її ID (ім'ям)
        /// </summary>
        public UIButton GetButtonById(string buttonId)
        {
            if (_buttonsById.TryGetValue(buttonId, out UIButton button))
            {
                return button;
            }

            return null;
        }

        /// <summary>
        /// Отримує всі кнопки в категорії
        /// </summary>
        public List<UIButton> GetButtonsByCategory(string category = "Default")
        {
            if (_buttonsByCategory.TryGetValue(category, out List<UIButton> buttons))
            {
                return buttons;
            }

            return new List<UIButton>();
        }

        /// <summary>
        /// Змінює стан інтерактивності для всіх кнопок у категорії
        /// </summary>
        public void SetCategoryInteractable(string category, bool interactable)
        {
            if (!_buttonsByCategory.ContainsKey(category)) return;

            foreach (var button in _buttonsByCategory[category])
            {
                if (button != null && button.Button != null)
                {
                    button.Button.interactable = interactable;
                }
            }
        }

        /// <summary>
        /// Додає дію до всіх кнопок у категорії
        /// </summary>
        public void AddActionToCategory(string category, UnityEngine.Events.UnityAction action)
        {
            if (!_buttonsByCategory.ContainsKey(category)) return;

            foreach (var button in _buttonsByCategory[category])
            {
                if (button != null)
                {
                    button.AddCustomAction(action);
                }
            }
        }

        /// <summary>
        /// Видаляє кнопку з реєстру
        /// </summary>
        public void UnregisterButton(UIButton button)
        {
            if (button == null) return;

            string buttonId = button.gameObject.name;

            // Видаляємо з словника за ID
            if (_buttonsById.ContainsKey(buttonId))
            {
                _buttonsById.Remove(buttonId);
            }

            // Видаляємо з категорій
            foreach (var category in _buttonsByCategory.Keys)
            {
                _buttonsByCategory[category].Remove(button);
            }
        }

        /// <summary>
        /// Скидає весь реєстр
        /// </summary>
        public void Reset()
        {
            _buttonsByCategory.Clear();
            _buttonsById.Clear();
        }
    }
}