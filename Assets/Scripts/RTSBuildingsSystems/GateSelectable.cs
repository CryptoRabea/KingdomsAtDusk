using UnityEngine;
using RTS.Core.Events;

namespace RTS.Buildings
{
    /// <summary>
    /// Gate-specific selection component that provides manual open/close controls.
    /// Attach this to gate prefabs alongside BuildingSelectable and Gate components.
    /// </summary>
    [RequireComponent(typeof(Gate))]
    [RequireComponent(typeof(BuildingSelectable))]
    public class GateSelectable : MonoBehaviour
    {
        [Header("Manual Control Settings")]
        [SerializeField] private bool enableKeyboardControls = true;
        [SerializeField] private KeyCode openCloseKey = KeyCode.G;

        private Gate gate;
        private BuildingSelectable buildingSelectable;
        private bool controlsActive = false;

        private void Awake()
        {
            gate = GetComponent<Gate>();
            buildingSelectable = GetComponent<BuildingSelectable>();

            if (gate == null)
            {
                Debug.LogError($"GateSelectable on {name} requires a Gate component!");
            }

            if (buildingSelectable == null)
            {
                Debug.LogError($"GateSelectable on {name} requires a BuildingSelectable component!");
            }
        }

        private void OnEnable()
        {
            // Subscribe to selection events
            EventBus.Subscribe<BuildingSelectedEvent>(OnBuildingSelected);
            EventBus.Subscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
        }

        private void OnDisable()
        {
            // Unsubscribe from selection events
            EventBus.Unsubscribe<BuildingSelectedEvent>(OnBuildingSelected);
            EventBus.Unsubscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
        }

        private void Update()
        {
            if (!controlsActive || !enableKeyboardControls) return;

            // Check for manual control input
            if (Input.GetKeyDown(openCloseKey))
            {
                TogateGate();
            }
        }

        private void OnBuildingSelected(BuildingSelectedEvent evt)
        {
            // Only activate controls if this gate was selected
            if (evt.Building == gameObject)
            {
                controlsActive = true;
                Debug.Log($"Gate {name} selected. Press {openCloseKey} to toggle open/close.");
            }
        }

        private void OnBuildingDeselected(BuildingDeselectedEvent evt)
        {
            // Deactivate controls if this gate was deselected
            if (evt.Building == gameObject)
            {
                controlsActive = false;
            }
        }

        /// <summary>
        /// Toggle the gate open/closed (manual control).
        /// </summary>
        public void TogateGate()
        {
            if (gate == null) return;

            if (!gate.GateData.allowManualControl)
            {
                Debug.Log($"Manual control is disabled for gate {name}");
                return;
            }

            gate.Toggle();
        }

        /// <summary>
        /// Manually open the gate.
        /// </summary>
        public void OpenGate()
        {
            if (gate == null) return;

            if (!gate.GateData.allowManualControl)
            {
                Debug.Log($"Manual control is disabled for gate {name}");
                return;
            }

            gate.Open();
        }

        /// <summary>
        /// Manually close the gate.
        /// </summary>
        public void CloseGate()
        {
            if (gate == null) return;

            if (!gate.GateData.allowManualControl)
            {
                Debug.Log($"Manual control is disabled for gate {name}");
                return;
            }

            gate.Close();
        }

        /// <summary>
        /// Lock the gate (prevents opening/closing).
        /// </summary>
        public void LockGate()
        {
            if (gate == null) return;
            gate.Lock();
        }

        /// <summary>
        /// Unlock the gate (allows opening/closing).
        /// </summary>
        public void UnlockGate()
        {
            if (gate == null) return;
            gate.Unlock();
        }

        #region Debug

        [ContextMenu("Toggle Gate (Manual)")]
        private void DebugToggle()
        {
            TogateGate();
        }

        [ContextMenu("Open Gate (Manual)")]
        private void DebugOpen()
        {
            OpenGate();
        }

        [ContextMenu("Close Gate (Manual)")]
        private void DebugClose()
        {
            CloseGate();
        }

        #endregion
    }
}
