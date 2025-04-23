using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using static UnityEngine.GraphicsBuffer;

namespace GameCore.Core.Editor
{
    [CustomEditor(typeof(UIPanel))]
    public class UIPanelEditor : UnityEditor.Editor
    {
        private SerializedProperty showAnimationTypeProp;
        private SerializedProperty hideAnimationTypeProp;
        private SerializedProperty animationDurationProp;
        private SerializedProperty startHiddenProp;

        private List<string> availableButtons = new List<string>();
        private UIPanelRegistry panelRegistry;

        private void OnEnable()
        {
            showAnimationTypeProp = serializedObject.FindProperty("showAnimationType");
            hideAnimationTypeProp = serializedObject.FindProperty("hideAnimationType");
            animationDurationProp = serializedObject.FindProperty("animationDuration");
            startHiddenProp = serializedObject.FindProperty("startHidden");

            // ��������� UIPanelRegistry
            panelRegistry = FindObjectOfType<UIPanelRegistry>();

            LoadAvailableButtons();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("UIPanel Settings", EditorStyles.boldLabel);

            // ������ ���������
            UIPanel panel = (UIPanel)target;
            string panelName = panel.gameObject.name;

            if (panelRegistry != null)
            {
                bool isRegistered = panelRegistry.HasPanel(panelName);
                EditorGUILayout.LabelField($"Registration Status: {(isRegistered ? "Registered" : "Not Registered")}");

                if (!isRegistered)
                {
                    if (GUILayout.Button("Register in UIPanelRegistry"))
                    {
                        RegisterInPanelRegistry();
                    }
                }
            }

            // ������ ������������
            EditorGUILayout.PropertyField(startHiddenProp);

            // �������� ������������
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(showAnimationTypeProp);
            EditorGUILayout.PropertyField(hideAnimationTypeProp);
            EditorGUILayout.PropertyField(animationDurationProp);

            // ��������� �������� �������
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Preview Show Animation"))
            {
                PreviewAnimation(true);
            }

            if (GUILayout.Button("Preview Hide Animation"))
            {
                PreviewAnimation(false);
            }

            EditorGUILayout.EndHorizontal();

            // ����������� ��� ������ � ��������
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Button Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Add New Button"))
            {
                AddButtonToPanel();
            }

            DrawButtonsList();

            // ����������� ��� ���������� � ������������
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Panel Tools", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Save as Prefab"))
            {
                SavePanelAsPrefab();
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawButtonsList()
        {
            UIPanel panel = (UIPanel)target;

            // �������� �� ������ ������
            UIButton[] buttons = panel.GetComponentsInChildren<UIButton>(true);

            if (buttons.Length == 0)
            {
                EditorGUILayout.HelpBox("No buttons found in this panel", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Panel contains {buttons.Length} buttons:", EditorStyles.miniBoldLabel);

            foreach (UIButton button in buttons)
            {
                EditorGUILayout.BeginHorizontal(GUI.skin.box);

                // ³��������� ��'� ������ � �� ���
                string buttonType = "Unknown";
                if (button.isBackButton)
                {
                    buttonType = "Back Button";
                }
                else if (!string.IsNullOrEmpty(button.showPanelName))
                {
                    buttonType = $"Shows '{button.showPanelName}'";
                }
                else
                {
                    buttonType = "Regular Button";
                }

                EditorGUILayout.LabelField(button.gameObject.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(buttonType, EditorStyles.miniLabel);

                // ������ ��� ������ � ��������
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = button.gameObject;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void PreviewAnimation(bool showAnimation)
        {
            UIPanel panel = (UIPanel)target;

            if (Application.isPlaying)
            {
                if (showAnimation)
                {
                    panel.Show();
                }
                else
                {
                    panel.Hide();
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Preview Not Available", "Animation preview is only available in Play Mode", "OK");
            }
        }

        private void AddButtonToPanel()
        {
            UIPanel panel = (UIPanel)target;
            GameObject panelObj = panel.gameObject;

            // ��������� ����� ��� ������������ ���� ������
            AddButtonDialog dialog = ScriptableObject.CreateInstance<AddButtonDialog>();
            dialog.Initialize(panelObj.transform, panelRegistry);
            dialog.ShowUtility();
        }

        private void LoadAvailableButtons()
        {
            availableButtons.Clear();

            // ����������� ������� ������ � Resources
            Object[] buttonPrefabs = Resources.LoadAll("UI/Buttons", typeof(GameObject));
            foreach (Object obj in buttonPrefabs)
            {
                availableButtons.Add(obj.name);
            }

            // ������ ��������� ������
            if (!availableButtons.Contains("StandardButton"))
            {
                availableButtons.Add("StandardButton");
            }
        }

        private void SavePanelAsPrefab()
        {
            UIPanel panel = (UIPanel)target;
            GameObject panelObj = panel.gameObject;

            // ����������, �� ���� ���������
            string directory = "Assets/Resources/UI/Panels";
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // ��������� ���� �� �������
            string prefabPath = $"{directory}/{panelObj.name}.prefab";

            // �������� �� ������
            GameObject prefab = null;
#if UNITY_2018_3_OR_NEWER
            prefab = PrefabUtility.SaveAsPrefabAsset(panelObj, prefabPath);
#else
                prefab = PrefabUtility.CreatePrefab(prefabPath, panelObj);
#endif

            if (prefab != null)
            {
                EditorUtility.DisplayDialog("Prefab Created", $"Panel saved as prefab at: {prefabPath}", "OK");
                Selection.activeObject = prefab;

                // �������� � UIPanelRegistry, ���� ��������
                if (panelRegistry != null)
                {
                    panelRegistry.RegisterPanel(panelObj.name, prefab);
                    Debug.Log($"Panel '{panelObj.name}' registered in UIPanelRegistry");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Failed to create prefab", "OK");
            }
        }

        private void RegisterInPanelRegistry()
        {
            UIPanel panel = (UIPanel)target;
            GameObject panelObj = panel.gameObject;

            // ���������� �������� UIPanelRegistry
            if (panelRegistry == null)
            {
                EditorUtility.DisplayDialog("Error", "UIPanelRegistry not found in scene", "OK");
                return;
            }

            // ����������, �� ������ ��� � ��������
            GameObject prefabObj = panelObj;
            bool isPrefab = false;

#if UNITY_2018_3_OR_NEWER
            isPrefab = PrefabUtility.IsPartOfPrefabAsset(panelObj);
#else
                isPrefab = PrefabUtility.GetPrefabType(panelObj) == PrefabType.Prefab;
#endif

            // ���� �� ������, �������� �� ������
            if (!isPrefab)
            {
                // �������� �� ������
                string directory = "Assets/Resources/UI/Panels";
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                string prefabPath = $"{directory}/{panelObj.name}.prefab";

#if UNITY_2018_3_OR_NEWER
                prefabObj = PrefabUtility.SaveAsPrefabAsset(panelObj, prefabPath);
#else
                    prefabObj = PrefabUtility.CreatePrefab(prefabPath, panelObj);
#endif

                if (prefabObj == null)
                {
                    EditorUtility.DisplayDialog("Error", "Failed to create prefab for registration", "OK");
                    return;
                }
            }

            // �������� � UIPanelRegistry
            panelRegistry.RegisterPanel(prefabObj.name, prefabObj);
            EditorUtility.DisplayDialog("Registration Complete", $"Panel '{prefabObj.name}' registered in UIPanelRegistry", "OK");
        }
    }

    // ĳ������� ���� ��� ��������� ������ �� �����
    public class AddButtonDialog : EditorWindow
    {
        private Transform panelTransform;
        private string buttonText = "New Button";
        private string buttonCategory = "Default";
        private bool isBackButton = false;
        private string targetPanelName = "";
        private string clickSound = "ButtonClick";
        private string buttonPrefab = "StandardButton";

        private List<string> availablePanels = new List<string>();
        private List<string> availableSounds = new List<string>();
        private List<string> availableButtonPrefabs = new List<string>();
        private UIPanelRegistry panelRegistry;

        public void Initialize(Transform panel, UIPanelRegistry registry)
        {
            panelTransform = panel;
            panelRegistry = registry;
            LoadAvailableOptions();
        }

        private void OnGUI()
        {
            titleContent = new GUIContent("Add Button");

            EditorGUILayout.LabelField("Add Button to Panel", EditorStyles.boldLabel);

            buttonText = EditorGUILayout.TextField("Button Text", buttonText);
            buttonCategory = EditorGUILayout.TextField("Button Category", buttonCategory);

            // ���� ���� ������
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Button Type", EditorStyles.boldLabel);

            isBackButton = EditorGUILayout.Toggle("Is Back Button", isBackButton);

            if (!isBackButton)
            {
                // ���� ������� �����
                int currentIndex = 0;
                if (!string.IsNullOrEmpty(targetPanelName))
                {
                    currentIndex = availablePanels.IndexOf(targetPanelName);
                    if (currentIndex < 0) currentIndex = 0;
                }

                int newIndex = EditorGUILayout.Popup("Target Panel", currentIndex, availablePanels.ToArray());

                if (newIndex != currentIndex)
                {
                    targetPanelName = (newIndex > 0) ? availablePanels[newIndex] : "";
                }
            }

            // ����
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sound", EditorStyles.boldLabel);

            int soundIndex = availableSounds.IndexOf(clickSound);
            if (soundIndex < 0) soundIndex = 0;

            int newSoundIndex = EditorGUILayout.Popup("Click Sound", soundIndex, availableSounds.ToArray());
            if (newSoundIndex != soundIndex)
            {
                clickSound = availableSounds[newSoundIndex];
            }

            // ���� �������
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Button Prefab", EditorStyles.boldLabel);

            int prefabIndex = availableButtonPrefabs.IndexOf(buttonPrefab);
            if (prefabIndex < 0) prefabIndex = 0;

            int newPrefabIndex = EditorGUILayout.Popup("Button Template", prefabIndex, availableButtonPrefabs.ToArray());
            if (newPrefabIndex != prefabIndex)
            {
                buttonPrefab = availableButtonPrefabs[newPrefabIndex];
            }

            // ������ ��
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Cancel"))
            {
                Close();
            }

            if (GUILayout.Button("Create Button"))
            {
                CreateButton();
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CreateButton()
        {
            // �������� ����������� ������ ������
            GameObject buttonPrefabObj = Resources.Load<GameObject>($"UI/Buttons/{buttonPrefab}");

            GameObject buttonObj;

            if (buttonPrefabObj != null)
            {
                // ��������� �� �������
                buttonObj = Instantiate(buttonPrefabObj, panelTransform);
                buttonObj.name = buttonText + "Button";

                // ������ ��������� ��������� � ������������ �����
                TMPro.TextMeshProUGUI text = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = buttonText;
                }
            }
            else
            {
                // ��������� ���� ������
                buttonObj = new GameObject(buttonText + "Button");
                RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
                buttonObj.AddComponent<CanvasRenderer>();
                Image image = buttonObj.AddComponent<Image>();
                Button button = buttonObj.AddComponent<Button>();

                // ������������ ����������� ��'���
                buttonObj.transform.SetParent(panelTransform);
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
                TMPro.TextMeshProUGUI text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
                text.text = buttonText;
                text.color = Color.black;
                text.fontSize = 14;
                text.alignment = TMPro.TextAlignmentOptions.Center;
            }

            // ������ UIButton ���������, ���� ���� �� ����
            UIButton uiButton = buttonObj.GetComponent<UIButton>();
            if (uiButton == null)
            {
                uiButton = buttonObj.AddComponent<UIButton>();
            }

            // ����������� ����������
            SerializedObject buttonSerializedObj = new SerializedObject(uiButton);
            buttonSerializedObj.FindProperty("buttonCategory").stringValue = buttonCategory;
            buttonSerializedObj.FindProperty("isBackButton").boolValue = isBackButton;
            buttonSerializedObj.FindProperty("showPanelName").stringValue = isBackButton ? "" : targetPanelName;
            buttonSerializedObj.FindProperty("clickSoundName").stringValue = clickSound;
            buttonSerializedObj.ApplyModifiedProperties();

            // �������� ������������ ������
            Selection.activeGameObject = buttonObj;

            Debug.Log($"Button '{buttonText}' added to panel");
        }

        private void LoadAvailableOptions()
        {
            // ����������� ������� �����
            availablePanels = new List<string> { "None" };

            // �������� ����� � UIPanelRegistry
            if (panelRegistry != null)
            {
                string[] registeredPanels = panelRegistry.GetAllRegisteredPanelNames();
                if (registeredPanels != null && registeredPanels.Length > 0)
                {
                    availablePanels.AddRange(registeredPanels);
                }
            }

            // ��������� ����������� � Resources
            Object[] resourcePanels = Resources.LoadAll("UI/Panels", typeof(GameObject));
            foreach (Object obj in resourcePanels)
            {
                string panelName = obj.name;
                if (!availablePanels.Contains(panelName))
                {
                    availablePanels.Add(panelName);
                }
            }

            // ����������� ������� �����
            availableSounds = new List<string>();
            availableSounds.Add("ButtonClick"); // ����������� ����

            // ����������� ����� � Resources
            Object[] audioClips = Resources.LoadAll("Audio", typeof(AudioClip));
            foreach (Object obj in audioClips)
            {
                string soundName = obj.name;
                if (!availableSounds.Contains(soundName))
                {
                    availableSounds.Add(soundName);
                }
            }

            // ����������� ������� ������� ������
            availableButtonPrefabs = new List<string>();
            availableButtonPrefabs.Add("StandardButton"); // ����������� ������

            Object[] buttonPrefabs = Resources.LoadAll("UI/Buttons", typeof(GameObject));
            foreach (Object obj in buttonPrefabs)
            {
                string prefabName = obj.name;
                if (!availableButtonPrefabs.Contains(prefabName))
                {
                    availableButtonPrefabs.Add(prefabName);
                }
            }
        }
    }
}