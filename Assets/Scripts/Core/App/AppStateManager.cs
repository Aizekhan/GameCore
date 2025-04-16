// Assets/Scripts/Core/App/AppStateManager.cs
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameCore.Core.EventSystem;
namespace GameCore.Core
{
    /// <summary>
    /// ���� ������� ������� �� ���������� �� ����.
    /// </summary>
    public class AppStateManager : MonoBehaviour, IService, IInitializable
    {
        public enum AppState
        {
            None,
            Initializing,
            Splash,
            MainMenu,
            Loading,
            Gameplay,
            Paused,
            GameOver
        }

        [Serializable]
        public class StateMapping
        {
            public AppState state;
            public string sceneName;
            public string defaultPanel;
        }

        [Header("State Configurations")]
        [SerializeField] private StateMapping[] stateMappings;

        private AppState _currentState = AppState.None;
        private AppState _previousState = AppState.None;

        // ��������� IInitializable
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 95; // ������� ��������

        // ��䳿 ��� �������
        public event Action<AppState, AppState> OnStateChanged;

        public AppState CurrentState => _currentState;
        public AppState PreviousState => _previousState;

        public async Task Initialize()
        {
            EventBus.Subscribe("App/ChangeState", OnChangeStateEvent);

            // ϳ������ �� ��䳿 �� SceneLoader
            EventBus.Subscribe("Scene/Loaded", OnSceneLoaded);

            CoreLogger.Log("APP", "AppStateManager initialized");
            IsInitialized = true;
            await Task.CompletedTask;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe("App/ChangeState", OnChangeStateEvent);
            EventBus.Unsubscribe("Scene/Loaded", OnSceneLoaded);
        }

        private void OnChangeStateEvent(object data)
        {
            if (data is AppState newState)
            {
                ChangeState(newState);
            }
            else if (data is string stateName && Enum.TryParse<AppState>(stateName, out var parsedState))
            {
                ChangeState(parsedState);
            }
        }

        private void OnSceneLoaded(object data)
        {
            // ����������� ��������� ���� �� ����� ����������� �����
            if (data is string sceneName)
            {
                foreach (var mapping in stateMappings)
                {
                    if (mapping.sceneName == sceneName)
                    {
                        // ���� ���� �� ������� ���� �� ��� ������������, ������������ ��������� ����
                        if (_currentState == AppState.Loading)
                        {
                            ChangeState(mapping.state, false);

                            // ���� � ������ �� �������������, �������� ��
                            if (!string.IsNullOrEmpty(mapping.defaultPanel))
                            {
                                EventBus.Emit("UI/ShowPanel", mapping.defaultPanel);
                            }
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// ����� �������� ���� �������.
        /// </summary>
        /// <param name="newState">����� ����</param>
        /// <param name="loadScene">�� ������������� �������� ����� �����������</param>
        public void ChangeState(AppState newState, bool loadScene = true)
        {
            if (_currentState == newState)
                return;

            var oldState = _currentState;
            _previousState = oldState;
            _currentState = newState;

            CoreLogger.Log("APP", $"State changed: {oldState} -> {newState}");

            // �������� �������� 䳿 ��� ��� �����
            HandleStateChange(oldState, newState);

            // ��������� ����
            OnStateChanged?.Invoke(oldState, newState);
            EventBus.Emit("App/StateChanged", new { OldState = oldState, NewState = newState });

            // ���� ������� ����������� �����, �� ������� ������ �����
            if (loadScene)
            {
                LoadSceneForState(newState);
            }
        }

        private void HandleStateChange(AppState oldState, AppState newState)
        {
            switch (newState)
            {
                case AppState.Paused:
                    Time.timeScale = 0f;
                    break;

                case AppState.Loading:
                    // ��������� 䳿 ��� ������� � ���� ������������
                    break;

                default:
                    // ³��������� ���������� timeScale, ���� �������� � �����
                    if (oldState == AppState.Paused)
                        Time.timeScale = 1f;
                    break;
            }
        }

        private void LoadSceneForState(AppState state)
        {
            foreach (var mapping in stateMappings)
            {
                if (mapping.state == state)
                {
                    // ����������, �� ����� �� ����������� ���
                    Scene currentScene = SceneManager.GetActiveScene();
                    if (currentScene.name != mapping.sceneName)
                    {
                        // ������������ ���� ������������ ����� ����� �����
                        _currentState = AppState.Loading;

                        // ������������� SceneLoader, ���� �� ���������
                        if (ServiceLocator.Instance.HasService<SceneLoader>())
                        {
                            var sceneLoader = ServiceLocator.Instance.GetService<SceneLoader>();
                            sceneLoader.LoadSceneAsync(mapping.sceneName).ConfigureAwait(false);
                        }
                        else
                        {
                            // ��������� ������ � ����������� ��������������
                            SceneManager.LoadSceneAsync(mapping.sceneName);
                        }
                    }
                    else
                    {
                        // ���� ����� ��� �����������, ������ �������� �������� ������
                        if (!string.IsNullOrEmpty(mapping.defaultPanel))
                        {
                            EventBus.Emit("UI/ShowPanel", mapping.defaultPanel);
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// ���������� �� ������������ �����.
        /// </summary>
        public void ReturnToPreviousState()
        {
            if (_previousState != AppState.None)
            {
                ChangeState(_previousState);
            }
        }
    }
}