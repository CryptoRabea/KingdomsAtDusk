using UnityEngine;
using RTS.Core.Events;

namespace RTS.Buildings
{
    /// <summary>
    /// Gate building component - extends Building with opening/closing capabilities.
    /// Attach this to gate prefabs alongside GateAnimation component.
    /// </summary>
    [RequireComponent(typeof(GateAnimation))]
    public class Gate : Building
    {
        [Header("Gate Specific")]
        [SerializeField] private GateDataSO gateData;

        private GateAnimation gateAnimation;
        private GateAutoOpenController autoOpenController;
        private GameObject replacedWall; // Reference to wall that was replaced
        private bool isOpen = false;
        private bool isLocked = false;

        public GateDataSO GateData => gateData;
        public GateAnimation Animation => gateAnimation;
        public bool IsOpen => isOpen;
        public bool IsLocked => isLocked;

        private new void Start()
        {
            // Get GateAnimation component
            gateAnimation = GetComponent<GateAnimation>();
            if (gateAnimation == null)
            {
            }

            // Set gate data on animation component
            if (gateAnimation != null && gateData != null)
            {
                gateAnimation.SetGateData(gateData);
            }

            // Add auto-open controller if enabled
            if (gateData != null && gateData.enableAutoOpen)
            {
                autoOpenController = gameObject.AddComponent<GateAutoOpenController>();
                autoOpenController.Initialize(this, gateData);
            }

            // Call base Start
            base.Start();

            // Publish gate placed event
            if (gateData != null)
            {
                EventBus.Publish(new GatePlacedEvent(gameObject, transform.position, gateData.animationType));
            }

        }

        /// <summary>
        /// Set the gate data (called by placement system).
        /// </summary>
        public void SetGateData(GateDataSO data)
        {
            gateData = data;

            // Also set on base Building component
            SetData(data);

            // Set on animation component
            if (gateAnimation != null)
            {
                gateAnimation.SetGateData(data);
            }
        }

        /// <summary>
        /// Open the gate.
        /// </summary>
        public void Open()
        {
            if (isOpen || isLocked)
            {
                return;
            }

            if (gateAnimation != null)
            {
                gateAnimation.Open(() =>
                {
                    isOpen = true;
                    EventBus.Publish(new GateOpenedEvent(gameObject));
                });
            }
        }

        /// <summary>
        /// Close the gate.
        /// </summary>
        public void Close()
        {
            if (!isOpen || isLocked)
            {
                return;
            }

            if (gateAnimation != null)
            {
                gateAnimation.Close(() =>
                {
                    isOpen = false;
                    EventBus.Publish(new GateClosedEvent(gameObject));
                });
            }
        }

        /// <summary>
        /// Toggle gate open/closed.
        /// </summary>
        public void Toggle()
        {
            if (isOpen)
                Close();
            else
                Open();
        }

        /// <summary>
        /// Lock the gate in its current state.
        /// </summary>
        public void Lock()
        {
            isLocked = true;
        }

        /// <summary>
        /// Unlock the gate to allow opening/closing.
        /// </summary>
        public void Unlock()
        {
            isLocked = false;
        }

        /// <summary>
        /// Set reference to wall that was replaced (for tracking).
        /// </summary>
        public void SetReplacedWall(GameObject wall)
        {
            replacedWall = wall;
        }

        /// <summary>
        /// Get the wall that was replaced by this gate (if any).
        /// </summary>
        public GameObject GetReplacedWall()
        {
            return replacedWall;
        }

        private new void OnDestroy()
        {
            // Optionally: Restore wall when gate is destroyed
            // (This is a design choice - you might want to leave it destroyed)

            // Publish gate destroyed event
            if (gateData != null)
            {
                EventBus.Publish(new GateDestroyedEvent(gameObject, gateData.animationType));
            }

            // Call base OnDestroy
            base.OnDestroy();
        }

        #region Debug

        [ContextMenu("Open Gate")]
        private void DebugOpen()
        {
            Open();
        }

        [ContextMenu("Close Gate")]
        private void DebugClose()
        {
            Close();
        }

        [ContextMenu("Toggle Gate")]
        private void DebugToggle()
        {
            Toggle();
        }

        [ContextMenu("Lock Gate")]
        private void DebugLock()
        {
            Lock();
        }

        [ContextMenu("Unlock Gate")]
        private void DebugUnlock()
        {
            Unlock();
        }

        [ContextMenu("Print Gate Stats")]
        private void DebugPrintStats()
        {
            if (gateData == null)
            {
                return;
            }

        }

        #endregion
    }

    #region Events

    /// <summary>
    /// Event published when a gate is placed.
    /// </summary>
    public struct GatePlacedEvent
    {
        public GameObject Gate { get; }
        public Vector3 Position { get; }
        public GateAnimationType AnimationType { get; }

        public GatePlacedEvent(GameObject gate, Vector3 position, GateAnimationType animationType)
        {
            Gate = gate;
            Position = position;
            AnimationType = animationType;
        }
    }

    /// <summary>
    /// Event published when a gate is destroyed.
    /// </summary>
    public struct GateDestroyedEvent
    {
        public GameObject Gate { get; }
        public GateAnimationType AnimationType { get; }

        public GateDestroyedEvent(GameObject gate, GateAnimationType animationType)
        {
            Gate = gate;
            AnimationType = animationType;
        }
    }

    /// <summary>
    /// Event published when a gate opens.
    /// </summary>
    public struct GateOpenedEvent
    {
        public GameObject Gate { get; }

        public GateOpenedEvent(GameObject gate)
        {
            Gate = gate;
        }
    }

    /// <summary>
    /// Event published when a gate closes.
    /// </summary>
    public struct GateClosedEvent
    {
        public GameObject Gate { get; }

        public GateClosedEvent(GameObject gate)
        {
            Gate = gate;
        }
    }

    #endregion
}
