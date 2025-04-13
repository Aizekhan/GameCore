using UnityEngine;

public static class Logger
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        Debug.Log($"<color=#00c3ff><b>[LOG]</b></color> {message}");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string context, string message)
    {
        Debug.Log($"<color=#00c3ff><b>[{context}]</b></color> {message}");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message)
    {
        Debug.LogWarning($"<color=orange><b>[WARNING]</b></color> {message}");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string context, string message)
    {
        Debug.LogWarning($"<color=orange><b>[{context}]</b></color> {message}");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string message)
    {
        Debug.LogError($"<color=red><b>[ERROR]</b></color> {message}");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string context, string message)
    {
        Debug.LogError($"<color=red><b>[{context}]</b></color> {message}");
    }
}
