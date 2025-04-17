// Assets/Scripts/Core/Services/ResourceManager/IResourceService.cs
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameCore.Core
{
    /// <summary>
    /// ��������� ��� ������ ��������� ���������.
    /// </summary>
    public interface IResourceService : IService
    {
        /// <summary>
        /// ��������� ��������� ������ ��������� ����.
        /// </summary>
        T Load<T>(ResourceManager.ResourceType resourceType, string resourceName) where T : Object;

        /// <summary>
        /// ���������� ��������� ������ ��������� ����.
        /// </summary>
        Task<T> LoadAsync<T>(ResourceManager.ResourceType resourceType, string resourceName) where T : Object;

        /// <summary>
        /// ���������� ������ � ���� ��'���� ��� ������� ����� ���������.
        /// </summary>
        GameObject Instantiate(ResourceManager.ResourceType resourceType, string resourceName,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null);

        /// <summary>
        /// ���������� ���������� ������ � ���� ��'���� ��� ������� ����� ���������.
        /// </summary>
        Task<GameObject> InstantiateAsync(ResourceManager.ResourceType resourceType, string resourceName,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null);

        /// <summary>
        /// ������� ��'��� �� ���� ��� ���������� ������������.
        /// </summary>
        void ReturnToPool(GameObject obj, string resourceName = null);

        /// <summary>
        /// ����� ��� �������.
        /// </summary>
        void ClearCache(ResourceManager.ResourceType? resourceType = null);

        /// <summary>
        /// ����� ���� ��'����.
        /// </summary>
        void ClearObjectPools();

        /// <summary>
        /// ������� ����� �� ������������ ������� � ��������� ���������� ��������.
        /// </summary>
        ResourceRequest<T> CreateRequest<T>(ResourceManager.ResourceType resourceType, string resourceName,
            bool instantiate = false, Vector3 position = default, Quaternion rotation = default,
            Transform parent = null) where T : Object;
    }
}