using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using System.Threading.Tasks;
namespace GameCore.Core
{
    /// <summary>
    /// Централізований реєстр кнопок в додатку
    /// </summary>
    public class UIButtonRegistry : MonoBehaviour, IService, IInitializable
    {
        // Колекція всіх кнопок в проекті
        private Dictionary<string, List<UIButton>> _buttonCategories = new Dictionary<string, List<UIButton>>();
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 55;

        // Реєстрація кнопки
        public void RegisterButton(UIButton button, string category = "Default")
        {
            if (!_buttonCategories.ContainsKey(category))
            {
                _buttonCategories[category] = new List<UIButton>();
            }

            _buttonCategories[category].Add(button);

            // Логування реєстрації
            CoreLogger.Log($"Button registered: {button.name} in category {category}");
        }

        // Отримання всіх кнопок певної категорії
        public List<UIButton> GetButtonsByCategory(string category)
        {
            return _buttonCategories.ContainsKey(category)
                ? _buttonCategories[category]
                : new List<UIButton>();
        }

        // Ініціалізація сервісу
  

        public async Task Initialize()
        {
            if (IsInitialized) return;

            CoreLogger.Log("UIButtonRegistry initialized");
            IsInitialized = true;

            await Task.CompletedTask;
        }

        // Деініціалізація та очищення
        public void ClearCategory(string category)
        {
            if (_buttonCategories.ContainsKey(category))
            {
                _buttonCategories[category].Clear();
                CoreLogger.Log($"Cleared buttons in category: {category}");
            }
        }

        // Пошук кнопки за іменем
        public UIButton FindButtonByName(string buttonName, string category = "Default")
        {
            if (_buttonCategories.TryGetValue(category, out var buttons))
            {
                return buttons.Find(btn => btn.name == buttonName);
            }
            return null;
        }
    }
}