// Assets/Scripts/Core/Services/ResourceManager/ResourceBundleManager.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GameCore.Core.EventSystem;
using System.Linq;

namespace GameCore.Core
{
    /// <summary>
    /// Менеджер для управління різними бандлами ресурсів.
    /// </summary>
    public class ResourceBundleManager : MonoBehaviour, IService, IInitializable
    {
        [SerializeField] private List<ResourceBundle> preloadBundles = new List<ResourceBundle>();
        [SerializeField] private bool logBundleOperations = true;

        private Dictionary<string, ResourceBundle> _loadedBundles = new Dictionary<string, ResourceBundle>();
        private Dictionary<string, TaskCompletionSource<ResourceBundle>> _loadingTasks = new Dictionary<string, TaskCompletionSource<ResourceBundle>>();

        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 79; // Має бути трохи нижчий, ніж у ResourceManager

        private void Awake()
        {
            // Шукаємо всі бандли та тримаємо їх під рукою
            ResourceBundle[] allBundles = Resources.LoadAll<ResourceBundle>("");
            foreach (var bundle in allBundles)
            {
                _loadedBundles[bundle.BundleId] = bundle;
            }
        }

        public async Task Initialize()
        {
            // Підписуємося на події зміни сцени
            EventBus.Subscribe("Scene/LoadingCompleted", OnSceneChanged);

            // Завантажуємо всі бандли, які позначені для попереднього завантаження
            if (preloadBundles.Count > 0)
            {
                await PreloadBundlesAsync();
            }

            IsInitialized = true;
            CoreLogger.Log("RESOURCE", "✅ ResourceBundleManager initialized");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Асинхронно завантажує всі бандли з списку preloadBundles.
        /// </summary>
        private async Task PreloadBundlesAsync()
        {
            if (logBundleOperations)
            {
                CoreLogger.Log("RESOURCE", $"Почато попереднє завантаження {preloadBundles.Count} бандлів");
            }

            List<Task> loadingTasks = new List<Task>();
            foreach (var bundle in preloadBundles)
            {
                if (bundle != null)
                {
                    loadingTasks.Add(LoadBundleAsync(bundle.BundleId));
                }
            }

            await Task.WhenAll(loadingTasks);

            if (logBundleOperations)
            {
                CoreLogger.Log("RESOURCE", "✅ Попереднє завантаження бандлів завершено");
            }
        }

        /// <summary>
        /// Асинхронно завантажує бандл за ідентифікатором.
        /// </summary>
        public async Task<ResourceBundle> LoadBundleAsync(string bundleId, Action<float> progressCallback = null)
        {
            // Перевіряємо, чи бандл вже завантажено
            if (_loadedBundles.TryGetValue(bundleId, out ResourceBundle bundle) && bundle.IsLoaded)
            {
                return bundle;
            }

            // Перевіряємо, чи бандл зараз завантажується
            if (_loadingTasks.TryGetValue(bundleId, out TaskCompletionSource<ResourceBundle> existingTask))
            {
                return await existingTask.Task;
            }

            // Якщо бандл не знайдено у словнику, спробуємо завантажити його
            if (bundle == null)
            {
                bundle = Resources.Load<ResourceBundle>($"ResourceBundles/{bundleId}");
                if (bundle == null)
                {
                    CoreLogger.LogError("RESOURCE", $"❌ Бандл {bundleId} не знайдено");
                    return null;
                }
                _loadedBundles[bundleId] = bundle;
            }

            // Створюємо новий таск для завантаження
            var loadingTask = new TaskCompletionSource<ResourceBundle>();
            _loadingTasks[bundleId] = loadingTask;

            try
            {
                if (logBundleOperations)
                {
                    CoreLogger.Log("RESOURCE", $"Початок завантаження бандлу: {bundleId}");
                }

                // Завантажуємо бандл
                bool success = await bundle.LoadAsync(progressCallback);
                if (success)
                {
                    if (logBundleOperations)
                    {
                        CoreLogger.Log("RESOURCE", $"✅ Бандл {bundleId} успішно завантажено");
                    }

                    loadingTask.SetResult(bundle);
                    return bundle;
                }
                else
                {
                    CoreLogger.LogError("RESOURCE", $"❌ Помилка завантаження бандлу {bundleId}");
                    loadingTask.SetResult(null);
                    return null;
                }
            }
            catch (Exception ex)
            {
                CoreLogger.LogError("RESOURCE", $"❌ Виняток при завантаженні бандлу {bundleId}: {ex.Message}");
                loadingTask.SetException(ex);
                throw;
            }
            finally
            {
                _loadingTasks.Remove(bundleId);
            }
        }

        /// <summary>
        /// Вивантажує бандл за ідентифікатором.
        /// </summary>
        public void UnloadBundle(string bundleId, bool includeDependencies = true)
        {
            if (_loadedBundles.TryGetValue(bundleId, out ResourceBundle bundle) && bundle.IsLoaded)
            {
                if (logBundleOperations)
                {
                    CoreLogger.Log("RESOURCE", $"Вивантаження бандлу: {bundleId}");
                }

                bundle.Unload(includeDependencies);
            }
        }

        /// <summary>
        /// Вивантажує всі завантажені бандли.
        /// </summary>
        public void UnloadAllBundles()
        {
            foreach (var bundle in _loadedBundles.Values.Where(b => b.IsLoaded))
            {
                if (logBundleOperations)
                {
                    CoreLogger.Log("RESOURCE", $"Вивантаження бандлу: {bundle.BundleId}");
                }

                bundle.Unload(false); // Не рекурсивно, щоб уникнути повторного вивантаження
            }

            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Перевіряє, чи завантажено бандл.
        /// </summary>
        public bool IsBundleLoaded(string bundleId)
        {
            return _loadedBundles.TryGetValue(bundleId, out ResourceBundle bundle) && bundle.IsLoaded;
        }

        /// <summary>
        /// Отримує ресурс з бандлу.
        /// </summary>
        public T GetResourceFromBundle<T>(string bundleId, string resourceName) where T : UnityEngine.Object
        {
            if (!_loadedBundles.TryGetValue(bundleId, out ResourceBundle bundle))
            {
                CoreLogger.LogWarning("RESOURCE", $"⚠️ Бандл {bundleId} не знайдено");
                return null;
            }

            return bundle.GetResource<T>(resourceName);
        }

        /// <summary>
        /// Обробник події зміни сцени.
        /// </summary>
        private void OnSceneChanged(object data)
        {
            // Повідомляємо всі бандли про зміну сцени
            foreach (var bundle in _loadedBundles.Values)
            {
                bundle.OnSceneChanged();
            }
        }

        /// <summary>
        /// Додає бандл до списку попереднього завантаження.
        /// </summary>
        public void AddToPreloadList(ResourceBundle bundle)
        {
            if (bundle != null && !preloadBundles.Contains(bundle))
            {
                preloadBundles.Add(bundle);
            }
        }

        /// <summary>
        /// Видаляє бандл зі списку попереднього завантаження.
        /// </summary>
        public void RemoveFromPreloadList(ResourceBundle bundle)
        {
            if (bundle != null)
            {
                preloadBundles.Remove(bundle);
            }
        }
    }
}