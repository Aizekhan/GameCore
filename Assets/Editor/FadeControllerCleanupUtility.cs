#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace GameCore.Core.Editor
{
    /// <summary>
    /// ������ ��� ������ � ��������� �������� �� ��������� FadeController
    /// </summary>
    public class FadeControllerCleanupUtility : EditorWindow
    {
        private Vector2 scrollPosition;
        private string searchPath = "Assets/Scripts";
        private List<string> results = new List<string>();
        private Dictionary<string, List<int>> foundLines = new Dictionary<string, List<int>>();
        private bool showDetails = true;
        private List<string> scriptsToFix = new List<string>();
        private Color highlightColor = new Color(1f, 0.5f, 0.5f, 0.3f);

        [MenuItem("GameCore/Tools/FadeController Cleanup")]
        public static void ShowWindow()
        {
            GetWindow<FadeControllerCleanupUtility>("FadeController Cleanup");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("FadeController Cleanup Utility", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("�� ������ �������� ������ � �������� ��������� �� ��������� FadeController.", MessageType.Info);

            EditorGUILayout.Space();

            // ���� ������
            searchPath = EditorGUILayout.TextField("���� ������:", searchPath);

            EditorGUILayout.Space();

            // ������ ��
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("������ ������� ���������", GUILayout.Height(30)))
            {
                FindObsoleteReferences();
            }

            GUI.enabled = results.Count > 0;
            if (GUILayout.Button("³������ �� �����", GUILayout.Height(30)))
            {
                OpenAllFoundFiles();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // ³���������� ����������
            EditorGUILayout.LabelField("���������� ������:", EditorStyles.boldLabel);
            showDetails = EditorGUILayout.Toggle("�������� �����", showDetails);

            if (results.Count == 0)
            {
                EditorGUILayout.HelpBox("���� ����������. �������� '������ ������� ���������'.", MessageType.Info);
            }
            else
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                foreach (var file in results)
                {
                    EditorGUILayout.BeginHorizontal();

                    // ³������� ���� �� �����
                    string displayPath = file.Replace(Application.dataPath, "Assets");

                    EditorGUILayout.LabelField(displayPath, EditorStyles.boldLabel);

                    if (GUILayout.Button("³������", GUILayout.Width(100)))
                    {
                        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(file, 1);
                    }

                    EditorGUILayout.EndHorizontal();

                    if (showDetails && foundLines.ContainsKey(file))
                    {
                        string content = File.ReadAllText(file);
                        string[] lines = content.Split('\n');

                        foreach (int lineNumber in foundLines[file])
                        {
                            if (lineNumber >= 0 && lineNumber < lines.Length)
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Space(20);

                                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                                EditorGUILayout.LabelField($"����� {lineNumber + 1}:", EditorStyles.boldLabel);

                                // �������� ������� ���������
                                string line = lines[lineNumber].TrimStart();
                                GUIStyle style = new GUIStyle(EditorStyles.textArea);
                                style.wordWrap = true;

                                Rect rect = EditorGUILayout.GetControlRect(false, style.CalcHeight(new GUIContent(line), EditorGUIUtility.currentViewWidth - 40));
                                EditorGUI.DrawRect(rect, highlightColor);
                                EditorGUI.LabelField(rect, line, style);

                                EditorGUILayout.EndVertical();
                                EditorGUILayout.EndHorizontal();
                            }
                        }

                        EditorGUILayout.Space();
                    }
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space();

            // ���������� � �������
            EditorGUILayout.LabelField("���������� � �������:", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "1. �������������� UIManager.FadeToBlack() ������ fadeController.FadeToBlack()\n" +
                "2. �������������� UIManager.FadeFromBlack() ������ fadeController.FadeFromBlack()\n" +
                "3. ������� �� ��������� �� FadeController � �����������\n" +
                "4. ������� ���� FadeController.cs, ���� ��������� �������",
                MessageType.Info);
        }

        private void FindObsoleteReferences()
        {
            results.Clear();
            foundLines.Clear();

            string[] csFiles = Directory.GetFiles(searchPath, "*.cs", SearchOption.AllDirectories);

            foreach (string file in csFiles)
            {
                if (file.Contains("FadeController.cs") || file.Contains("FadeControllerCleanupUtility.cs"))
                    continue;

                string content = File.ReadAllText(file);

                // �������� ������ ��� ������ �������� �� FadeController
                var fadeControllerPattern = @"FadeController|fadeController";

                if (Regex.IsMatch(content, fadeControllerPattern))
                {
                    results.Add(file);

                    // �������� ������ ����� � �����������
                    var lines = content.Split('\n');
                    var lineNumberList = new List<int>();

                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (Regex.IsMatch(lines[i], fadeControllerPattern))
                        {
                            lineNumberList.Add(i);
                        }
                    }

                    foundLines[file] = lineNumberList;
                }
            }

            Debug.Log($"�������� {results.Count} ����� � ����������� �� FadeController.");
        }

        private void OpenAllFoundFiles()
        {
            foreach (var file in results)
            {
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(file, 1);
            }
        }

        /// <summary>
        /// ����� ��� ������� ����� �������� �� FadeController � ��� ��������� ������
        /// </summary>
        private void BatchReplaceReferences()
        {
            if (results.Count == 0)
                return;

            if (!EditorUtility.DisplayDialog("ϳ����������� �����",
                $"�� �� ������� ��������� �� FadeController � {results.Count} ������. ������������� �������� ������� �������� ����. ����������?",
                "���", "���������"))
                return;

            int filesModified = 0;

            foreach (var file in results)
            {
                string content = File.ReadAllText(file);
                bool modified = false;

                // ������ ������� �����
                var replacements = new Dictionary<string, string>
                {
                    { @"fadeController\.FadeToBlack\(\s*\)", "UIManager.Instance.FadeToBlack()" },
                    { @"fadeController\.FadeFromBlack\(\s*\)", "UIManager.Instance.FadeFromBlack()" },
                    { @"fadeController\.FadeToBlack\(\s*([^)]+)\s*\)", "UIManager.Instance.FadeToBlack($1)" },
                    { @"fadeController\.FadeFromBlack\(\s*([^)]+)\s*\)", "UIManager.Instance.FadeFromBlack($1)" },
                    { @"\[SerializeField\]\s+private\s+FadeController\s+fadeController\s*;", "// Removed FadeController reference" },
                    { @"using\s+FadeController\s*;", "" }
                };

                foreach (var replacement in replacements)
                {
                    string newContent = Regex.Replace(content, replacement.Key, replacement.Value);
                    if (newContent != content)
                    {
                        content = newContent;
                        modified = true;
                    }
                }

                if (modified)
                {
                    File.WriteAllText(file, content);
                    filesModified++;
                }
            }

            EditorUtility.DisplayDialog("����� ���������",
                $"������� ��������� � {filesModified} � {results.Count} �����.",
                "OK");

            // ��������� ����� ���� �����
            FindObsoleteReferences();
        }

        /// <summary>
        /// ����� ��� ��������� FadeController.cs, ���� �� ��������� �������
        /// </summary>
        private void DeleteFadeController()
        {
            string fadeControllerPath = Path.Combine(Application.dataPath, "Scripts/Managers/UIManager/FadeController.cs");

            if (!File.Exists(fadeControllerPath))
            {
                fadeControllerPath = EditorUtility.OpenFilePanel("������� ���� FadeController.cs", Application.dataPath, "cs");
                if (string.IsNullOrEmpty(fadeControllerPath))
                    return;
            }

            if (!EditorUtility.DisplayDialog("ϳ����������� ���������",
                "�� �� �������� ���� FadeController.cs. �������������, �� �� ��������� �� ����� �������. ����������?",
                "���", "���������"))
                return;

            try
            {
                File.Delete(fadeControllerPath);
                string metaFile = fadeControllerPath + ".meta";
                if (File.Exists(metaFile))
                {
                    File.Delete(metaFile);
                }

                AssetDatabase.Refresh();
                Debug.Log($"�������� ����: {fadeControllerPath}");

                EditorUtility.DisplayDialog("����", "FadeController.cs ������ ��������!", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"������� ��� �������� �����: {ex.Message}");
                EditorUtility.DisplayDialog("�������", $"�� ������� �������� ����: {ex.Message}", "OK");
            }
        }
    }
}
#endif