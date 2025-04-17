// Assets/Scripts/Core/Services/ResourceManager/ResourceBundle.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameCore.Core
{
    /// <summary>
    /// ���� ��� ��������� ������� �������, �� ������� �������������/������������� �����.
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
        /// ��������� ������������� ������.
        /// </summary>
        public string BundleId => string.IsNullOrEmpty(bundleId) ? name : bundleId;

        /// <summary>
        /// �� ����������� �����.
        /// </summary>
        public bool IsLoaded => _isLoaded;

        /// <summary>
        /// �� ���������� ������������ ������.
        /// </summary>
        public bool IsLoading => _isLoading;

        /// <summary>
        /// ������� ������������ ������ (0-1).
        /// </summary>
        public float LoadProgress => _loadProgress;

        /// <summary>
        /// ���������� ������� ������.
        /// </summary>
        public IReadOnlyList<BundleEntry> Resources => resources;

        /// <summary>
        /// ��������� �� ������� ������.
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

            // ����������� ���������
            foreach (var dependency in dependencies)
            {
                if (!dependency.IsLoaded && !dependency.IsLoading)
                {
                    await dependency.LoadAsync();
                }
            }

            // �������� �������� �������
            ResourceManager resourceManager = ServiceLocator.Instance?.GetService<ResourceManager>();
            if (resourceManager == null)
            {
                Debug.LogError($"ResourceManager �� �������� � ServiceLocator ��� ������ {BundleId}");
                _isLoading = false;
                _loadingTask.SetResult(false);
                return false;
            }

            try
            {
                float totalProgress = 0f;
                float progressStep = 1f / resources.Count;

                // ����������� �� �������
                for (int i = 0; i < resources.Count; i++)
                {
                    var entry = resources[i];

                    // ����������� ������
                    if (entry.resourceType == ResourceManager.ResourceType.Prefab && entry.preload && entry.poolSize > 0)
                    {
                        // ���������� ����������� � ���
                        await resourceManager.PreloadAsync(entry.resourceType, entry.resourceName, entry.poolSize);
                        var resource = await resourceManager.LoadAsync<Object>(entry.resourceType, entry.resourceName);
                        entry.loadedResource = resource;
                    }
                    else
                    {
                        // ������ ����������� ������
                        var resource = await resourceManager.LoadAsync<Object>(entry.resourceType, entry.resourceName);
                        entry.loadedResource = resource;
                    }

                    // ��������� �������
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
                Debug.LogError($"������� ������������ ������ {BundleId}: {ex.Message}");
                _isLoading = false;
                _loadingTask.SetException(ex);
                return false;
            }
        }

        /// <summary>
        /// ��������� �� ������� ������.
        /// </summary>
        public void Unload(bool includeDependencies = true)
        {
            if (!_isLoaded)
                return;

            foreach (var entry in resources)
            {
                entry.loadedResource = null;
            }

            // ����������� ���������, ���� �������
            if (includeDependencies)
            {
                foreach (var dependency in dependencies)
                {
                    dependency.Unload(false); // �� ����������, ��� �������� �����
                }
            }

            UnityEngine.Resources.UnloadUnusedAssets();
            _isLoaded = false;
            _loadProgress = 0f;
        }

        /// <summary>
        /// ������ ������ � ������ �� ��'��.
        /// </summary>
        public T GetResource<T>(string resourceName) where T : Object
        {
            if (!_isLoaded)
            {
                if (loadOnDemand)
                {
                    // ��������� ������������, ���� ������� ������������� �� �������
                    LoadAsync().ConfigureAwait(false);
                }
                else
                {
                    Debug.LogWarning($"����� {BundleId} �� �����������, � loadOnDemand = false");
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
        /// �������� ��䳿 ���� �����.
        /// </summary>
        public void OnSceneChanged()
        {
            if (_isLoaded && unloadOnSceneChange)
            {
                Unload(false); // �� ����������� ���������, ���� ������� ���
            }
        }

        /// <summary>
        /// ���� ������ �� ������.
        /// </summary>
        public void AddResource(string resourceName, ResourceManager.ResourceType resourceType, bool preload = false, int poolSize = 0)
        {
            if (resources.Exists(r => r.resourceName == resourceName && r.resourceType == resourceType))
            {
                Debug.LogWarning($"������ {resourceName} ��� ������ �� ������ {BundleId}");
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