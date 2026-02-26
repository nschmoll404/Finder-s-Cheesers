using UnityEngine;
using UnityEngine.Splines;
using FindersCheesers;

namespace FindersCheesers.Splines
{
    /// <summary>
    /// Marks beginning point of a spline that a SplineRider can attach to.
    /// Requires a collider to be detected by SplineRider.
    /// Implements IInteractable for player interaction.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SplineBegin : MonoBehaviour, IInteractable
    {
        [Header("Spline Settings")]
        [SerializeField]
        [Tooltip("The spline that this begin point is associated with")]
        private SplineContainer splineContainer;

        [SerializeField]
        [Tooltip("The index of the spline to use (if multiple splines exist in the container)")]
        private int splineIndex = 0;

        [SerializeField]
        [Tooltip("The direction to travel along the spline (1 = forward, -1 = backward)")]
        private int travelDirection = 1;

        [SerializeField]
        [Tooltip("Should the rider automatically attach when colliding?")]
        private bool autoAttachOnCollision = false;

        [SerializeField]
        [Tooltip("The speed at which the rider travels along the spline")]
        private float rideSpeed = 5f;

        [SerializeField]
        [Tooltip("Should the rider detach when reaching the end of the spline?")]
        private bool detachAtEnd = true;

        [Header("Visual Settings")]
        [SerializeField]
        [Tooltip("Color to change to when a rider is nearby")]
        private Color highlightColor = Color.yellow;

        [SerializeField]
        [Tooltip("Should this object highlight when a rider is nearby?")]
        private bool highlightOnProximity = true;

        [SerializeField]
        [Tooltip("Detection radius for highlighting")]
        private float proximityRadius = 3f;

        [Header("Interaction Settings")]
        [SerializeField]
        [Tooltip("Description shown when player can interact with this spline")]
        private string interactionDescription = "Ride Spline";

        // Cached references
        private Collider splineCollider;
        private Renderer objectRenderer;
        private Color originalColor;
        private bool hasOriginalColor;

        // IInteractable event
        public event System.Action<GameObject, bool> OnInteracted;

        // Public properties
        public SplineContainer SplineContainer => splineContainer;
        public int SplineIndex => splineIndex;
        public int TravelDirection => travelDirection;
        public bool AutoAttachOnCollision => autoAttachOnCollision;
        public float RideSpeed => rideSpeed;
        public bool DetachAtEnd => detachAtEnd;

        // IInteractable implementation
        string IInteractable.InteractionDescription => interactionDescription;
        Transform IInteractable.Transform => transform;

        private void Awake()
        {
            splineCollider = GetComponent<Collider>();
            
            // Ensure the collider is a trigger
            if (splineCollider != null && !splineCollider.isTrigger)
            {
                splineCollider.isTrigger = true;
            }

            // Get renderer for highlighting
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null && objectRenderer.material.HasProperty("_Color"))
            {
                originalColor = objectRenderer.material.color;
                hasOriginalColor = true;
            }
        }

        private void Update()
        {
            if (highlightOnProximity)
            {
                CheckForNearbyRiders();
            }
        }

        private void CheckForNearbyRiders()
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, proximityRadius);
            bool riderNearby = false;

            foreach (Collider col in nearbyColliders)
            {
                if (col.TryGetComponent<SplineRider>(out _))
                {
                    riderNearby = true;
                    break;
                }
            }

            if (objectRenderer != null && hasOriginalColor)
            {
                objectRenderer.material.color = riderNearby ? highlightColor : originalColor;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (autoAttachOnCollision)
            {
                SplineRider rider = other.GetComponent<SplineRider>();
                if (rider != null)
                {
                    rider.AttachToSpline(this);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw detection radius
            Gizmos.color = highlightColor;
            Gizmos.DrawWireSphere(transform.position, proximityRadius);

            // Draw direction arrow
            Gizmos.color = Color.cyan;
            Vector3 direction = transform.forward * travelDirection;
            Gizmos.DrawLine(transform.position, transform.position + direction * 2f);

            // Draw arrow head
            Vector3 arrowBase = transform.position + direction * 2f;
            Vector3 arrowLeft = arrowBase - Quaternion.Euler(0, 30, 0) * direction * 0.5f;
            Vector3 arrowRight = arrowBase - Quaternion.Euler(0, -30, 0) * direction * 0.5f;
            Gizmos.DrawLine(arrowBase, arrowLeft);
            Gizmos.DrawLine(arrowBase, arrowRight);
        }

        /// <summary>
        /// Gets the starting position along the spline.
        /// </summary>
        /// <param name="normalizedPosition">The normalized position (0-1) along the spline</param>
        public void GetStartPosition(out Vector3 position, out Vector3 tangent)
        {
            if (splineContainer != null && splineContainer.Splines.Count > splineIndex)
            {
                float t = travelDirection > 0 ? 0f : 1f;
                position = splineContainer.EvaluatePosition(splineIndex, t);
                tangent = splineContainer.EvaluateTangent(splineIndex, t);
            }
            else
            {
                position = transform.position;
                tangent = transform.forward * travelDirection;
            }
        }

        /// <summary>
        /// Gets the end position along the spline.
        /// </summary>
        public Vector3 GetEndPosition()
        {
            if (splineContainer != null && splineContainer.Splines.Count > splineIndex)
            {
                float t = travelDirection > 0 ? 1f : 0f;
                return splineContainer.EvaluatePosition(splineIndex, t);
            }
            return transform.position;
        }

        #region IInteractable Implementation

        /// <summary>
        /// Checks if this spline can currently be interacted with.
        /// </summary>
        /// <returns>True if the spline is valid and can be ridden, false otherwise.</returns>
        public bool CanInteract()
        {
            return splineContainer != null && splineContainer.Splines.Count > splineIndex;
        }

        /// <summary>
        /// Performs the interaction with this spline.
        /// Attempts to attach the interactor's SplineRider component to this spline.
        /// </summary>
        /// <param name="interactor">The GameObject performing the interaction.</param>
        /// <returns>True if the interaction was successful, false otherwise.</returns>
        public bool Interact(GameObject interactor)
        {
            bool success = false;

            if (interactor == null)
            {
                OnInteracted?.Invoke(interactor, false);
                return false;
            }

            // Try to find SplineRider component on the interactor
            SplineRider rider = interactor.GetComponent<SplineRider>();
            if (rider != null)
            {
                rider.AttachToSpline(this);
                success = true;
            }
            else
            {
                // Try to find SplineRider on children
                rider = interactor.GetComponentInChildren<SplineRider>();
                if (rider != null)
                {
                    rider.AttachToSpline(this);
                    success = true;
                }
            }

            OnInteracted?.Invoke(interactor, success);
            return success;
        }

        #endregion
    }
}
