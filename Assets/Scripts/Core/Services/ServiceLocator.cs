// Assets/Scripts/Core/Services/ServiceLocator.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GameCore.Core
{
    /// <summary>
    /// Центральний реєстр усіх сервісів у грі.
    /// Забезпечує єдину точку доступу до сервісів без прямих залежностей.
    /// </summary>
    public class ServiceLocator : MonoBehaviour, IInitializable
    {
        public static ServiceLocator Instance { get; private set; }

        private readonly Dictionary<Type, IService> _services = new Dictionary<Type, IService>();

        // Реалізація IInitializable
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 100; // Найвищий пріоритет

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            CoreLogger.Log("ServiceLocator", "ServiceLocator instance created");
        }

        /// <summary>
        /// Ініціалізує ServiceLocator. Викликається App.cs через інтерфейс IInitializable.
        /// </summary>
        public async Task Initialize()
        {
            CoreLogger.Log("ServiceLocator", "ServiceLocator initialized");
            IsInitialized = true;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Реєструє сервіс у системі. Якщо сервіс з таким типом вже існує, він буде перезаписаний.
        /// </summary>
        /// <typeparam name="T">Тип сервісу, має реалізувати IService</typeparam>
        /// <param name="service">Екземпляр сервісу</param>
        /// <returns>Task, який завершується після ініціалізації сервісу</returns>
        public async Task RegisterService<T>(T service) where T : IService
        {
            Type type = typeof(T);

            if (_services.ContainsKey(type))
            {
                CoreLogger.LogWarning("ServiceLocator", $"Service of type {type.Name} is already registered and will be replaced");
                _services.Remove(type);
            }

            _services[type] = service;
            CoreLogger.Log("ServiceLocator", $"Service registered: {type.Name}");

            await service.Initialize();
        }

        /// <summary>
        /// Отримує зареєстрований сервіс за його типом.
        /// </summary>
        /// <typeparam name="T">Тип сервісу</typeparam>
        /// <returns>Екземпляр сервісу або null, якщо сервіс не зареєстрований</returns>
        public T GetService<T>() where T : class, IService
        {
            Type type = typeof(T);

            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }

            CoreLogger.LogWarning("ServiceLocator", $"Service of type {type.Name} is not registered");
            return null;
        }

        /// <summary>
        /// Перевіряє, чи зареєстрований сервіс вказаного типу.
        /// </summary>
        /// <typeparam name="T">Тип сервісу</typeparam>
        /// <returns>true, якщо сервіс зареєстрований, інакше false</returns>
        public bool HasService<T>() where T : IService
        {
            return _services.ContainsKey(typeof(T));
        }
    }
}