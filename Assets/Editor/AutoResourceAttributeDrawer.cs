#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace GameCore.Core.Editor
{
    /// <summary>
    /// ��������� drawer ��� �������� AutoResource.
    /// �������� ������� ������� ����� ���������.
    /// </summary>
    [CustomPropertyDrawer(typeof(AutoResourceAttribute))]
    public class AutoResourceAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // �������� �������
            AutoResourceAttribute autoResourceAttr = (AutoResourceAttribute)attribute;

            // ����������, �� �� AssetReference<T>
            if (property.propertyType == SerializedPropertyType.ManagedReference &&
                property.managedReferenceFullTypename.Contains("AssetReference"))
            {
                // �������� ����������
                SerializedProperty resourcePathProp = property.FindPropertyRelative("resourcePath");
                SerializedProperty resourceTypeProp = property.FindPropertyRelative("resourceType");
                SerializedProperty autoLoadProp = property.FindPropertyRelative("autoLoad");
                SerializedProperty autoReleaseProp = property.FindPropertyRelative("autoRelease");

                if (resourcePathProp != null && resourceTypeProp != null &&
                    autoLoadProp != null && autoReleaseProp != null)
                {
                    // ���� ���������� �������, ���������� �� ���������� � ��������
                    if (string.IsNullOrEmpty(resourcePathProp.stringValue))
                    {
                        resourcePathProp.stringValue = autoResourceAttr.ResourcePath;
                        resourceTypeProp.enumValueIndex = (int)autoResourceAttr.ResourceType;
                        autoLoadProp.boolValue = autoResourceAttr.AutoLoad;
                        autoReleaseProp.boolValue = autoResourceAttr.AutoRelease;
                    }

                    // ������� ����
                    EditorGUI.PropertyField(position, property, label, true);
                }
                else
                {
                    EditorGUI.PropertyField(position, property, label, true);
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }

    /// <summary>
    /// ��������� drawer ��� �������� AutoGameObject.
    /// �������� ������� ������� ����� ��������� � ������� ������.
    /// </summary>
    [CustomPropertyDrawer(typeof(AutoGameObjectAttribute))]
    public class AutoGameObjectAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // �������� �������
            AutoGameObjectAttribute autoGameObjectAttr = (AutoGameObjectAttribute)attribute;

            // ����������, �� �� GameObjectReference
            if (property.propertyType == SerializedPropertyType.ManagedReference &&
                property.managedReferenceFullTypename.Contains("GameObjectReference"))
            {
                // �������� ����������
                SerializedProperty resourcePathProp = property.FindPropertyRelative("resourcePath");
                SerializedProperty resourceTypeProp = property.FindPropertyRelative("resourceType");
                SerializedProperty autoLoadProp = property.FindPropertyRelative("autoLoad");
                SerializedProperty autoReleaseProp = property.FindPropertyRelative("autoRelease");
                SerializedProperty usePoolingProp = property.FindPropertyRelative("usePooling");
                SerializedProperty preloadCountProp = property.FindPropertyRelative("preloadCount");

                if (resourcePathProp != null && resourceTypeProp != null &&
                    autoLoadProp != null && autoReleaseProp != null &&
                    usePoolingProp != null && preloadCountProp != null)
                {
                    // ���� ���������� �������, ���������� �� ���������� � ��������
                    if (string.IsNullOrEmpty(resourcePathProp.stringValue))
                    {
                        resourcePathProp.stringValue = autoGameObjectAttr.ResourcePath;
                        resourceTypeProp.enumValueIndex = (int)autoGameObjectAttr.ResourceType;
                        autoLoadProp.boolValue = autoGameObjectAttr.AutoLoad;
                        autoReleaseProp.boolValue = autoGameObjectAttr.AutoRelease;
                        usePoolingProp.boolValue = autoGameObjectAttr.UsePooling;
                        preloadCountProp.intValue = autoGameObjectAttr.PreloadCount;
                    }

                    // ������� ����
                    EditorGUI.PropertyField(position, property, label, true);
                }
                else
                {
                    EditorGUI.PropertyField(position, property, label, true);
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif