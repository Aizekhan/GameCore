// Assets/Scripts/Core/Services/ResourceManager/ResourceBundle.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameCore.Core
{
    /// <summary>
    /// Клас для управління групами ресурсів, які потрібно завантажувати/вивантажувати разом.
    /// </summary>
    [CreateAssetMenu(fileName = "NewResourceBundle", menuName = "GameCore/ResourceBundle")]
    public class ResourceBundle : ScriptableObject
    {
        [Serializable]
        public class BundleEntry
        {
            public string resourceName;
            public ResourceManager.ResourceType resourceType;
            public bool preload = false;
            public int poolSize = 0;
            [HideInInspector] public Object loadedResource;
        }

        [Header("Bundle Settings")]
        [SerializeField] private string bundleId;
        [SerializeField] private bool loadOnDemand = true;
        [SerializeField] private bool unloadOnSceneChange = true;

        [Header("Resources")]
        [SerializeField] private List<BundleEntry> resources = new List<BundleEntry>();

        [Header("Dependencies")]
        [SerializeField] private List<ResourceBundle> dependencies = new List<ResourceBundle>();

        private bool _isLoaded = false;
        private bool _isLoading = false;
        private float _loadProgress = 0f;
        private TaskCompletionSource<bool> _loadingTask;

        /// <summary>
        /// Унікальний ідентифікатор бандлу.
        /// </summary>
        public string BundleId => string.IsNullOrEmpty(bundleId) ? name : bundleId;

        /// <summary>
        /// Чи завантажено бандл.
        /// </summary>
        public bool IsLoaded => _isLoaded;

        /// <summary>
        /// Чи відбувається завантаження бандлу.
        /// </summary>
        public bool IsLoading => _isLoading;

        /// <summary>
        /// Прогрес завантаження бандлу (0-1).
        /// </summary>
        public float LoadProgress => _loadProgress;

        /// <summary>
        /// Завантажені ресурси бандлу.
        /// </summary>
        public IReadOnlyList<BundleEntry> Resources => resources;

        /// <summary>
        /// Завантажує всі ресурси бандлу.
        /// </summary>
        public async Task<bool> LoadAsync(Action<float> progressCallback = null)
        {
            if (_isLoaded)
                return true;

            if (_isLoading)
            {
                if (_loadingTask != null)
                {
                    return await _loadingTask.Task;
                }
            }

            _isLoading = true;
            _loadingTask = new TaskCompletionSource<bool>();

            // Завантажуємо залежності
            foreach (var dependency in dependencies)
            {
                if (!dependency.IsLoaded && !dependency.IsLoading)
                {
                    await dependency.LoadAsync();
                }
            }

            // Отримуємо менеджер ресурсів
            ResourceManager resourceManager = ServiceLocator.Instance?.GetService<ResourceManager>();
            if (resourceManager == null)
            {
                Debug.LogError($"ResourceManager не знайдено в ServiceLocator для бандлу {BundleId}");
                _isLoading = false;
                _loadingTask.SetResult(false);
                return false;
            }

            try
            {
                float totalProgress = 0f;
                float progressStep = 1f / resources.Count;

                // Завантажуємо всі ресурси
                for (int i = 0; i < resources.Count; i++)
                {
                    var entry = resources[i];

                    // Завантажуємо ресурс
                    if (entry.resourceType == ResourceManager.ResourceType.Prefab && entry.preload && entry.poolSize > 0)
                    {
                        // Попередньо завантажуємо в пул
                        await resourceManager.PreloadAsync(entry.resourceType, entry.resourceName, entry.poolSize);
                        var resource = await resourceManager.LoadAsync<Object>(entry.resourceType, entry.resourceName);
                        entry.loadedResource = resource;
                    }
                    else
                    {
                        // Просто завантажуємо ресурс
                        var resource = await resourceManager.LoadAsync<Object>(entry.resourceType, entry.resourceName);
                        entry.loadedResource = resource;
                    }

                    // Оновлюємо прогрес
                    totalProgress += progressStep;
                    _loadProgress = totalProgress;
                    progressCallback?.Invoke(_loadProgress);
                }

                _isLoaded = true;
                _isLoading = false;
                _loadingTask.SetResult(true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Помилка завантаження бандлу {BundleId}: {ex.Message}");
                _isLoading = false;
                _loadingTask.SetException(ex);
                return false;
            }
        }

        /// <summary>
        /// Вивантажує всі ресурси бандлу.
        /// </summary>
        public void Unload(bool includeDependencies = true)
        {
            if (!_isLoaded)
                return;

            foreach (var entry in resources)
            {
                entry.loadedResource = null;
            }

            // Вивантажуємо залежності, якщо потрібно
            if (includeDependencies)
            {
                foreach (var dependency in dependencies)
                {
                    dependency.Unload(false); // не рекурсивно, щоб уникнути циклів
                }
            }

            UnityEngine.Resources.UnloadUnusedAssets();
            _isLoaded = false;
            _loadProgress = 0f;
        }

        /// <summary>
        /// Отримує ресурс з бандлу за ім'ям.
        /// </summary>
        public T GetResource<T>(string resourceName) where T : Object
        {
            if (!_isLoaded)
            {
                if (loadOnDemand)
                {
                    // Запускаємо завантаження, якщо потрібно завантажувати за вимогою
                    LoadAsync().ConfigureAwait(false);
                }
                else
                {
                    Debug.LogWarning($"Бандл {BundleId} не завантажено, а loadOnDemand = false");
                    return null;
                }
            }

            foreach (var entry in resources)
            {
                if (entry.resourceName == resourceName)
                {
                    return entry.loadedResource as T;
                }
            }

            return null;
        }

        /// <summary>
        /// Обробник події зміни сцени.
        /// </summary>
        public void OnSceneChanged()
        {
            if (_isLoaded && unloadOnSceneChange)
            {
                Unload(false); // не вивантажуємо залежності, вони вирішать самі
            }
        }

        /// <summary>
        /// Додає ресурс до бандлу.
        /// </summary>
        public void AddResource(string resourceName, ResourceManager.ResourceType resourceType, bool preload = false, int poolSize = 0)
        {
            if (resources.Exists(r => r.resourceName == resourceName && r.resourceType == resourceType))
            {
                Debug.LogWarning($"Ресурс {resourceName} вже додано до бандлу {BundleId}");
                return;
            }

            resources.Add(new BundleEntry
            {
                resourceName = resourceName,
                resourceType = resourceType,
                preload = preload,
                poolSize = poolSize
            });
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(bundleId))
            {
                bundleId = name;
            }
        }
#endif
    }
}