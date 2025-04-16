// Assets/Scripts/Core/App/AppStateManager.cs
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameCore.Core.EventSystem;
namespace GameCore.Core
{
    /// <summary>
    /// Керує станами додатку та переходами між ними.
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

        // Реалізація IInitializable
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 95; // Високий пріоритет

        // Події для підписки
        public event Action<AppState, AppState> OnStateChanged;

        public AppState CurrentState => _currentState;
        public AppState PreviousState => _previousState;

        public async Task Initialize()
        {
            EventBus.Subscribe("App/ChangeState", OnChangeStateEvent);

            // Підписка на події від SceneLoader
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
            // Автоматично визначаємо стан на основі завантаженої сцени
            if (data is string sceneName)
            {
                foreach (var mapping in stateMappings)
                {
                    if (mapping.sceneName == sceneName)
                    {
                        // Якщо стан не змінився явно під час завантаження, встановлюємо відповідний стан
                        if (_currentState == AppState.Loading)
                        {
                            ChangeState(mapping.state, false);

                            // Якщо є панель за замовчуванням, показуємо її
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
        /// Змінює поточний стан додатку.
        /// </summary>
        /// <param name="newState">Новий стан</param>
        /// <param name="loadScene">Чи завантажувати відповідну сцену автоматично</param>
        public void ChangeState(AppState newState, bool loadScene = true)
        {
            if (_currentState == newState)
                return;

            var oldState = _currentState;
            _previousState = oldState;
            _currentState = newState;

            CoreLogger.Log("APP", $"State changed: {oldState} -> {newState}");

            // Виконуємо додаткові дії при зміні стану
            HandleStateChange(oldState, newState);

            // Викликаємо подію
            OnStateChanged?.Invoke(oldState, newState);
            EventBus.Emit("App/StateChanged", new { OldState = oldState, NewState = newState });

            // Якщо потрібно завантажити сцену, що відповідає новому стану
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
                    // Специфічні дії при переході в стан завантаження
                    break;

                default:
                    // Відновлюємо нормальний timeScale, якщо виходимо з паузи
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
                    // Перевіряємо, чи сцена не завантажена вже
                    Scene currentScene = SceneManager.GetActiveScene();
                    if (currentScene.name != mapping.sceneName)
                    {
                        // Встановлюємо стан завантаження перед зміною сцени
                        _currentState = AppState.Loading;

                        // Використовуємо SceneLoader, якщо він доступний
                        if (ServiceLocator.Instance.HasService<SceneLoader>())
                        {
                            var sceneLoader = ServiceLocator.Instance.GetService<SceneLoader>();
                            sceneLoader.LoadSceneAsync(mapping.sceneName).ConfigureAwait(false);
                        }
                        else
                        {
                            // Резервний варіант з стандартним завантажувачем
                            SceneManager.LoadSceneAsync(mapping.sceneName);
                        }
                    }
                    else
                    {
                        // Якщо сцена вже завантажена, просто показуємо відповідну панель
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
        /// Повернення до попереднього стану.
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