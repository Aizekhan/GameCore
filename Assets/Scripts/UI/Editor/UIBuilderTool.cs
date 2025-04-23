using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace GameCore.Core.Editor
{
    public class UIBuilderTool : EditorWindow
    {
        [MenuItem("GameCore/UI Builder")]
        public static void ShowWindow()
        {
            GetWindow<UIBuilderTool>("UI Builder");
        }

        private enum TabType
        {
            Buttons,
            Panels,
            Navigation
        }

        private TabType currentTab = TabType.Buttons;
        private Transform selectedParent;

        // ���������� ��� ������
        private string buttonText = "New Button";
        private UIPanelAnimationType hoverAnimationType = UIPanelAnimationType.Scale;
        private string buttonSound = "ButtonClick";
        private string buttonCategory = "Default";
        private string targetPanelName = "";
        private bool isBackButton = false;

        // ���������� ��� �������
        private string panelName = "NewPanel";
        private UIPanelAnimationType showAnimationType = UIPanelAnimationType.Fade;
        private UIPanelAnimationType hideAnimationType = UIPanelAnimationType.Fade;
        private float animationDuration = 0.3f;
        private bool registerInRegistry = true;

        // ������� ���
        private List<string> _cachedPanelNames;
        private List<string> _cachedSoundNames;
        private UIPanelRegistry _panelRegistry;

        private void OnEnable()
        {
            // ��������� UIPanelRegistry � ����
            _panelRegistry = FindObjectOfType<UIPanelRegistry>();
            RefreshCachedData();
        }

        private void RefreshCachedData()
        {
            // ��������� ������� ������ ������� � �����
            _cachedPanelNames = GetAvailablePanels();
            _cachedSoundNames = GetAvailableSounds();
        }

        private void OnGUI()
        {
            // ���������
            GUILayout.Label("GameCore UI Builder", EditorStyles.boldLabel);

            // ������ ��������� �����
            if (GUILayout.Button("Refresh Data"))
            {
                RefreshCachedData();
            }

            // ���� ������������ ��'����
            EditorGUILayout.Space();
            GUILayout.Label("Parent Transform (Canvas)", EditorStyles.boldLabel);
            selectedParent = EditorGUILayout.ObjectField("Parent", selectedParent, typeof(Transform), true) as Transform;

            if (selectedParent == null)
            {
                EditorGUILayout.HelpBox("Please select a parent Transform (should be a Canvas or Panel)", MessageType.Warning);
            }

            // �������
            EditorGUILayout.Space();
            currentTab = (TabType)GUILayout.Toolbar((int)currentTab, new string[] { "Buttons", "Panels", "Navigation" });

            EditorGUILayout.Space();

            // ³���������� �������� �������
            switch (currentTab)
            {
                case TabType.Buttons:
                    DrawButtonsTab();
                    break;
                case TabType.Panels:
                    DrawPanelsTab();
                    break;
                case TabType.Navigation:
                    DrawNavigationTab();
                    break;
            }
        }

        private void DrawButtonsTab()
        {
            GUILayout.Label("Create Button", EditorStyles.boldLabel);

            buttonText = EditorGUILayout.TextField("Button Text", buttonText);
            buttonCategory = EditorGUILayout.TextField("Button Category", buttonCategory);

            EditorGUILayout.Space();
            GUILayout.Label("Animation & Sound", EditorStyles.boldLabel);
            hoverAnimationType = (UIPanelAnimationType)EditorGUILayout.EnumPopup("Hover Animation", hoverAnimationType);

            // ���� ����� � ������
            int soundIndex = _cachedSoundNames.IndexOf(buttonSound);
            if (soundIndex < 0) soundIndex = 0;

            soundIndex = EditorGUILayout.Popup("Click Sound", soundIndex, _cachedSoundNames.ToArray());
            if (soundIndex >= 0 && soundIndex < _cachedSoundNames.Count)
            {
                buttonSound = _cachedSoundNames[soundIndex];
            }

            EditorGUILayout.Space();
            GUILayout.Label("Navigation", EditorStyles.boldLabel);
            isBackButton = EditorGUILayout.Toggle("Is Back Button", isBackButton);

            if (!isBackButton)
            {
                // ���� ������� ����� � ������ �������������
                int selectedIndex = _cachedPanelNames.IndexOf(targetPanelName);
                if (selectedIndex < 0) selectedIndex = 0;

                selectedIndex = EditorGUILayout.Popup("Target Panel", selectedIndex, _cachedPanelNames.ToArray());

                if (selectedIndex >= 0 && selectedIndex < _cachedPanelNames.Count)
                {
                    targetPanelName = _cachedPanelNames[selectedIndex];
                    if (targetPanelName == "None") targetPanelName = "";
                }
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Button"))
            {
                if (selectedParent != null)
                {
                    CreateButton();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please select a parent Transform", "OK");
                }
            }
        }

        private void DrawPanelsTab()
        {
            GUILayout.Label("Create Panel", EditorStyles.boldLabel);

            panelName = EditorGUILayout.TextField("Panel Name", panelName);

            EditorGUILayout.Space();
            GUILayout.Label("Animation Settings", EditorStyles.boldLabel);
            showAnimationType = (UIPanelAnimationType)EditorGUILayout.EnumPopup("Show Animation", showAnimationType);
            hideAnimationType = (UIPanelAnimationType)EditorGUILayout.EnumPopup("Hide Animation", hideAnimationType);
            animationDuration = EditorGUILayout.Slider("Animation Duration", animationDuration, 0.1f, 2.0f);

            EditorGUILayout.Space();
            GUILayout.Label("Registration", EditorStyles.boldLabel);
            registerInRegistry = EditorGUILayout.Toggle("Register in UIPanelRegistry", registerInRegistry);

            // ��������, �� ������ ��� ������������
            if (_panelRegistry != null && !string.IsNullOrEmpty(panelName))
            {
                bool isRegistered = _panelRegistry.HasPanel(panelName);
                if (isRegistered)
                {
                    EditorGUILayout.HelpBox($"Panel '{panelName}' is already registered in UIPanelRegistry.", MessageType.Info);
                }
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Panel"))
            {
                if (selectedParent != null)
                {
                    CreatePanel();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please select a parent Transform", "OK");
                }
            }
        }

        private void DrawNavigationTab()
        {
            GUILayout.Label("UI Navigation Setup", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("This tab allows you to create navigation flows between panels.", MessageType.Info);

            if (_cachedPanelNames != null && _cachedPanelNames.Count > 1)
            {
                EditorGUILayout.LabelField("Available Panels:", EditorStyles.boldLabel);

                foreach (var panel in _cachedPanelNames)
                {
                    if (panel != "None")
                    {
                        EditorGUILayout.LabelField($"� {panel}");
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Select panels in the Buttons tab to create navigation between them.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No panels found. Create panels first in the 'Panels' tab.", MessageType.Warning);
            }
        }

        private void CreateButton()
        {
            // ��������� ������ ������ Unity
            GameObject buttonObj = new GameObject(buttonText + "Button");
            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            buttonObj.AddComponent<CanvasRenderer>();
            Image image = buttonObj.AddComponent<Image>();
            Button button = buttonObj.AddComponent<Button>();

            // ������������ ����������� ��'���
            buttonObj.transform.SetParent(selectedParent);
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(160, 30);

            // ��������� ��������� ���������
            GameObject textObj = new GameObject("Text");
            RectTransform textRectTransform = textObj.AddComponent<RectTransform>();
            textObj.transform.SetParent(buttonObj.transform);
            textRectTransform.localScale = Vector3.one;
            textRectTransform.anchoredPosition = Vector2.zero;
            textRectTransform.sizeDelta = new Vector2(160, 30);

            // ������ TextMeshPro ���������
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = buttonText;
            text.color = Color.black;
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;

            // ������ UIButton ���������
            UIButton uiButton = buttonObj.AddComponent<UIButton>();

            // ����������� ����������
            var buttonSerializedObj = new SerializedObject(uiButton);
            buttonSerializedObj.FindProperty("buttonCategory").stringValue = buttonCategory;
            buttonSerializedObj.FindProperty("isBackButton").boolValue = isBackButton;
            buttonSerializedObj.FindProperty("showPanelName").stringValue = isBackButton ? "" : targetPanelName;
            buttonSerializedObj.FindProperty("clickSoundName").stringValue = buttonSound;
            buttonSerializedObj.FindProperty("useHoverAnimation").boolValue = true;
            buttonSerializedObj.ApplyModifiedProperties();

            // �������� ������������ ������ � ��������
            Selection.activeGameObject = buttonObj;

            Debug.Log($"Button '{buttonText}' created successfully");

            // ��������� ���������, ��� �������� ��� ������������
            EditorUtility.SetDirty(buttonObj);
        }

        private void CreatePanel()
        {
            // ����������, �� ������ ��� ������������
            if (_panelRegistry != null && _panelRegistry.HasPanel(panelName))
            {
                if (!EditorUtility.DisplayDialog("Panel Already Exists",
                    $"Panel '{panelName}' is already registered. Do you want to create a new instance?",
                    "Yes", "Cancel"))
                {
                    return;
                }
            }

            // ��������� ������ ������
            GameObject panelObj = new GameObject(panelName);
            RectTransform rectTransform = panelObj.AddComponent<RectTransform>();
            panelObj.AddComponent<CanvasRenderer>();
            Image image = panelObj.AddComponent<Image>();
            CanvasGroup canvasGroup = panelObj.AddComponent<CanvasGroup>();

            // ������������ ����������� ��'���
            panelObj.transform.SetParent(selectedParent);
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;

            // ������ UIPanel ���������
            UIPanel uiPanel = panelObj.AddComponent<UIPanel>();

            // ����������� ����������
            var panelSerializedObj = new SerializedObject(uiPanel);
            panelSerializedObj.FindProperty("showAnimationType").enumValueIndex = (int)showAnimationType;
            panelSerializedObj.FindProperty("hideAnimationType").enumValueIndex = (int)hideAnimationType;
            panelSerializedObj.FindProperty("animationDuration").floatValue = animationDuration;
            panelSerializedObj.ApplyModifiedProperties();

            // �������� � UIPanelRegistry, ���� �������
            if (registerInRegistry && _panelRegistry != null)
            {
                // �������� �������� �� ������
                string directory = "Assets/Resources/UI/Panels";
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                // ��������� ���� �� �������
                string prefabPath = $"{directory}/{panelName}.prefab";

                // �������� �� ������
                GameObject prefabObj = null;
#if UNITY_2018_3_OR_NEWER
                prefabObj = PrefabUtility.SaveAsPrefabAsset(panelObj, prefabPath);
#else
                    prefabObj = PrefabUtility.CreatePrefab(prefabPath, panelObj);
#endif

                if (prefabObj != null)
                {
                    // �������� � �����
                    _panelRegistry.RegisterPanel(panelName, prefabObj);
                    Debug.Log($"Panel '{panelName}' registered in UIPanelRegistry");

                    // ��������� ��������� ������ �������
                    RefreshCachedData();
                }
            }

            // �������� ������������ ������ � ��������
            Selection.activeGameObject = panelObj;

            Debug.Log($"Panel '{panelName}' created successfully");

            // ��������� ���������
            EditorUtility.SetDirty(panelObj);
        }

        private List<string> GetAvailablePanels()
        {
            List<string> panels = new List<string> { "None" };

            // �������� ����� � UIPanelRegistry
            if (_panelRegistry != null)
            {
                string[] registeredPanels = _panelRegistry.GetAllRegisteredPanelNames();
                if (registeredPanels != null && registeredPanels.Length > 0)
                {
                    panels.AddRange(registeredPanels);
                }
            }

            // ������ ����� � Resources, ���� �� ���� � ������
            Object[] resourcePanels = Resources.LoadAll("UI/Panels", typeof(GameObject));
            foreach (Object obj in resourcePanels)
            {
                string panelName = obj.name;
                if (!panels.Contains(panelName))
                {
                    panels.Add(panelName);
                }
            }

            return panels;
        }

        private List<string> GetAvailableSounds()
        {
            List<string> sounds = new List<string>();

            // ��������� �����
            sounds.AddRange(new[] { "ButtonClick", "ButtonHover", "MenuOpen", "MenuClose" });

            // ����������� ����� � Resources
            Object[] audioClips = Resources.LoadAll("Audio", typeof(AudioClip));
            foreach (Object obj in audioClips)
            {
                string soundName = obj.name;
                if (!sounds.Contains(soundName))
                {
                    sounds.Add(soundName);
                }
            }

            return sounds;
        }
    }
}