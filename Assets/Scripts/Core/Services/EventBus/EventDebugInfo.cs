using System;

namespace GameCore.Core.EventSystem
{
    public class EventDebugInfo
    {
        public string EventName { get; private set; }
        public int SubscriberCount { get; set; }
        public int EmitCount { get; set; }
        public DateTime LastEmittedAt { get; set; }
        public DateTime LastSubscribedAt { get; set; }
        public object LastEmittedData { get; set; }

        public EventDebugInfo(string name)
        {
            EventName = name;
            SubscriberCount = 0;
            EmitCount = 0;
        }
    }
}