// Assets/Scripts/Core/Services/ResourceManager/ResourceRequest.cs
using System;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameCore.Core
{
    /// <summary>
    /// Клас для обробки завантаження ресурсів з підтримкою прогресу і колбеків.
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
        private TaskCompletionSource<T> _completionSource;
        private Action<float> _progressCallback;
        private Action<T> _completionCallback;

        /// <summary>
        /// Поточний прогрес завантаження (0-1)
        /// </summary>
        public float Progress => _progress;

        /// <summary>
        /// Чи завершено завантаження
        /// </summary>
        public bool IsDone { get; private set; }

        /// <summary>
        /// Завантажений ресурс
        /// </summary>
        public T Result { get; private set; }

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
            _completionSource = new TaskCompletionSource<T>();
        }

        /// <summary>
        /// Додає колбек для відстеження прогресу завантаження.
        /// </summary>
        public ResourceRequest<T> OnProgress(Action<float> callback)
        {
            _progressCallback = callback;
            return this;
        }

        /// <summary>
        /// Додає колбек, який буде викликаний після завершення завантаження.
        /// </summary>
        public ResourceRequest<T> OnCompletion(Action<T> callback)
        {
            _completionCallback = callback;
            return this;
        }

        /// <summary>
        /// Починає завантаження ресурсу.
        /// </summary>
        public async Task<T> StartLoading()
        {
            try
            {
                await UpdateProgress(0.1f);

                if (_instantiate && typeof(T) == typeof(GameObject))
                {
                    var result = await _resourceManager.InstantiateAsync(_resourceType, _resourceName, _position, _rotation, _parent);
                    await UpdateProgress(1f);
                    Result = result as T;
                    IsDone = true;
                    _completionCallback?.Invoke(Result);
                    _completionSource.TrySetResult(Result);
                }
                else
                {
                    await UpdateProgress(0.5f);
                    var result = await _resourceManager.LoadAsync<T>(_resourceType, _resourceName);
                    await UpdateProgress(1f);
                    Result = result;
                    IsDone = true;
                    _completionCallback?.Invoke(Result);
                    _completionSource.TrySetResult(Result);
                }

                return Result;
            }
            catch (Exception ex)
            {
                CoreLogger.LogError("RESOURCE", $"Помилка завантаження ресурсу {_resourceName}: {ex.Message}");
                _completionSource.TrySetException(ex);
                throw;
            }
        }

        /// <summary>
        /// Повертає Task, який завершиться після завантаження ресурсу.
        /// </summary>
        public Task<T> WaitForCompletion()
        {
            return _completionSource.Task;
        }

        private async Task UpdateProgress(float progress)
        {
            _progress = progress;
            _progressCallback?.Invoke(progress);
            // Невелика затримка, щоб симулювати завантаження для тестування колбеків
            await Task.Delay(10);
        }
    }
}