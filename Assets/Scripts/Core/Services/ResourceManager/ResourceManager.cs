// Assets/Scripts/Core/Services/ResourceManager/ResourceManager.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
using GameCore.Core.EventSystem;

namespace GameCore.Core
{
    /// <summary>
    /// Централізований менеджер ресурсів для асинхронного завантаження, кешування та керування асетами.
    /// </summary>
    public class ResourceManager : MonoBehaviour, IResourceService, IInitializable
    {
        [Header("Налаштування")]
        [SerializeField] private bool useObjectPooling = true;
        [SerializeField] private int defaultPoolSize = 10;
        [SerializeField] private bool logResourceOperations = true;

        // Кеш для завантажених ресурсів
        private Dictionary<string, Object> _resourceCache = new Dictionary<string, Object>();

        // Пул об'єктів для швидкого доступу та перевикористання
        private Dictionary<string, List<GameObject>> _objectPools = new Dictionary<string, List<GameObject>>();

        // Кеш для шляхів до ресурсів
        private Dictionary<ResourceType, string> _resourcePaths = new Dictionary<ResourceType, string>();

        // Трансформ для об'єктів в пулі
        private Transform _poolRoot;

        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 80;

        /// <summary>
        /// Тип ресурсу для визначення шляху.
        /// </summary>
        public enum ResourceType
        {
            Prefab,
            UI,
            Audio,
            VFX,
            Materials,
            ScriptableObjects,
            Textures,
            Sprites,
            Models,
            Animations
        }

        private void Awake()
        {
            InitializeResourcePaths();
        }

        public async Task Initialize()
        {
            // Створюємо рут для пула об'єктів
            GameObject poolRootObj = new GameObject("[ObjectPool]");
            DontDestroyOnLoad(poolRootObj);
            _poolRoot = poolRootObj.transform;
            _poolRoot.SetParent(transform);

            // Підписуємося на події
            EventBus.Subscribe("Scene/LoadingCompleted", OnSceneLoadCompleted);

            IsInitialized = true;
            CoreLogger.Log("RESOURCE", "✅ ResourceManager initialized");
            await Task.CompletedTask;
        }

        private void InitializeResourcePaths()
        {
            // Встановлюємо базові шляхи для ресурсів
            _resourcePaths[ResourceType.Prefab] = "Prefabs";
            _resourcePaths[ResourceType.UI] = "UI";
            _resourcePaths[ResourceType.Audio] = "Audio";
            _resourcePaths[ResourceType.VFX] = "Effects";
            _resourcePaths[ResourceType.Materials] = "Materials";
            _resourcePaths[ResourceType.ScriptableObjects] = "ScriptableObjects";
            _resourcePaths[ResourceType.Textures] = "Textures";
            _resourcePaths[ResourceType.Sprites] = "Sprites";
            _resourcePaths[ResourceType.Models] = "Models";
            _resourcePaths[ResourceType.Animations] = "Animations";
        }

        /// <summary>
        /// Завантажує ресурс синхронно і кешує його.
        /// </summary>
        public T Load<T>(ResourceType type, string name) where T : Object
        {
            string path = GetResourcePath(type, name);
            string cacheKey = $"{path}_{typeof(T).Name}";

            // Перевіряємо кеш
            if (_resourceCache.TryGetValue(cacheKey, out Object cachedResource))
            {
                return cachedResource as T;
            }

            // Завантажуємо ресурс
            T resource = Resources.Load<T>(path);
            if (resource != null)
            {
                _resourceCache[cacheKey] = resource;
                if (logResourceOperations)
                {
                    CoreLogger.Log("RESOURCE", $"Завантажено: {path}");
                }
            }
            else
            {
                CoreLogger.LogWarning("RESOURCE", $"❌ Не вдалося завантажити ресурс: {path}");
            }

            return resource;
        }

        /// <summary>
        /// Завантажує ресурс асинхронно і кешує його.
        /// </summary>
        public async Task<T> LoadAsync<T>(ResourceType type, string name) where T : Object
        {
            string path = GetResourcePath(type, name);
            string cacheKey = $"{path}_{typeof(T).Name}";

            // Перевіряємо кеш
            if (_resourceCache.TryGetValue(cacheKey, out Object cachedResource))
            {
                return cachedResource as T;
            }

            // Асинхронне завантаження
            ResourceRequest request = Resources.LoadAsync<T>(path);
            while (!request.isDone)
            {
                await Task.Yield();
            }

            T resource = request.asset as T;
            if (resource != null)
            {
                _resourceCache[cacheKey] = resource;
                if (logResourceOperations)
                {
                    CoreLogger.Log("RESOURCE", $"Асинхронно завантажено: {path}");
                }
            }
            else
            {
                CoreLogger.LogWarning("RESOURCE", $"❌ Не вдалося асинхронно завантажити ресурс: {path}");
            }

            return resource;
        }

        /// <summary>
        /// Інстантує префаб, використовуючи пулінг об'єктів, якщо увімкнено.
        /// </summary>
        public GameObject Instantiate(ResourceType type, string name, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            string path = GetResourcePath(type, name);
            GameObject obj;

            if (useObjectPooling)
            {
                obj = GetFromPool(path, position, rotation, parent);
                if (obj != null)
                {
                    return obj;
                }
            }

            // Якщо не вдалося отримати з пула або пулінг вимкнено
            GameObject prefab = Load<GameObject>(type, name);
            if (prefab == null)
            {
                return null;
            }

            obj = Instantiate(prefab, position, rotation, parent);
            return obj;
        }

        /// <summary>
        /// Асинхронно інстантує префаб, використовуючи пулінг об'єктів, якщо увімкнено.
        /// </summary>
        public async Task<GameObject> InstantiateAsync(ResourceType type, string name, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            string path = GetResourcePath(type, name);

            if (useObjectPooling)
            {
                GameObject _obj = GetFromPool(path, position, rotation, parent);
                if (_obj != null)
                {
                    return _obj;
                }
            }

            // Якщо не вдалося отримати з пула або пулінг вимкнено
            GameObject prefab = await LoadAsync<GameObject>(type, name);
            if (prefab == null)
            {
                return null;
            }

            GameObject obj = Instantiate(prefab, position, rotation, parent);
            return obj;
        }

        /// <summary>
        /// Повертає об'єкт до пулу для подальшого використання.
        /// </summary>
        public void ReturnToPool(GameObject obj, string resourceName = null)
        {
            if (!useObjectPooling || obj == null)
            {
                if (obj != null) Destroy(obj);
                return;
            }

            // Отримуємо ім'я ресурсу
            string path = resourceName;
            if (string.IsNullOrEmpty(path))
            {
                PoolableObject poolable = obj.GetComponent<PoolableObject>();
                if (poolable != null)
                {
                    path = poolable.ResourcePath;
                }
                else
                {
                    Destroy(obj);
                    return;
                }
            }

            // Перевіряємо чи є пул для цього типу
            if (!_objectPools.ContainsKey(path))
            {
                _objectPools[path] = new List<GameObject>();
            }

            // Додаємо до пулу
            _objectPools[path].Add(obj);
            obj.transform.SetParent(_poolRoot);
            obj.SetActive(false);

            if (logResourceOperations)
            {
                CoreLogger.Log("RESOURCE", $"Повернуто до пулу: {path}");
            }
        }

        /// <summary>
        /// Попереднє завантаження ресурсів для пулу.
        /// </summary>
        public async Task PreloadAsync(ResourceType type, string name, int count)
        {
            if (!useObjectPooling || count <= 0)
            {
                return;
            }

            string path = GetResourcePath(type, name);
            GameObject prefab = await LoadAsync<GameObject>(type, name);
            if (prefab == null)
            {
                return;
            }

            if (!_objectPools.ContainsKey(path))
            {
                _objectPools[path] = new List<GameObject>(count);
            }

            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                obj.transform.SetParent(_poolRoot);

                // Додаємо компонент для ідентифікації
                PoolableObject poolable = obj.GetComponent<PoolableObject>();
                if (poolable == null)
                {
                    poolable = obj.AddComponent<PoolableObject>();
                    poolable.ResourcePath = path;
                }

                _objectPools[path].Add(obj);
            }

            if (logResourceOperations)
            {
                CoreLogger.Log("RESOURCE", $"Попередньо завантажено в пул {count} об'єктів: {path}");
            }
        }

        /// <summary>
        /// Очищає кеш для вказаного типу ресурсів або всіх, якщо тип не вказано.
        /// </summary>
        public void ClearCache(ResourceType? type = null)
        {
            if (type.HasValue)
            {
                string prefix = _resourcePaths[type.Value];
                List<string> keysToRemove = new List<string>();

                foreach (var key in _resourceCache.Keys)
                {
                    if (key.StartsWith(prefix))
                    {
                        keysToRemove.Add(key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _resourceCache.Remove(key);
                }

                if (logResourceOperations)
                {
                    CoreLogger.Log("RESOURCE", $"Очищено кеш для типу: {type}");
                }
            }
            else
            {
                _resourceCache.Clear();
                if (logResourceOperations)
                {
                    CoreLogger.Log("RESOURCE", "Повністю очищено кеш ресурсів");
                }
            }

            // Пропонуємо GC прибрати невикористані ресурси
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Очищає пул ресурсів і знищує всі об'єкти.
        /// </summary>
        public void ClearObjectPools()
        {
            foreach (var pool in _objectPools.Values)
            {
                foreach (var obj in pool)
                {
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
                pool.Clear();
            }
            _objectPools.Clear();

            if (logResourceOperations)
            {
                CoreLogger.Log("RESOURCE", "Очищено всі пули об'єктів");
            }
        }

        /// <summary>
        /// Отримує об'єкт з пулу, якщо він доступний.
        /// </summary>
        private GameObject GetFromPool(string path, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (!_objectPools.TryGetValue(path, out List<GameObject> pool) || pool.Count == 0)
            {
                return null;
            }

            // Беремо перший доступний об'єкт
            GameObject obj = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);

            if (obj == null)
            {
                return null; // Об'єкт був знищений
            }

            // Скидаємо трансформацію
            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            // Сповіщаємо об'єкт про відновлення з пулу
            PoolableObject poolable = obj.GetComponent<PoolableObject>();
            if (poolable != null)
            {
                poolable.OnPoolRetrieve();
            }

            if (logResourceOperations)
            {
                CoreLogger.Log("RESOURCE", $"Отримано з пулу: {path}");
            }

            return obj;
        }

        /// <summary>
        /// Отримує повний шлях до ресурсу.
        /// </summary>
        private string GetResourcePath(ResourceType type, string name)
        {
            string basePath = _resourcePaths.ContainsKey(type) ? _resourcePaths[type] : "";
            return string.IsNullOrEmpty(basePath) ? name : $"{basePath}/{name}";
        }

        /// <summary>
        /// Обробник події завершення завантаження сцени.
        /// </summary>
        private void OnSceneLoadCompleted(object data)
        {
            // Автоматично очищаємо ресурси при зміні сцени
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Встановлює кастомний шлях для типу ресурсу.
        /// </summary>
        public void SetResourcePath(ResourceType type, string path)
        {
            _resourcePaths[type] = path;
            if (logResourceOperations)
            {
                CoreLogger.Log("RESOURCE", $"Встановлено шлях для {type}: {path}");
            }
        }

        /// <summary>
        /// Створює запит на завантаження ресурсу з можливістю відстеження прогресу.
        /// </summary>
        public ResourceRequest<T> CreateRequest<T>(ResourceType resourceType, string resourceName,
            bool instantiate = false, Vector3 position = default, Quaternion rotation = default,
            Transform parent = null) where T : Object
        {
            var request = new ResourceRequest<T>(
                this,
                resourceType,
                resourceName,
                instantiate,
                position,
                rotation,
                parent);

            return request;
        }
    }

    /// <summary>
    /// Компонент для об'єктів в пулі, який дозволяє зберігати інформацію про ресурс
    /// і викликати події при взятті/поверненні до пулу.
    /// </summary>
    public class PoolableObject : MonoBehaviour
    {
        public string ResourcePath { get; set; }

        /// <summary>
        /// Викликається, коли об'єкт повертається з пулу.
        /// </summary>
        public virtual void OnPoolRetrieve()
        {
            // Скидаємо стан об'єкта до початкового
        }

        /// <summary>
        /// Викликається, коли об'єкт повертається до пулу.
        /// </summary>
        public virtual void OnPoolReturn()
        {
            // Очищаємо стан об'єкта перед поверненням до пулу
        }
    }
}