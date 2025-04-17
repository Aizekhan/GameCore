// Assets/Scripts/Core/Services/ResourceManager/IResourceService.cs
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameCore.Core
{
    /// <summary>
    /// Інтерфейс для сервісу управління ресурсами.
    /// </summary>
    public interface IResourceService : IService
    {
        /// <summary>
        /// Синхронно завантажує ресурс вказаного типу.
        /// </summary>
        T Load<T>(ResourceManager.ResourceType resourceType, string resourceName) where T : Object;

        /// <summary>
        /// Асинхронно завантажує ресурс вказаного типу.
        /// </summary>
        Task<T> LoadAsync<T>(ResourceManager.ResourceType resourceType, string resourceName) where T : Object;

        /// <summary>
        /// Інстанціює префаб з пулу об'єктів або створює новий екземпляр.
        /// </summary>
        GameObject Instantiate(ResourceManager.ResourceType resourceType, string resourceName,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null);

        /// <summary>
        /// Асинхронно інстанціює префаб з пулу об'єктів або створює новий екземпляр.
        /// </summary>
        Task<GameObject> InstantiateAsync(ResourceManager.ResourceType resourceType, string resourceName,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null);

        /// <summary>
        /// Повертає об'єкт до пулу для повторного використання.
        /// </summary>
        void ReturnToPool(GameObject obj, string resourceName = null);

        /// <summary>
        /// Очищає кеш ресурсів.
        /// </summary>
        void ClearCache(ResourceManager.ResourceType? resourceType = null);

        /// <summary>
        /// Очищає пули об'єктів.
        /// </summary>
        void ClearObjectPools();

        /// <summary>
        /// Створює запит на завантаження ресурсу з можливістю відстеження прогресу.
        /// </summary>
        ResourceRequest<T> CreateRequest<T>(ResourceManager.ResourceType resourceType, string resourceName,
            bool instantiate = false, Vector3 position = default, Quaternion rotation = default,
            Transform parent = null) where T : Object;
    }
}