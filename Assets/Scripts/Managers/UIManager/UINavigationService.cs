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

            // ϳ������ �� ���� �������
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
                    // ������ � ������
                    if (_panelHistory.Count == 0 || _panelHistory.Peek() != currentPanel)
                    {
                        _panelHistory.Push(currentPanel);
                    }

                    // ���������� ����� �����
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
            // ���� ������ �����
            if (_panelHistory.Count == 0)
            {
                // ���� �� � GameScene � ������� GameplayPanel, ����������� ����� �� ���
                if (SceneManager.GetActiveScene().name == "GameScene" &&
                    _uiManager.GetCurrentPanel()?.PanelName == "GameplayPanel")
                {
                    // �������� ����� ������������ ������
                    await _uiManager.ShowPanelByName("ExitConfirmationPanel");
                    return;
                }

                // ������ ��������� ������� ������, �� ������� ��� � ��������
                _uiManager.HideAll();
                return;
            }

            // ����������� �� ���������� �����
            var previousPanel = _panelHistory.Pop();

            // ���� �� MainMenu, ���������� �� �� �� ���� MainMenu
            if (previousPanel.PanelName == "MainMenuPanel" &&
                SceneManager.GetActiveScene().name != "MainMenu")
            {
                // ����������� ����� MainMenu ������ ������ �����
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