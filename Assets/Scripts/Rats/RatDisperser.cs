using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// A trigger component that disperses rats from a RatGatherer attached to the entering collider.
    /// Useful for creating penalty zones or hazards that cause the player to lose rats.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Rat Disperser")]
    public class RatDisperser : MonoBehaviour
    {
        [Header("Disperse Settings")]
        [Tooltip("Number of rats to disperse when an object enters the trigger")]
        [SerializeField]
        private int disperseAmount = 3;

        [Tooltip("If enabled, only disperses rats from objects with a specific tag")]
        [SerializeField]
        private bool requireTag = false;

        [Tooltip("The tag required for the object to trigger dispersal (only used if requireTag is enabled)")]
        [SerializeField]
        private string requiredTag = "Player";

        [Tooltip("If enabled, only disperses rats if the object has a RatGatherer component")]
        [SerializeField]
        private bool requireRatGatherer = true;

        [Tooltip("If enabled, adds a cooldown between dispersals for the same object")]
        [SerializeField]
        private bool useCooldown = false;

        [Tooltip("Cooldown time in seconds between dispersals for the same object")]
        [SerializeField]
        private float cooldownTime = 1.0f;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Visualize trigger area in Scene view")]
        [SerializeField]
        private bool visualizeTrigger = true;

        // Track last dispersal time for each object
        private System.Collections.Generic.Dictionary<Collider, float> lastDispersalTimes;

        private void Awake()
        {
            if (useCooldown)
            {
                lastDispersalTimes = new System.Collections.Generic.Dictionary<Collider, float>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check tag requirement
            if (requireTag && !other.CompareTag(requiredTag))
            {
                if (debugMode)
                {
                    Debug.Log($"[RatDisperser] Object '{other.name}' does not have required tag '{requiredTag}'. Skipping.");
                }
                return;
            }

            // Check cooldown
            if (useCooldown && lastDispersalTimes != null)
            {
                if (lastDispersalTimes.TryGetValue(other, out float lastTime))
                {
                    if (Time.time - lastTime < cooldownTime)
                    {
                        if (debugMode)
                        {
                            Debug.Log($"[RatDisperser] Object '{other.name}' is on cooldown. Skipping.");
                        }
                        return;
                    }
                }
            }

            // Try to get RatGatherer component
            RatGatherer ratGatherer = other.GetComponent<RatGatherer>();

            if (ratGatherer == null)
            {
                if (requireRatGatherer)
                {
                    if (debugMode)
                    {
                        Debug.LogWarning($"[RatDisperser] Object '{other.name}' does not have a RatGatherer component. Skipping.");
                    }
                }
                return;
            }

            // Disperse rats
            ratGatherer.DisperseRats(disperseAmount);

            // Update cooldown timer
            if (useCooldown && lastDispersalTimes != null)
            {
                lastDispersalTimes[other] = Time.time;
            }

            if (debugMode)
            {
                Debug.Log($"[RatDisperser] Dispersed {disperseAmount} rats from '{other.name}'.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Remove from cooldown tracking when object exits
            if (useCooldown && lastDispersalTimes != null)
            {
                lastDispersalTimes.Remove(other);
            }
        }

        private void OnDrawGizmos()
        {
            if (!visualizeTrigger)
            {
                return;
            }

            // Get the collider
            Collider collider = GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

            // Set color based on settings
            Gizmos.color = new Color(1.0f, 0.5f, 0.0f, 0.3f); // Orange with transparency

            // Draw the collider shape
            if (collider is BoxCollider boxCollider)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
            else if (collider is SphereCollider sphereCollider)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(Vector3.zero, capsuleCollider.radius);
                // Note: Capsule visualization is simplified
            }
        }

        private void Reset()
        {
            // Ensure the collider is set as a trigger
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            disperseAmount = 3;
            requiredTag = "Player";
            cooldownTime = 1.0f;
        }
    }
}
