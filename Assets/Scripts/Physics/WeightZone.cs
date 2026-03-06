using UnityEngine;
using System.Collections.Generic;

namespace FindersCheesers
{
    /// <summary>
    /// A zone that detects all colliders within an overlap box and calculates their total weight.
    /// Other components can listen to weight change events to trigger actions based on weight thresholds.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Physics/Weight Zone")]
    public class WeightZone : MonoBehaviour
    {
        #region Settings

        [Header("Zone Settings")]
        [Tooltip("The size of the overlap box zone")]
        [SerializeField]
        private Vector3 zoneSize = new Vector3(1f, 1f, 1f);

        [Tooltip("The center offset of the overlap box zone (local space)")]
        [SerializeField]
        private Vector3 zoneCenter = Vector3.zero;

        [Tooltip("Layer mask to filter which objects contribute to the weight")]
        [SerializeField]
        private LayerMask targetLayers = -1;

        [Tooltip("How often to update the weight calculation (in seconds). 0 = update every frame")]
        [SerializeField]
        private float updateInterval = 0.1f;

        [Tooltip("The weight threshold for triggering events")]
        [SerializeField]
        private float thresholdWeight = 5f;

        [Tooltip("Whether to show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Header("Visualization")]
        [Tooltip("Color for gizmo visualization when weight is below threshold")]
        [SerializeField]
        private Color belowThresholdColor = new Color(0f, 1f, 0f, 0.3f);

        [Tooltip("Color for gizmo visualization when weight is above threshold")]
        [SerializeField]
        private Color aboveThresholdColor = new Color(1f, 0f, 0f, 0.3f);

        #endregion

        #region Events

        /// <summary>
        /// Event fired when the total weight in the zone changes.
        /// </summary>
        public event System.Action<float> OnWeightChanged;

        /// <summary>
        /// Event fired when an object enters the zone (detected by weight calculation).
        /// </summary>
        public event System.Action<GameObject> OnObjectEntered;

        /// <summary>
        /// Event fired when an object leaves the zone (detected by weight calculation).
        /// </summary>
        public event System.Action<GameObject> OnObjectExited;

        /// <summary>
        /// Event fired when the total weight crosses above the threshold.
        /// </summary>
        public event System.Action OnThresholdReached;

        /// <summary>
        /// Event fired when the total weight falls below the threshold.
        /// </summary>
        public event System.Action OnThresholdLost;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current total weight in the zone.
        /// </summary>
        public float TotalWeight { get; private set; }

        /// <summary>
        /// Gets the number of weighted objects currently in the zone.
        /// </summary>
        public int ObjectCount { get; private set; }

        /// <summary>
        /// Gets the collection of weighted objects currently in the zone.
        /// </summary>
        public IReadOnlyCollection<GameObject> WeightedObjects => currentObjects;

        /// <summary>
        /// Gets the threshold weight.
        /// </summary>
        public float ThresholdWeight => thresholdWeight;

        /// <summary>
        /// Gets whether the current total weight meets or exceeds the threshold.
        /// </summary>
        public bool IsThresholdMet => TotalWeight >= thresholdWeight;

        #endregion

        #region Private Fields

        private float lastUpdateTime;
        private HashSet<GameObject> currentObjects = new HashSet<GameObject>();
        private HashSet<GameObject> previousObjects = new HashSet<GameObject>();
        private bool wasThresholdMet = false;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            // Update weight at the specified interval
            if (updateInterval <= 0f || Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateWeight();
                lastUpdateTime = Time.time;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually triggers a weight calculation update.
        /// </summary>
        public void ForceUpdate()
        {
            UpdateWeight();
        }

        /// <summary>
        /// Gets the total weight of all objects in the zone.
        /// </summary>
        public float GetTotalWeight()
        {
            return TotalWeight;
        }

        /// <summary>
        /// Gets the number of weighted objects in the zone.
        /// </summary>
        public int GetObjectCount()
        {
            return ObjectCount;
        }

        /// <summary>
        /// Sets the threshold weight for triggering events.
        /// </summary>
        /// <param name="threshold">The new threshold weight value.</param>
        public void SetThresholdWeight(float threshold)
        {
            thresholdWeight = Mathf.Max(0f, threshold);
            
            if (debugMode)
            {
                Debug.Log($"[WeightZone] {gameObject.name} threshold weight set to {thresholdWeight}");
            }
            
            // Check if threshold state changed
            bool isNowMet = IsThresholdMet;
            if (isNowMet != wasThresholdMet)
            {
                wasThresholdMet = isNowMet;
                if (isNowMet)
                {
                    OnThresholdReached?.Invoke();
                }
                else
                {
                    OnThresholdLost?.Invoke();
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the weight calculation by detecting all colliders in the overlap box.
        /// </summary>
        private void UpdateWeight()
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

            float totalWeight = 0f;
            HashSet<GameObject> newObjects = new HashSet<GameObject>();

            // Calculate weight from all detected colliders
            for (int i = 0; i < numColliders; i++)
            {
                Collider col = hitColliders[i];
                if (col == null || col.gameObject == gameObject)
                {
                    continue; // Skip null colliders and self
                }

                // Get the Weight component
                Weight weightComponent = col.GetComponentInParent<Weight>();
                if (weightComponent != null)
                {
                    totalWeight += weightComponent.WeightValue;
                    currentObjects.Add(col.gameObject);
                    newObjects.Add(col.gameObject);
                }
            }

            // Detect objects that entered
            foreach (var obj in newObjects)
            {
                if (!previousObjects.Contains(obj))
                {
                    OnObjectEntered?.Invoke(obj);
                    if (debugMode)
                    {
                        Debug.Log($"[WeightZone] {obj.name} entered {gameObject.name}");
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
                        Debug.Log($"[WeightZone] {obj.name} exited {gameObject.name}");
                    }
                }
            }

            // Check if weight changed
            if (!Mathf.Approximately(TotalWeight, totalWeight))
            {
                TotalWeight = totalWeight;
                ObjectCount = currentObjects.Count;
                OnWeightChanged?.Invoke(TotalWeight);

                // Check if threshold was crossed
                bool isNowMet = IsThresholdMet;
                if (isNowMet != wasThresholdMet)
                {
                    wasThresholdMet = isNowMet;
                    if (isNowMet)
                    {
                        OnThresholdReached?.Invoke();
                        if (debugMode)
                        {
                            Debug.Log($"[WeightZone] {gameObject.name} threshold reached! Weight: {TotalWeight}, Threshold: {thresholdWeight}");
                        }
                    }
                    else
                    {
                        OnThresholdLost?.Invoke();
                        if (debugMode)
                        {
                            Debug.Log($"[WeightZone] {gameObject.name} threshold lost. Weight: {TotalWeight}, Threshold: {thresholdWeight}");
                        }
                    }
                }

                if (debugMode)
                {
                    Debug.Log($"[WeightZone] {gameObject.name} total weight: {TotalWeight}, objects: {ObjectCount}");
                }
            }
        }

        #endregion

        #region Editor

        /// <summary>
        /// Draws gizmos to visualize the weight zone.
        /// </summary>
        private void OnDrawGizmos()
        {
            // Use different colors based on threshold
            Gizmos.color = IsThresholdMet ? aboveThresholdColor : belowThresholdColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(zoneCenter, zoneSize);
            
            // Draw a semi-transparent fill
            Gizmos.color = IsThresholdMet ?
                new Color(aboveThresholdColor.r, aboveThresholdColor.g, aboveThresholdColor.b, 0.1f) :
                new Color(belowThresholdColor.r, belowThresholdColor.g, belowThresholdColor.b, 0.1f);
            Gizmos.DrawCube(zoneCenter, zoneSize);

            // Draw weight and threshold label
            if (TotalWeight > 0f || debugMode)
            {
                UnityEditor.Handles.Label(transform.position + zoneCenter,
                    $"Weight: {TotalWeight:F1}/{thresholdWeight:F1}\nObjects: {ObjectCount}");
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
            
            // Ensure threshold is non-negative
            thresholdWeight = Mathf.Max(0f, thresholdWeight);
        }

        #endregion
    }
}
