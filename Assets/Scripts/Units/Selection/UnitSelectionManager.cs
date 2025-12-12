// File: UnitSelectionManager.cs
// Patched: Fix double/triple-click behavior and SO reference comparisons

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using RTS.Core.Events;
using RTS.Units.AI;
using RTS.Buildings;
using KAD.RTSBuildingsSystems;

namespace RTS.Units
{
    /// <summary>
    /// Advanced unit selection system with comprehensive RTS features:
    /// - Single/multi-select with box selection
    /// - Double-click to select all visible units of same SO
    /// - Triple-click to select all units in scene (or all of same SO if clicked on one)
    /// - Mouse hover highlighting
    /// - Max selection limits with distance sorting
    /// - Unit type filtering (owned/friendly/neutral/enemy)
    /// - Drag-to-highlight before selection
    /// - OPTIMIZED: Cached unit references for performance
    /// ADD THIS TO ONE GAMEOBJECT IN YOUR SCENE (like a "SelectionManager").
    /// DO NOT ADD TO UNITS!
    /// </summary>
    public class UnitSelectionManager : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private InputActionReference clickAction;
        [SerializeField] private InputActionReference positionAction;

        [Header("Selection Settings")]
        [SerializeField] private LayerMask selectableLayer;
        [SerializeField] private LayerMask buildingLayer;
        [SerializeField] private Camera mainCamera;

        [Header("Selection Behavior")]
        [SerializeField] private bool enableMaxSelection = false;
        [SerializeField] private int maxSelectionCount = 50;
        [SerializeField] private bool sortByDistance = true;
        [SerializeField] private bool enableDoubleClick = true;
        [SerializeField] private float doubleClickTime = 0.3f;
        [SerializeField] private SelectionPriority overlapPriority = SelectionPriority.Nearest;

        [Header("Unit Type Filter")]
        [SerializeField] private bool enableTypeFilter = false;
        [SerializeField] private bool selectOwned = true;
        [SerializeField] private bool selectFriendly = true;
        [SerializeField] private bool selectNeutral = false;
        [SerializeField] private bool selectEnemy = false;

        [Header("Drag Selection")]
        [SerializeField] private Image selectionBoxUI;
        [SerializeField] private Color selectionBoxColor = new Color(0, 1, 0, 0.2f);
        [SerializeField] private float dragThreshold = 5f;
        [SerializeField] private bool highlightDuringDrag = true;

        [Header("Hover Highlighting")]
        [SerializeField] private bool enableHoverHighlight = true;
        [SerializeField] private Color hoverColor = new Color(1, 1, 0, 0.5f);

        [Header("Performance")]
        [SerializeField] private bool useCachedUnits = true;
        [SerializeField] private bool showPerformanceStats = false;

        private List<UnitSelectable> selectedUnits = new List<UnitSelectable>();
        private List<UnitSelectable> highlightedUnits = new List<UnitSelectable>();
        private UnitSelectable hoveredUnit = null;
        private Vector2 dragStartPosition;
        private bool isDragging = false;
        private RectTransform selectionBoxRect;

        // Click tracking for single/double/triple
        private int clickCount = 0;
        private float lastClickTimestamp = 0f;

        // Track last clicked SO references for consistency checks
        private UnitConfigSO lastClickedUnitConfig = null;
        private BuildingDataSO lastClickedBuildingData = null;

        //  UI Detection - Cached
        private PointerEventData cachedPointerEventData;
        private List<RaycastResult> cachedRaycastResults = new List<RaycastResult>();

        //  Performance Optimization - Cached Unit References
        private HashSet<UnitSelectable> allSelectableUnits = new HashSet<UnitSelectable>();

        // Placement mode detection
        private RTS.Managers.BuildingManager buildingManager;
        private WallPlacementController wallPlacementController;

        public IReadOnlyList<UnitSelectable> SelectedUnits => selectedUnits;
        public int SelectionCount => selectedUnits.Count;
        public int TotalSelectableUnits => allSelectableUnits.Count;
        private RaycastHit[] raycastHitsCache = new RaycastHit[50]; // Adjust size based on max overlapping units

        public enum SelectionPriority
        {
            Nearest,
            Furthest
        }

        private void Awake()
        {
            // Initialize cached pointer data
            if (EventSystem.current != null)
            {
                cachedPointerEventData = new PointerEventData(EventSystem.current);
            }

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (selectionBoxUI != null)
            {
                selectionBoxRect = selectionBoxUI.GetComponent<RectTransform>();
                selectionBoxUI.color = selectionBoxColor;
                selectionBoxUI.gameObject.SetActive(false);

                // Ensure pivot and anchors are at bottom-left (0, 0) for proper corner-to-corner scaling
                selectionBoxRect.pivot = new Vector2(0, 0);
                selectionBoxRect.anchorMin = new Vector2(0, 0);
                selectionBoxRect.anchorMax = new Vector2(0, 0);
            }

            //  Initialize unit cache if enabled
            if (useCachedUnits)
            {
                InitializeUnitCache();
            }

            // Find BuildingManager and WallPlacementController to check placement mode
            buildingManager = Object.FindAnyObjectByType<RTS.Managers.BuildingManager>();
            wallPlacementController = Object.FindAnyObjectByType<WallPlacementController>();
        }

        private void OnEnable()
        {
            if (clickAction != null)
            {
                clickAction.action.Enable();
                clickAction.action.started += OnClickStarted;
                clickAction.action.canceled += OnClickReleased;
            }

            if (positionAction != null)
            {
                positionAction.action.Enable();
            }

            //  Subscribe to unit events for cache management
            if (useCachedUnits)
            {
                EventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);
                EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
            }
        }

        private void OnDisable()
        {
            if (clickAction != null)
            {
                clickAction.action.Disable();
                clickAction.action.started -= OnClickStarted;
                clickAction.action.canceled -= OnClickReleased;
            }

            if (positionAction != null)
            {
                positionAction.action.Disable();
            }

            //  Unsubscribe from unit events
            if (useCachedUnits)
            {
                EventBus.Unsubscribe<UnitSpawnedEvent>(OnUnitSpawned);
                EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
            }
        }

        private void Update()
        {
            if (isDragging && positionAction != null)
            {
                Vector2 currentPosition = positionAction.action.ReadValue<Vector2>();
                float dragDistance = Vector2.Distance(dragStartPosition, currentPosition);

                // Only show selection box if we've dragged past the threshold
                if (dragDistance > dragThreshold)
                {
                    if (selectionBoxUI != null && !selectionBoxUI.gameObject.activeSelf)
                    {
                        selectionBoxUI.gameObject.SetActive(true);
                    }
                    UpdateSelectionBox(dragStartPosition, currentPosition);

                    // Highlight units during drag if enabled
                    if (highlightDuringDrag)
                    {
                        UpdateDragHighlight(dragStartPosition, currentPosition);
                    }
                }
            }
            else if (enableHoverHighlight && !isDragging)
            {
                // Check for hover highlighting when not dragging
                UpdateHoverHighlight();
            }

            // Show performance stats if enabled
            if (showPerformanceStats && Time.frameCount % 60 == 0)
            {
            }
        }

        #region Unit Cache Management

        /// <summary>
        ///  NEW: Initialize unit cache by finding all existing units
        /// </summary>
        private void InitializeUnitCache()
        {
            allSelectableUnits.Clear();
            UnitSelectable[] existingUnits = FindObjectsByType<UnitSelectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var unit in existingUnits)
            {
                if (unit != null)
                {
                    allSelectableUnits.Add(unit);
                }
            }

            if (showPerformanceStats)
            {
            }
        }

        /// <summary>
        ///  NEW: Add unit to cache when spawned
        /// </summary>
        private void OnUnitSpawned(UnitSpawnedEvent evt)
        {
            if (evt.Unit != null && evt.Unit.TryGetComponent<UnitSelectable>(out var selectable))
            {
                allSelectableUnits.Add(selectable);

                if (showPerformanceStats)
                {
                }
            }
        }

        /// <summary>
        ///  NEW: Remove unit from cache when died
        /// </summary>
        private void OnUnitDied(UnitDiedEvent evt)
        {
            if (evt.Unit != null && evt.Unit.TryGetComponent<UnitSelectable>(out var selectable))
            {
                allSelectableUnits.Remove(selectable);

                // Also remove from selection if selected
                if (selectedUnits.Contains(selectable))
                {
                    selectedUnits.Remove(selectable);
                    EventBus.Publish(new SelectionChangedEvent(selectedUnits.Count));
                }

                if (showPerformanceStats)
                {
                }
            }
        }

        /// <summary>
        ///  NEW: Manual cache refresh (use if units exist before manager enables)
        /// </summary>
        [ContextMenu("Refresh Unit Cache")]
        public void RefreshUnitCache()
        {
            InitializeUnitCache();
        }

        #endregion

        #region UI Detection

        private bool IsMouseOverUI()
        {
            if (EventSystem.current == null)
                return false;

            // Initialize if needed (in case EventSystem wasn't ready at Awake)
            if (cachedPointerEventData == null)
            {
                cachedPointerEventData = new PointerEventData(EventSystem.current);
            }

            // Update position
            cachedPointerEventData.position = Mouse.current.position.ReadValue();

            // Clear previous results and raycast
            cachedRaycastResults.Clear();
            EventSystem.current.RaycastAll(cachedPointerEventData, cachedRaycastResults);

            return cachedRaycastResults.Count > 0;
        }

        #endregion

        #region Input Handling

        private void OnClickStarted(InputAction.CallbackContext context)
        {
            // Don't process if clicking on UI
            if (IsMouseOverUI())
            {
                return;
            }

            // Don't process selection if currently placing a building or wall
            if (buildingManager != null && buildingManager.IsPlacingBuilding)
            {
                return;
            }

            if (wallPlacementController != null && wallPlacementController.IsPlacingWalls)
            {
                return;
            }

            Vector2 mousePosition = positionAction.action.ReadValue<Vector2>();

            // Start tracking for potential drag or click
            dragStartPosition = mousePosition;
            isDragging = true;

            // Don't show selection box yet - wait for drag threshold in Update
        }

        private void OnClickReleased(InputAction.CallbackContext context)
        {
            if (isDragging)
            {
                Vector2 mousePosition = positionAction.action.ReadValue<Vector2>();
                float dragDistance = Vector2.Distance(dragStartPosition, mousePosition);

                // Determine if this was a drag or a single click
                if (dragDistance > dragThreshold)
                {
                    // Was a drag - perform drag selection
                    PerformDragSelection(dragStartPosition, mousePosition);
                }
                else
                {
                    // Was a click - handle single/double/triple click logic
                    HandleClickLogic(mousePosition);

                    // If clickCount ended up being 1 (single click) we still need to do single selection
                    if (clickCount == 1)
                    {
                        TrySingleSelection(mousePosition);
                    }
                }

                // Clear drag highlights
                ClearDragHighlights();

                isDragging = false;
                if (selectionBoxUI != null)
                {
                    selectionBoxUI.gameObject.SetActive(false);
                }
            }
        }

        #endregion

        #region Click Logic (single / double / triple)

        private void HandleClickLogic(Vector2 screenPosition)
        {
            if (!enableDoubleClick)
            {
                // If double-click handling disabled, always treat as single
                clickCount = 1;
                lastClickTimestamp = Time.time;
                return;
            }

            float now = Time.time;
            float delta = now - lastClickTimestamp;

            if (delta <= doubleClickTime)
            {
                clickCount++;
            }
            else
            {
                clickCount = 1;
            }

            lastClickTimestamp = now;

            // Clamp to 3 (we don't need more)
            if (clickCount > 3) clickCount = 3;

            if (clickCount == 2)
            {
                HandleDoubleClick(screenPosition);
            }
            else if (clickCount == 3)
            {
                HandleTripleClick(screenPosition);
                // reset after triple-click so next click starts fresh
                clickCount = 0;
                lastClickTimestamp = 0f;
            }
        }

        /// <summary>
        /// Handles double-click: select visible units of same SO (by reference) or all visible units on empty
        /// NOTE: Buildings are NOT selected here - they have their own BuildingSelectionManager
        /// </summary>
        private void HandleDoubleClick(Vector2 screenPosition)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            ResolveClickedObject(screenPosition, out UnitSelectable clickedUnit, out UnitConfigSO unitConfig, out BuildingSelectable clickedBuilding, out BuildingDataSO buildingData);

            // Update last clicked SO refs for potential checks (optional)
            lastClickedUnitConfig = unitConfig;
            lastClickedBuildingData = buildingData;

            if (clickedUnit != null && unitConfig != null)
            {
                SelectAllVisibleUnitsOfType(unitConfig);
                return;
            }

            // Don't select buildings or units from UnitSelectionManager when clicking on buildings
            if (clickedBuilding != null && buildingData != null)
            {
                // Clear unit selection when clicking on a building
                ClearSelection();
                return;
            }

            // Double-click on empty space selects all visible units (NOT buildings)
            SelectAllVisibleUnits();
            lastClickedUnitConfig = null;
            lastClickedBuildingData = null;
        }

        /// <summary>
        /// Handles triple-click: select all units in scene (or all of same SO if clicked on one)
        /// NOTE: Buildings are NOT selected here - they have their own BuildingSelectionManager
        /// </summary>
        private void HandleTripleClick(Vector2 screenPosition)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            ResolveClickedObject(screenPosition, out UnitSelectable clickedUnit, out UnitConfigSO unitConfig, out BuildingSelectable clickedBuilding, out BuildingDataSO buildingData);

            if (clickedUnit != null && unitConfig != null)
            {
                SelectAllUnitsOfTypeInScene(unitConfig);
                return;
            }

            // Don't select buildings or units from UnitSelectionManager when clicking on buildings
            if (clickedBuilding != null && buildingData != null)
            {
                // Clear unit selection when clicking on a building
                ClearSelection();
                return;
            }

            // Triple-click on empty space = select ALL units in scene (NOT buildings)
            SelectAllUnitsSceneWide();
        }

        /// <summary>
        /// Resolve the clicked object (unit or building) and return the ScriptableObject references by direct reference
        /// NOTE: Now checks BOTH unit layer and building layer to properly detect buildings
        /// </summary>
        private void ResolveClickedObject(
            Vector2 screenPos,
            out UnitSelectable unit,
            out UnitConfigSO unitConfig,
            out BuildingSelectable building,
            out BuildingDataSO buildingData)
        {
            unit = null;
            building = null;
            unitConfig = null;
            buildingData = null;

            if (mainCamera == null)
                mainCamera = Camera.main;

            Ray ray = mainCamera.ScreenPointToRay(screenPos);

            // Combine both layers to check for units AND buildings
            LayerMask combinedLayers = selectableLayer | buildingLayer;
            int hitCount = Physics.RaycastNonAlloc(ray, raycastHitsCache, 1000f, combinedLayers);

            float nearest = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                float dist = raycastHitsCache[i].distance;
                if (dist > nearest) continue; // we only care about nearest

                // Using TryGetComponent for better performance
                if (raycastHitsCache[i].collider.TryGetComponent<UnitSelectable>(out var hitUnit) && PassesTypeFilter(hitUnit))
                {
                    if (hitUnit.TryGetComponent<UnitAIController>(out var aiController) && aiController.Config != null)
                    {
                        unit = hitUnit;
                        unitConfig = aiController.Config;
                        building = null;
                        buildingData = null;
                        nearest = dist;
                        continue;
                    }
                }

                if (raycastHitsCache[i].collider.TryGetComponent<BuildingSelectable>(out var hitBuilding))
                {
                    if (hitBuilding.TryGetComponent<Building>(out var b) && b.Data != null)
                    {
                        building = hitBuilding;
                        buildingData = b.Data;
                        unit = null;
                        unitConfig = null;
                        nearest = dist;
                    }
                }
            }
        }

        #endregion

        #region Selection Logic

        private bool TrySingleSelection(Vector2 screenPosition)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return false;

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            // Check if we clicked on a building first - if so, don't process unit selection
            // Let BuildingSelectionManager handle it
            if (Physics.Raycast(ray, 1000f, buildingLayer))
            {
                // Clicked on building - clear unit selection and let BuildingSelectionManager handle it
                ClearSelection();
                return false;
            }

            //  Use RaycastNonAlloc instead of RaycastAll - zero GC!
            int hitCount = Physics.RaycastNonAlloc(ray, raycastHitsCache, 1000f, selectableLayer);

            if (hitCount > 0)
            {
                UnitSelectable selectedUnit = null;

                if (hitCount == 1)
                {
                    // Only one unit - select it
                    selectedUnit = raycastHitsCache[0].collider.GetComponent<UnitSelectable>();
                }
                else
                {
                    // Multiple units - apply priority
                    UnitSelectable nearestUnit = null;
                    UnitSelectable furthestUnit = null;
                    float nearestDistance = float.MaxValue;
                    float furthestDistance = float.MinValue;

                    //  OPTIMIZED: Find nearest/furthest in single pass instead of sorting
                    for (int i = 0; i < hitCount; i++)
                    {
                        if (raycastHitsCache[i].collider.TryGetComponent<UnitSelectable>(out var selectable) && PassesTypeFilter(selectable))
                        {
                            float distance = raycastHitsCache[i].distance;

                            if (distance < nearestDistance)
                            {
                                nearestDistance = distance;
                                nearestUnit = selectable;
                            }

                            if (distance > furthestDistance)
                            {
                                furthestDistance = distance;
                                furthestUnit = selectable;
                            }
                        }
                    }

                    // Select based on priority
                    selectedUnit = overlapPriority == SelectionPriority.Nearest
                        ? nearestUnit
                        : furthestUnit;
                }

                if (selectedUnit != null && PassesTypeFilter(selectedUnit))
                {
                    // Add to selection or replace (based on shift key)
                    bool additive = Keyboard.current?.shiftKey.isPressed ?? false;

                    if (!additive)
                    {
                        ClearSelection();
                        // Clear any selected buildings/gates when selecting units
                        var buildingManager = FindAnyObjectByType<BuildingSelectionManager>();
                        if (buildingManager != null)
                        {
                            buildingManager.DeselectBuilding();
                        }
                    }

                    SelectUnit(selectedUnit);
                    return true;
                }

                // Hit something but no valid unit found (e.g., building) - clear unit selection only
                // Let BuildingSelectionManager handle building selections
                ClearSelection();
                return false;
            }

            // Clicked empty space - clear unit selection only
            // Let BuildingSelectionManager handle building selections on empty clicks
            ClearSelection();
            return false;
        }

        /// <summary>
        ///  OPTIMIZED: Uses cached units instead of FindObjectsByType
        ///  NEW: Supports buildings/walls, favors type with more count
        /// </summary>
        private void PerformDragSelection(Vector2 start, Vector2 end)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            ClearSelection();

            // Clear any selected buildings/gates when drag-selecting units
            var buildingManager = FindAnyObjectByType<BuildingSelectionManager>();
            if (buildingManager != null)
            {
                buildingManager.DeselectBuilding();
            }

            Rect selectionRect = GetScreenRect(start, end);
            List<UnitSelectable> unitsInBox = new List<UnitSelectable>();

            //  Use cached units if available, otherwise fallback to FindObjectsByType
            IEnumerable<UnitSelectable> unitsToCheck = useCachedUnits ? allSelectableUnits : FindObjectsByType<UnitSelectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            // Check units only (selection box should not select buildings)
            foreach (var selectable in unitsToCheck)
            {
                if (selectable == null || !PassesTypeFilter(selectable))
                    continue;

                Vector3 screenPos = mainCamera.WorldToScreenPoint(selectable.transform.position);

                // Check if within selection rectangle and in front of camera
                if (screenPos.z > 0 && selectionRect.Contains(screenPos))
                {
                    unitsInBox.Add(selectable);
                }
            }

            // Apply max selection limit and distance sorting if enabled
            if (enableMaxSelection && unitsInBox.Count > maxSelectionCount)
            {
                if (sortByDistance)
                {
                    // Sort by distance from drag start position
                    Vector3 dragStartWorld = mainCamera.ScreenToWorldPoint(new Vector3(start.x, start.y, mainCamera.nearClipPlane));
                    unitsInBox.Sort((a, b) =>
                    {
                        float distA = Vector3.Distance(a.transform.position, dragStartWorld);
                        float distB = Vector3.Distance(b.transform.position, dragStartWorld);
                        return distA.CompareTo(distB);
                    });
                }

                // Take only the maximum allowed
                unitsInBox = unitsInBox.GetRange(0, maxSelectionCount);
            }

            // Select all units in the final list
            foreach (var unit in unitsInBox)
            {
                SelectUnit(unit);
            }
        }

        /// <summary>
        ///  OPTIMIZED: Uses cached units instead of FindObjectsByType
        /// </summary>
        private void SelectAllVisibleUnits()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            ClearSelection();

            List<UnitSelectable> visibleUnits = new List<UnitSelectable>();

            //  Use cached units if available, otherwise fallback to FindObjectsByType
            IEnumerable<UnitSelectable> unitsToCheck = useCachedUnits ? allSelectableUnits : FindObjectsByType<UnitSelectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var selectable in unitsToCheck)
            {
                if (selectable == null || !PassesTypeFilter(selectable))
                    continue;

                // Check if unit is visible to camera
                Vector3 screenPos = mainCamera.WorldToScreenPoint(selectable.transform.position);

                // Unit is visible if it's in front of camera and within screen bounds
                if (screenPos.z > 0 &&
                    screenPos.x >= 0 && screenPos.x <= Screen.width &&
                    screenPos.y >= 0 && screenPos.y <= Screen.height)
                {
                    visibleUnits.Add(selectable);
                }
            }

            // Apply max selection limit and distance sorting if enabled
            if (enableMaxSelection && visibleUnits.Count > maxSelectionCount)
            {
                if (sortByDistance)
                {
                    // Sort by distance from camera
                    Vector3 cameraPos = mainCamera.transform.position;
                    visibleUnits.Sort((a, b) =>
                    {
                        float distA = Vector3.Distance(a.transform.position, cameraPos);
                        float distB = Vector3.Distance(b.transform.position, cameraPos);
                        return distA.CompareTo(distB);
                    });
                }

                visibleUnits = visibleUnits.GetRange(0, maxSelectionCount);
            }

            foreach (var unit in visibleUnits)
            {
                SelectUnit(unit);
            }
        }

        /// <summary>
        /// NEW: Select all visible units of a specific type (by UnitConfigSO)
        /// NOTE: Ignores type filter to select based on ScriptableObject type only
        /// OPTIMIZED: No debug logging for performance
        /// </summary>
        private void SelectAllVisibleUnitsOfType(UnitConfigSO targetConfig)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null || targetConfig == null)
                return;

            ClearSelection();

            List<UnitSelectable> matchingUnits = new List<UnitSelectable>();

            IEnumerable<UnitSelectable> unitsToCheck = useCachedUnits ? allSelectableUnits : FindObjectsByType<UnitSelectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var selectable in unitsToCheck)
            {
                if (selectable == null)
                    continue;

                // Check if unit config matches by ScriptableObject reference (not AI type filter)
                // Using TryGetComponent for better performance
                if (!selectable.TryGetComponent<UnitAIController>(out var aiController) ||
                    aiController.Config == null ||
                    aiController.Config != targetConfig)
                    continue;

                // Check if unit is visible to camera
                Vector3 screenPos = mainCamera.WorldToScreenPoint(selectable.transform.position);

                if (screenPos.z > 0 &&
                    screenPos.x >= 0 && screenPos.x <= Screen.width &&
                    screenPos.y >= 0 && screenPos.y <= Screen.height)
                {
                    matchingUnits.Add(selectable);
                }
            }

            // Apply max selection limit and distance sorting if enabled
            if (enableMaxSelection && matchingUnits.Count > maxSelectionCount)
            {
                if (sortByDistance)
                {
                    Vector3 cameraPos = mainCamera.transform.position;
                    matchingUnits.Sort((a, b) =>
                    {
                        float distA = Vector3.Distance(a.transform.position, cameraPos);
                        float distB = Vector3.Distance(b.transform.position, cameraPos);
                        return distA.CompareTo(distB);
                    });
                }

                matchingUnits = matchingUnits.GetRange(0, maxSelectionCount);
            }

            foreach (var unit in matchingUnits)
            {
                SelectUnit(unit);
            }
        }

        /// <summary>
        /// NEW: Select all units of a specific type (by UnitConfigSO) in entire scene
        /// NOTE: Ignores type filter to select based on ScriptableObject type only
        /// </summary>
        private void SelectAllUnitsOfTypeInScene(UnitConfigSO targetConfig)
        {
            if (targetConfig == null)
                return;

            ClearSelection();

            List<UnitSelectable> matchingUnits = new List<UnitSelectable>();

            IEnumerable<UnitSelectable> unitsToCheck = useCachedUnits ? allSelectableUnits : FindObjectsByType<UnitSelectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var selectable in unitsToCheck)
            {
                if (selectable == null)
                    continue;

                // Check if unit config matches by ScriptableObject reference (not AI type filter)
                // Using TryGetComponent for better performance
                if (!selectable.TryGetComponent<UnitAIController>(out var aiController) ||
                    aiController.Config == null ||
                    aiController.Config != targetConfig)
                    continue;

                matchingUnits.Add(selectable);
            }

            // Apply max selection limit and distance sorting if enabled
            if (enableMaxSelection && matchingUnits.Count > maxSelectionCount)
            {
                if (sortByDistance && mainCamera != null)
                {
                    Vector3 cameraPos = mainCamera.transform.position;
                    matchingUnits.Sort((a, b) =>
                    {
                        float distA = Vector3.Distance(a.transform.position, cameraPos);
                        float distB = Vector3.Distance(b.transform.position, cameraPos);
                        return distA.CompareTo(distB);
                    });
                }

                matchingUnits = matchingUnits.GetRange(0, maxSelectionCount);
            }

            foreach (var unit in matchingUnits)
            {
                SelectUnit(unit);
            }
        }

        /// <summary>
        /// NEW: Scene-wide selection of all units (triple-click empty space)
        /// </summary>
        private void SelectAllUnitsSceneWide()
        {
            ClearSelection();

            IEnumerable<UnitSelectable> unitsToCheck = useCachedUnits ? allSelectableUnits : FindObjectsByType<UnitSelectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var unit in unitsToCheck)
            {
                if (unit == null || !PassesTypeFilter(unit))
                    continue;

                SelectUnit(unit);
            }
        }


        #endregion

        #region Visual Feedback

        private void UpdateSelectionBox(Vector2 start, Vector2 end)
        {
            if (selectionBoxRect == null) return;

            // Calculate the difference between start and end
            Vector2 diff = end - start;

            // Determine the actual starting corner based on drag direction
            Vector2 boxStart = new Vector2(
                diff.x < 0 ? start.x + diff.x : start.x,
                diff.y < 0 ? start.y + diff.y : start.y
            );

            // Size is always positive (absolute values)
            Vector2 boxSize = new Vector2(
                Mathf.Abs(diff.x),
                Mathf.Abs(diff.y)
            );

            // Set position to the calculated starting corner
            selectionBoxRect.anchoredPosition = boxStart;

            // Set size (always positive)
            selectionBoxRect.sizeDelta = boxSize;
        }

        private void UpdateHoverHighlight()
        {
            if (positionAction == null) return;

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            // Clear previous hover
            if (hoveredUnit != null)
            {
                hoveredUnit.SetHoverHighlight(false, Color.white);
                hoveredUnit = null;
            }

            // Don't highlight if mouse is over UI
            if (IsMouseOverUI())
            {
                return;
            }

            Vector2 mousePosition = positionAction.action.ReadValue<Vector2>();
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);

            // Check for new hover
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, selectableLayer))
            {
                if (hit.collider.TryGetComponent<UnitSelectable>(out var selectable) && !selectable.IsSelected && PassesTypeFilter(selectable))
                {
                    hoveredUnit = selectable;
                    hoveredUnit.SetHoverHighlight(true, hoverColor);
                }
            }
        }

        /// <summary>
        ///  OPTIMIZED: Uses cached units instead of FindObjectsByType
        /// </summary>
        private void UpdateDragHighlight(Vector2 start, Vector2 end)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            // Clear previous highlights
            ClearDragHighlights();

            Rect selectionRect = GetScreenRect(start, end);

            //  Use cached units if available, otherwise fallback to FindObjectsByType
            IEnumerable<UnitSelectable> unitsToCheck = useCachedUnits ? allSelectableUnits : FindObjectsByType<UnitSelectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var selectable in unitsToCheck)
            {
                if (selectable == null || !PassesTypeFilter(selectable))
                    continue;

                Vector3 screenPos = mainCamera.WorldToScreenPoint(selectable.transform.position);

                if (screenPos.z > 0 && selectionRect.Contains(screenPos))
                {
                    if (!selectable.IsSelected)
                    {
                        selectable.SetHoverHighlight(true, selectionBoxColor);
                        highlightedUnits.Add(selectable);
                    }
                }
            }
        }

        private void ClearDragHighlights()
        {
            foreach (var unit in highlightedUnits)
            {
                if (unit != null)
                {
                    unit.SetHoverHighlight(false, Color.white);
                }
            }
            highlightedUnits.Clear();
        }

        #endregion

        #region Selection Management

        private Rect GetScreenRect(Vector2 start, Vector2 end)
        {
            Vector2 min = Vector2.Min(start, end);
            Vector2 max = Vector2.Max(start, end);
            return new Rect(min, max - min);
        }

        private void SelectUnit(UnitSelectable unit)
        {
            if (!selectedUnits.Contains(unit))
            {
                selectedUnits.Add(unit);
                unit.Select();
            }

            EventBus.Publish(new SelectionChangedEvent(selectedUnits.Count));
        }

        private void ClearSelection()
        {
            foreach (var unit in selectedUnits)
            {
                if (unit != null)
                {
                    unit.Deselect();
                }
            }

            selectedUnits.Clear();
            EventBus.Publish(new SelectionChangedEvent(0));
        }

        #endregion

        #region Filtering

        private bool PassesTypeFilter(UnitSelectable selectable)
        {
            if (!enableTypeFilter)
                return true;

            // Get unit type from layer or component
            UnitType unitType = GetUnitType(selectable.gameObject);

            switch (unitType)
            {
                case UnitType.Owned:
                    return selectOwned;
                case UnitType.Friendly:
                    return selectFriendly;
                case UnitType.Neutral:
                    return selectNeutral;
                case UnitType.Enemy:
                    return selectEnemy;
                default:
                    return true;
            }
        }

        private UnitType GetUnitType(GameObject obj)
        {
            int layer = obj.layer;

            // Check common layer names
            if (layer == LayerMask.NameToLayer("Player") || layer == LayerMask.NameToLayer("PlayerUnit"))
                return UnitType.Owned;

            if (layer == LayerMask.NameToLayer("Enemy") || layer == LayerMask.NameToLayer("EnemyUnit"))
                return UnitType.Enemy;

            if (layer == LayerMask.NameToLayer("Friendly") || layer == LayerMask.NameToLayer("Ally"))
                return UnitType.Friendly;

            if (layer == LayerMask.NameToLayer("Neutral"))
                return UnitType.Neutral;

            // Default to owned if no specific layer found
            return UnitType.Owned;
        }

        #endregion

        #region Command Methods (for RTS controls)

        public void MoveSelectedUnits(Vector3 destination)
        {
            foreach (var unit in selectedUnits)
            {
                if (unit == null) continue;

                if (unit.TryGetComponent<UnitMovement>(out var movement))
                {
                }
                movement?.SetDestination(destination);
            }
        }

        public void AttackMoveSelectedUnits(Vector3 destination)
        {
            // Implement attack-move logic
            MoveSelectedUnits(destination);
        }

        #endregion

        #region Helper Types

        public enum UnitType
        {
            Owned,
            Friendly,
            Neutral,
            Enemy
        }

        #endregion
    }
}


