using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FindersCheesers
{
    /// <summary>
    /// Stores position and rotation data for trail following.
    /// </summary>
    public struct TrailPoint
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Timestamp;

        public TrailPoint(Vector3 position, Quaternion rotation, float timestamp)
        {
            Position = position;
            Rotation = rotation;
            Timestamp = timestamp;
        }
    }

    /// <summary>
    /// A component that manages an inventory of rats.
    /// This can be used by Rat Pack and other mechanics that need to track rats.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Rat Inventory")]
    public class RatInventory : MonoBehaviour
    {
        /// <summary>
        /// Defines when rat positioning updates occur.
        /// </summary>
        public enum RatPositioningUpdateMode
        {
            Update,
            FixedUpdate,
            LateUpdate
        }

        /// <summary>
        /// Defines how rats follow the inventory owner.
        /// </summary>
        public enum RatFollowingMode
        {
            /// <summary>
            /// Rats form a circle around the owner (current behavior).
            /// </summary>
            Crowd,
            /// <summary>
            /// Rats follow in a trail/line behind the owner.
            /// </summary>
            Trail
        }

        [Header("Inventory Settings")]
        [Tooltip("Maximum number of rats that can be stored in this inventory")]
        [SerializeField]
        private int maxCapacity = 8;

        [Tooltip("Radius around this object where rats should be positioned")]
        [SerializeField]
        private float supportRadius = 1.5f;

        [Tooltip("How quickly rats move to their target positions")]
        [SerializeField]
        private float ratPositioningSpeed = 5f;

        [Tooltip("When to update rat positions")]
        [SerializeField]
        private RatPositioningUpdateMode ratPositioningUpdateMode = RatPositioningUpdateMode.Update;

        [Header("Following Mode Settings")]
        [Tooltip("Current following mode for rats (Crowd = circle, Trail = line behind)")]
        [SerializeField]
        private RatFollowingMode followingMode = RatFollowingMode.Crowd;

        [Tooltip("Spacing between rats when in trail mode")]
        [SerializeField]
        private float trailSpacing = 0.5f;

        [Tooltip("Offset distance behind the owner for trail mode")]
        [SerializeField]
        private float trailStartOffset = 0.5f;

        [Tooltip("How frequently to record trail positions (lower = smoother but more memory)")]
        [SerializeField]
        private float trailRecordInterval = 0.05f;

        [Tooltip("Maximum trail history duration in seconds")]
        [SerializeField]
        private float trailHistoryDuration = 5f;

        [Tooltip("How quickly rats rotate to face movement direction in trail mode")]
        [SerializeField]
        private float trailRotationSpeed = 10f;

        [Tooltip("Speed threshold below which rats start crowding instead of trailing")]
        [SerializeField]
        private float stopSpeedThreshold = 0.5f;

        [Tooltip("Radius for crowding when stopped in trail mode")]
        [SerializeField]
        private float trailCrowdRadius = 1.0f;

        [Tooltip("How quickly rats transition from trail to crowd formation when stopped")]
        [SerializeField]
        private float crowdTransitionSpeed = 3f;

        [Tooltip("Minimum distance between rats when crowding")]
        [SerializeField]
        private float minRatSeparation = 0.3f;

        [Header("Auto-Scaling Radius Settings")]
        [Tooltip("Enable automatic scaling of support radius based on rat count and size")]
        [SerializeField]
        private bool autoScaleRadius = false;

        [Tooltip("Minimum radius when auto-scaling is enabled")]
        [SerializeField]
        private float minRadius = 0.5f;

        [Tooltip("Maximum radius when auto-scaling is enabled")]
        [SerializeField]
        private float maxRadius = 3.0f;

        [Tooltip("Base radius per rat (multiplied by rat size)")]
        [SerializeField]
        private float radiusPerRat = 0.2f;

        [Tooltip("Consider rat scale when calculating auto-radius")]
        [SerializeField]
        private bool useRatScaleForRadius = true;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Visualize rat positions in Scene view")]
        [SerializeField]
        private bool visualizeRats = true;

        [Header("Gather/Disperse Settings")]
        [Tooltip("Radius to search for rats when gathering")]
        [SerializeField]
        private float gatherRadius = 5f;

        [Tooltip("Layer mask for finding rats")]
        [SerializeField]
        private LayerMask ratLayerMask = 1;

        [Tooltip("Tag that identifies rat GameObjects")]
        [SerializeField]
        private string ratTag = "Rat";

        [Tooltip("Distance rats will run away when dispersing")]
        [SerializeField]
        private float disperseDistance = 10f;

        [Header("Rats To Add On Start")]
        [Tooltip("List of rats that can be configured in the Inspector. These rats will be automatically added to the inventory on Start.")]
        [SerializeField]
        private List<Rat> ratsToAddOnStart = new List<Rat>();

        [Header("Rat Pack Reference")]
        [Tooltip("Reference to Rat Pack controller (for positioning rats in front)")]
        [SerializeField]
        private RatPackController ratPackController;

        [Header("Drop Rat Settings")]
        [Tooltip("Input action for dropping a rat from the inventory")]
        [SerializeField]
        private InputActionReference dropRatInput;

        [Tooltip("Distance in front of the player to drop the rat")]
        [SerializeField]
        private float dropDistance = 2f;

        [Tooltip("Whether to drop rats randomly around the drop point")]
        [SerializeField]
        private bool randomizeDropPosition = false;

        [Tooltip("Random radius for dropping rats when randomized")]
        [SerializeField]
        private float dropRandomRadius = 0.5f;

        // Inventory of rats
        private readonly List<Rat> rats = new List<Rat>();

        // Support calculation
        private Vector3 supportCenter;
        private float totalSupportStrength;

        // Trail following history
        private readonly Queue<TrailPoint> trailHistory = new Queue<TrailPoint>();
        private float lastTrailRecordTime;
        private Vector3 lastRecordedPosition;
        private Quaternion lastRecordedRotation;

        // Crowding state for trail mode
        private float crowdBlendFactor = 0f; // 0 = trail, 1 = crowd
        private float lastMovementSpeed = 0f;

        /// <summary>
        /// Event fired when a rat is added to the inventory.
        /// </summary>
        public event System.Action<Rat> OnRatAdded;

        /// <summary>
        /// Event fired when a rat is removed from the inventory.
        /// </summary>
        public event System.Action<Rat> OnRatRemoved;

        /// <summary>
        /// Gets the maximum capacity of the inventory.
        /// </summary>
        public int MaxCapacity => maxCapacity;

        /// <summary>
        /// Gets the current number of rats in the inventory.
        /// </summary>
        public int Count => rats.Count;

        /// <summary>
        /// Simple property for getting the rat count (alias for Count).
        /// </summary>
        public int RatCount => rats.Count;

        /// <summary>
        /// Gets whether the inventory is at maximum capacity.
        /// </summary>
        public bool IsFull => rats.Count >= maxCapacity;

        /// <summary>
        /// Gets the list of rats in the inventory.
        /// </summary>
        public IReadOnlyList<Rat> Rats => rats.AsReadOnly();

        /// <summary>
        /// Gets the center of support from all rats.
        /// </summary>
        public Vector3 SupportCenter => supportCenter;

        /// <summary>
        /// Gets the total support strength from all rats.
        /// </summary>
        public float TotalSupportStrength => totalSupportStrength;

        /// <summary>
        /// Gets the current rat positioning update mode.
        /// </summary>
        public RatPositioningUpdateMode UpdateMode => ratPositioningUpdateMode;

        /// <summary>
        /// Gets the current following mode for rats.
        /// </summary>
        public RatFollowingMode FollowingMode => followingMode;

        /// <summary>
        /// Gets the current effective support radius (auto-scaled if enabled).
        /// </summary>
        public float EffectiveSupportRadius => CalculateEffectiveRadius();

        /// <summary>
        /// Gets whether auto-scaling is enabled for the support radius.
        /// </summary>
        public bool IsAutoScaleEnabled => autoScaleRadius;

        /// <summary>
        /// Gets the gather radius for finding rats.
        /// </summary>
        public float GatherRadius => gatherRadius;

        /// <summary>
        /// Gets the disperse distance for rats running away.
        /// </summary>
        public float DisperseDistance => disperseDistance;

        private void Start()
        {
            // Get Rat Pack controller reference
            if (ratPackController == null)
            {
                ratPackController = GetComponentInParent<RatPackController>();
            }

            // Add rats from inspector list to the inventory
            if (ratsToAddOnStart != null)
            {
                foreach (Rat rat in ratsToAddOnStart)
                {
                    if (rat != null)
                    {
                        AddRat(rat);
                    }
                }
            }
        }

        private void OnEnable()
        {
            // Subscribe to drop rat input
            if (dropRatInput != null)
            {
                dropRatInput.action.Enable();
                dropRatInput.action.performed += OnDropRatInput;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from drop rat input
            if (dropRatInput != null)
            {
                dropRatInput.action.performed -= OnDropRatInput;
                dropRatInput.action.Disable();
            }
        }

        /// <summary>
        /// Handles the drop rat input action.
        /// </summary>
        /// <param name="context">The input action context.</param>
        private void OnDropRatInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                DropRat();
            }
        }

        private void Update()
        {
            // Update rat positions in Update if that mode is selected
            if (ratPositioningUpdateMode == RatPositioningUpdateMode.Update)
            {
                UpdateRatPositions();
            }

            CalculateSupportMetrics();
        }

        private void FixedUpdate()
        {
            // Update rat positions in FixedUpdate if that mode is selected
            if (ratPositioningUpdateMode == RatPositioningUpdateMode.FixedUpdate)
            {
                UpdateRatPositions();
            }
        }

        private void LateUpdate()
        {
            // Update rat positions in LateUpdate if that mode is selected
            if (ratPositioningUpdateMode == RatPositioningUpdateMode.LateUpdate)
            {
                UpdateRatPositions();
            }
        }

        /// <summary>
        /// Updates the positions of rats based on the current following mode.
        /// </summary>
        private void UpdateRatPositions()
        {
            if (rats.Count == 0)
            {
                return;
            }

            switch (followingMode)
            {
                case RatFollowingMode.Crowd:
                    UpdateCrowdPositions();
                    break;
                case RatFollowingMode.Trail:
                    UpdateTrailPositions();
                    break;
            }
        }

        /// <summary>
        /// Updates rat positions in a circle around the owner (crowd following).
        /// Rats are positioned in front of the Rat Pack (so the Rat Pack trails behind).
        /// </summary>
        private void UpdateCrowdPositions()
        {
            // Get appropriate delta time based on update mode
            float deltaTime = ratPositioningUpdateMode == RatPositioningUpdateMode.FixedUpdate
                ? Time.fixedDeltaTime
                : Time.deltaTime;

            // Get Rat Pack's movement direction and speed
            Vector3 packMovementDirection = Vector3.zero;
            float packSpeed = 0f;

            if (ratPackController != null)
            {
                packMovementDirection = ratPackController.GetVelocity();
                packMovementDirection.y = 0f; // Ignore vertical movement
                packSpeed = packMovementDirection.magnitude;
            }

            // Use effective radius (auto-scaled if enabled)
            float effectiveRadius = CalculateEffectiveRadius();

            // Calculate target positions for each rat in a circle
            for (int i = 0; i < rats.Count; i++)
            {
                Rat rat = rats[i];
                if (rat == null)
                {
                    continue;
                }

                // Calculate angle for this rat (evenly distributed)
                float angle = (360f / rats.Count) * i * Mathf.Deg2Rad;

                // Calculate base position in circle
                Vector3 targetPosition = transform.position;
                targetPosition.x += Mathf.Cos(angle) * effectiveRadius;
                targetPosition.z += Mathf.Sin(angle) * effectiveRadius;

                // If Rat Pack is moving, position rats in front of movement direction
                // This makes Rat Pack trail behind rats (rats carry Rat Pack)
                if (packSpeed > 0.1f && packMovementDirection != Vector3.zero)
                {
                    Vector3 movementDirection = packMovementDirection.normalized;
                    targetPosition += movementDirection * 0.5f; // Rats are slightly in front
                }

                // Smoothly move the rat to its target position
                rat.transform.position = Vector3.Lerp(
                    rat.transform.position,
                    targetPosition,
                    ratPositioningSpeed * deltaTime
                );
            }
        }

        /// <summary>
        /// Updates rat positions in a trail behind the owner (trail following).
        /// Uses position history for Pikmin-style following where each rat follows
        /// the path of the leader at a time offset.
        /// When stopped, rats spread out in a crowd formation to avoid overlapping.
        /// </summary>
        private void UpdateTrailPositions()
        {
            // Get appropriate delta time based on update mode
            float deltaTime = ratPositioningUpdateMode == RatPositioningUpdateMode.FixedUpdate
                ? Time.fixedDeltaTime
                : Time.deltaTime;

            float currentTime = Time.time;

            // Get movement speed
            float packSpeed = 0f;
            if (ratPackController != null)
            {
                Vector3 velocity = ratPackController.GetVelocity();
                velocity.y = 0f;
                packSpeed = velocity.magnitude;
            }

            // Smooth the speed for better transitions
            lastMovementSpeed = Mathf.Lerp(lastMovementSpeed, packSpeed, deltaTime * 5f);

            // Update crowd blend factor based on movement speed
            bool isMoving = lastMovementSpeed > stopSpeedThreshold;
            float targetBlendFactor = isMoving ? 0f : 1f;
            crowdBlendFactor = Mathf.Lerp(crowdBlendFactor, targetBlendFactor, crowdTransitionSpeed * deltaTime);

            // Record current position to trail history
            RecordTrailPosition(currentTime);

            // Clean up old trail points
            CleanupTrailHistory(currentTime);

            // Calculate target positions for all rats
            Vector3[] targetPositions = new Vector3[rats.Count];
            Quaternion[] targetRotations = new Quaternion[rats.Count];

            for (int i = 0; i < rats.Count; i++)
            {
                Rat rat = rats[i];
                if (rat == null)
                {
                    continue;
                }

                // Calculate rat scale
                float ratScale = 1f;
                if (useRatScaleForRadius && rat != null)
                {
                    ratScale = rat.transform.localScale.magnitude;
                }

                // Calculate trail position
                float timeDelay = (trailStartOffset + (i * trailSpacing * ratScale)) / Mathf.Max(0.1f, ratPositioningSpeed);
                TrailPoint trailPoint = GetTrailPointAtTime(currentTime - timeDelay);
                Vector3 trailPosition = trailPoint.Position;
                Quaternion trailRotation = trailPoint.Rotation;

                // Calculate crowd position (circle formation around owner)
                Vector3 crowdPosition = CalculateCrowdPosition(i, rats.Count, ratScale);

                // Blend between trail and crowd positions
                targetPositions[i] = Vector3.Lerp(trailPosition, crowdPosition, crowdBlendFactor);
                targetRotations[i] = Quaternion.Slerp(trailRotation, transform.rotation, crowdBlendFactor);
            }

            // Apply separation to prevent overlapping when crowding
            if (crowdBlendFactor > 0.1f)
            {
                ApplySeparation(ref targetPositions, deltaTime);
            }

            // Move rats to their target positions
            for (int i = 0; i < rats.Count; i++)
            {
                Rat rat = rats[i];
                if (rat == null)
                {
                    continue;
                }

                // Smoothly move the rat to the target position
                rat.transform.position = Vector3.Lerp(
                    rat.transform.position,
                    targetPositions[i],
                    ratPositioningSpeed * deltaTime
                );

                // Smoothly rotate to match the target rotation
                rat.transform.rotation = Quaternion.Slerp(
                    rat.transform.rotation,
                    targetRotations[i],
                    trailRotationSpeed * deltaTime
                );
            }
        }

        /// <summary>
        /// Calculates the crowd position for a rat in a circle formation.
        /// </summary>
        /// <param name="index">The index of the rat.</param>
        /// <param name="totalRats">Total number of rats.</param>
        /// <param name="ratScale">The scale of the rat.</param>
        /// <returns>The target crowd position.</returns>
        private Vector3 CalculateCrowdPosition(int index, int totalRats, float ratScale)
        {
            // Calculate angle for this rat (evenly distributed)
            float angle = (360f / totalRats) * index * Mathf.Deg2Rad;

            // Calculate radius based on number of rats and their scales
            float crowdRadius = trailCrowdRadius;
            if (autoScaleRadius)
            {
                crowdRadius = CalculateEffectiveRadius();
            }

            // Calculate position in circle
            Vector3 targetPosition = transform.position;
            targetPosition.x += Mathf.Cos(angle) * crowdRadius;
            targetPosition.z += Mathf.Sin(angle) * crowdRadius;

            return targetPosition;
        }

        /// <summary>
        /// Applies separation between rats to prevent overlapping.
        /// </summary>
        /// <param name="positions">Array of target positions to modify.</param>
        /// <param name="deltaTime">Delta time for smooth movement.</param>
        private void ApplySeparation(ref Vector3[] positions, float deltaTime)
        {
            if (positions.Length <= 1)
            {
                return;
            }

            // Multiple passes for better separation
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    Vector3 separationForce = Vector3.zero;
                    int neighborCount = 0;

                    for (int j = 0; j < positions.Length; j++)
                    {
                        if (i == j) continue;

                        float distance = Vector3.Distance(positions[i], positions[j]);
                        if (distance < minRatSeparation && distance > 0.001f)
                        {
                            // Calculate repulsion force
                            Vector3 direction = (positions[i] - positions[j]).normalized;
                            float forceMagnitude = (minRatSeparation - distance) / minRatSeparation;
                            separationForce += direction * forceMagnitude;
                            neighborCount++;
                        }
                    }

                    if (neighborCount > 0)
                    {
                        // Apply average separation force
                        separationForce /= neighborCount;
                        positions[i] += separationForce * 0.5f;
                    }
                }
            }
        }

        /// <summary>
        /// Records the current position to the trail history.
        /// </summary>
        /// <param name="currentTime">Current game time.</param>
        private void RecordTrailPosition(float currentTime)
        {
            // Only record at specified intervals
            if (currentTime - lastTrailRecordTime < trailRecordInterval)
            {
                return;
            }

            Vector3 currentPosition = transform.position;
            Quaternion currentRotation = transform.rotation;

            // Only record if position has changed significantly
            if (Vector3.Distance(currentPosition, lastRecordedPosition) < 0.01f)
            {
                return;
            }

            // Add new trail point
            trailHistory.Enqueue(new TrailPoint(currentPosition, currentRotation, currentTime));

            // Update last recorded values
            lastRecordedPosition = currentPosition;
            lastRecordedRotation = currentRotation;
            lastTrailRecordTime = currentTime;
        }

        /// <summary>
        /// Removes old trail points that are no longer needed.
        /// </summary>
        /// <param name="currentTime">Current game time.</param>
        private void CleanupTrailHistory(float currentTime)
        {
            float oldestAllowedTime = currentTime - trailHistoryDuration;

            while (trailHistory.Count > 0 && trailHistory.Peek().Timestamp < oldestAllowedTime)
            {
                trailHistory.Dequeue();
            }
        }

        /// <summary>
        /// Gets the trail point at a specific time, interpolating between recorded points.
        /// </summary>
        /// <param name="targetTime">The time to get the trail point for.</param>
        /// <returns>The interpolated trail point.</returns>
        private TrailPoint GetTrailPointAtTime(float targetTime)
        {
            if (trailHistory.Count == 0)
            {
                // No history, return current position
                return new TrailPoint(transform.position, transform.rotation, targetTime);
            }

            // Convert queue to array for easier searching
            TrailPoint[] points = trailHistory.ToArray();

            // If target time is before our oldest point, return the oldest
            if (targetTime <= points[0].Timestamp)
            {
                return points[0];
            }

            // If target time is after our newest point, return current position
            if (targetTime >= points[points.Length - 1].Timestamp)
            {
                return new TrailPoint(transform.position, transform.rotation, targetTime);
            }

            // Find the two points to interpolate between
            for (int i = 0; i < points.Length - 1; i++)
            {
                if (points[i].Timestamp <= targetTime && points[i + 1].Timestamp >= targetTime)
                {
                    // Calculate interpolation factor
                    float timeRange = points[i + 1].Timestamp - points[i].Timestamp;
                    float t = timeRange > 0f ? (targetTime - points[i].Timestamp) / timeRange : 0f;

                    // Interpolate position and rotation
                    Vector3 interpolatedPosition = Vector3.Lerp(points[i].Position, points[i + 1].Position, t);
                    Quaternion interpolatedRotation = Quaternion.Slerp(points[i].Rotation, points[i + 1].Rotation, t);

                    return new TrailPoint(interpolatedPosition, interpolatedRotation, targetTime);
                }
            }

            // Fallback: return current position
            return new TrailPoint(transform.position, transform.rotation, targetTime);
        }

        /// <summary>
        /// Clears the trail history. Call this when switching modes or resetting.
        /// </summary>
        public void ClearTrailHistory()
        {
            trailHistory.Clear();
            lastTrailRecordTime = 0f;
        }

        /// <summary>
        /// Calculates the effective support radius, accounting for auto-scaling if enabled.
        /// </summary>
        /// <returns>The effective radius to use for rat positioning.</returns>
        private float CalculateEffectiveRadius()
        {
            if (!autoScaleRadius)
            {
                return supportRadius;
            }

            if (rats.Count == 0)
            {
                return minRadius;
            }

            // Calculate total size factor from all rats
            float totalSizeFactor = 0f;
            foreach (Rat rat in rats)
            {
                if (rat == null)
                {
                    continue;
                }

                if (useRatScaleForRadius)
                {
                    // Use the average scale of the rat
                    totalSizeFactor += rat.transform.localScale.magnitude;
                }
                else
                {
                    totalSizeFactor += 1f;
                }
            }

            // Calculate radius based on rat count and size
            float calculatedRadius = minRadius + (totalSizeFactor * radiusPerRat);

            // Clamp to min/max range
            return Mathf.Clamp(calculatedRadius, minRadius, maxRadius);
        }

        /// <summary>
        /// Calculates the center of support and total support strength.
        /// </summary>
        private void CalculateSupportMetrics()
        {
            if (rats.Count == 0)
            {
                supportCenter = transform.position;
                totalSupportStrength = 0f;
                return;
            }

            // Calculate the center of all rats
            Vector3 centerSum = Vector3.zero;
            totalSupportStrength = 0f;

            foreach (Rat rat in rats)
            {
                if (rat == null)
                {
                    continue;
                }

                centerSum += rat.Position * rat.SupportStrength;
                totalSupportStrength += rat.SupportStrength;
            }

            if (totalSupportStrength > 0f)
            {
                supportCenter = centerSum / totalSupportStrength;
            }
            else
            {
                supportCenter = transform.position;
            }

            if (debugMode)
            {
                Debug.Log($"[RatInventory] Support Center: {supportCenter}, Total Strength: {totalSupportStrength}");
            }
        }

        /// <summary>
        /// Adds a rat to the inventory.
        /// </summary>
        /// <param name="rat">The rat to add.</param>
        /// <returns>True if the rat was added, false otherwise.</returns>
        public bool AddRat(Rat rat)
        {
            if (rat == null)
            {
                Debug.LogWarning("[RatInventory] Cannot add null rat.");
                return false;
            }

            if (rats.Contains(rat))
            {
                Debug.LogWarning("[RatInventory] Rat is already in this inventory.");
                return false;
            }

            if (IsFull)
            {
                Debug.LogWarning("[RatInventory] Inventory is at maximum capacity.");
                return false;
            }

            rats.Add(rat);
            rat.IsSupportingKing = true;
            rat.CurrentRatInventory = this;

            // Disable NavMeshAgent when rat is picked up
            rat.DisableNavAgent();

            // Reset drop cooldown when rat is added to inventory
            rat.ResetDropCooldown();

            OnRatAdded?.Invoke(rat);

            if (debugMode)
            {
                Debug.Log($"[RatInventory] Rat added. Total rats: {rats.Count}");
            }

            return true;
        }

        /// <summary>
        /// Removes a rat from the inventory.
        /// </summary>
        /// <param name="rat">The rat to remove.</param>
        /// <returns>True if the rat was removed, false otherwise.</returns>
        public bool RemoveRat(Rat rat)
        {
            if (rat == null)
            {
                Debug.LogWarning("[RatInventory] Cannot remove null rat.");
                return false;
            }

            bool removed = rats.Remove(rat);

            if (removed)
            {
                rat.IsSupportingKing = false;
                rat.CurrentRatInventory = null;

                // Re-enable NavMeshAgent when rat is removed from inventory
                rat.EnableNavAgent();

                OnRatRemoved?.Invoke(rat);

                if (debugMode)
                {
                    Debug.Log($"[RatInventory] Rat removed. Total rats: {rats.Count}");
                }
            }

            return removed;
        }

        /// <summary>
        /// Removes a rat at the specified index.
        /// </summary>
        /// <param name="index">The index of the rat to remove.</param>
        /// <returns>The removed rat, or null if index is invalid.</returns>
        public Rat RemoveRatAt(int index)
        {
            if (index < 0 || index >= rats.Count)
            {
                Debug.LogWarning($"[RatInventory] Invalid index: {index}");
                return null;
            }

            Rat rat = rats[index];
            bool removed = RemoveRat(rat);
            return removed ? rat : null;
        }

        /// <summary>
        /// Removes all rats from the inventory.
        /// </summary>
        public void Clear()
        {
            // Create a copy of the list to avoid modification during iteration
            List<Rat> ratsToClear = new List<Rat>(rats);

            foreach (Rat rat in ratsToClear)
            {
                RemoveRat(rat);
            }

            if (debugMode)
            {
                Debug.Log("[RatInventory] Inventory cleared.");
            }
        }

        /// <summary>
        /// Drops a rat from the inventory at a position in front of the owner.
        /// </summary>
        /// <returns>The dropped rat, or null if no rats were available.</returns>
        public Rat DropRat()
        {
            if (rats.Count == 0)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[RatInventory] No rats to drop.");
                }
                return null;
            }

            // Get the last rat in the inventory (most recently added)
            Rat ratToDrop = rats[rats.Count - 1];

            if (ratToDrop == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[RatInventory] Rat to drop is null.");
                }
                return null;
            }

            // Calculate drop position in front of the owner
            Vector3 dropPosition = CalculateDropPosition();

            // Remove rat from inventory
            RemoveRat(ratToDrop);

            // Set the rat's position
            ratToDrop.transform.position = dropPosition;

            // Reset rat's rotation to face forward
            ratToDrop.transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);

            // Mark the rat as dropped to start cooldown
            ratToDrop.MarkAsDropped();

            if (debugMode)
            {
                Debug.Log($"[RatInventory] Dropped rat at position: {dropPosition}");
            }

            return ratToDrop;
        }

        /// <summary>
        /// Calculates the position where a rat should be dropped.
        /// </summary>
        /// <returns>The drop position.</returns>
        private Vector3 CalculateDropPosition()
        {
            Vector3 dropPosition = transform.position;

            // Get forward direction, ignoring vertical component
            Vector3 forwardDirection = transform.forward;
            forwardDirection.y = 0f;
            forwardDirection = forwardDirection.normalized;

            // Calculate position in front of the owner
            dropPosition += forwardDirection * dropDistance;

            // Add random offset if enabled
            if (randomizeDropPosition)
            {
                Vector2 randomOffset = Random.insideUnitCircle * dropRandomRadius;
                dropPosition.x += randomOffset.x;
                dropPosition.z += randomOffset.y;
            }

            return dropPosition;
        }

        /// <summary>
        /// Gathers all rats within the gather radius and makes them move toward this inventory.
        /// </summary>
        /// <returns>The number of rats gathered.</returns>
        public int GatherRats()
        {
            int gatheredCount = 0;

            // Find all colliders within gather radius
            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                gatherRadius,
                ratLayerMask,
                QueryTriggerInteraction.Ignore
            );

            // Process each collider to find rats
            foreach (Collider collider in colliders)
            {
                if (collider == null)
                {
                    continue;
                }

                // Check if collider has a Rat component
                Rat rat = collider.GetComponent<Rat>();
                if (rat == null)
                {
                    // Check parent object
                    rat = collider.GetComponentInParent<Rat>();
                }

                if (rat == null)
                {
                    continue;
                }

                // Skip if already in this inventory
                if (rats.Contains(rat))
                {
                    continue;
                }

                // Skip if already moving to an inventory
                if (rat.IsMovingToInventory)
                {
                    continue;
                }

                // Skip if running away
                if (rat.IsRunningAway)
                {
                    continue;
                }

                // Check if rat can be gathered (drop cooldown check)
                if (!rat.CanBeGathered(transform.position))
                {
                    continue;
                }

                // Check if rat matches to tag (if specified)
                if (!string.IsNullOrEmpty(ratTag) && !collider.gameObject.CompareTag(ratTag))
                {
                    continue;
                }

                // Make rat move toward this inventory
                rat.MoveToInventory(this);
                gatheredCount++;

                if (debugMode)
                {
                    Debug.Log($"[RatInventory] Gathering rat. Rat ID: {rat.RatId}");
                }
            }

            if (debugMode && gatheredCount > 0)
            {
                Debug.Log($"[RatInventory] Gathered {gatheredCount} rats.");
            }

            return gatheredCount;
        }

        /// <summary>
        /// Disperses the specified number of rats from the inventory, making them run away.
        /// </summary>
        /// <param name="amount">The number of rats to disperse.</param>
        /// <returns>The actual number of rats dispersed.</returns>
        public int DisperseRats(int amount)
        {
            if (rats.Count == 0)
            {
                return 0;
            }

            // Clamp amount to available rats
            int actualAmount = Mathf.Min(amount, rats.Count);
            int dispersedCount = 0;

            // Collect rats to disperse first to avoid modifying list during iteration
            List<Rat> ratsToDisperse = new List<Rat>();
            for (int i = 0; i < actualAmount; i++)
            {
                int index = rats.Count - 1 - i;
                Rat rat = rats[index];

                if (rat != null)
                {
                    ratsToDisperse.Add(rat);
                }
            }

            // Disperse the collected rats
            foreach (Rat rat in ratsToDisperse)
            {
                // Make rat run away (this will call UnregisterFromRatInventory)
                rat.RunAway(disperseDistance);
                dispersedCount++;

                if (debugMode)
                {
                    Debug.Log($"[RatInventory] Dispersing rat. Rat ID: {rat.RatId}");
                }
            }

            if (debugMode && dispersedCount > 0)
            {
                Debug.Log($"[RatInventory] Dispersed {dispersedCount} rats.");
            }

            return dispersedCount;
        }

        /// <summary>
        /// Checks if a rat is in the inventory.
        /// </summary>
        /// <param name="rat">The rat to check.</param>
        /// <returns>True if the rat is in the inventory, false otherwise.</returns>
        public bool Contains(Rat rat)
        {
            return rats.Contains(rat);
        }

        /// <summary>
        /// Gets a rat at the specified index.
        /// </summary>
        /// <param name="index">The index of the rat to get.</param>
        /// <returns>The rat at the index, or null if index is invalid.</returns>
        public Rat GetRat(int index)
        {
            if (index < 0 || index >= rats.Count)
            {
                return null;
            }
            return rats[index];
        }

        /// <summary>
        /// Finds a rat by its ID.
        /// </summary>
        /// <param name="ratId">The ID of the rat to find.</param>
        /// <returns>The rat with the matching ID, or null if not found.</returns>
        public Rat FindRatById(string ratId)
        {
            if (string.IsNullOrEmpty(ratId))
            {
                return null;
            }

            foreach (Rat rat in rats)
            {
                if (rat != null && rat.RatId == ratId)
                {
                    return rat;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets all rats in the inventory.
        /// </summary>
        /// <returns>A copy of the rats list.</returns>
        public List<Rat> GetAllRats()
        {
            return new List<Rat>(rats);
        }

        /// <summary>
        /// Calculates the support imbalance (0 = perfectly balanced, 1 = maximum imbalance).
        /// </summary>
        /// <returns>The support imbalance value.</returns>
        public float CalculateSupportImbalance()
        {
            if (rats.Count == 0)
            {
                return 0f;
            }

            Vector3 offset = supportCenter - transform.position;
            offset.y = 0f; // Ignore height difference
            return offset.magnitude / CalculateEffectiveRadius();
        }

        /// <summary>
        /// Sets the maximum capacity of the inventory.
        /// </summary>
        /// <param name="capacity">The new maximum capacity.</param>
        public void SetMaxCapacity(int capacity)
        {
            maxCapacity = Mathf.Max(0, capacity);
        }

        /// <summary>
        /// Sets the support radius.
        /// </summary>
        /// <param name="radius">The new support radius.</param>
        public void SetSupportRadius(float radius)
        {
            supportRadius = Mathf.Max(0.1f, radius);
        }

        /// <summary>
        /// Sets the rat positioning speed.
        /// </summary>
        /// <param name="speed">The new rat positioning speed.</param>
        public void SetRatPositioningSpeed(float speed)
        {
            ratPositioningSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>
        /// Sets the rat positioning update mode.
        /// </summary>
        /// <param name="mode">The new update mode.</param>
        public void SetRatPositioningUpdateMode(RatPositioningUpdateMode mode)
        {
            ratPositioningUpdateMode = mode;
            if (debugMode)
            {
                Debug.Log($"[RatInventory] Rat positioning update mode set to: {mode}");
            }
        }

        /// <summary>
        /// Sets the following mode for rats (Crowd or Trail).
        /// </summary>
        /// <param name="mode">The new following mode.</param>
        public void SetFollowingMode(RatFollowingMode mode)
        {
            // Clear trail history when switching modes
            if (mode != followingMode)
            {
                ClearTrailHistory();
            }

            followingMode = mode;
            if (debugMode)
            {
                Debug.Log($"[RatInventory] Following mode set to: {mode}");
            }
        }

        /// <summary>
        /// Switches to crowd following mode.
        /// </summary>
        public void UseCrowdFollowing()
        {
            SetFollowingMode(RatFollowingMode.Crowd);
        }

        /// <summary>
        /// Switches to trail following mode.
        /// </summary>
        public void UseTrailFollowing()
        {
            SetFollowingMode(RatFollowingMode.Trail);
        }

        /// <summary>
        /// Toggles between crowd and trail following modes.
        /// </summary>
        public void ToggleFollowingMode()
        {
            followingMode = followingMode == RatFollowingMode.Crowd
                ? RatFollowingMode.Trail
                : RatFollowingMode.Crowd;

            if (debugMode)
            {
                Debug.Log($"[RatInventory] Toggled following mode to: {followingMode}");
            }
        }

        /// <summary>
        /// Enables or disables auto-scaling for the support radius.
        /// </summary>
        /// <param name="enabled">Whether to enable auto-scaling.</param>
        public void SetAutoScaleRadius(bool enabled)
        {
            autoScaleRadius = enabled;
            if (debugMode)
            {
                Debug.Log($"[RatInventory] Auto-scale radius set to: {enabled}");
            }
        }

        /// <summary>
        /// Sets the minimum and maximum radius for auto-scaling.
        /// </summary>
        /// <param name="min">Minimum radius.</param>
        /// <param name="max">Maximum radius.</param>
        public void SetRadiusRange(float min, float max)
        {
            minRadius = Mathf.Max(0.1f, min);
            maxRadius = Mathf.Max(minRadius, max);
        }

        private void OnDrawGizmos()
        {
            if (!visualizeRats)
            {
                return;
            }

            // Use effective radius for visualization
            float effectiveRadius = Application.isPlaying ? CalculateEffectiveRadius() : supportRadius;

            // Draw support radius based on following mode
            if (followingMode == RatFollowingMode.Crowd)
            {
                // Draw circle for crowd mode
                Gizmos.color = autoScaleRadius ? Color.magenta : Color.cyan;
                Gizmos.DrawWireSphere(transform.position, effectiveRadius);
            }
            else
            {
                // Draw trail visualization for trail mode
                Gizmos.color = Color.blue;

                // Draw trail history path
                if (Application.isPlaying && trailHistory.Count > 1)
                {
                    TrailPoint[] points = trailHistory.ToArray();
                    Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.5f); // Light blue

                    for (int i = 0; i < points.Length - 1; i++)
                    {
                        Gizmos.DrawLine(points[i].Position, points[i + 1].Position);
                    }
                }

                // Draw target positions for each rat in trail
                float currentTime = Time.time;
                Gizmos.color = Color.cyan;
                for (int i = 0; i < rats.Count; i++)
                {
                    Rat rat = rats[i];
                    if (rat == null) continue;

                    float ratScale = 1f;
                    if (useRatScaleForRadius && rat != null)
                    {
                        ratScale = rat.transform.localScale.magnitude;
                    }

                    float timeDelay = (trailStartOffset + (i * trailSpacing * ratScale)) / Mathf.Max(0.1f, ratPositioningSpeed);
                    TrailPoint targetPoint = GetTrailPointAtTime(currentTime - timeDelay);
                    Gizmos.DrawWireSphere(targetPoint.Position, 0.15f);
                }
            }

            // Draw support center
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(supportCenter, 0.2f);

                // Draw lines to rats
                Gizmos.color = followingMode == RatFollowingMode.Crowd ? Color.green : Color.cyan;
                foreach (Rat rat in rats)
                {
                    if (rat != null)
                    {
                        Gizmos.DrawLine(transform.position, rat.Position);
                    }
                }

                // Draw inspector rats (in red)
                Gizmos.color = Color.red;
                foreach (Rat rat in ratsToAddOnStart)
                {
                    if (rat != null && !rats.Contains(rat))
                    {
                        Gizmos.DrawWireSphere(rat.transform.position, 0.3f);
                    }
                }

                // Draw min/max radius indicators when auto-scaling
                if (autoScaleRadius)
                {
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange
                    Gizmos.DrawWireSphere(transform.position, minRadius);
                    Gizmos.color = new Color(0.5f, 0f, 1f, 0.5f); // Purple
                    Gizmos.DrawWireSphere(transform.position, maxRadius);
                }
            }
        }

        private void Reset()
        {
            maxCapacity = 8;
            supportRadius = 1.5f;
            ratPositioningSpeed = 5f;
            ratPositioningUpdateMode = RatPositioningUpdateMode.Update;
            followingMode = RatFollowingMode.Crowd;
            trailSpacing = 0.5f;
            trailStartOffset = 0.5f;
            trailRecordInterval = 0.05f;
            trailHistoryDuration = 5f;
            trailRotationSpeed = 10f;
            stopSpeedThreshold = 0.5f;
            trailCrowdRadius = 1.0f;
            crowdTransitionSpeed = 3f;
            minRatSeparation = 0.3f;
            autoScaleRadius = false;
            minRadius = 0.5f;
            maxRadius = 3.0f;
            radiusPerRat = 0.2f;
            useRatScaleForRadius = true;
        }
    }
}
