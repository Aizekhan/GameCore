// Assets/Scripts/Core/Services/EventBus.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Core
{
    /// <summary>
    /// ������� ���� ��� ���������� �� ������������ ��� ������ �����������.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<string, List<Action<object>>> _eventSubscriptions =
            new Dictionary<string, List<Action<object>>>();

        /// <summary>
        /// ϳ������ �� ����.
        /// </summary>
        /// <param name="eventName">�������� ��'� ��䳿</param>
        /// <param name="callback">�������, ��� ����������� ��� ��䳿</param>
        public static void Subscribe(string eventName, Action<object> callback)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                CoreLogger.LogError("EventBus", "Cannot subscribe to empty event name");
                return;
            }

            if (callback == null)
            {
                CoreLogger.LogError("EventBus", "Cannot subscribe with null callback");
                return;
            }

            if (!_eventSubscriptions.TryGetValue(eventName, out var callbacks))
            {
                callbacks = new List<Action<object>>();
                _eventSubscriptions[eventName] = callbacks;
            }

            if (!callbacks.Contains(callback))
            {
                callbacks.Add(callback);
                CoreLogger.Log("EventBus", $"Subscribed to event: {eventName}");
            }
        }

        /// <summary>
        /// ³������ �� ��䳿.
        /// </summary>
        /// <param name="eventName">��'� ��䳿</param>
        /// <param name="callback">�������, �������� �� ����</param>
        public static void Unsubscribe(string eventName, Action<object> callback)
        {
            if (string.IsNullOrEmpty(eventName) || callback == null)
                return;

            if (_eventSubscriptions.TryGetValue(eventName, out var callbacks))
            {
                if (callbacks.Remove(callback))
                {
                    CoreLogger.Log("EventBus", $"Unsubscribed from event: {eventName}");

                    // ��������� ������� ������
                    if (callbacks.Count == 0)
                    {
                        _eventSubscriptions.Remove(eventName);
                    }
                }
            }
        }

        /// <summary>
        /// ³�������� ���� ��� ����������.
        /// </summary>
        /// <param name="eventName">��'� ��䳿</param>
        /// <param name="data">��� ��䳿 (������ ���� null)</param>
        public static void Emit(string eventName, object data = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                CoreLogger.LogError("EventBus", "Cannot emit event with empty name");
                return;
            }

            if (_eventSubscriptions.TryGetValue(eventName, out var callbacks))
            {
                // ������� ������ ��� ������� (���� callback ����� �������)
                Action<object>[] callbacksCopy = callbacks.ToArray();

                foreach (var callback in callbacksCopy)
                {
                    try
                    {
                        callback.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        CoreLogger.LogError("EventBus", $"Error in event handler for {eventName}: {e.Message}\n{e.StackTrace}");
                    }
                }

                CoreLogger.Log("EventBus", $"Event emitted: {eventName}");
            }
        }

        /// <summary>
        /// ����� �� �������. ��������������� ��� ��������������� ���� ��� ������� ���.
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            _eventSubscriptions.Clear();
            CoreLogger.Log("EventBus", "All event subscriptions cleared");
        }
    }
}