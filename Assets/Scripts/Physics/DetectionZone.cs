using UnityEngine;
using System.Collections.Generic;

namespace FindersCheesers
{
    /// <summary>
    /// A zone that detects the number of colliders within an overlap box area.
    /// When the detected count reaches the trigger amount, the zone becomes "Triggered".
    /// Useful for pressure plates, area sensors, and other detection-based triggers.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Physics/Detection Zone")]
    public class DetectionZone : MonoBehaviour
    {
        #region Settings

        [Header("Zone Settings")]
        [Tooltip("The size of the overlap box zone")]
        [SerializeField]
        private Vector3 zoneSize = new Vector3(1f, 1f, 1f);

        [Tooltip("The center offset of the overlap box zone (local space)")]
        [SerializeField]
        private Vector3 zoneCenter = Vector3.zero;

        [Tooltip("Layer mask to filter which objects are detected")]
        [SerializeField]
        private LayerMask targetLayers = -1;

        [Tooltip("How often to update the detection (in seconds). 0 = update every frame")]
        [SerializeField]
        private float updateInterval = 0.1f;

        [Tooltip("The number of colliders required to trigger the zone")]
        [SerializeField]
        private int triggerAmount = 1;

        [Tooltip("Whether to show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Header("Visualization")]
        [Tooltip("Color for gizmo visualization when not triggered")]
        [SerializeField]
        private Color notTriggeredColor = new Color(0f, 1f, 0f, 0.3f);

        [Tooltip("Color for gizmo visualization when triggered")]
        [SerializeField]
        private Color triggeredColor = new Color(1f, 0f, 0f, 0.3f);

        #endregion

        #region Events

        /// <summary>
        /// Event fired when an object enters the zone.
        /// </summary>
        public event System.Action<GameObject> OnObjectEntered;

        /// <summary>
        /// Event fired when an object leaves the zone.
        /// </summary>
        public event System.Action<GameObject> OnObjectExited;

        /// <summary>
        /// Event fired when the zone becomes triggered (detected count reaches trigger amount).
        /// </summary>
        public event System.Action OnTriggered;

        /// <summary>
        /// Event fired when the zone becomes untriggered (detected count falls below trigger amount).
        /// </summary>
        public event System.Action OnUntriggered;

        /// <summary>
        /// Event fired when the detected count changes.
        /// </summary>
        public event System.Action<int> OnDetectedCountChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current number of detected colliders in the zone.
        /// </summary>
        public int DetectedCount { get; private set; }

        /// <summary>
        /// Gets whether any colliders are currently detected.
        /// </summary>
        public bool IsDetected => DetectedCount > 0;

        /// <summary>
        /// Gets whether the zone is currently triggered (detected count >= trigger amount).
        /// </summary>
        public bool IsTriggered { get; private set; }

        /// <summary>
        /// Gets the trigger amount.
        /// </summary>
        public int TriggerAmount => triggerAmount;

        /// <summary>
        /// Gets the collection of detected objects currently in the zone.
        /// </summary>
        public IReadOnlyCollection<GameObject> DetectedObjects => currentObjects;

        #endregion

        #region Private Fields

        private float lastUpdateTime;
        private HashSet<GameObject> currentObjects = new HashSet<GameObject>();
        private HashSet<GameObject> previousObjects = new HashSet<GameObject>();
        private bool wasTriggered = false;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            // Update detection at the specified interval
            if (updateInterval <= 0f || Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateDetection();
                lastUpdateTime = Time.time;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually triggers a detection update.
        /// </summary>
        public void ForceUpdate()
        {
            UpdateDetection();
        }

        /// <summary>
        /// Gets the current number of detected colliders.
        /// </summary>
        public int GetDetectedCount()
        {
            return DetectedCount;
        }

        /// <summary>
        /// Sets the trigger amount.
        /// </summary>
        /// <param name="amount">The new trigger amount value.</param>
        public void SetTriggerAmount(int amount)
        {
            triggerAmount = Mathf.Max(1, amount);

            if (debugMode)
            {
                Debug.Log($"[DetectionZone] {gameObject.name} trigger amount set to {triggerAmount}");
            }

            // Check if trigger state changed
            bool isNowTriggered = DetectedCount >= triggerAmount;
            if (isNowTriggered != wasTriggered)
            {
                wasTriggered = isNowTriggered;
                IsTriggered = isNowTriggered;
                if (isNowTriggered)
                {
                    OnTriggered?.Invoke();
                }
                else
                {
                    OnUntriggered?.Invoke();
                }
            }
        }

        /// <summary>
        /// Checks if a specific GameObject is currently detected in the zone.
        /// </summary>
        /// <param name="obj">The GameObject to check.</param>
        /// <returns>True if the object is detected, false otherwise.</returns>
        public bool IsObjectDetected(GameObject obj)
        {
            return currentObjects.Contains(obj);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the detection by finding all colliders in the overlap box.
        /// </summary>
        private void UpdateDetection()
        {
            // Swap the hash sets
            var temp = previousObjects;
            previousObjects = currentObjects;
            currentObjects = temp;
            currentObjects.Clear();

            // Calculate the center and half extents of the overlap box
            Vector3 center = transform.TransformPoint(zoneCenter);
            Vector3 halfExtents = Vector3.Scale(zoneSize, transform.lossyScale) * 0.5f;

            // Find all colliders in the overlap box
            Collider[] hitColliders = new Collider[50]; // Maximum of 50 objects
            int numColliders = Physics.OverlapBoxNonAlloc(center, halfExtents, hitColliders, transform.rotation, targetLayers);

            HashSet<GameObject> newObjects = new HashSet<GameObject>();

            // Count all detected colliders
            for (int i = 0; i < numColliders; i++)
            {
                Collider col = hitColliders[i];
                if (col == null || col.gameObject == gameObject)
                {
                    continue; // Skip null colliders and self
                }

                currentObjects.Add(col.gameObject);
                newObjects.Add(col.gameObject);
            }

            int newCount = currentObjects.Count;

            // Detect objects that entered
            foreach (var obj in newObjects)
            {
                if (!previousObjects.Contains(obj))
                {
                    OnObjectEntered?.Invoke(obj);
                    if (debugMode)
                    {
                        Debug.Log($"[DetectionZone] {obj.name} entered {gameObject.name}");
                    }
                }
            }

            // Detect objects that exited
            foreach (var obj in previousObjects)
            {
                if (!newObjects.Contains(obj))
                {
                    OnObjectExited?.Invoke(obj);
                    if (debugMode)
                    {
                        Debug.Log($"[DetectionZone] {obj.name} exited {gameObject.name}");
                    }
                }
            }

            // Check if count changed
            if (DetectedCount != newCount)
            {
                DetectedCount = newCount;
                OnDetectedCountChanged?.Invoke(DetectedCount);

                // Check if trigger threshold was crossed
                bool isNowTriggered = DetectedCount >= triggerAmount;
                if (isNowTriggered != wasTriggered)
                {
                    wasTriggered = isNowTriggered;
                    IsTriggered = isNowTriggered;
                    if (isNowTriggered)
                    {
                        OnTriggered?.Invoke();
                        if (debugMode)
                        {
                            Debug.Log($"[DetectionZone] {gameObject.name} triggered! Count: {DetectedCount}, Threshold: {triggerAmount}");
                        }
                    }
                    else
                    {
                        OnUntriggered?.Invoke();
                        if (debugMode)
                        {
                            Debug.Log($"[DetectionZone] {gameObject.name} untriggered. Count: {DetectedCount}, Threshold: {triggerAmount}");
                        }
                    }
                }

                if (debugMode)
                {
                    Debug.Log($"[DetectionZone] {gameObject.name} detected count: {DetectedCount}");
                }
            }
        }

        #endregion

        #region Editor

        /// <summary>
        /// Draws gizmos to visualize the detection zone.
        /// </summary>
        private void OnDrawGizmos()
        {
            // Use different colors based on trigger state
            Gizmos.color = IsTriggered ? triggeredColor : notTriggeredColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(zoneCenter, zoneSize);
            
            // Draw a semi-transparent fill
            Gizmos.color = IsTriggered ?
                new Color(triggeredColor.r, triggeredColor.g, triggeredColor.b, 0.1f) :
                new Color(notTriggeredColor.r, notTriggeredColor.g, notTriggeredColor.b, 0.1f);
            Gizmos.DrawCube(zoneCenter, zoneSize);

            // Draw count and threshold label
            if (DetectedCount > 0 || debugMode)
            {
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + zoneCenter,
                    $"Count: {DetectedCount}/{triggerAmount}\nTriggered: {IsTriggered}");
                #endif
            }
        }

        /// <summary>
        /// Validates settings in the editor.
        /// </summary>
        private void OnValidate()
        {
            // Ensure update interval is non-negative
            updateInterval = Mathf.Max(0f, updateInterval);
            
            // Ensure zone size is positive
            zoneSize = Vector3.Max(zoneSize, Vector3.one * 0.01f);
            
            // Ensure trigger amount is at least 1
            triggerAmount = Mathf.Max(1, triggerAmount);
        }

        #endregion
    }
}
