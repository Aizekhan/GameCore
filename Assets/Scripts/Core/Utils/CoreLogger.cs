// Assets/Scripts/Core/Utils/CoreLogger.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Core
{
    /// <summary>
    /// Розширена система логування з підтримкою категорій та рівнів.
    /// </summary>
    public static class CoreLogger
    {
        // Рівні логування, які можна вмикати/вимикати
        [Flags]
        public enum LogLevel
        {
            None = 0,
            Info = 1,
            Warning = 2,
            Error = 4,
            Debug = 8,
            All = Info | Warning | Error | Debug
        }

        // Поточні налаштування логування
        private static LogLevel _currentLogLevel = LogLevel.All;
        private static readonly HashSet<string> _enabledCategories = new HashSet<string>();
        private static bool _filterByCategory = false;

        // Кольори для різних категорій логів
        private static readonly Dictionary<string, string> _categoryColors = new Dictionary<string, string>
        {
            { "UI", "#00c3ff" },
            { "INPUT", "#ffcc00" },
            { "SCENE", "#33cc33" },
            { "AUDIO", "#ff66ff" },
            { "SAVE", "#ff9900" },
            { "EVENT", "#99cc00" },
            { "SERVICE", "#cc99ff" },
            { "DEFAULT", "#ffffff" }
        };

        // Базові методи логування з підтримкою категорій

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message)
        {
            if ((_currentLogLevel & LogLevel.Info) == 0) return;

            Debug.Log($"<color=#00c3ff><b>[LOG]</b></color> {message}");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string category, string message)
        {
            if ((_currentLogLevel & LogLevel.Info) == 0) return;
            if (_filterByCategory && !_enabledCategories.Contains(category)) return;

            string color = GetCategoryColor(category);
            Debug.Log($"<color={color}><b>[{category}]</b></color> {message}");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string message)
        {
            if ((_currentLogLevel & LogLevel.Warning) == 0) return;

            Debug.LogWarning($"<color=orange><b>[WARNING]</b></color> {message}");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string category, string message)
        {
            if ((_currentLogLevel & LogLevel.Warning) == 0) return;
            if (_filterByCategory && !_enabledCategories.Contains(category)) return;

            string color = GetCategoryColor(category);
            Debug.LogWarning($"<color={color}><b>[{category} WARNING]</b></color> {message}");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(string message)
        {
            if ((_currentLogLevel & LogLevel.Error) == 0) return;

            Debug.LogError($"<color=red><b>[ERROR]</b></color> {message}");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(string category, string message)
        {
            if ((_currentLogLevel & LogLevel.Error) == 0) return;
            if (_filterByCategory && !_enabledCategories.Contains(category)) return;

            string color = GetCategoryColor(category);
            Debug.LogError($"<color={color}><b>[{category} ERROR]</b></color> {message}");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogDebug(string message)
        {
            if ((_currentLogLevel & LogLevel.Debug) == 0) return;

            Debug.Log($"<color=#aaaaaa><b>[DEBUG]</b></color> {message}");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogDebug(string category, string message)
        {
            if ((_currentLogLevel & LogLevel.Debug) == 0) return;
            if (_filterByCategory && !_enabledCategories.Contains(category)) return;

            string color = GetCategoryColor(category);
            Debug.Log($"<color={color}><b>[{category} DEBUG]</b></color> {message}");
        }

        // Методи управління системою логування

        /// <summary>
        /// Встановлює рівень логування.
        /// </summary>
        public static void SetLogLevel(LogLevel level)
        {
            _currentLogLevel = level;
        }

        /// <summary>
        /// Додає категорію до фільтру логування.
        /// </summary>
        public static void EnableCategory(string category)
        {
            _enabledCategories.Add(category.ToUpper());
            _filterByCategory = true;
        }

        /// <summary>
        /// Видаляє категорію з фільтру логування.
        /// </summary>
        public static void DisableCategory(string category)
        {
            _enabledCategories.Remove(category.ToUpper());
        }

        /// <summary>
        /// Вимикає фільтрацію за категоріями.
        /// </summary>
        public static void DisableCategoryFiltering()
        {
            _filterByCategory = false;
        }

        /// <summary>
        /// Додає кольорову схему для категорії.
        /// </summary>
        public static void SetCategoryColor(string category, string hexColor)
        {
            _categoryColors[category.ToUpper()] = hexColor;
        }

        // Приватні допоміжні методи

        private static string GetCategoryColor(string category)
        {
            category = category.ToUpper();
            if (_categoryColors.TryGetValue(category, out string color))
            {
                return color;
            }
            return _categoryColors["DEFAULT"];
        }
    }
}