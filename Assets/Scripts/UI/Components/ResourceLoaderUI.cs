// Assets/Scripts/UI/Components/ResourceLoaderUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace GameCore.Core
{
    /// <summary>
    /// Компонент UI для відображення прогресу завантаження ресурсів.
    /// </summary>
    public class ResourceLoaderUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject loadingPanel;

        [Header("Settings")]
        [SerializeField] private bool showPercentage = true;
        [SerializeField] private string loadingFormat = "Завантаження: {0}";
        [SerializeField] private bool hideWhenDone = true;
        [SerializeField] private float hideDelay = 1.0f;

        [Header("Bundle Loading")]
        [SerializeField] private List<string> bundlesToLoad = new List<string>();
        [SerializeField] private bool loadOnStart = false;

        private Dictionary<string, float> _bundleProgress = new Dictionary<string, float>();
        private int _totalBundles;
        private int _loadedBundles;
        private bool _isLoading;

        private ResourceBundleManager _bundleManager;

        private void Start()
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }

            // Отримуємо менеджер бандлів
            _bundleManager = ServiceLocator.Instance?.GetService<ResourceBundleManager>();

            if (_bundleManager == null)
            {
                CoreLogger.LogError("UI", "❌ ResourceBundleManager не знайдено в ServiceLocator");
                return;
            }

            if (loadOnStart && bundlesToLoad.Count > 0)
            {
                LoadBundles(bundlesToLoad);
            }
        }

        /// <summary>
        /// Завантажує вказані бандли і відображає прогрес.
        /// </summary>
        public void LoadBundles(List<string> bundleIds)
        {
            if (_isLoading)
            {
                CoreLogger.LogWarning("UI", "⚠️ Вже відбувається завантаження");
                return;
            }

            if (bundleIds == null || bundleIds.Count == 0)
            {
                return;
            }

            _isLoading = true;
            _totalBundles = bundleIds.Count;
            _loadedBundles = 0;
            _bundleProgress.Clear();

            // Ініціалізуємо прогрес для кожного бандлу
            foreach (var bundleId in bundleIds)
            {
                _bundleProgress[bundleId] = 0f;
            }

            // Показуємо панель завантаження
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
            }

            // Скидаємо прогрес
            UpdateProgressUI(0f);

            // Завантажуємо кожен бандл з відстеженням прогресу
            foreach (var bundleId in bundleIds)
            {
                LoadBundle(bundleId);
            }
        }

        /// <summary>
        /// Завантажує один бандл з відстеженням прогресу.
        /// </summary>
        private async void LoadBundle(string bundleId)
        {
            try
            {
                // Встановлюємо статус
                if (statusText != null)
                {
                    statusText.text = string.Format(loadingFormat, bundleId);
                }

                // Завантажуємо бандл
                await _bundleManager.LoadBundleAsync(bundleId, (progress) =>
                {
                    // Оновлюємо прогрес для цього бандлу
                    _bundleProgress[bundleId] = progress;
                    UpdateTotalProgress();
                });

                // Збільшуємо лічильник завантажених бандлів
                _loadedBundles++;
                _bundleProgress[bundleId] = 1f;
                UpdateTotalProgress();

                // Перевіряємо, чи всі бандли завантажено
                if (_loadedBundles >= _totalBundles)
                {
                    OnLoadingComplete();
                }
            }
            catch (Exception ex)
            {
                CoreLogger.LogError("UI", $"❌ Помилка завантаження бандлу {bundleId}: {ex.Message}");

                // Встановлюємо статус помилки
                if (statusText != null)
                {
                    statusText.text = $"Помилка: {ex.Message}";
                }

                // Помічаємо бандл як завантажений, щоб не блокувати інші
                _loadedBundles++;
                _bundleProgress[bundleId] = 1f;
                UpdateTotalProgress();

                // Перевіряємо, чи всі бандли завантажено
                if (_loadedBundles >= _totalBundles)
                {
                    OnLoadingComplete();
                }
            }
        }

        /// <summary>
        /// Оновлює загальний прогрес завантаження.
        /// </summary>
        private void UpdateTotalProgress()
        {
            float totalProgress = 0f;

            // Обчислюємо середній прогрес для всіх бандлів
            foreach (var progress in _bundleProgress.Values)
            {
                totalProgress += progress;
            }

            totalProgress /= _totalBundles;

            // Оновлюємо UI
            UpdateProgressUI(totalProgress);
        }

        /// <summary>
        /// Оновлює елементи UI відповідно до прогресу.
        /// </summary>
        private void UpdateProgressUI(float progress)
        {
            // Оновлюємо прогрес-бар
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            // Оновлюємо текст прогресу
            if (progressText != null && showPercentage)
            {
                progressText.text = $"{Mathf.Round(progress * 100)}%";
            }
        }

        /// <summary>
        /// Викликається, коли всі бандли завантажено.
        /// </summary>
        private void OnLoadingComplete()
        {
            _isLoading = false;

            // Встановлюємо фінальний прогрес
            UpdateProgressUI(1f);

            // Оновлюємо статус
            if (statusText != null)
            {
                statusText.text = "Завантаження завершено";
            }

            // Приховуємо панель після затримки, якщо потрібно
            if (hideWhenDone && loadingPanel != null)
            {
                Invoke(nameof(HideLoadingPanel), hideDelay);
            }

            // Викликаємо подію завершення
            SendMessage("OnResourceLoadingComplete", SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// Приховує панель завантаження.
        /// </summary>
        private void HideLoadingPanel()
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Завантажує конкретний бандл.
        /// </summary>
        public void LoadSingleBundle(string bundleId)
        {
            LoadBundles(new List<string> { bundleId });
        }
    }
}