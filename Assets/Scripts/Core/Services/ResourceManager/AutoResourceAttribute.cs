// Assets/Scripts/Core/Services/ResourceManager/AutoResourceAttribute.cs
using System;
using UnityEngine;

namespace GameCore.Core
{
    /// <summary>
    /// Атрибут для автоматичного завантаження ресурсів через рефлексію.
    /// Застосовується до полів типу AssetReference<T>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoResourceAttribute : PropertyAttribute
    {
        public string ResourcePath { get; private set; }
        public ResourceManager.ResourceType ResourceType { get; private set; }
        public bool AutoLoad { get; private set; }
        public bool AutoRelease { get; private set; }

        /// <summary>
        /// Створює атрибут для автоматичного завантаження ресурсу.
        /// </summary>
        /// <param name="resourcePath">Шлях до ресурсу в Resources</param>
        /// <param name="resourceType">Тип ресурсу</param>
        /// <param name="autoLoad">Чи завантажувати ресурс автоматично</param>
        /// <param name="autoRelease">Чи вивантажувати ресурс автоматично при знищенні</param>
        public AutoResourceAttribute(string resourcePath, ResourceManager.ResourceType resourceType = ResourceManager.ResourceType.Prefab, bool autoLoad = true, bool autoRelease = true)
        {
            ResourcePath = resourcePath;
            ResourceType = resourceType;
            AutoLoad = autoLoad;
            AutoRelease = autoRelease;
        }
    }

    /// <summary>
    /// Атрибут для автоматичного завантаження GameObject з підтримкою пулінгу.
    /// Застосовується до полів типу GameObjectReference.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoGameObjectAttribute : PropertyAttribute
    {
        public string ResourcePath { get; private set; }
        public ResourceManager.ResourceType ResourceType { get; private set; }
        public bool UsePooling { get; private set; }
        public int PreloadCount { get; private set; }
        public bool AutoLoad { get; private set; }
        public bool AutoRelease { get; private set; }

        /// <summary>
        /// Створює атрибут для автоматичного завантаження GameObject з підтримкою пулінгу.
        /// </summary>
        /// <param name="resourcePath">Шлях до ресурсу в Resources</param>
        /// <param name="resourceType">Тип ресурсу</param>
        /// <param name="usePooling">Чи використовувати пулінг об'єктів</param>
        /// <param name="preloadCount">Кількість об'єктів для попереднього завантаження в пул</param>
        /// <param name="autoLoad">Чи завантажувати ресурс автоматично</param>
        /// <param name="autoRelease">Чи вивантажувати ресурс автоматично при знищенні</param>
        public AutoGameObjectAttribute(string resourcePath, ResourceManager.ResourceType resourceType = ResourceManager.ResourceType.Prefab,
            bool usePooling = true, int preloadCount = 0, bool autoLoad = true, bool autoRelease = true)
        {
            ResourcePath = resourcePath;
            ResourceType = resourceType;
            UsePooling = usePooling;
            PreloadCount = preloadCount;
            AutoLoad = autoLoad;
            AutoRelease = autoRelease;
        }
    }
}