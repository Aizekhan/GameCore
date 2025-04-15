// Assets/Scripts/Managers/SceneManager/SceneLoader.cs
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace GameCore.Core
{
    public class SceneLoader : MonoBehaviour, IService, IInitializable
    {
        public static SceneLoader Instance { get; private set; }
        public static string sceneToLoad = "GameScene";

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private Slider loadingBar;

        [Header("Settings")]
        [SerializeField] private float minLoadTime = 0.5f;
        [SerializeField] private float fadeTime = 0.3f;

        private bool _isLoading = false;

        // IInitializable implementation
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 85;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Ініціалізація при старті LoadingScene
            StartCoroutine(LoadTargetScene());
            StartCoroutine(AnimateLoadingDots());
        }

        public async Task Initialize()
        {
            if (IsInitialized) return;

            // Підписка на події
            EventBus.Subscribe("Scene/LoadScene", OnLoadSceneEvent);

            IsInitialized = true;
            CoreLogger.Log("SCENE", "SceneLoader initialized");

            await Task.CompletedTask;
        }

        private void OnLoadSceneEvent(object data)
        {
            if (data is string sceneName)
            {
                LoadScene(sceneName);
            }
        }

        public void LoadScene(string sceneName)
        {
            if (_isLoading) return;

            sceneToLoad = sceneName;
            SceneManager.LoadScene("LoadingScene");
        }

        public async Task LoadSceneAsync(string sceneName, Action<float> onProgressUpdate = null)
        {
            if (_isLoading) return;

            _isLoading = true;
            float startTime = Time.time;

            // Сповіщаємо про початок завантаження
            EventBus.Emit("Scene/LoadingStarted", sceneName);

            // Отримуємо доступ до FadeController, якщо він є
            var uiManager = ServiceLocator.Instance?.GetService<UIManager>();

            // Затемнення, якщо UIManager доступний
            if (uiManager != null && fadeTime > 0)
            {
                await uiManager.FadeToBlack(fadeTime);
            }

            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;

            while (!asyncOp.isDone)
            {
                float progress = Mathf.Clamp01(asyncOp.progress / 0.9f);

                // Виклик колбеку з прогресом
                onProgressUpdate?.Invoke(progress);

                // Відправка події з прогресом
                EventBus.Emit("Scene/LoadingProgress", progress);

                // Почекати мінімальний час завантаження
                if (asyncOp.progress >= 0.9f && Time.time - startTime >= minLoadTime)
                {
                    asyncOp.allowSceneActivation = true;
                }

                await Task.Yield();
            }

            // Сповіщаємо про завершення завантаження
            EventBus.Emit("Scene/LoadingCompleted", sceneName);
            _isLoading = false;
        }

        IEnumerator LoadTargetScene()
        {
            yield return new WaitForSeconds(0.3f);

            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneToLoad);
            asyncOp.allowSceneActivation = false;

            while (asyncOp.progress < 0.9f)
            {
                loadingBar.value = asyncOp.progress;
                EventBus.Emit("Scene/LoadingProgress", asyncOp.progress);
                yield return null;
            }

            // Останній крок
            loadingBar.value = 1f;
            EventBus.Emit("Scene/LoadingProgress", 1f);
            yield return new WaitForSeconds(0.5f);
            asyncOp.allowSceneActivation = true;

            // Сповіщаємо про завершення завантаження
            EventBus.Emit("Scene/LoadingCompleted", sceneToLoad);
        }

        IEnumerator AnimateLoadingDots()
        {
            string baseText = "Завантаження";
            while (true)
            {
                for (int i = 0; i <= 3; i++)
                {
                    if (loadingText != null)
                        loadingText.text = baseText + new string('.', i);
                    yield return new WaitForSeconds(0.4f);
                }
            }
        }
    }
}