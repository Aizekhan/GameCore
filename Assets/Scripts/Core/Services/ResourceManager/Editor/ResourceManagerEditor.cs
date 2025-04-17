#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using System;
using UnityEditor.SceneManagement;

namespace GameCore.Core.Editor
{
    /// <summary>
    /// EditorWindow для управління ресурсами в проекті.
    /// Дозволяє створювати та редагувати бандли, аналізувати ресурси та вносити їх 
    /// налаштування централізовано.
    /// </summary>
    public class ResourceManagerEditor : EditorWindow
    {
        // Закладки
        private enum Tab
        {
            BundleManager,
            ResourceAnalyzer,
            Settings
        }

        private Tab _currentTab = Tab.BundleManager;

        // Секція "Менеджер бандлів"
        private List<ResourceBundle> _bundles = new List<ResourceBundle>();
        private ResourceBundle _selectedBundle;
        private Vector2 _bundleListScroll;
        private Vector2 _bundleDetailScroll;
        private bool _showBundleFoldout = true;
        private ResourceManager.ResourceType _newResourceType = ResourceManager.ResourceType.Prefab;
        private string _newResourcePath = "";
        private bool _newResourcePreload = false;
        private int _newResourcePoolSize = 0;
        private string _bundlesPath = "Assets/Resources/ResourceBundles";

        // Секція "Аналізатор ресурсів"
        private Vector2 _analyzerScroll;
        private bool _showUnusedResources = true;
        private bool _showDuplicateResources = true;
        private bool _showLargeResources = true;
        private bool _analyzingResources = false;
        private List<string> _unusedResources = new List<string>();
        private List<string> _duplicateResources = new List<string>();
        private List<KeyValuePair<string, long>> _largeResources = new List<KeyValuePair<string, long>>();
        private int _largeResourceThreshold = 1024 * 1024; // 1MB

        // Секція "Налаштування"
        private Vector2 _settingsScroll;
        private bool _useObjectPooling = true;
        private int _defaultPoolSize = 10;
        private bool _logResourceOperations = true;
        private bool _autoCleanupOnSceneChange = true;
        private string[] _resourcePaths = new string[10]; // Для 10 типів ресурсів

        [MenuItem("GameCore/Resource Manager")]
        public static void ShowWindow()
        {
            GetWindow<ResourceManagerEditor>("Resource Manager");
        }

        private void OnEnable()
        {
            LoadAllBundles();
            LoadSettings();
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.Space();

            switch (_currentTab)
            {
                case Tab.BundleManager:
                    DrawBundleManager();
                    break;
                case Tab.ResourceAnalyzer:
                    DrawResourceAnalyzer();
                    break;
                case Tab.Settings:
                    DrawSettings();
                    break;
            }
        }

        #region Базові функції UI

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Вкладки
            if (GUILayout.Toggle(_currentTab == Tab.BundleManager, "Bundle Manager", EditorStyles.toolbarButton))
                _currentTab = Tab.BundleManager;

            if (GUILayout.Toggle(_currentTab == Tab.ResourceAnalyzer, "Resource Analyzer", EditorStyles.toolbarButton))
                _currentTab = Tab.ResourceAnalyzer;

            if (GUILayout.Toggle(_currentTab == Tab.Settings, "Settings", EditorStyles.toolbarButton))
                _currentTab = Tab.Settings;

            GUILayout.FlexibleSpace();

            // Кнопки додаткових дій
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                RefreshData();

            if (GUILayout.Button("Apply", EditorStyles.toolbarButton))
                ApplyChanges();

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshData()
        {
            LoadAllBundles();

            // Оновлюємо аналіз ресурсів, якщо вкладка відкрита
            if (_currentTab == Tab.ResourceAnalyzer && _analyzingResources)
                AnalyzeResources();

            Debug.Log("Resource Manager: Data refreshed");
        }

        private void ApplyChanges()
        {
            SaveSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Resource Manager: Changes applied");
        }

        #endregion

        #region Менеджер бандлів

        private void DrawBundleManager()
        {
            EditorGUILayout.BeginHorizontal();

            // Ліва панель - список бандлів
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            DrawBundleList();
            EditorGUILayout.EndVertical();

            // Права панель - деталі бандлу
            EditorGUILayout.BeginVertical();
            DrawBundleDetails();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBundleList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Заголовок і кнопки
            EditorGUILayout.BeginHorizontal();
            _showBundleFoldout = EditorGUILayout.Foldout(_showBundleFoldout, "Resource Bundles", true);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+", GUILayout.Width(25)))
                CreateNewBundle();

            EditorGUILayout.EndHorizontal();

            if (_showBundleFoldout)
            {
                _bundleListScroll = EditorGUILayout.BeginScrollView(_bundleListScroll);

                // Список бандлів
                for (int i = 0; i < _bundles.Count; i++)
                {
                    ResourceBundle bundle = _bundles[i];
                    EditorGUILayout.BeginHorizontal();

                    // Виділення
                    bool isSelected = _selectedBundle == bundle;
                    bool newSelection = GUILayout.Toggle(isSelected, bundle.name, EditorStyles.miniButton);

                    if (!isSelected && newSelection)
                    {
                        _selectedBundle = bundle;
                    }

                    // Кнопка видалення
                    if (GUILayout.Button("✕", GUILayout.Width(25)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Bundle",
                            $"Are you sure you want to delete the bundle '{bundle.name}'?",
                            "Yes", "Cancel"))
                        {
                            DeleteBundle(bundle);
                            i--;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBundleDetails()
        {
            if (_selectedBundle == null)
            {
                EditorGUILayout.HelpBox("Select a bundle to edit its details", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Заголовок
            EditorGUILayout.LabelField($"Bundle: {_selectedBundle.name}", EditorStyles.boldLabel);

            _bundleDetailScroll = EditorGUILayout.BeginScrollView(_bundleDetailScroll);

            // Редагування властивостей бандлу
            EditorGUI.BeginChangeCheck();

            // ID бандлу
            SerializedObject serializedObject = new SerializedObject(_selectedBundle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bundleId"));

            // Налаштування
            EditorGUILayout.PropertyField(serializedObject.FindProperty("loadOnDemand"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unloadOnSceneChange"));

            // Ресурси
            EditorGUILayout.PropertyField(serializedObject.FindProperty("resources"), true);

            // Залежності
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dependencies"), true);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_selectedBundle);
            }

            EditorGUILayout.Space();

            // Додавання нового ресурсу
            EditorGUILayout.LabelField("Add New Resource", EditorStyles.boldLabel);

            _newResourceType = (ResourceManager.ResourceType)EditorGUILayout.EnumPopup("Resource Type", _newResourceType);

            // Вибір шляху до ресурсу через тип
            EditorGUILayout.BeginHorizontal();
            _newResourcePath = EditorGUILayout.TextField("Resource Path", _newResourcePath);

            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string extension = GetExtensionForResourceType(_newResourceType);
                string path = EditorUtility.OpenFilePanel("Select Resource", "Assets/Resources", extension);

                if (!string.IsNullOrEmpty(path))
                {
                    // Конвертуємо абсолютний шлях у відносний шлях Resources
                    int resourcesIndex = path.IndexOf("/Resources/");
                    if (resourcesIndex >= 0)
                    {
                        _newResourcePath = path.Substring(resourcesIndex + "/Resources/".Length);

                        // Видаляємо розширення
                        _newResourcePath = Path.ChangeExtension(_newResourcePath, null);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            // Додаткові опції для префабів (пулінг)
            if (_newResourceType == ResourceManager.ResourceType.Prefab)
            {
                _newResourcePreload = EditorGUILayout.Toggle("Preload", _newResourcePreload);

                if (_newResourcePreload)
                {
                    _newResourcePoolSize = EditorGUILayout.IntSlider("Pool Size", _newResourcePoolSize, 1, 50);
                }
            }

            // Кнопка додавання ресурсу
            if (GUILayout.Button("Add Resource"))
            {
                if (!string.IsNullOrEmpty(_newResourcePath))
                {
                    AddResourceToBundle();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Resource path cannot be empty", "OK");
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void CreateNewBundle()
        {
            // Створюємо директорію для бандлів, якщо вона не існує
            if (!Directory.Exists(_bundlesPath))
            {
                Directory.CreateDirectory(_bundlesPath);
            }

            // Створюємо новий бандл
            ResourceBundle newBundle = CreateInstance<ResourceBundle>();
            string bundleName = "NewBundle";
            int counter = 1;

            // Знаходимо унікальне ім'я
            string assetPath = $"{_bundlesPath}/{bundleName}.asset";
            while (File.Exists(assetPath))
            {
                bundleName = $"NewBundle_{counter}";
                assetPath = $"{_bundlesPath}/{bundleName}.asset";
                counter++;
            }

            // Зберігаємо бандл
            AssetDatabase.CreateAsset(newBundle, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Оновлюємо список і вибираємо новий бандл
            LoadAllBundles();
            _selectedBundle = newBundle;
        }

        private void DeleteBundle(ResourceBundle bundle)
        {
            // Видаляємо бандл
            string assetPath = AssetDatabase.GetAssetPath(bundle);
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Оновлюємо список
            LoadAllBundles();

            // Скидаємо вибраний бандл, якщо він був видалений
            if (_selectedBundle == bundle)
            {
                _selectedBundle = null;
            }
        }

        private void AddResourceToBundle()
        {
            // Перевіряємо, чи існує ресурс
            string fullPath = $"Assets/Resources/{_newResourcePath}";
            string extension = GetExtensionForResourceType(_newResourceType);

            if (!File.Exists(fullPath + extension) && !Directory.Exists(fullPath))
            {
                EditorUtility.DisplayDialog("Error", $"Resource not found: {_newResourcePath}", "OK");
                return;
            }

            // Додаємо ресурс до бандлу
            SerializedObject serializedObject = new SerializedObject(_selectedBundle);
            SerializedProperty resourcesProperty = serializedObject.FindProperty("resources");

            int index = resourcesProperty.arraySize;
            resourcesProperty.arraySize++;

            SerializedProperty entryProperty = resourcesProperty.GetArrayElementAtIndex(index);
            entryProperty.FindPropertyRelative("resourceName").stringValue = _newResourcePath;
            entryProperty.FindPropertyRelative("resourceType").enumValueIndex = (int)_newResourceType;
            entryProperty.FindPropertyRelative("preload").boolValue = _newResourcePreload;
            entryProperty.FindPropertyRelative("poolSize").intValue = _newResourcePoolSize;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(_selectedBundle);

            // Скидаємо поля введення
            _newResourcePath = "";
            _newResourcePreload = false;
            _newResourcePoolSize = 0;
        }

        private string GetExtensionForResourceType(ResourceManager.ResourceType type)
        {
            switch (type)
            {
                case ResourceManager.ResourceType.Prefab:
                    return ".prefab";
                case ResourceManager.ResourceType.Textures:
                    return "png,jpg,jpeg,tga,psd";
                case ResourceManager.ResourceType.Audio:
                    return "mp3,wav,ogg";
                case ResourceManager.ResourceType.Models:
                    return "fbx,obj";
                case ResourceManager.ResourceType.ScriptableObjects:
                    return ".asset";
                default:
                    return "";
            }
        }

        private void LoadAllBundles()
        {
            _bundles.Clear();

            // Шукаємо всі бандли в директорії Resources/ResourceBundles
            string[] guids = AssetDatabase.FindAssets("t:ResourceBundle", new[] { "Assets/Resources" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ResourceBundle bundle = AssetDatabase.LoadAssetAtPath<ResourceBundle>(path);

                if (bundle != null)
                {
                    _bundles.Add(bundle);
                }
            }
        }

        #endregion

        #region Аналізатор ресурсів

        private void DrawResourceAnalyzer()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Resource Analyzer", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // Опції аналізу
            EditorGUILayout.BeginVertical(GUILayout.Width(200));

            _showUnusedResources = EditorGUILayout.Toggle("Show Unused Resources", _showUnusedResources);
            _showDuplicateResources = EditorGUILayout.Toggle("Show Duplicate Resources", _showDuplicateResources);
            _showLargeResources = EditorGUILayout.Toggle("Show Large Resources", _showLargeResources);

            if (_showLargeResources)
            {
                EditorGUI.indentLevel++;
                _largeResourceThreshold = EditorGUILayout.IntField("Size Threshold (KB)", _largeResourceThreshold / 1024) * 1024;
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            if (GUILayout.Button(_analyzingResources ? "Stop Analysis" : "Start Analysis"))
            {
                _analyzingResources = !_analyzingResources;

                if (_analyzingResources)
                {
                    AnalyzeResources();
                }
            }

            EditorGUILayout.EndVertical();

            // Результати аналізу
            EditorGUILayout.BeginVertical();

            _analyzerScroll = EditorGUILayout.BeginScrollView(_analyzerScroll);

            if (!_analyzingResources)
            {
                EditorGUILayout.HelpBox("Click 'Start Analysis' to scan your project resources", MessageType.Info);
            }
            else
            {
                // Невикористані ресурси
                if (_showUnusedResources && _unusedResources.Count > 0)
                {
                    EditorGUILayout.LabelField("Unused Resources", EditorStyles.boldLabel);

                    foreach (string resource in _unusedResources)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(resource);

                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(resource);
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space();
                }

                // Дублікати ресурсів
                if (_showDuplicateResources && _duplicateResources.Count > 0)
                {
                    EditorGUILayout.LabelField("Duplicate Resources", EditorStyles.boldLabel);

                    foreach (string resource in _duplicateResources)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(resource);

                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(resource);
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space();
                }

                // Великі ресурси
                if (_showLargeResources && _largeResources.Count > 0)
                {
                    EditorGUILayout.LabelField("Large Resources", EditorStyles.boldLabel);

                    foreach (var resource in _largeResources)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{resource.Key} ({FormatSize(resource.Value)})");

                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(resource.Key);
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void AnalyzeResources()
        {
            _unusedResources.Clear();
            _duplicateResources.Clear();
            _largeResources.Clear();

            // Отримуємо всі ресурси в папці Resources
            string[] resourceGuids = AssetDatabase.FindAssets("", new[] { "Assets/Resources" });
            List<string> resourcePaths = new List<string>();

            foreach (string guid in resourceGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Пропускаємо папки
                if (AssetDatabase.IsValidFolder(path))
                    continue;

                resourcePaths.Add(path);

                // Аналіз великих ресурсів
                if (_showLargeResources)
                {
                    long fileSize = new FileInfo(path).Length;

                    if (fileSize > _largeResourceThreshold)
                    {
                        _largeResources.Add(new KeyValuePair<string, long>(path, fileSize));
                    }
                }
            }

            // Сортуємо великі ресурси за розміром (від більшого до меншого)
            _largeResources.Sort((a, b) => b.Value.CompareTo(a.Value));

            // Аналіз дублікатів (за хешами вмісту)
            if (_showDuplicateResources)
            {
                Dictionary<string, List<string>> contentHashes = new Dictionary<string, List<string>>();

                foreach (string path in resourcePaths)
                {
                    try
                    {
                        // Читаємо файл
                        byte[] fileData = File.ReadAllBytes(path);
                        string hash = ComputeHash(fileData);

                        if (!contentHashes.ContainsKey(hash))
                        {
                            contentHashes[hash] = new List<string>();
                        }

                        contentHashes[hash].Add(path);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error analyzing file {path}: {ex.Message}");
                    }
                }

                // Знаходимо дублікати (файли з однаковим хешем)
                foreach (var pair in contentHashes)
                {
                    if (pair.Value.Count > 1)
                    {
                        _duplicateResources.AddRange(pair.Value.Skip(1)); // Перший файл не вважається дублікатом
                    }
                }
            }

            // Аналіз невикористаних ресурсів (для цього потрібно перевірити посилання в сценах і скриптах)
            if (_showUnusedResources)
            {
                HashSet<string> referencedResources = new HashSet<string>();

                // Перевіряємо всі сцени
                string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
                foreach (string guid in sceneGuids)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                    FindReferencesInScene(scenePath, referencedResources);
                }

                // Перевіряємо всі скрипти
                string[] scriptGuids = AssetDatabase.FindAssets("t:Script");
                foreach (string guid in scriptGuids)
                {
                    string scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                    FindReferencesInScript(scriptPath, referencedResources);
                }

                // Знаходимо невикористані ресурси
                foreach (string path in resourcePaths)
                {
                    if (!referencedResources.Contains(path))
                    {
                        _unusedResources.Add(path);
                    }
                }
            }
        }

        private string ComputeHash(byte[] data)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(data);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private void FindReferencesInScene(string scenePath, HashSet<string> referencedResources)
        {
            // Відкриваємо сцену і шукаємо посилання
            try
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                GameObject[] rootObjects = scene.GetRootGameObjects();

                foreach (GameObject obj in rootObjects)
                {
                    FindReferencesInGameObject(obj, referencedResources);
                }

                EditorSceneManager.CloseScene(scene, true);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error analyzing scene {scenePath}: {ex.Message}");
            }
        }

        private void FindReferencesInGameObject(GameObject obj, HashSet<string> referencedResources)
        {
            // Перевіряємо всі компоненти
            Component[] components = obj.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                    continue;

                // Використовуємо серіалізацію для пошуку посилань на ресурси
                SerializedObject serializedObject = new SerializedObject(component);
                SerializedProperty iterator = serializedObject.GetIterator();

                while (iterator.NextVisible(true))
                {
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue != null)
                    {
                        string path = AssetDatabase.GetAssetPath(iterator.objectReferenceValue);

                        if (!string.IsNullOrEmpty(path) && path.Contains("Resources/"))
                        {
                            referencedResources.Add(path);
                        }
                    }
                }
            }

            // Рекурсивно для дочірніх об'єктів
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                FindReferencesInGameObject(obj.transform.GetChild(i).gameObject, referencedResources);
            }
        }

        private void FindReferencesInScript(string scriptPath, HashSet<string> referencedResources)
        {
            try
            {
                string scriptContent = File.ReadAllText(scriptPath);

                // Шукаємо рядки типу "Resources.Load"
                int index = 0;
                while ((index = scriptContent.IndexOf("Resources.Load", index)) != -1)
                {
                    // Знаходимо рядок у лапках після Resources.Load
                    int startQuote = scriptContent.IndexOf('"', index);
                    if (startQuote != -1)
                    {
                        int endQuote = scriptContent.IndexOf('"', startQuote + 1);
                        if (endQuote != -1)
                        {
                            string resourcePath = scriptContent.Substring(startQuote + 1, endQuote - startQuote - 1);

                            // Додаємо шлях до списку
                            string fullPath = $"Assets/Resources/{resourcePath}";
                            if (File.Exists(fullPath))
                            {
                                referencedResources.Add(fullPath);
                            }
                        }
                    }

                    index += "Resources.Load".Length;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error analyzing script {scriptPath}: {ex.Message}");
            }
        }

        private string FormatSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            double size = bytes;

            while (size > 1024 && counter < suffixes.Length - 1)
            {
                size /= 1024;
                counter++;
            }

            return $"{size:0.##} {suffixes[counter]}";
        }

        #endregion

        #region Налаштування

        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Resource Manager Settings", EditorStyles.boldLabel);

            _settingsScroll = EditorGUILayout.BeginScrollView(_settingsScroll);

            // Основні налаштування
            EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
            _useObjectPooling = EditorGUILayout.Toggle("Use Object Pooling", _useObjectPooling);
            _defaultPoolSize = EditorGUILayout.IntField("Default Pool Size", _defaultPoolSize);
            _logResourceOperations = EditorGUILayout.Toggle("Log Resource Operations", _logResourceOperations);
            _autoCleanupOnSceneChange = EditorGUILayout.Toggle("Auto Cleanup On Scene Change", _autoCleanupOnSceneChange);

            EditorGUILayout.Space();

            // Шляхи до ресурсів
            EditorGUILayout.LabelField("Resource Paths", EditorStyles.boldLabel);

            string[] resourceTypeNames = System.Enum.GetNames(typeof(ResourceManager.ResourceType));
            for (int i = 0; i < resourceTypeNames.Length && i < _resourcePaths.Length; i++)
            {
                _resourcePaths[i] = EditorGUILayout.TextField(resourceTypeNames[i], _resourcePaths[i]);
            }

            EditorGUILayout.Space();

            // Кнопки
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset to Defaults"))
            {
                ResetSettingsToDefaults();
            }

            if (GUILayout.Button("Apply Settings"))
            {
                SaveSettings();
                EditorUtility.DisplayDialog("Settings", "Settings have been saved", "OK");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void LoadSettings()
        {
            // Завантажуємо налаштування з EditorPrefs
            _useObjectPooling = EditorPrefs.GetBool("ResourceManager_UseObjectPooling", true);
            _defaultPoolSize = EditorPrefs.GetInt("ResourceManager_DefaultPoolSize", 10);
            _logResourceOperations = EditorPrefs.GetBool("ResourceManager_LogResourceOperations", true);
            _autoCleanupOnSceneChange = EditorPrefs.GetBool("ResourceManager_AutoCleanupOnSceneChange", true);

            // Завантажуємо шляхи до ресурсів
            string[] resourceTypeNames = System.Enum.GetNames(typeof(ResourceManager.ResourceType));
            for (int i = 0; i < resourceTypeNames.Length && i < _resourcePaths.Length; i++)
            {
                _resourcePaths[i] = EditorPrefs.GetString($"ResourceManager_Path_{resourceTypeNames[i]}", resourceTypeNames[i]);
            }
        }

        private void SaveSettings()
        {
            // Зберігаємо налаштування в EditorPrefs
            EditorPrefs.SetBool("ResourceManager_UseObjectPooling", _useObjectPooling);
            EditorPrefs.SetInt("ResourceManager_DefaultPoolSize", _defaultPoolSize);
            EditorPrefs.SetBool("ResourceManager_LogResourceOperations", _logResourceOperations);
            EditorPrefs.SetBool("ResourceManager_AutoCleanupOnSceneChange", _autoCleanupOnSceneChange);

            // Зберігаємо шляхи до ресурсів
            string[] resourceTypeNames = System.Enum.GetNames(typeof(ResourceManager.ResourceType));
            for (int i = 0; i < resourceTypeNames.Length && i < _resourcePaths.Length; i++)
            {
                EditorPrefs.SetString($"ResourceManager_Path_{resourceTypeNames[i]}", _resourcePaths[i]);
            }
        }

        private void ResetSettingsToDefaults()
        {
            _useObjectPooling = true;
            _defaultPoolSize = 10;
            _logResourceOperations = true;
            _autoCleanupOnSceneChange = true;

            // Скидаємо шляхи до ресурсів
            string[] resourceTypeNames = System.Enum.GetNames(typeof(ResourceManager.ResourceType));
            for (int i = 0; i < resourceTypeNames.Length && i < _resourcePaths.Length; i++)
            {
                _resourcePaths[i] = resourceTypeNames[i];
            }
        }

        #endregion
    }
}
#endif