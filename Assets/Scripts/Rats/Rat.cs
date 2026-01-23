using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Represents a rat that can support the King Rat.
    /// Rats can be registered with the King Rat to help hold him up.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Rat")]
    public class Rat : MonoBehaviour
    {
        [Header("Rat Settings")]
        [Tooltip("The unique ID of this rat")]
        [SerializeField]
        private string ratId;

        [Tooltip("Is this rat currently supporting the King Rat?")]
        [SerializeField]
        private bool isSupportingKing = false;

        [Tooltip("The RatInventory this rat is currently supporting")]
        [SerializeField]
        private RatInventory currentRatInventory;

        [Tooltip("The strength of this rat for supporting the King Rat")]
        [SerializeField]
        private float supportStrength = 1f;

        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        /// <summary>
        /// Gets the unique ID of this rat.
        /// </summary>
        public string RatId
        {
            get
            {
                if (string.IsNullOrEmpty(ratId))
                {
                    ratId = System.Guid.NewGuid().ToString();
                }
                return ratId;
            }
        }

        /// <summary>
        /// Gets or sets whether this rat is currently supporting the King Rat.
        /// </summary>
        public bool IsSupportingKing
        {
            get => isSupportingKing;
            set => isSupportingKing = value;
        }

        /// <summary>
        /// Gets or sets the RatInventory this rat is currently supporting.
        /// </summary>
        public RatInventory CurrentRatInventory
        {
            get => currentRatInventory;
            set => currentRatInventory = value;
        }

        /// <summary>
        /// Gets the support strength of this rat.
        /// </summary>
        public float SupportStrength => supportStrength;

        /// <summary>
        /// Gets the position of this rat in world space.
        /// </summary>
        public Vector3 Position => transform.position;

        private void OnValidate()
        {
            // Generate a unique ID if none is set
            if (string.IsNullOrEmpty(ratId))
            {
                ratId = System.Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        /// Registers this rat with a RatInventory.
        /// </summary>
        /// <param name="ratInventory">The RatInventory to register with.</param>
        /// <returns>True if registration was successful, false otherwise.</returns>
        public bool RegisterWithRatInventory(RatInventory ratInventory)
        {
            if (ratInventory == null)
            {
                Debug.LogWarning("[Rat] Cannot register with null RatInventory.");
                return false;
            }

            if (currentRatInventory != null && currentRatInventory != ratInventory)
            {
                Debug.LogWarning($"[Rat] Already registered with another RatInventory. Unregister first.");
                return false;
            }

            bool success = ratInventory.AddRat(this);
            
            if (success)
            {
                currentRatInventory = ratInventory;
                isSupportingKing = true;

                if (debugMode)
                {
                    Debug.Log($"[Rat] Registered with RatInventory. Rat ID: {RatId}");
                }
            }

            return success;
        }

        /// <summary>
        /// Unregisters this rat from its current RatInventory.
        /// </summary>
        /// <returns>True if unregistration was successful, false otherwise.</returns>
        public bool UnregisterFromRatInventory()
        {
            if (currentRatInventory == null)
            {
                Debug.LogWarning("[Rat] Not registered with any RatInventory.");
                return false;
            }

            bool success = currentRatInventory.RemoveRat(this);
            
            if (success)
            {
                currentRatInventory = null;
                isSupportingKing = false;

                if (debugMode)
                {
                    Debug.Log($"[Rat] Unregistered from RatInventory. Rat ID: {RatId}");
                }
            }

            return success;
        }

        /// <summary>
        /// Sets the support strength of this rat.
        /// </summary>
        /// <param name="strength">The new support strength.</param>
        public void SetSupportStrength(float strength)
        {
            supportStrength = Mathf.Max(0f, strength);
        }

        private void OnDestroy()
        {
            // Unregister from RatInventory when destroyed
            if (isSupportingKing && currentRatInventory != null)
            {
                currentRatInventory.RemoveRat(this);
            }
        }

        private void OnDrawGizmos()
        {
            // Draw a visual indicator for this rat
            Gizmos.color = isSupportingKing ? Color.green : Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            if (isSupportingKing && currentRatInventory != null)
            {
                // Draw line to RatInventory owner
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, currentRatInventory.transform.position);
            }
        }
    }
}
