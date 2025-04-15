// Assets/Scripts/Managers/UIManager/UINavigationService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

namespace GameCore.Core
{
    public class UINavigationService : MonoBehaviour, IService
    {
        private Stack<UIPanel> _panelHistory = new Stack<UIPanel>();
        private UIManager _uiManager;

        public async Task Initialize()
        {
            _uiManager = ServiceLocator.Instance.GetService<UIManager>();

            if (_uiManager == null)
            {
                CoreLogger.LogError("UI", "UINavigationService: UIManager not found");
                return;
            }

            // Підписка на зміну панелей
            EventBus.Subscribe("UI/PanelChanged", OnPanelChanged);
            EventBus.Subscribe("Input/Cancel", OnCancelPressed);

            CoreLogger.Log("UI", "UINavigationService initialized");
            await Task.CompletedTask;
        }

        private void OnPanelChanged(object data)
        {
            if (data is string panelName)
            {
                var currentPanel = _uiManager.GetCurrentPanel();
                if (currentPanel != null)
                {
                    // Додаємо в історію
                    if (_panelHistory.Count == 0 || _panelHistory.Peek() != currentPanel)
                    {
                        _panelHistory.Push(currentPanel);
                    }

                    // Перемикаємо режим вводу
                    var inputManager = ServiceLocator.Instance.GetService<InputSchemeManager>();
                    if (inputManager != null)
                    {
                        if (currentPanel.PanelName != "GameplayPanel")
                            inputManager.SwitchToUI();
                        else
                            inputManager.SwitchToGameplay();
                    }
                }
            }
        }

        private void OnCancelPressed(object _)
        {
            GoBack();
        }

        public async void GoBack()
        {
            // Якщо історія пуста
            if (_panelHistory.Count == 0)
            {
                // Якщо ми в GameScene і відкрита GameplayPanel, запропонуємо вихід із гри
                if (SceneManager.GetActiveScene().name == "GameScene" &&
                    _uiManager.GetCurrentPanel()?.PanelName == "GameplayPanel")
                {
                    // Показуємо діалог підтвердження виходу
                    await _uiManager.ShowPanelByName("ExitConfirmationPanel");
                    return;
                }

                // Інакше приховуємо поточну панель, що поверне нас у геймплей
                _uiManager.HideAll();
                return;
            }

            // Повертаємось до попередньої панелі
            var previousPanel = _panelHistory.Pop();

            // Якщо це MainMenu, перевіряємо чи ми на сцені MainMenu
            if (previousPanel.PanelName == "MainMenuPanel" &&
                SceneManager.GetActiveScene().name != "MainMenu")
            {
                // Завантажуємо сцену MainMenu замість показу панелі
                SceneLoader.Instance.LoadScene("MainMenu");
                return;
            }

            await _uiManager.ShowPanel(previousPanel.gameObject);
        }

        public void ClearHistory()
        {
            _panelHistory.Clear();
        }
    }
}