using UnityEngine;
using UnityEngine.InputSystem;

namespace FindersCheesers
{
    /// <summary>
    /// A component that throws the King Rat to a destination using a physics arc.
    /// Uses pointer input for point-and-click targeting and visualizes the arc with a LineRenderer.
    /// The throw distance is based on the number of rats in the inventory - more rats = further throw.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/King Rat Thrower")]
    public class KingRatThrower : MonoBehaviour
    {
        [Header("Input References")]
        [Tooltip("Reference to the Pointer input action for getting mouse position")]
        [SerializeField]
        private InputActionReference pointerActionReference;

        [Tooltip("Reference to the Throw input action (click to throw)")]
        [SerializeField]
        private InputActionReference throwActionReference;

        [Tooltip("Use PlayerInputSingleton to get PlayerInput")]
        [SerializeField]
        private bool usePlayerInputSingleton = false;

        [Header("Camera Settings")]
        [Tooltip("Camera to use for raycasting to world position")]
        [SerializeField]
        private Camera mainCamera;

        [Header("Rat Pack")]
        [Tooltip("Reference to the RatPackController component")]
        [SerializeField]
        private RatPackController ratPackController;

        [Tooltip("Reference to the RatInventory component")]
        [SerializeField]
        private RatInventory ratInventory;

        [Header("King Rat")]
        [Tooltip("Reference to the KingRatGrabber component")]
        [SerializeField]
        private KingRatGrabber kingRatGrabber;

        [Header("Arc Visualization")]
        [Tooltip("LineRenderer for visualizing the throw arc")]
        [SerializeField]
        private LineRenderer arcLineRenderer;

        [Tooltip("Number of segments in the arc visualization")]
        [SerializeField]
        private int arcSegments = 30;

        [Tooltip("Color of the arc line")]
        [SerializeField]
        private Color arcColor = Color.yellow;

        [Header("Throw Settings")]
        [Tooltip("Base launch speed for the King Rat")]
        [SerializeField]
        private float baseLaunchSpeed = 10f;

        [Tooltip("Additional launch speed per rat in inventory")]
        [SerializeField]
        private float speedPerRat = 2f;

        [Tooltip("Maximum launch speed")]
        [SerializeField]
        private float maxLaunchSpeed = 30f;

        [Tooltip("Launch angle in degrees")]
        [SerializeField]
        private float launchAngle = 45f;

        [Tooltip("Duration of the throw animation")]
        [SerializeField]
        private float throwDuration = 1f;

        [Tooltip("Height offset for the launch position")]
        [SerializeField]
        private float launchHeightOffset = 1f;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Visualize throw target in Scene view")]
        [SerializeField]
        private bool visualizeTarget = true;

        // Component references
        private PlayerInput playerInput;
        private InputAction pointerAction;
        private InputAction throwAction;
        private Rigidbody kingRatRigidbody;

        // Current state
        private Vector2 pointerPosition;
        private Vector3? targetPosition;
        private bool isThrowing;
        private Vector3 throwStartPosition;
        private Vector3 throwEndPosition;
        private float throwTimer;
        private Vector3[] arcPoints;
        private Vector3 kingRatOriginalPosition;
        private Quaternion kingRatOriginalRotation;

        /// <summary>
        /// Event fired when the King Rat is thrown.
        /// </summary>
        public event System.Action<Vector3> OnKingRatThrown;

        /// <summary>
        /// Event fired when the King Rat lands.
        /// </summary>
        public event System.Action<Vector3> OnKingRatLanded;

        private void Awake()
        {
            // Initialize arc points array
            arcPoints = new Vector3[arcSegments + 1];

            // Get or create LineRenderer
            if (arcLineRenderer == null)
            {
                arcLineRenderer = GetComponent<LineRenderer>();
                if (arcLineRenderer == null)
                {
                    arcLineRenderer = gameObject.AddComponent<LineRenderer>();
                }
            }

            // Configure LineRenderer
            arcLineRenderer.positionCount = arcSegments + 1;
            arcLineRenderer.startWidth = 0.1f;
            arcLineRenderer.endWidth = 0.05f;
            arcLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            arcLineRenderer.startColor = arcColor;
            arcLineRenderer.endColor = arcColor;
            arcLineRenderer.enabled = false;
        }

        private void Start()
        {
            // Get PlayerInput - either from singleton or from the same GameObject
            if (usePlayerInputSingleton)
            {
                if (PlayerInputSingleton.IsInitialized())
                {
                    playerInput = PlayerInputSingleton.Instance.PlayerInput;
                    
                    if (debugMode)
                    {
                        Debug.Log("[KingRatThrower] Using PlayerInputSingleton for input.");
                    }
                }
                else
                {
                    Debug.LogError("[KingRatThrower] PlayerInputSingleton is not initialized!");
                }
            }
            else
            {
                playerInput = GetComponent<PlayerInput>();
                
                if (playerInput == null)
                {
                    Debug.LogError("[KingRatThrower] PlayerInput component not found on this GameObject!");
                }
            }

            // Get the pointer action using the ID from InputActionReference
            if (playerInput != null && pointerActionReference != null)
            {
                pointerAction = playerInput.actions.FindAction(pointerActionReference.action.id);
                
                if (pointerAction == null)
                {
                    Debug.LogError("[KingRatThrower] Pointer action not found in PlayerInput actions!");
                }
            }
            else if (pointerActionReference == null)
            {
                Debug.LogError("[KingRatThrower] Pointer Action Reference is not assigned!");
            }

            // Get the throw action using the ID from InputActionReference
            if (playerInput != null && throwActionReference != null)
            {
                throwAction = playerInput.actions.FindAction(throwActionReference.action.id);
                
                if (throwAction == null)
                {
                    Debug.LogError("[KingRatThrower] Throw action not found in PlayerInput actions!");
                }
            }
            else if (throwActionReference == null)
            {
                Debug.LogError("[KingRatThrower] Throw Action Reference is not assigned!");
            }

            // Get RatPackController component
            if (ratPackController == null)
            {
                ratPackController = GetComponent<RatPackController>();
                
                if (ratPackController == null)
                {
                    Debug.LogError("[KingRatThrower] RatPackController component not found on this GameObject!");
                }
            }

            // Get RatInventory component
            if (ratInventory == null)
            {
                ratInventory = GetComponent<RatInventory>();
                
                if (ratInventory == null)
                {
                    ratInventory = GetComponentInParent<RatInventory>();
                }

                if (ratInventory == null)
                {
                    Debug.LogError("[KingRatThrower] RatInventory component not found!");
                }
            }

            // Get camera if not assigned
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // Get KingRatGrabber component
            if (kingRatGrabber == null)
            {
                kingRatGrabber = GetComponent<KingRatGrabber>();
                
                if (kingRatGrabber == null)
                {
                    kingRatGrabber = GetComponentInParent<KingRatGrabber>();
                }

                if (kingRatGrabber == null)
                {
                    Debug.LogError("[KingRatThrower] KingRatGrabber component not found!");
                }
            }

            // Get King Rat Rigidbody from KingRatGrabber
            if (kingRatGrabber != null && kingRatGrabber.KingRat != null)
            {
                kingRatRigidbody = kingRatGrabber.KingRat.GetComponent<Rigidbody>();
                kingRatOriginalPosition = kingRatGrabber.KingRat.transform.position;
                kingRatOriginalRotation = kingRatGrabber.KingRat.transform.rotation;
            }
            else
            {
                Debug.LogError("[KingRatThrower] King Rat is not assigned in KingRatGrabber!");
            }
        }

        private void Update()
        {
            // Read pointer position
            if (pointerAction != null)
            {
                pointerPosition = pointerAction.ReadValue<Vector2>();
                UpdateTargetPosition();
            }

            // Check for throw input
            if (throwAction != null && !isThrowing)
            {
                if (throwAction.WasPressedThisFrame())
                {
                    TryThrowKingRat();
                }
            }

            // Update arc visualization
            UpdateArcVisualization();

            // Update throw animation
            if (isThrowing)
            {
                UpdateThrowAnimation();
            }
        }

        /// <summary>
        /// Updates the target position based on pointer input.
        /// </summary>
        private void UpdateTargetPosition()
        {
            if (mainCamera == null)
            {
                return;
            }

            // Raycast from camera to ground plane
            Ray ray = mainCamera.ScreenPointToRay(pointerPosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            
            if (groundPlane.Raycast(ray, out float distance))
            {
                targetPosition = ray.GetPoint(distance);
            }
            else
            {
                targetPosition = null;
            }
        }

        /// <summary>
        /// Gets the current launch speed based on the number of rats in the inventory.
        /// </summary>
        private float GetLaunchSpeed()
        {
            int ratCount = (ratInventory != null) ? ratInventory.Count : 0;
            float speed = baseLaunchSpeed + (ratCount * speedPerRat);
            return Mathf.Min(speed, maxLaunchSpeed);
        }

        /// <summary>
        /// Updates the arc visualization.
        /// </summary>
        private void UpdateArcVisualization()
        {
            if (arcLineRenderer == null)
            {
                return;
            }

            // Only show arc if we have a target and King Rat is being grabbed
            if (targetPosition.HasValue && kingRatGrabber != null && kingRatGrabber.IsGrabbing && !isThrowing)
            {
                Vector3 start = GetLaunchPosition();
                Vector3 end = targetPosition.Value;
                
                // Calculate arc points
                CalculateArcPoints(start, end, arcPoints);
                
                // Update LineRenderer positions
                for (int i = 0; i < arcPoints.Length; i++)
                {
                    arcLineRenderer.SetPosition(i, arcPoints[i]);
                }
                
                arcLineRenderer.enabled = true;
            }
            else
            {
                arcLineRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Gets the launch position for the King Rat.
        /// </summary>
        private Vector3 GetLaunchPosition()
        {
            return transform.position + Vector3.up * launchHeightOffset;
        }

        /// <summary>
        /// Calculates arc points for visualization.
        /// Uses a quadratic Bezier curve to ensure the arc always completes from start to end.
        /// </summary>
        private void CalculateArcPoints(Vector3 start, Vector3 end, Vector3[] points)
        {
            // Calculate horizontal distance
            float distance = Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(end.x, 0, end.z));
            
            // Calculate control point for the arc (midpoint horizontally, elevated vertically)
            Vector3 midPoint = Vector3.Lerp(start, end, 0.5f);
            float arcHeight = Mathf.Max(distance * 0.3f, GetLaunchSpeed() * 0.15f);
            Vector3 controlPoint = new Vector3(midPoint.x, Mathf.Max(start.y, end.y) + arcHeight, midPoint.z);
            
            // Generate arc points using quadratic Bezier curve
            for (int i = 0; i < points.Length; i++)
            {
                float t = (float)i / (points.Length - 1);
                
                // Quadratic Bezier formula: (1-t)² * P0 + 2(1-t)t * P1 + t² * P2
                float oneMinusT = 1f - t;
                Vector3 point = (oneMinusT * oneMinusT * start) + 
                                (2f * oneMinusT * t * controlPoint) + 
                                (t * t * end);
                
                points[i] = point;
            }
        }

        /// <summary>
        /// Attempts to throw the King Rat to the target position.
        /// </summary>
        private void TryThrowKingRat()
        {
            if (kingRatGrabber == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatThrower] KingRatGrabber is not assigned!");
                }
                return;
            }

            if (!kingRatGrabber.IsGrabbing)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatThrower] King Rat is not being grabbed!");
                }
                return;
            }

            if (!targetPosition.HasValue)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[KingRatThrower] No target position set!");
                }
                return;
            }

            // Start throw animation
            StartThrowAnimation(targetPosition.Value);
        }

        /// <summary>
        /// Starts the throw animation for the King Rat.
        /// </summary>
        private void StartThrowAnimation(Vector3 destination)
        {
            isThrowing = true;
            throwStartPosition = GetLaunchPosition();
            throwEndPosition = destination;
            throwTimer = 0f;

            // Release King Rat from grabber before throwing
            if (kingRatGrabber != null)
            {
                kingRatGrabber.ReleaseKingRat();
            }

            // Move King Rat to launch position
            if (kingRatGrabber != null && kingRatGrabber.KingRat != null)
            {
                kingRatGrabber.KingRat.transform.position = throwStartPosition;
                kingRatOriginalPosition = throwStartPosition;
                kingRatOriginalRotation = kingRatGrabber.KingRat.transform.rotation;
            }

            // Fire event
            OnKingRatThrown?.Invoke(destination);

            if (debugMode)
            {
                int ratCount = (ratInventory != null) ? ratInventory.Count : 0;
                float launchSpeed = GetLaunchSpeed();
                Debug.Log($"[KingRatThrower] Throwing King Rat to {destination} (Rats: {ratCount}, Speed: {launchSpeed:F2})");
            }
        }

        /// <summary>
        /// Updates the throw animation.
        /// </summary>
        private void UpdateThrowAnimation()
        {
            if (kingRatGrabber == null || kingRatGrabber.KingRat == null)
            {
                isThrowing = false;
                return;
            }

            throwTimer += Time.deltaTime;
            float t = Mathf.Clamp01(throwTimer / throwDuration);

            // Calculate arc position
            Vector3 position = CalculateArcPosition(throwStartPosition, throwEndPosition, t);
            kingRatGrabber.KingRat.transform.position = position;

            // Check if throw is complete
            if (t >= 1f)
            {
                FinishThrowAnimation();
            }
        }

        /// <summary>
        /// Calculates the position along the arc at time t (0-1).
        /// </summary>
        private Vector3 CalculateArcPosition(Vector3 start, Vector3 end, float t)
        {
            // Linear interpolation for X and Z
            Vector3 flatPosition = Vector3.Lerp(
                new Vector3(start.x, 0, start.z),
                new Vector3(end.x, 0, end.z),
                t
            );

            // Parabolic arc for Y
            float height = Mathf.Lerp(start.y, end.y, t);
            float arcHeight = Mathf.Sin(t * Mathf.PI) * GetLaunchSpeed() * 0.2f;

            return new Vector3(flatPosition.x, height + arcHeight, flatPosition.z);
        }

        /// <summary>
        /// Finishes the throw animation.
        /// </summary>
        private void FinishThrowAnimation()
        {
            if (kingRatGrabber != null && kingRatGrabber.KingRat != null)
            {
                // Fire event
                OnKingRatLanded?.Invoke(throwEndPosition);

                if (debugMode)
                {
                    Debug.Log($"[KingRatThrower] King Rat landed at {throwEndPosition}");
                }
            }

            isThrowing = false;
        }

        /// <summary>
        /// Gets the current target position.
        /// </summary>
        public Vector3? GetTargetPosition()
        {
            return targetPosition;
        }

        /// <summary>
        /// Gets whether the King Rat is currently being thrown.
        /// </summary>
        public bool IsThrowing()
        {
            return isThrowing;
        }

        /// <summary>
        /// Gets the current launch speed based on rat count.
        /// </summary>
        public float GetCurrentLaunchSpeed()
        {
            return GetLaunchSpeed();
        }

        /// <summary>
        /// Sets the base launch speed.
        /// </summary>
        public void SetBaseLaunchSpeed(float speed)
        {
            baseLaunchSpeed = Mathf.Max(1f, speed);
        }

        /// <summary>
        /// Sets the additional launch speed per rat.
        /// </summary>
        public void SetSpeedPerRat(float speed)
        {
            speedPerRat = Mathf.Max(0f, speed);
        }

        /// <summary>
        /// Sets the maximum launch speed.
        /// </summary>
        public void SetMaxLaunchSpeed(float speed)
        {
            maxLaunchSpeed = Mathf.Max(1f, speed);
        }

        /// <summary>
        /// Sets the launch angle.
        /// </summary>
        public void SetLaunchAngle(float angle)
        {
            launchAngle = Mathf.Clamp(angle, 10f, 80f);
        }

        /// <summary>
        /// Sets the throw duration.
        /// </summary>
        public void SetThrowDuration(float duration)
        {
            throwDuration = Mathf.Max(0.1f, duration);
        }

        private void OnDrawGizmos()
        {
            if (!visualizeTarget || !targetPosition.HasValue)
            {
                return;
            }

            // Draw target position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition.Value, 0.3f);
            Gizmos.DrawLine(transform.position + Vector3.up * launchHeightOffset, targetPosition.Value);

            // Draw arc if in play mode
            if (Application.isPlaying && arcPoints != null && arcPoints.Length > 0)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < arcPoints.Length - 1; i++)
                {
                    Gizmos.DrawLine(arcPoints[i], arcPoints[i + 1]);
                }
            }

            // Draw King Rat indicator
            if (kingRatGrabber != null && kingRatGrabber.KingRat != null)
            {
                Gizmos.color = isThrowing ? Color.magenta : (kingRatGrabber.IsGrabbing ? Color.green : Color.gray);
                Gizmos.DrawWireSphere(kingRatGrabber.KingRat.transform.position, 0.5f);
            }
        }

        private void Reset()
        {
            baseLaunchSpeed = 10f;
            speedPerRat = 2f;
            maxLaunchSpeed = 30f;
            launchAngle = 45f;
            throwDuration = 1f;
            launchHeightOffset = 1f;
            arcSegments = 30;
            arcColor = Color.yellow;
        }
    }
}
