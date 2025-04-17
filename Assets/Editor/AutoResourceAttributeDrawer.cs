#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace GameCore.Core.Editor
{
    /// <summary>
    /// Кастомний drawer для атрибута AutoResource.
    /// Дозволяє обирати ресурси через інспектор.
    /// </summary>
    [CustomPropertyDrawer(typeof(AutoResourceAttribute))]
    public class AutoResourceAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Отримуємо атрибут
            AutoResourceAttribute autoResourceAttr = (AutoResourceAttribute)attribute;

            // Перевіряємо, чи це AssetReference<T>
            if (property.propertyType == SerializedPropertyType.ManagedReference &&
                property.managedReferenceFullTypename.Contains("AssetReference"))
            {
                // Отримуємо властивості
                SerializedProperty resourcePathProp = property.FindPropertyRelative("resourcePath");
                SerializedProperty resourceTypeProp = property.FindPropertyRelative("resourceType");
                SerializedProperty autoLoadProp = property.FindPropertyRelative("autoLoad");
                SerializedProperty autoReleaseProp = property.FindPropertyRelative("autoRelease");

                if (resourcePathProp != null && resourceTypeProp != null &&
                    autoLoadProp != null && autoReleaseProp != null)
                {
                    // Якщо властивість порожня, заповнюємо її значеннями з атрибута
                    if (string.IsNullOrEmpty(resourcePathProp.stringValue))
                    {
                        resourcePathProp.stringValue = autoResourceAttr.ResourcePath;
                        resourceTypeProp.enumValueIndex = (int)autoResourceAttr.ResourceType;
                        autoLoadProp.boolValue = autoResourceAttr.AutoLoad;
                        autoReleaseProp.boolValue = autoResourceAttr.AutoRelease;
                    }

                    // Малюємо поля
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
    /// Кастомний drawer для атрибута AutoGameObject.
    /// Дозволяє обирати префаби через інспектор з опціями пулінга.
    /// </summary>
    [CustomPropertyDrawer(typeof(AutoGameObjectAttribute))]
    public class AutoGameObjectAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Отримуємо атрибут
            AutoGameObjectAttribute autoGameObjectAttr = (AutoGameObjectAttribute)attribute;

            // Перевіряємо, чи це GameObjectReference
            if (property.propertyType == SerializedPropertyType.ManagedReference &&
                property.managedReferenceFullTypename.Contains("GameObjectReference"))
            {
                // Отримуємо властивості
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
                    // Якщо властивість порожня, заповнюємо її значеннями з атрибута
                    if (string.IsNullOrEmpty(resourcePathProp.stringValue))
                    {
                        resourcePathProp.stringValue = autoGameObjectAttr.ResourcePath;
                        resourceTypeProp.enumValueIndex = (int)autoGameObjectAttr.ResourceType;
                        autoLoadProp.boolValue = autoGameObjectAttr.AutoLoad;
                        autoReleaseProp.boolValue = autoGameObjectAttr.AutoRelease;
                        usePoolingProp.boolValue = autoGameObjectAttr.UsePooling;
                        preloadCountProp.intValue = autoGameObjectAttr.PreloadCount;
                    }

                    // Малюємо поля
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