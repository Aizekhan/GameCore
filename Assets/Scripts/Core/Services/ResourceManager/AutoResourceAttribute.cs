// Assets/Scripts/Core/Services/ResourceManager/AutoResourceAttribute.cs
using System;
using UnityEngine;

namespace GameCore.Core
{
    /// <summary>
    /// ������� ��� ������������� ������������ ������� ����� ��������.
    /// ������������� �� ���� ���� AssetReference<T>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoResourceAttribute : PropertyAttribute
    {
        public string ResourcePath { get; private set; }
        public ResourceManager.ResourceType ResourceType { get; private set; }
        public bool AutoLoad { get; private set; }
        public bool AutoRelease { get; private set; }

        /// <summary>
        /// ������� ������� ��� ������������� ������������ �������.
        /// </summary>
        /// <param name="resourcePath">���� �� ������� � Resources</param>
        /// <param name="resourceType">��� �������</param>
        /// <param name="autoLoad">�� ������������� ������ �����������</param>
        /// <param name="autoRelease">�� ������������� ������ ����������� ��� �������</param>
        public AutoResourceAttribute(string resourcePath, ResourceManager.ResourceType resourceType = ResourceManager.ResourceType.Prefab, bool autoLoad = true, bool autoRelease = true)
        {
            ResourcePath = resourcePath;
            ResourceType = resourceType;
            AutoLoad = autoLoad;
            AutoRelease = autoRelease;
        }
    }

    /// <summary>
    /// ������� ��� ������������� ������������ GameObject � ��������� ������.
    /// ������������� �� ���� ���� GameObjectReference.
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
        /// ������� ������� ��� ������������� ������������ GameObject � ��������� ������.
        /// </summary>
        /// <param name="resourcePath">���� �� ������� � Resources</param>
        /// <param name="resourceType">��� �������</param>
        /// <param name="usePooling">�� ��������������� ����� ��'����</param>
        /// <param name="preloadCount">ʳ������ ��'���� ��� ������������ ������������ � ���</param>
        /// <param name="autoLoad">�� ������������� ������ �����������</param>
        /// <param name="autoRelease">�� ������������� ������ ����������� ��� �������</param>
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