using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using static UnityEngine.GraphicsBuffer;

namespace GameCore.Core.Editor
{
    [CustomEditor(typeof(UIButton))]
    public class UIButtonEditor : UnityEditor.Editor
    {
        private SerializedProperty buttonCategoryProp;
        private SerializedProperty isBackButtonProp;
        private SerializedProperty showPanelNameProp;
        private SerializedProperty clickSoundNameProp;
        private SerializedProperty hoverSoundNameProp;
        private SerializedProperty soundTypeProp;
        private SerializedProperty useHoverAnimationProp;
        private SerializedProperty hoverScaleProp;
        private SerializedProperty animationSpeedProp;

        private List<string> availablePanels = new List<string>();
        private List<string> availableSounds = new List<string>();
        private UIPanelRegistry panelRegistry;
        private AudioManager audioManager;

        private void OnEnable()
        {
            buttonCategoryProp = serializedObject.FindProperty("buttonCategory");
            isBackButtonProp = serializedObject.FindProperty("isBackButton");
            showPanelNameProp = serializedObject.FindProperty("showPanelName");
            clickSoundNameProp = serializedObject.FindProperty("clickSoundName");
            hoverSoundNameProp = serializedObject.FindProperty("hoverSoundName");
            soundTypeProp = serializedObject.FindProperty("soundType");
            useHoverAnimationProp = serializedObject.FindProperty("useHoverAnimation");
            hoverScaleProp = serializedObject.FindProperty("hoverScale");
            animationSpeedProp = serializedObject.FindProperty("animationSpeed");

            // Знаходимо необхідні сервіси
            panelRegistry = FindObjectOfType<UIPanelRegistry>();
            audioManager = FindObjectOfType<AudioManager>();

            LoadAvailablePanels();
            LoadAvailableSounds();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("UIButton Settings", EditorStyles.boldLabel);

            // Кнопка оновлення даних
            if (GUILayout.Button("Refresh Available Panels"))
            {
                LoadAvailablePanels();
            }

            // Основні налаштування
            EditorGUILayout.PropertyField(buttonCategoryProp);

            // Навігація
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Navigation", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(isBackButtonProp);

            if (!isBackButtonProp.boolValue)
            {
                DrawPanelSelector();
            }

            // Аудіо
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);

            DrawSoundSelector("Click Sound", clickSoundNameProp);
            DrawSoundSelector("Hover Sound", hoverSoundNameProp);
            EditorGUILayout.PropertyField(soundTypeProp);

            // Анімація
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(useHoverAnimationProp);

            if (useHoverAnimationProp.boolValue)
            {
                EditorGUILayout.PropertyField(hoverScaleProp);
                EditorGUILayout.PropertyField(animationSpeedProp);

                // Попередній перегляд анімації
                if (GUILayout.Button("Preview Hover Animation"))
                {
                    PreviewHoverAnimation();
                }
            }

            EditorGUILayout.Space();

            // Кнопки дій
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Duplicate Button"))
            {
                DuplicateButton();
            }

            if (GUILayout.Button("Reset to Defaults"))
            {
                ResetButtonToDefaults();
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPanelSelector()
        {
            int currentIndex = 0;
            string currentPanel = showPanelNameProp.stringValue;

            if (!string.IsNullOrEmpty(currentPanel))
            {
                currentIndex = availablePanels.IndexOf(currentPanel);
                if (currentIndex < 0) currentIndex = 0;
            }

            int newIndex = EditorGUILayout.Popup("Target Panel", currentIndex, availablePanels.ToArray());

            if (newIndex != currentIndex)
            {
                showPanelNameProp.stringValue = (newIndex > 0) ? availablePanels[newIndex] : "";
            }
        }

        private void DrawSoundSelector(string label, SerializedProperty soundProp)
        {
            int currentIndex = 0;
            string currentSound = soundProp.stringValue;

            if (!string.IsNullOrEmpty(currentSound))
            {
                currentIndex = availableSounds.IndexOf(currentSound);
                if (currentIndex < 0) currentIndex = 0;
            }

            int newIndex = EditorGUILayout.Popup(label, currentIndex, availableSounds.ToArray());

            if (newIndex != currentIndex)
            {
                soundProp.stringValue = (newIndex > 0) ? availableSounds[newIndex] : "";
            }

            // Додаємо можливість прослуховувати звук
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            if (GUILayout.Button("Play Sound", GUILayout.Width(100)))
            {
                PlaySound(soundProp.stringValue);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void LoadAvailablePanels()
        {
            availablePanels = new List<string> { "None" };

            // Отримуємо панелі з UIPanelRegistry
            if (panelRegistry != null)
            {
                string[] registeredPanels = panelRegistry.GetAllRegisteredPanelNames();
                if (registeredPanels != null && registeredPanels.Length > 0)
                {
                    availablePanels.AddRange(registeredPanels);
                }
            }

            // Також можна пошукати панелі в Resources
            Object[] resourcePanels = Resources.LoadAll("UI/Panels", typeof(GameObject));
            foreach (Object obj in resourcePanels)
            {
                string panelName = obj.name;
                if (!availablePanels.Contains(panelName))
                {
                    availablePanels.Add(panelName);
                }
            }
        }

        private void LoadAvailableSounds()
        {
            availableSounds = new List<string> { "None" };

            // Завантажуємо звуки з Resources
            Object[] audioClips = Resources.LoadAll("Audio", typeof(AudioClip));
            foreach (Object obj in audioClips)
            {
                availableSounds.Add(obj.name);
            }

            // Додаємо стандартні звуки, якщо вони ще не в списку
            string[] defaultSounds = { "ButtonClick", "ButtonHover", "MenuOpen", "MenuClose" };
            foreach (string sound in defaultSounds)
            {
                if (!availableSounds.Contains(sound))
                {
                    availableSounds.Add(sound);
                }
            }
        }

        private void PreviewHoverAnimation()
        {
            UIButton button = (UIButton)target;

            if (Application.isPlaying)
            {
                // Симулюємо наведення миші
                button.SendMessage("OnPointerEnter", null, SendMessageOptions.DontRequireReceiver);

                // Через 1 секунду симулюємо виведення миші
                EditorApplication.delayCall += () => {
                    button.SendMessage("OnPointerExit", null, SendMessageOptions.DontRequireReceiver);
                };
            }
            else
            {
                EditorUtility.DisplayDialog("Preview Not Available", "Animation preview is only available in Play Mode", "OK");
            }
        }

        private void PlaySound(string soundName)
        {
            if (string.IsNullOrEmpty(soundName) || soundName == "None")
                return;

            // Спробуємо через AudioManager, якщо в Play Mode
            if (Application.isPlaying && audioManager != null)
            {
                audioManager.PlaySound(soundName, AudioType.UI);
                return;
            }

            // Якщо не в Play Mode або AudioManager недоступний, використовуємо AudioSource.PlayClipAtPoint
            AudioClip clip = Resources.Load<AudioClip>($"Audio/{soundName}");

            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, Vector3.zero);
            }
            else
            {
                EditorUtility.DisplayDialog("Sound Not Found", $"Could not find audio clip: {soundName}", "OK");
            }
        }

        private void DuplicateButton()
        {
            UIButton originalButton = (UIButton)target;
            GameObject original = originalButton.gameObject;

            GameObject duplicate = Instantiate(original, original.transform.parent);
            duplicate.name = original.name + " (Copy)";

            // Оновлюємо вибір в ієрархії
            Selection.activeGameObject = duplicate;

            EditorUtility.DisplayDialog("Button Duplicated", $"Created a copy of '{original.name}'", "OK");
        }

        private void ResetButtonToDefaults()
        {
            buttonCategoryProp.stringValue = "Default";
            isBackButtonProp.boolValue = false;
            showPanelNameProp.stringValue = "";
            clickSoundNameProp.stringValue = "ButtonClick";
            hoverSoundNameProp.stringValue = "ButtonHover";
            useHoverAnimationProp.boolValue = true;
            hoverScaleProp.floatValue = 1.05f;
            animationSpeedProp.floatValue = 10f;

            serializedObject.ApplyModifiedProperties();

            EditorUtility.DisplayDialog("Reset Complete", "Button has been reset to default settings", "OK");
        }
    }
}