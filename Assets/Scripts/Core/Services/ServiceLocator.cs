// Assets/Scripts/Core/Services/ServiceLocator.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GameCore.Core
{
    /// <summary>
    /// ÷ентральний реЇстр ус≥х серв≥с≥в у гр≥.
    /// «абезпечуЇ Їдину точку доступу до серв≥с≥в без пр€мих залежностей.
    /// </summary>
    public class ServiceLocator : MonoBehaviour
    {
        public static ServiceLocator Instance { get; private set; }

        private readonly Dictionary<Type, IService> _services = new Dictionary<Type, IService>();
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_isInitialized) return;
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
         

            CoreLogger.Log("ServiceLocator initialized");
            _isInitialized = true;
        }

        /// <summary>
        /// –еЇструЇ серв≥с у систем≥. якщо серв≥с з таким типом вже ≥снуЇ, в≥н буде перезаписаний.
        /// </summary>
        /// <typeparam name="T">“ип серв≥су, маЇ реал≥зувати IService</typeparam>
        /// <param name="service">≈кземпл€р серв≥су</param>
        /// <returns>Task, €кий завершуЇтьс€ п≥сл€ ≥н≥ц≥ал≥зац≥њ серв≥су</returns>
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
        /// ќтримуЇ зареЇстрований серв≥с за його типом.
        /// </summary>
        /// <typeparam name="T">“ип серв≥су</typeparam>
        /// <returns>≈кземпл€р серв≥су або null, €кщо серв≥с не зареЇстрований</returns>
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
        /// ѕерев≥р€Ї, чи зареЇстрований серв≥с вказаного типу.
        /// </summary>
        /// <typeparam name="T">“ип серв≥су</typeparam>
        /// <returns>true, €кщо серв≥с зареЇстрований, ≥накше false</returns>
        public bool HasService<T>() where T : IService
        {
            return _services.ContainsKey(typeof(T));
        }
    }
}