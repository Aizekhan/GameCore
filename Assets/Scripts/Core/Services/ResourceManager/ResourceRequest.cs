using System;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameCore.Core
{
    /// <summary>
    /// Клас запиту ресурсу, який дозволяє відстежувати стан та прогрес завантаження.
    /// </summary>
    public class ResourceRequest<T> where T : Object
    {
        private readonly ResourceManager _resourceManager;
        private readonly ResourceManager.ResourceType _resourceType;
        private readonly string _resourceName;
        private readonly bool _instantiate;
        private readonly Vector3 _position;
        private readonly Quaternion _rotation;
        private readonly Transform _parent;

        private float _progress;
        private bool _isComplete;
        private T _result;
        private GameObject _instantiatedObject;
        private Exception _error;

        public float Progress => _progress;
        public bool IsComplete => _isComplete;
        public T Result => _result;
        public GameObject InstantiatedObject => _instantiatedObject;
        public Exception Error => _error;

        public ResourceRequest(
            ResourceManager resourceManager,
            ResourceManager.ResourceType resourceType,
            string resourceName,
            bool instantiate = false,
            Vector3 position = default,
            Quaternion rotation = default,
            Transform parent = null)
        {
            _resourceManager = resourceManager;
            _resourceType = resourceType;
            _resourceName = resourceName;
            _instantiate = instantiate;
            _position = position;
            _rotation = rotation;
            _parent = parent;

            _progress = 0f;
            _isComplete = false;
        }

        /// <summary>
        /// Асинхронне завантаження ресурсу з поверненням результату.
        /// </summary>
        public async Task<T> GetResultAsync()
        {
            if (_isComplete)
            {
                if (_error != null)
                    throw _error;

                return _result;
            }

            try
            {
                // Завантаження ресурсу
                _result = await _resourceManager.LoadAsync<T>(_resourceType, _resourceName);
                _progress = 0.5f;

                if (_result != null && _instantiate && _result is GameObject prefab)
                {
                    // Інстанціювання об'єкта, якщо потрібно
                    _instantiatedObject = GameObject.Instantiate(prefab, _position, _rotation, _parent);
                    _progress = 1f;
                    _isComplete = true;

                    // Повертаємо інстанційований об'єкт, якщо запитувався GameObject
                    if (typeof(T) == typeof(GameObject))
                    {
                        return _instantiatedObject as T;
                    }
                }
                else
                {
                    _progress = 1f;
                    _isComplete = true;
                }

                return _result;
            }
            catch (Exception ex)
            {
                _error = ex;
                _isComplete = true;
                throw;
            }
        }

        /// <summary>
        /// Скасовує завантаження та звільняє ресурси.
        /// </summary>
        public void Cancel()
        {
            if (_isComplete && _instantiatedObject != null)
            {
                GameObject.Destroy(_instantiatedObject);
                _instantiatedObject = null;
            }

            _result = null;
            _isComplete = true;
        }
    }
}