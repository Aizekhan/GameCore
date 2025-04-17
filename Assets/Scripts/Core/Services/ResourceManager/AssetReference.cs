// Assets/Scripts/Core/Services/ResourceManager/AssetReference.cs
using System;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameCore.Core
{
    /// <summary>
    /// ���� ��� �������� ��������� ����������� �� �������.
    /// ����������� ��������� � ��������� ������� �� �������.
    /// </summary>
    [Serializable]
    public class AssetReference<T> where T : Object
    {
        [SerializeField] private string resourcePath;
        [SerializeField] private ResourceManager.ResourceType resourceType;
        [SerializeField] private bool autoLoad = false;
        [SerializeField] private bool autoRelease = true;

        private T _asset;
        private bool _isLoading;
        private TaskCompletionSource<T> _loadingTask;

        /// <summary>
        /// ���� �� �������.
        /// </summary>
        public string ResourcePath => resourcePath;

        /// <summary>
        /// ��� �������.
        /// </summary>
        public ResourceManager.ResourceType ResourceType => resourceType;

        /// <summary>
        /// �� ���� ����������� ������.
        /// </summary>
        public bool IsLoaded => _asset != null;

        /// <summary>
        /// �� ����� ���������� ������������.
        /// </summary>
        public bool IsLoading => _isLoading;

        public AssetReference(string path, ResourceManager.ResourceType type, bool autoLoad = false, bool autoRelease = true)
        {
            resourcePath = path;
            resourceType = type;
            this.autoLoad = autoLoad;
            this.autoRelease = autoRelease;

            if (autoLoad)
            {
                _ = LoadAssetAsync();
            }
        }

        /// <summary>
        /// �������� ������������ ������. ���� ������ �� �� ����������� � autoLoad=true, �� ���� ������������ ���������.
        /// </summary>
        public T Asset
        {
            get
            {
                if (_asset != null)
                    return _asset;

                if (autoLoad && !_isLoading)
                {
                    LoadAsset();
                }

                return _asset;
            }
        }

        /// <summary>
        /// ��������� ��������� ������.
        /// </summary>
        public T LoadAsset()
        {
            if (_asset != null)
                return _asset;

            if (_isLoading)
            {
                Debug.LogWarning($"������ {resourcePath} ��� ������������� ����������");
                return null;
            }

            ResourceManager resourceManager = ServiceLocator.Instance?.GetService<ResourceManager>();
            if (resourceManager == null)
            {
                Debug.LogError("ResourceManager �� �������� � ServiceLocator");
                return null;
            }

            _asset = resourceManager.Load<T>(resourceType, resourcePath);
            return _asset;
        }

        /// <summary>
        /// ���������� ��������� ������.
        /// </summary>
        public async Task<T> LoadAssetAsync()
        {
            if (_asset != null)
                return _asset;

            if (_isLoading)
            {
                if (_loadingTask != null)
                {
                    return await _loadingTask.Task;
                }
            }

            _isLoading = true;
            _loadingTask = new TaskCompletionSource<T>();

            ResourceManager resourceManager = ServiceLocator.Instance?.GetService<ResourceManager>();
            if (resourceManager == null)
            {
                Debug.LogError("ResourceManager �� �������� � ServiceLocator");
                _isLoading = false;
                _loadingTask.SetResult(null);
                return null;
            }

            try
            {
                _asset = await resourceManager.LoadAsync<T>(resourceType, resourcePath);
                _loadingTask.SetResult(_asset);
                return _asset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"������� ������������ ������� {resourcePath}: {ex.Message}");
                _loadingTask.SetException(ex);
                throw;
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// ���������� ��'��� � �������.
        /// </summary>
        public GameObject Instantiate(Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            if (typeof(T) != typeof(GameObject))
            {
                Debug.LogError($"��������� ������������� ������ ���� {typeof(T).Name}. ҳ���� GameObject �����������.");
                return null;
            }

            ResourceManager resourceManager = ServiceLocator.Instance?.GetService<ResourceManager>();
            if (resourceManager == null)
            {
                Debug.LogError("ResourceManager �� �������� � ServiceLocator");
                return null;
            }

            return resourceManager.Instantiate(resourceType, resourcePath, position, rotation, parent);
        }

        /// <summary>
        /// ���������� ���������� ��'��� � �������.
        /// </summary>
        public async Task<GameObject> InstantiateAsync(Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            if (typeof(T) != typeof(GameObject))
            {
                Debug.LogError($"��������� ������������� ������ ���� {typeof(T).Name}. ҳ���� GameObject �����������.");
                return null;
            }

            ResourceManager resourceManager = ServiceLocator.Instance?.GetService<ResourceManager>();
            if (resourceManager == null)
            {
                Debug.LogError("ResourceManager �� �������� � ServiceLocator");
                return null;
            }

            return await resourceManager.InstantiateAsync(resourceType, resourcePath, position, rotation, parent);
        }

        /// <summary>
        /// ������� ������, ���� �� ��� ������������.
        /// </summary>
        public void ReleaseAsset()
        {
            if (_asset != null)
            {
                _asset = null;
                Resources.UnloadUnusedAssets();
            }
        }

        /// <summary>
        /// ����������� ��� ������� ��'����.
        /// </summary>
        public void OnDestroy()
        {
            if (autoRelease && _asset != null)
            {
                ReleaseAsset();
            }
        }
    }

    /// <summary>
    /// ���� ��� ��������� �� ������� ��'��� � ������������ �������.
    /// </summary>
    [Serializable]
    public class GameObjectReference : AssetReference<GameObject>
    {
        [SerializeField] private bool usePooling = true;
        [SerializeField] private int preloadCount = 0;

        public GameObjectReference(string path, ResourceManager.ResourceType type = ResourceManager.ResourceType.Prefab,
            bool autoLoad = false, bool autoRelease = true, bool usePooling = true, int preloadCount = 0)
            : base(path, type, autoLoad, autoRelease)
        {
            this.usePooling = usePooling;
            this.preloadCount = preloadCount;

            if (preloadCount > 0)
            {
                _ = PreloadAsync(preloadCount);
            }
        }

        /// <summary>
        /// ���������� ��������� ������� ������� ���������� � ���.
        /// </summary>
        public async Task PreloadAsync(int count)
        {
            if (!usePooling || count <= 0)
                return;

            ResourceManager resourceManager = ServiceLocator.Instance?.GetService<ResourceManager>();
            if (resourceManager == null)
            {
                Debug.LogError("ResourceManager �� �������� � ServiceLocator");
                return;
            }

            await resourceManager.PreloadAsync(ResourceType, ResourcePath, count);
        }

        /// <summary>
        /// ������� ��'��� �� ����.
        /// </summary>
        public void ReturnToPool(GameObject instance)
        {
            if (!usePooling || instance == null)
                return;

            ResourceManager resourceManager = ServiceLocator.Instance?.GetService<ResourceManager>();
            if (resourceManager == null)
            {
                Debug.LogError("ResourceManager �� �������� � ServiceLocator");
                GameObject.Destroy(instance);
                return;
            }

            resourceManager.ReturnToPool(instance, ResourcePath);
        }
    }
}