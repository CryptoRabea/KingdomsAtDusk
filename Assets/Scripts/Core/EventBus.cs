using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Core.Events
{
    /// <summary>
    /// Simple event bus for decoupled communication between systems.
    /// Uses a publish-subscribe pattern.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> eventHandlers = new Dictionary<Type, List<Delegate>>();

        /// <summary>
        /// Subscribe to an event type.
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var eventType = typeof(T);
            
            if (!eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType] = new List<Delegate>();
            }

            if (!eventHandlers[eventType].Contains(handler))
            {
                eventHandlers[eventType].Add(handler);
            }
        }

        /// <summary>
        /// Unsubscribe from an event type.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var eventType = typeof(T);
            
            if (eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType].Remove(handler);
            }
        }

        /// <summary>
        /// Publish an event to all subscribers.
        /// </summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            var eventType = typeof(T);
            
            if (eventHandlers.ContainsKey(eventType))
            {
                // Create a copy to avoid issues if handlers modify the list
                var handlers = new List<Delegate>(eventHandlers[eventType]);
                
                foreach (var handler in handlers)
                {
                    try
                    {
                        (handler as Action<T>)?.Invoke(eventData);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error invoking event handler for {eventType.Name}: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Clear all event handlers. Use with caution!
        /// </summary>
        public static void Clear()
        {
            eventHandlers.Clear();
        }

        /// <summary>
        /// Clear handlers for a specific event type.
        /// </summary>
        public static void Clear<T>() where T : struct
        {
            var eventType = typeof(T);
            if (eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType].Clear();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            eventHandlers.Clear();
        }

        internal class EventSubscription<T>
        {
        }
    }
}
