// Assets/Scripts/Core/Services/ResourceManager/ResourceInitializer.cs
using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace GameCore.Core
{
    /// <summary>
    /// Компонент для автоматичної ініціалізації ресурсів через атрибути.
    /// </summary>
    public class ResourceInitializer : MonoBehaviour
    {
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool logInitialization = true;

        private ResourceManager _resourceManager;
        private MonoBehaviour[] _components;

        private void Awake()
        {
            if (initializeOnAwake)
            {
                _resourceManager = ServiceLocator.Instance?.GetService<ResourceManager>();
                if (_resourceManager == null)
                {
                    CoreLogger.LogError("RESOURCE", "❌ ResourceManager не знайдено в ServiceLocator");
                    return;
                }

                // Отримуємо всі MonoBehaviour компоненти на об'єкті та його дочірніх об'єктах
                _components = GetComponentsInChildren<MonoBehaviour>(true);
                InitializeResources();
            }
        }

        /// <summary>
        /// Ініціалізує всі ресурси з атрибутами.
        /// </summary>
        public async void InitializeResources()
        {
            if (_resourceManager == null)
            {
                _resourceManager = ServiceLocator.Instance?.GetService<ResourceManager>();
                if (_resourceManager == null)
                {
                    CoreLogger.LogError("RESOURCE", "❌ ResourceManager не знайдено в ServiceLocator");
                    return;
                }
            }

            if (_components == null || _components.Length == 0)
            {
                _components = GetComponentsInChildren<MonoBehaviour>(true);
            }

            foreach (var component in _components)
            {
                // Пропускаємо цей компонент, щоб уникнути зациклення
                if (component == this)
                    continue;

                // Пропускаємо вимкнені компоненти
                if (!component.enabled)
                    continue;

                // Отримуємо всі поля компонента
                var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    // Перевіряємо, чи є атрибут AutoResource
                    var autoResourceAttr = field.GetCustomAttribute<AutoResourceAttribute>();
                    if (autoResourceAttr != null)
                    {
                        await InitializeAssetReference(component, field, autoResourceAttr);
                    }

                    // Перевіряємо, чи є атрибут AutoGameObject
                    var autoGameObjectAttr = field.GetCustomAttribute<AutoGameObjectAttribute>();
                    if (autoGameObjectAttr != null)
                    {
                        await InitializeGameObjectReference(component, field, autoGameObjectAttr);
                    }
                }
            }

            if (logInitialization)
            {
                CoreLogger.Log("RESOURCE", "✅ Ресурси ініціалізовано");
            }
        }

        /// <summary>
        /// Ініціалізує поле AssetReference<T> за допомогою атрибута AutoResource.
        /// </summary>
        private async Task InitializeAssetReference(MonoBehaviour component, FieldInfo field, AutoResourceAttribute attribute)
        {
            // Перевіряємо, чи поле є типом AssetReference<T>
            if (!field.FieldType.IsGenericType || field.FieldType.GetGenericTypeDefinition() != typeof(AssetReference<>))
            {
                CoreLogger.LogWarning("RESOURCE", $"⚠️ Поле {field.Name} в {component.GetType().Name} має атрибут [AutoResource], але не є типом AssetReference<T>");
                return;
            }

            // Отримуємо тип параметра T
            Type assetType = field.FieldType.GetGenericArguments()[0];

            // Створюємо екземпляр AssetReference<T>
            Type assetRefType = typeof(AssetReference<>).MakeGenericType(assetType);
            object assetRef = Activator.CreateInstance(
                assetRefType,
                attribute.ResourcePath,
                attribute.ResourceType,
                attribute.AutoLoad,
                attribute.AutoRelease);

            // Присвоюємо створений екземпляр полю
            field.SetValue(component, assetRef);

            if (logInitialization)
            {
                CoreLogger.Log("RESOURCE", $"💾 Ініціалізовано {field.Name} в {component.GetType().Name} з {attribute.ResourcePath}");
            }

            // Завантажуємо ресурс, якщо потрібно
            if (attribute.AutoLoad)
            {
                // Викликаємо LoadAssetAsync через рефлексію
                MethodInfo loadMethod = assetRefType.GetMethod("LoadAssetAsync");
                var task = (Task)loadMethod.Invoke(assetRef, null);
                await task;
            }
        }

        /// <summary>
        /// Ініціалізує поле GameObjectReference за допомогою атрибута AutoGameObject.
        /// </summary>
        private async Task InitializeGameObjectReference(MonoBehaviour component, FieldInfo field, AutoGameObjectAttribute attribute)
        {
            // Перевіряємо, чи поле є типом GameObjectReference
            if (field.FieldType != typeof(GameObjectReference))
            {
                CoreLogger.LogWarning("RESOURCE", $"⚠️ Поле {field.Name} в {component.GetType().Name} має атрибут [AutoGameObject], але не є типом GameObjectReference");
                return;
            }

            // Створюємо екземпляр GameObjectReference
            GameObjectReference gameObjectRef = new GameObjectReference(
                attribute.ResourcePath,
                attribute.ResourceType,
                attribute.AutoLoad,
                attribute.AutoRelease,
                attribute.UsePooling,
                attribute.PreloadCount);

            // Присвоюємо створений екземпляр полю
            field.SetValue(component, gameObjectRef);

            if (logInitialization)
            {
                CoreLogger.Log("RESOURCE", $"🎮 Ініціалізовано {field.Name} в {component.GetType().Name} з {attribute.ResourcePath}");
            }

            // Завантажуємо ресурс і попередньо завантажуємо пул, якщо потрібно
            if (attribute.AutoLoad)
            {
                await gameObjectRef.LoadAssetAsync();

                if (attribute.UsePooling && attribute.PreloadCount > 0)
                {
                    await gameObjectRef.PreloadAsync(attribute.PreloadCount);
                }
            }
        }
    }
}