using System.Collections.Generic;
using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// A component that manages an inventory of rats.
    /// This can be used by King Rat and other mechanics that need to track rats.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Rat Inventory")]
    public class RatInventory : MonoBehaviour
    {
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

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Visualize rat positions in Scene view")]
        [SerializeField]
        private bool visualizeRats = true;

        [Header("Rats To Add On Start")]
        [Tooltip("List of rats that can be configured in the Inspector. These rats will be automatically added to the inventory on Start.")]
        [SerializeField]
        private List<Rat> ratsToAddOnStart = new List<Rat>();

        [Header("King Rat Reference")]
        [Tooltip("Reference to the King Rat controller (for positioning rats in front)")]
        [SerializeField]
        private KingRatController kingRatController;

        // Inventory of rats
        private readonly List<Rat> rats = new List<Rat>();

        // Support calculation
        private Vector3 supportCenter;
        private float totalSupportStrength;

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

        private void Start()
        {
            // Get King Rat controller reference
            if (kingRatController == null)
            {
                kingRatController = GetComponentInParent<KingRatController>();
            }

            // Add rats from inspector list to inventory
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

        private void Update()
        {
            UpdateRatPositions();
            CalculateSupportMetrics();
        }

        /// <summary>
        /// Updates the positions of rats to be evenly distributed in the support radius.
        /// Rats move instantly to follow the King Rat, and the King Rat trails slightly behind.
        /// Rats are positioned in front of the King Rat (so King Rat trails behind).
        /// </summary>
        private void UpdateRatPositions()
        {
            if (rats.Count == 0)
            {
                return;
            }

            // Get King Rat's movement direction and speed
            Vector3 kingMovementDirection = Vector3.zero;
            float kingSpeed = 0f;
            
            if (kingRatController != null)
            {
                kingMovementDirection = kingRatController.GetVelocity();
                kingMovementDirection.y = 0f; // Ignore vertical movement
                kingSpeed = kingMovementDirection.magnitude;
            }

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
                targetPosition.x += Mathf.Cos(angle) * supportRadius;
                targetPosition.z += Mathf.Sin(angle) * supportRadius;

                // If King Rat is moving, position rats in front of movement direction
                // This makes King Rat trail behind the rats (rats carry King Rat)
                if (kingSpeed > 0.1f && kingMovementDirection != Vector3.zero)
                {
                    Vector3 movementDirection = kingMovementDirection.normalized;
                    targetPosition += movementDirection * 0.5f; // Rats are slightly in front
                }

                // Smoothly move the rat to its target position
                rat.transform.position = Vector3.Lerp(
                    rat.transform.position,
                    targetPosition,
                    ratPositioningSpeed * Time.deltaTime
                );
            }
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
            return offset.magnitude / supportRadius;
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

        private void OnDrawGizmos()
        {
            if (!visualizeRats)
            {
                return;
            }

            // Draw support radius
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, supportRadius);

            // Draw support center
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(supportCenter, 0.2f);

                // Draw lines to rats
                Gizmos.color = Color.green;
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
            }
        }

        private void Reset()
        {
            maxCapacity = 8;
            supportRadius = 1.5f;
            ratPositioningSpeed = 5f;
        }
    }
}
