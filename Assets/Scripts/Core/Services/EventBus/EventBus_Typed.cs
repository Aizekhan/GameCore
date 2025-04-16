// Assets/Scripts/Core/Services/EventBus/EventBus_Typed.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Core.EventSystem
{
    public static class EventBus<T>
    {
        private static Dictionary<string, List<Action<T>>> _typedSubscriptions = new();

        public static void Subscribe(string eventName, Action<T> callback)
        {
            if (string.IsNullOrEmpty(eventName) || callback == null) return;

            if (!_typedSubscriptions.TryGetValue(eventName, out var list))
                _typedSubscriptions[eventName] = list = new List<Action<T>>();

            if (!list.Contains(callback))
                list.Add(callback);
        }

        public static void Unsubscribe(string eventName, Action<T> callback)
        {
            if (string.IsNullOrEmpty(eventName) || callback == null) return;

            if (_typedSubscriptions.TryGetValue(eventName, out var list))
            {
                list.Remove(callback);
                if (list.Count == 0)
                    _typedSubscriptions.Remove(eventName);
            }
        }

        public static void Emit(string eventName, T data)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            if (_typedSubscriptions.TryGetValue(eventName, out var list))
            {
                foreach (var callback in list.ToArray())
                {
                    try { callback.Invoke(data); }
                    catch (Exception e)
                    {
                        CoreLogger.LogError("EventBus<T>", $"Error in {eventName}: {e.Message}");
                    }
                }
            }
        }

        public static void ClearAllSubscriptions() => _typedSubscriptions.Clear();
    }
}