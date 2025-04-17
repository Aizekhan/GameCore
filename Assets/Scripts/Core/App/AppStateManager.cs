// AppStateManager.cs — слухає подію "App/Ready" перед переходом у MainMenu
using System.Threading.Tasks;
using GameCore.Core.EventSystem;
using GameCore.Core.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore.Core
{
    public class AppStateManager : MonoBehaviour, IService, IInitializable
    {
        public enum AppState
        {
            Initializing,
            MainMenu,
            Loading,
            Gameplay,
            Paused
        }

        public AppState CurrentState { get; private set; } = AppState.Initializing;

        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 100;

        [SerializeField] private bool _autoLoadScene = true;

        public async Task Initialize()
        {
            CoreLogger.Log("STATE", "AppStateManager initialized via IService");
            EventBus.Subscribe("App/Ready", (_) =>
            {
                if (_autoLoadScene)
                {
                    ChangeState(AppState.MainMenu, true);
                }
            });

            await Task.CompletedTask;
        }

        public void ChangeState(AppState newState, bool loadScene = true)
        {
            if (CurrentState == newState) return;

            CoreLogger.Log("STATE", $"🔄 State changed: {CurrentState} → {newState}");
            CurrentState = newState;

            if (loadScene)
                LoadSceneForState(newState);
        }

        private void LoadSceneForState(AppState state)
        {
            string sceneName = state switch
            {
                AppState.MainMenu => "MainMenu",
                AppState.Loading => "LoadingScene",
                AppState.Gameplay => "GameScene",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(sceneName))
            {
                var sceneLoader = ServiceLocator.Instance?.GetService<SceneLoader>();
                if (sceneLoader != null)
                {
                    _ = sceneLoader.LoadSceneAsync(sceneName);
                }
                else
                {
                    CoreLogger.LogWarning("STATE", $"SceneLoader not found. Fallback to SceneManager.LoadScene");
                    SceneManager.LoadScene(sceneName);
                }
            }
        }
    }
}
