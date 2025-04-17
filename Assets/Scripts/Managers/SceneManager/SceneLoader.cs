// Assets/Scripts/Managers/SceneManager/SceneLoader.cs
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using GameCore.Core.EventSystem;

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

        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 75;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            EnsureUI();
            StartCoroutine(LoadTargetScene());
            StartCoroutine(AnimateLoadingDots());
        }

        private void EnsureUI()
        {
            if (loadingText == null)
            {
                var textGO = new GameObject("LoadingText");
                textGO.transform.SetParent(transform);
                loadingText = textGO.AddComponent<TextMeshProUGUI>();
                loadingText.text = "Завантаження";
                loadingText.alignment = TextAlignmentOptions.Center;
                loadingText.fontSize = 36;
            }

            if (loadingBar == null)
            {
                var sliderGO = new GameObject("LoadingBar");
                sliderGO.transform.SetParent(transform);
                loadingBar = sliderGO.AddComponent<Slider>();
                loadingBar.minValue = 0;
                loadingBar.maxValue = 1;
                loadingBar.value = 0;
            }
        }

        public async Task Initialize()
        {
            if (IsInitialized) return;

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

            EventBus.Emit("Scene/LoadingStarted", sceneName);

            var uiManager = ServiceLocator.Instance?.GetService<UIManager>();
            if (uiManager != null && fadeTime > 0)
            {
                await uiManager.FadeToBlack(fadeTime);
            }

            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;

            while (!asyncOp.isDone)
            {
                float progress = Mathf.Clamp01(asyncOp.progress / 0.9f);
                onProgressUpdate?.Invoke(progress);
                EventBus.Emit("Scene/LoadingProgress", progress);

                if (asyncOp.progress >= 0.9f && Time.time - startTime >= minLoadTime)
                {
                    asyncOp.allowSceneActivation = true;
                }

                await Task.Yield();
            }

            EventBus.Emit("Scene/LoadingCompleted", sceneName);
            EventBus.UnsubscribeCategory("Scene/");
            _isLoading = false;
        }

        IEnumerator LoadTargetScene()
        {
            yield return new WaitForSeconds(0.3f);

            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneToLoad);
            asyncOp.allowSceneActivation = false;

            while (asyncOp.progress < 0.9f)
            {
                if (loadingBar != null)
                {
                    loadingBar.value = asyncOp.progress;
                }
                EventBus.Emit("Scene/LoadingProgress", asyncOp.progress);
                yield return null;
            }

            if (loadingBar != null)
            {
                loadingBar.value = 1f;
            }
            EventBus.Emit("Scene/LoadingProgress", 1f);
            yield return new WaitForSeconds(0.5f);
            asyncOp.allowSceneActivation = true;

            EventBus.Emit("Scene/LoadingCompleted", sceneToLoad);
            EventBus.UnsubscribeCategory("Scene/");
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