using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FindersCheesers
{
    /// <summary>
    /// A component that detects nearby IRatInteractable objects and allows the player to spend/deposit rats to them.
    /// Uses an overlap box for detection and InputSystem for interaction input.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Rat Interacter")]
    public class RatInteracter : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("The size of the overlap box for detecting interactables")]
        [SerializeField]
        private Vector3 detectionSize = new Vector3(2f, 2f, 2f);

        [Tooltip("The offset of the detection box from this object's center")]
        [SerializeField]
        private Vector3 detectionOffset = Vector3.forward * 1.5f;

        [Tooltip("The layer mask for filtering interactable objects")]
        [SerializeField]
        private LayerMask interactableLayerMask = -1;

        [Header("Input Settings")]
        [Tooltip("The input action reference for the interact button")]
        [SerializeField]
        private InputActionReference interactActionReference;

        [Header("Rat Inventory Settings")]
        [Tooltip("The RatInventory component to spend rats from. If null, will search for one on this GameObject or in parents.")]
        [SerializeField]
        private RatInventory ratInventory;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        [Tooltip("Visualize the detection box in Scene view")]
        [SerializeField]
        private bool visualizeDetection = true;

        // Input action for interacting
        private InputAction interactAction;

        // Cached nearby interactables
        private List<IRatInteractable> nearbyInteractables = new List<IRatInteractable>();

        // The currently targeted interactable (closest valid one)
        private IRatInteractable currentTarget;

        /// <summary>
        /// Event fired when a new interactable is targeted.
        /// </summary>
        public event System.Action<IRatInteractable> OnTargetChanged;

        /// <summary>
        /// Event fired when an interaction is attempted.
        /// </summary>
        public event System.Action<IRatInteractable, bool, InteractionType> OnInteractionAttempted;

        /// <summary>
        /// Gets the currently targeted interactable.
        /// </summary>
        public IRatInteractable CurrentTarget => currentTarget;

        /// <summary>
        /// Gets the list of nearby interactables.
        /// </summary>
        public IReadOnlyList<IRatInteractable> NearbyInteractables => nearbyInteractables.AsReadOnly();

        /// <summary>
        /// Gets whether there is a valid target available.
        /// </summary>
        public bool HasValidTarget => currentTarget != null;

        /// <summary>
        /// Gets the type of interaction available with the current target.
        /// </summary>
        public InteractionType CurrentInteractionType => GetInteractionType(currentTarget);

        /// <summary>
        /// Types of interactions available.
        /// </summary>
        public enum InteractionType
        {
            None,
            Deposit,
            Withdraw
        }

        private void Start()
        {
            // Get RatInventory reference
            if (ratInventory == null)
            {
                ratInventory = GetComponent<RatInventory>();
                if (ratInventory == null)
                {
                    ratInventory = GetComponentInParent<RatInventory>();
                }
            }

            if (ratInventory == null)
            {
                Debug.LogWarning("[RatInteracter] No RatInventory component found. Interactions will not work.");
            }

            // Get the interact action from PlayerInputSingleton
            if (interactActionReference != null)
            {
                interactAction = PlayerInputSingleton.Instance?.GetAction(interactActionReference);

                if (interactAction == null)
                {
                    Debug.LogWarning("[RatInteracter] Failed to get interact action from PlayerInputSingleton.");
                }
            }
            else
            {
                Debug.LogWarning("[RatInteracter] Interact action reference is not assigned.");
            }
        }

        private void Update()
        {
            // Update nearby interactables
            UpdateNearbyInteractables();

            // Update current target
            UpdateCurrentTarget();

            // Check for interaction input
            CheckInteractionInput();
        }

        /// <summary>
        /// Updates the list of nearby interactables using overlap box detection.
        /// </summary>
        private void UpdateNearbyInteractables()
        {
            nearbyInteractables.Clear();

            // Calculate detection box position
            Vector3 boxCenter = transform.position + transform.TransformVector(detectionOffset);
            Vector3 boxHalfSize = detectionSize * 0.5f;

            // Find all colliders in the detection box
            Collider[] colliders = new Collider[32];
            int hitCount = Physics.OverlapBoxNonAlloc(boxCenter, boxHalfSize, colliders, transform.rotation, interactableLayerMask, QueryTriggerInteraction.Collide);

            // Check each collider for IRatInteractable
            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                {
                    continue;
                }

                // Try to get IRatInteractable from the collider or its parent
                IRatInteractable interactable = collider.GetComponent<IRatInteractable>();
                if (interactable == null)
                {
                    interactable = collider.GetComponentInParent<IRatInteractable>();
                }

                // Add if found and not already in the list
                if (interactable != null && !nearbyInteractables.Contains(interactable))
                {
                    nearbyInteractables.Add(interactable);
                }
            }

            if (debugMode && nearbyInteractables.Count > 0)
            {
                Debug.Log($"[RatInteracter] Found {nearbyInteractables.Count} nearby interactable(s).");
            }
        }

        /// <summary>
        /// Updates the current target to the closest valid interactable.
        /// </summary>
        private void UpdateCurrentTarget()
        {
            IRatInteractable newTarget = null;
            float closestDistance = float.MaxValue;

            // Find the closest interactable that can be interacted with
            foreach (IRatInteractable interactable in nearbyInteractables)
            {
                if (interactable == null)
                {
                    continue;
                }

                // Check if the interactable can be interacted with (deposit or withdraw)
                if (!CanInteractWith(interactable))
                {
                    continue;
                }

                // Calculate distance
                float distance = Vector3.Distance(transform.position, interactable.Transform.position);

                // Update if this is the closest
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    newTarget = interactable;
                }
            }

            // Fire event if target changed
            if (newTarget != currentTarget)
            {
                currentTarget = newTarget;
                OnTargetChanged?.Invoke(currentTarget);

                if (debugMode)
                {
                    Debug.Log($"[RatInteracter] Target changed to: {(currentTarget != null ? currentTarget.InteractionDescription : "None")}");
                }
            }
        }

        /// <summary>
        /// Checks if an interactable can be interacted with (deposit or withdraw).
        /// </summary>
        /// <param name="interactable">The interactable to check.</param>
        /// <returns>True if the interactable can be interacted with, false otherwise.</returns>
        private bool CanInteractWith(IRatInteractable interactable)
        {
            if (interactable == null)
            {
                return false;
            }

            // Check if we can deposit rats
            if (interactable.CanDepositRats())
            {
                // Check if we have enough rats
                if (ratInventory != null && ratInventory.Count >= interactable.RatCost)
                {
                    return true;
                }
            }

            // Check if we can withdraw rats
            if (interactable.CanWithdrawRats())
            {
                // Check if inventory has space
                if (ratInventory != null && ratInventory.Count + interactable.DepositedRatsCount <= ratInventory.MaxCapacity)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the type of interaction available with an interactable.
        /// </summary>
        /// <param name="interactable">The interactable to check.</param>
        /// <returns>The interaction type.</returns>
        private InteractionType GetInteractionType(IRatInteractable interactable)
        {
            if (interactable == null)
            {
                return InteractionType.None;
            }

            // Prioritize withdrawal if available
            if (interactable.CanWithdrawRats())
            {
                if (ratInventory != null && ratInventory.Count + interactable.DepositedRatsCount <= ratInventory.MaxCapacity)
                {
                    return InteractionType.Withdraw;
                }
            }

            // Check for deposit
            if (interactable.CanDepositRats())
            {
                if (ratInventory != null && ratInventory.Count >= interactable.RatCost)
                {
                    return InteractionType.Deposit;
                }
            }

            return InteractionType.None;
        }

        /// <summary>
        /// Checks for interaction input and attempts to interact if a target is available.
        /// </summary>
        private void CheckInteractionInput()
        {
            if (interactAction == null)
            {
                return;
            }

            // Check if the interact button was pressed this frame
            if (interactAction.WasPressedThisFrame())
            {
                AttemptInteraction();
            }
        }

        /// <summary>
        /// Attempts to interact with the current target.
        /// </summary>
        public bool AttemptInteraction()
        {
            if (currentTarget == null)
            {
                if (debugMode)
                {
                    Debug.Log("[RatInteracter] No valid target to interact with.");
                }
                OnInteractionAttempted?.Invoke(null, false, InteractionType.None);
                return false;
            }

            if (ratInventory == null)
            {
                Debug.LogWarning("[RatInteracter] No RatInventory component found. Cannot interact.");
                OnInteractionAttempted?.Invoke(currentTarget, false, InteractionType.None);
                return false;
            }

            // Determine the interaction type
            InteractionType interactionType = GetInteractionType(currentTarget);

            if (interactionType == InteractionType.None)
            {
                if (debugMode)
                {
                    Debug.Log("[RatInteracter] No valid interaction available with target.");
                }
                OnInteractionAttempted?.Invoke(currentTarget, false, InteractionType.None);
                return false;
            }

            // Perform the appropriate interaction
            bool success = false;
            if (interactionType == InteractionType.Withdraw)
            {
                success = AttemptWithdraw(currentTarget);
            }
            else if (interactionType == InteractionType.Deposit)
            {
                success = AttemptDeposit(currentTarget);
            }

            OnInteractionAttempted?.Invoke(currentTarget, success, interactionType);

            if (debugMode)
            {
                if (success)
                {
                    Debug.Log($"[RatInteracter] Successfully {interactionType.ToString().ToLower()}ed with: {currentTarget.InteractionDescription}");
                }
                else
                {
                    Debug.LogWarning($"[RatInteracter] Failed to {interactionType.ToString().ToLower()} with: {currentTarget.InteractionDescription}");
                }
            }

            return success;
        }

        /// <summary>
        /// Attempts to deposit rats to an interactable.
        /// </summary>
        /// <param name="interactable">The interactable to deposit to.</param>
        /// <returns>True if the deposit was successful, false otherwise.</returns>
        private bool AttemptDeposit(IRatInteractable interactable)
        {
            if (interactable == null)
            {
                Debug.LogWarning("[RatInteracter] Cannot deposit to null interactable.");
                return false;
            }

            if (ratInventory == null)
            {
                Debug.LogWarning("[RatInteracter] No RatInventory component found. Cannot deposit.");
                return false;
            }

            int ratCost = interactable.RatCost;
            if (ratInventory.Count < ratCost)
            {
                if (debugMode)
                {
                    Debug.Log($"[RatInteracter] Not enough rats to deposit. Required: {ratCost}, Available: {ratInventory.Count}");
                }
                return false;
            }

            // Collect the rats to deposit
            List<Rat> ratsToDeposit = new List<Rat>();
            for (int i = 0; i < ratCost; i++)
            {
                Rat rat = ratInventory.GetRat(i);
                if (rat != null)
                {
                    ratsToDeposit.Add(rat);
                }
            }

            if (ratsToDeposit.Count != ratCost)
            {
                Debug.LogWarning("[RatInteracter] Failed to collect required rats from inventory.");
                return false;
            }

            // Remove rats from inventory
            foreach (Rat rat in ratsToDeposit)
            {
                ratInventory.RemoveRat(rat);
            }

            // Deposit rats to the interactable
            bool success = interactable.DepositRats(ratsToDeposit);

            if (!success)
            {
                // Rollback - return rats to inventory
                foreach (Rat rat in ratsToDeposit)
                {
                    ratInventory.AddRat(rat);
                }
            }

            return success;
        }

        /// <summary>
        /// Attempts to withdraw rats from an interactable.
        /// </summary>
        /// <param name="interactable">The interactable to withdraw from.</param>
        /// <returns>True if the withdrawal was successful, false otherwise.</returns>
        private bool AttemptWithdraw(IRatInteractable interactable)
        {
            if (interactable == null)
            {
                Debug.LogWarning("[RatInteracter] Cannot withdraw from null interactable.");
                return false;
            }

            if (ratInventory == null)
            {
                Debug.LogWarning("[RatInteracter] No RatInventory component found. Cannot withdraw.");
                return false;
            }

            // Withdraw rats from the interactable
            List<Rat> withdrawnRats = interactable.WithdrawRats(ratInventory);

            return withdrawnRats != null;
        }

        /// <summary>
        /// Attempts to interact with a specific interactable.
        /// </summary>
        /// <param name="interactable">The interactable to interact with.</param>
        /// <returns>True if the interaction was successful, false otherwise.</returns>
        public bool AttemptInteractionWith(IRatInteractable interactable)
        {
            if (interactable == null)
            {
                Debug.LogWarning("[RatInteracter] Cannot interact with null interactable.");
                return false;
            }

            if (ratInventory == null)
            {
                Debug.LogWarning("[RatInteracter] No RatInventory component found. Cannot interact.");
                return false;
            }

            // Determine the interaction type
            InteractionType interactionType = GetInteractionType(interactable);

            if (interactionType == InteractionType.None)
            {
                if (debugMode)
                {
                    Debug.Log("[RatInteracter] No valid interaction available with target.");
                }
                return false;
            }

            // Perform the appropriate interaction
            bool success = false;
            if (interactionType == InteractionType.Withdraw)
            {
                success = AttemptWithdraw(interactable);
            }
            else if (interactionType == InteractionType.Deposit)
            {
                success = AttemptDeposit(interactable);
            }

            if (debugMode)
            {
                if (success)
                {
                    Debug.Log($"[RatInteracter] Successfully {interactionType.ToString().ToLower()}ed with: {interactable.InteractionDescription}");
                }
                else
                {
                    Debug.LogWarning($"[RatInteracter] Failed to {interactionType.ToString().ToLower()} with: {interactable.InteractionDescription}");
                }
            }

            return success;
        }

        /// <summary>
        /// Sets the RatInventory component to use for spending rats.
        /// </summary>
        /// <param name="inventory">The RatInventory component.</param>
        public void SetRatInventory(RatInventory inventory)
        {
            ratInventory = inventory;
            if (debugMode)
            {
                Debug.Log($"[RatInteracter] RatInventory set to: {(inventory != null ? inventory.name : "null")}");
            }
        }

        /// <summary>
        /// Sets the detection box size.
        /// </summary>
        /// <param name="size">The new size of the detection box.</param>
        public void SetDetectionSize(Vector3 size)
        {
            detectionSize = size;
        }

        /// <summary>
        /// Sets the detection box offset.
        /// </summary>
        /// <param name="offset">The new offset of the detection box.</param>
        public void SetDetectionOffset(Vector3 offset)
        {
            detectionOffset = offset;
        }

        private void OnDrawGizmos()
        {
            if (!visualizeDetection)
            {
                return;
            }

            // Draw detection box
            Vector3 boxCenter = transform.position + transform.TransformVector(detectionOffset);
            Vector3 boxHalfSize = detectionSize * 0.5f;

            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, detectionSize);
            Gizmos.matrix = Matrix4x4.identity;

            // Draw line to current target
            if (currentTarget != null)
            {
                InteractionType interactionType = GetInteractionType(currentTarget);
                Gizmos.color = interactionType == InteractionType.Withdraw ? Color.blue : Color.green;
                Gizmos.DrawLine(transform.position, currentTarget.Transform.position);

                // Draw indicator at target
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(currentTarget.Transform.position, 0.5f);
            }

            // Draw indicators for all nearby interactables
            Gizmos.color = Color.gray;
            foreach (IRatInteractable interactable in nearbyInteractables)
            {
                if (interactable != null && interactable != currentTarget)
                {
                    Gizmos.DrawWireSphere(interactable.Transform.position, 0.3f);
                }
            }
        }
    }
}
