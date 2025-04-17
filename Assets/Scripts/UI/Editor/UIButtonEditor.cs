using UnityEditor;
using UnityEngine;
using GameCore.Core;
using System.IO;
using System.Linq;

[CustomEditor(typeof(UIButton))]
public class UIButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var button = (UIButton)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🔍 Available Panels", EditorStyles.boldLabel);

        // Зчитуємо панелі
        string[] allPanels = GetPanelNames();
        string[] panelNames = new[] { "None" }.Concat(allPanels).ToArray();

        // Визначаємо індекс поточного значення
        int selectedIndex = 0;
        if (!string.IsNullOrEmpty(button.showPanelName))
        {
            int realIndex = System.Array.IndexOf(allPanels, button.showPanelName);
            selectedIndex = realIndex >= 0 ? realIndex + 1 : 0; // +1 бо зсув через "None"
        }

        // Малюємо Popup
        int newIndex = EditorGUILayout.Popup("Show Panel Name", selectedIndex, panelNames);

        // Якщо змінилось — зберігаємо
        if (newIndex != selectedIndex)
        {
            button.showPanelName = newIndex == 0 ? string.Empty : allPanels[newIndex - 1];
            EditorUtility.SetDirty(button);
        }
    }


    private string[] GetPanelNames()
    {
        string path = "Assets/Resources/UI/Panels";
        if (!Directory.Exists(path)) return new[] { "None" };

        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { path });
        string[] names = new string[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            names[i] = go.name;
        }

        return names;
    }
}
