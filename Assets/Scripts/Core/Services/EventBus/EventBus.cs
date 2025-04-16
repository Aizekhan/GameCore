using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
namespace GameCore.Core.EventSystem
{
    public static class EventBus
    {
        private static readonly Dictionary<string, List<Action<object>>> _eventSubscriptions = new();
        private static readonly Dictionary<string, EventDebugInfo> _debugInfo = new();

        public static void SetDebugMode(bool enabled)
        {
            EventBusSettings.DebugMode = enabled;
            CoreLogger.Log("EventBus", $"Debug mode {(enabled ? "enabled" : "disabled")}");
        }

        public static void Subscribe(string eventName, Action<object> callback)
        {
            if (string.IsNullOrEmpty(eventName) || callback == null) return;

            if (!_eventSubscriptions.TryGetValue(eventName, out var callbacks))
                _eventSubscriptions[eventName] = callbacks = new List<Action<object>>();

            if (!callbacks.Contains(callback))
            {
                callbacks.Add(callback);
                if (EventBusSettings.DebugMode)
                {
                    if (!_debugInfo.ContainsKey(eventName))
                        _debugInfo[eventName] = new EventDebugInfo(eventName);

                    _debugInfo[eventName].SubscriberCount++;
                    _debugInfo[eventName].LastSubscribedAt = DateTime.Now;
                }
            }
        }

        public static void Unsubscribe(string eventName, Action<object> callback)
        {
            if (string.IsNullOrEmpty(eventName) || callback == null) return;

            if (_eventSubscriptions.TryGetValue(eventName, out var callbacks))
            {
                if (callbacks.Remove(callback) && callbacks.Count == 0)
                    _eventSubscriptions.Remove(eventName);

                if (EventBusSettings.DebugMode && _debugInfo.ContainsKey(eventName))
                    _debugInfo[eventName].SubscriberCount--;
            }
        }

        public static void UnsubscribeCategory(string categoryPrefix)
        {
            foreach (var key in _eventSubscriptions.Keys.Where(k => k.StartsWith(categoryPrefix)).ToList())
            {
                _eventSubscriptions.Remove(key);
                if (EventBusSettings.DebugMode && _debugInfo.ContainsKey(key))
                    _debugInfo[key].SubscriberCount = 0;
            }
        }

        public static void Emit(string eventName, object data = null)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            if (_eventSubscriptions.TryGetValue(eventName, out var callbacks))
            {
                var copy = callbacks.ToArray();
                if (EventBusSettings.DebugMode && _debugInfo.ContainsKey(eventName))
                {
                    _debugInfo[eventName].EmitCount++;
                    _debugInfo[eventName].LastEmittedAt = DateTime.Now;
                    _debugInfo[eventName].LastEmittedData = data;
                }

                foreach (var callback in copy)
                {
                    try { callback.Invoke(data); }
                    catch (Exception e) { CoreLogger.LogError("EventBus", $"Error in {eventName}: {e.Message}"); }
                }
            }
            else if (EventBusSettings.DebugMode)
            {
                CoreLogger.LogWarning("EventBus", $"No subscribers for event: {eventName}");
            }
        }

        public static async void EmitDelayed(string eventName, object data, float delaySeconds)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            if (EventBusSettings.DebugMode)
                CoreLogger.Log("EventBus", $"Scheduled delayed event: {eventName} ({delaySeconds}s)");

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            Emit(eventName, data);
        }

        public static void ClearAllSubscriptions()
        {
            _eventSubscriptions.Clear();
            if (EventBusSettings.DebugMode) _debugInfo.Clear();
        }

        public static List<EventDebugInfo> GetDebugInfo()
        {
            if (!EventBusSettings.DebugMode) SetDebugMode(true);
            return _debugInfo.Values.ToList();
        }
    }
}
