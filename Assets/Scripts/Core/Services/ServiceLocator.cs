// Assets/Scripts/Core/Services/ServiceLocator.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GameCore.Core
{
    /// <summary>
    /// ����������� ����� ��� ������ � ��.
    /// ��������� ����� ����� ������� �� ������ ��� ������ �����������.
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
        /// ������ ����� � ������. ���� ����� � ����� ����� ��� ����, �� ���� �������������.
        /// </summary>
        /// <typeparam name="T">��� ������, �� ���������� IService</typeparam>
        /// <param name="service">��������� ������</param>
        /// <returns>Task, ���� ����������� ���� ����������� ������</returns>
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
        /// ������ ������������� ����� �� ���� �����.
        /// </summary>
        /// <typeparam name="T">��� ������</typeparam>
        /// <returns>��������� ������ ��� null, ���� ����� �� �������������</returns>
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
        /// ��������, �� ������������� ����� ��������� ����.
        /// </summary>
        /// <typeparam name="T">��� ������</typeparam>
        /// <returns>true, ���� ����� �������������, ������ false</returns>
        public bool HasService<T>() where T : IService
        {
            return _services.ContainsKey(typeof(T));
        }
    }
}