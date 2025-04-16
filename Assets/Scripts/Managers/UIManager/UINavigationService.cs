using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameCore.Core.EventSystem;
namespace GameCore.Core
{
    public class UINavigationService : MonoBehaviour, IService, IInitializable
    {
        private Stack<string> _panelHistory = new Stack<string>();
        private UIPanelFactory _panelFactory;
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 45;

        public async Task Initialize()
        {
            _panelFactory = ServiceLocator.Instance.GetService<UIPanelFactory>();

            if (_panelFactory == null)
            {
                CoreLogger.LogError("UI", "UINavigationService: UIPanelFactory not found");
                return;
            }

            EventBus.Subscribe("UI/PanelChanged", OnPanelChanged);
            EventBus.Subscribe("Input/Cancel", OnCancelPressed);

            CoreLogger.Log("UI", "UINavigationService initialized");
            await Task.CompletedTask;
        }

        private void OnPanelChanged(object data)
        {
            if (data is string panelName)
            {
                if (_panelHistory.Count == 0 || _panelHistory.Peek() != panelName)
                {
                    _panelHistory.Push(panelName);
                }

                if (ServiceLocator.Instance.HasService<InputSchemeManager>())
                {
                    var inputManager = ServiceLocator.Instance.GetService<InputSchemeManager>();
                    if (panelName != "GameplayPanel")
                        inputManager.SwitchToUI();
                    else
                        inputManager.SwitchToGameplay();
                }
            }
        }

        private async void OnCancelPressed(object _)
        {
            await GoBack();
        }

        public async Task GoBack()
        {
            if (_panelHistory.Count == 0)
            {
                if (SceneManager.GetActiveScene().name == "GameScene")
                {
                    EventBus.Emit("UI/ShowPanel", "ExitConfirmationPanel");
                    return;
                }

                EventBus.Emit("UI/HideAllPanels");
                return;
            }

            string previousPanelName = _panelHistory.Pop();

            if (previousPanelName == "MainMenuPanel" && SceneManager.GetActiveScene().name != "MainMenu")
            {
                await SceneLoader.Instance.LoadSceneAsync("MainMenu");
                return;
            }

            EventBus.Emit("UI/ShowPanel", previousPanelName);
        }


        public void ClearHistory()
        {
            _panelHistory.Clear();
        }
    }
}
