using System.Collections.Generic;
using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// A component that implements IRatInteractable and provides events for other components to react to.
    /// This can be attached to any GameObject that should accept rats (e.g., altars, machines, deposit points).
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Rat Interactable")]
    public class RatInteractable : MonoBehaviour, IRatInteractable
    {
        [Header("Interaction Settings")]
        [Tooltip("Description of what happens when rats are spent/deposited")]
        [SerializeField]
        private string interactionDescription = "Deposit rats";

        [Tooltip("Maximum number of rats this interactable can accept (-1 for unlimited)")]
        [SerializeField]
        private int maxRatsToAccept = -1;

        [Tooltip("Whether this interactable can currently accept rats")]
        [SerializeField]
        private bool canAcceptRats = true;

        [Tooltip("Whether rats can be withdrawn from this interactable")]
        [SerializeField]
        private bool allowWithdrawal = true;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        // List of rats that have been deposited to this interactable
        private List<Rat> depositedRats = new List<Rat>();

        /// <summary>
        /// Event fired when rats are about to be spent/deposited (before interaction).
        /// </summary>
        public event System.Action<List<Rat>> OnRatsSpending;

        /// <summary>
        /// Event fired when rats have been successfully spent/deposited.
        /// </summary>
        public event System.Action<List<Rat>> OnRatsDeposited;

        /// <summary>
        /// Event fired when rats interaction fails.
        /// </summary>
        public event System.Action<List<Rat>, string> OnRatsInteractionFailed;

        /// <summary>
        /// Event fired when this interactable becomes unable to accept more rats.
        /// </summary>
        public event System.Action OnInteractableFull;

        /// <summary>
        /// Event fired when rats are withdrawn from this interactable.
        /// </summary>
        public event System.Action<List<Rat>> OnRatsWithdrawn;

        /// <summary>
        /// Gets number of rats currently deposited in this interactable.
        /// </summary>
        public int DepositedRatsCount => depositedRats.Count;

        /// <summary>
        /// Gets whether rats can be withdrawn from this interactable.
        /// </summary>
        public bool AllowWithdrawal => allowWithdrawal;

        /// <summary>
        /// Gets or sets the description of what happens when rats are spent/deposited.
        /// </summary>
        public string InteractionDescription
        {
            get => interactionDescription;
            set => interactionDescription = value;
        }

        /// <summary>
        /// Gets the transform of this interactable.
        /// </summary>
        public Transform Transform => transform;

        /// <summary>
        /// Gets whether this interactable can currently accept rats.
        /// </summary>
        public bool CanAcceptRats => canAcceptRats && (maxRatsToAccept < 0 || depositedRats.Count < maxRatsToAccept);

        /// <summary>
        /// Gets the maximum number of rats this interactable can accept.
        /// </summary>
        public int MaxRatsToAccept => maxRatsToAccept;

        /// <summary>
        /// Gets whether this interactable is full (has reached maximum rats).
        /// </summary>
        public bool IsFull => maxRatsToAccept >= 0 && depositedRats.Count >= maxRatsToAccept;

        /// <summary>
        /// Gets whether this interactable has deposited rats that can be withdrawn.
        /// </summary>
        public bool HasDepositedRats => depositedRats.Count > 0;

        /// <summary>
        /// Checks if this interactable can accept rats (deposit).
        /// </summary>
        /// <returns>True if rats can be deposited, false otherwise.</returns>
        public bool CanDepositRats()
        {
            if (!CanAcceptRats)
            {
                return false;
            }

            // Check if there's room for at least one more rat
            if (maxRatsToAccept >= 0 && depositedRats.Count >= maxRatsToAccept)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if rats can be withdrawn from this interactable.
        /// </summary>
        /// <returns>True if rats can be withdrawn, false otherwise.</returns>
        public bool CanWithdrawRats()
        {
            return allowWithdrawal && depositedRats.Count > 0;
        }

        /// <summary>
        /// Deposits rats to this interactable.
        /// </summary>
        /// <param name="rats">The list of rats to deposit.</param>
        /// <returns>True if all rats were successfully deposited, false otherwise.</returns>
        public bool DepositRats(List<Rat> rats)
        {
            if (rats == null || rats.Count == 0)
            {
                string reason = "Cannot deposit null or empty rat list.";
                OnRatsInteractionFailed?.Invoke(null, reason);
                Debug.LogWarning($"[RatInteractable] {reason}");
                return false;
            }

            if (!CanDepositRats())
            {
                string reason = IsFull ? "Interactable is full." : "Interactable cannot accept rats.";
                OnRatsInteractionFailed?.Invoke(rats, reason);
                if (debugMode)
                {
                    Debug.Log($"[RatInteractable] {reason}");
                }
                return false;
            }

            // Validate all rats
            foreach (Rat rat in rats)
            {
                if (rat == null || !ValidateRat(rat))
                {
                    string reason = "One or more rats are invalid.";
                    OnRatsInteractionFailed?.Invoke(rats, reason);
                    Debug.LogWarning($"[RatInteractable] {reason}");
                    return false;
                }
            }

            // Fire the spending event before interaction
            OnRatsSpending?.Invoke(rats);

            // Process the rat interaction
            bool success = ProcessRatInteraction(rats);

            if (success)
            {
                // Mark all rats as deposited
                foreach (Rat rat in rats)
                {
                    rat.IsDeposited = true;
                }

                // Add all rats to the deposited list
                depositedRats.AddRange(rats);

                // Fire the deposited event after successful interaction
                OnRatsDeposited?.Invoke(rats);

                // Check if we've reached capacity
                if (IsFull)
                {
                    OnInteractableFull?.Invoke();
                }

                if (debugMode)
                {
                    Debug.Log($"[RatInteractable] Successfully deposited {rats.Count} rat(s). Total deposited: {depositedRats.Count}");
                }
            }
            else
            {
                string reason = "Failed to process rat interaction.";
                OnRatsInteractionFailed?.Invoke(rats, reason);
                if (debugMode)
                {
                    Debug.LogWarning($"[RatInteractable] {reason}");
                }
            }

            return success;
        }

        /// <summary>
        /// Withdraws rats from this interactable.
        /// </summary>
        /// <param name="ratInventory">The RatInventory to return rats to.</param>
        /// <returns>The list of withdrawn rats, or null if withdrawal failed.</returns>
        public List<Rat> WithdrawRats(RatInventory ratInventory)
        {
            if (!CanWithdrawRats())
            {
                if (debugMode)
                {
                    Debug.Log("[RatInteractable] Cannot withdraw rats.");
                }
                return null;
            }

            if (ratInventory == null)
            {
                Debug.LogWarning("[RatInteractable] Cannot withdraw rats to null RatInventory.");
                return null;
            }

            // Check if inventory has space for all deposited rats
            int availableSpace = ratInventory.MaxCapacity - ratInventory.Count;
            if (availableSpace < depositedRats.Count)
            {
                if (debugMode)
                {
                    Debug.Log($"[RatInteractable] Not enough space in inventory. Available: {availableSpace}, Required: {depositedRats.Count}");
                }
                return null;
            }

            // Create a copy of the deposited rats list
            List<Rat> ratsToWithdraw = new List<Rat>(depositedRats);

            // Try to add all rats back to the inventory
            bool allAdded = true;
            foreach (Rat rat in ratsToWithdraw)
            {
                if (!ratInventory.AddRat(rat))
                {
                    allAdded = false;
                    break;
                }
            }

            if (!allAdded)
            {
                // Rollback - remove any rats that were added
                foreach (Rat rat in ratsToWithdraw)
                {
                    ratInventory.RemoveRat(rat);
                }

                if (debugMode)
                {
                    Debug.LogWarning("[RatInteractable] Failed to return rats to inventory.");
                }
                return null;
            }

            // Mark all rats as no longer deposited
            foreach (Rat rat in ratsToWithdraw)
            {
                rat.IsDeposited = false;
            }

            // Clear the deposited rats list
            depositedRats.Clear();

            // Fire the withdrawn event
            OnRatsWithdrawn?.Invoke(ratsToWithdraw);

            if (debugMode)
            {
                Debug.Log($"[RatInteractable] Successfully withdrew {ratsToWithdraw.Count} rat(s).");
            }

            return ratsToWithdraw;
        }

        /// <summary>
        /// Validates if a rat can be accepted by this interactable.
        /// Override this method in derived classes for custom validation logic.
        /// </summary>
        /// <param name="rat">The rat to validate.</param>
        /// <returns>True if the rat is valid, false otherwise.</returns>
        protected virtual bool ValidateRat(Rat rat)
        {
            // Default implementation accepts all rats
            return true;
        }

        /// <summary>
        /// Processes the rat interaction.
        /// Override this method in derived classes to define what happens when rats are deposited.
        /// </summary>
        /// <param name="rats">The rats being deposited.</param>
        /// <returns>True if the interaction was successful, false otherwise.</returns>
        protected virtual bool ProcessRatInteraction(List<Rat> rats)
        {
            // Default implementation just accepts rats
            // Derived classes should override this to do something with rats
            return true;
        }

        /// <summary>
        /// Sets whether this interactable can accept rats.
        /// </summary>
        /// <param name="canAccept">Whether rats can be accepted.</param>
        public void SetCanAcceptRats(bool canAccept)
        {
            canAcceptRats = canAccept;
            if (debugMode)
            {
                Debug.Log($"[RatInteractable] Can accept rats set to: {canAccept}");
            }
        }

        /// <summary>
        /// Sets the maximum number of rats this interactable can accept.
        /// </summary>
        /// <param name="maxRats">The maximum number of rats (-1 for unlimited).</param>
        public void SetMaxRatsToAccept(int maxRats)
        {
            maxRatsToAccept = maxRats;
            if (debugMode)
            {
                Debug.Log($"[RatInteractable] Max rats to accept set to: {maxRats}");
            }
        }

        /// <summary>
        /// Sets whether rats can be withdrawn from this interactable.
        /// </summary>
        /// <param name="allow">Whether rats can be withdrawn.</param>
        public void SetAllowWithdrawal(bool allow)
        {
            allowWithdrawal = allow;
            if (debugMode)
            {
                Debug.Log($"[RatInteractable] Allow withdrawal set to: {allow}");
            }
        }

        /// <summary>
        /// Gets the interaction description for UI display.
        /// </summary>
        /// <returns>The interaction description.</returns>
        public string GetInteractionDescription()
        {
            if (HasDepositedRats && AllowWithdrawal)
            {
                return $"{interactionDescription} (Withdraw {depositedRats.Count} rats)";
            }
            if (IsFull)
            {
                return $"{interactionDescription} (Full)";
            }
            return $"{interactionDescription} ({depositedRats.Count}/{(maxRatsToAccept < 0 ? "∞" : maxRatsToAccept.ToString())})";
        }

        private void OnDrawGizmos()
        {
            // Draw a visual indicator for this interactable
            Gizmos.color = CanAcceptRats ? Color.cyan : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Draw a label with the deposited count
            if (Application.isPlaying)
            {
                #if UNITY_EDITOR
                string status = HasDepositedRats && AllowWithdrawal ? "Withdraw" : (IsFull ? "Full" : "Deposit");
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
                    $"{interactionDescription}\n{status}: {depositedRats.Count}/{(maxRatsToAccept < 0 ? "∞" : maxRatsToAccept.ToString())}");
                #endif
            }
        }

        private void OnDestroy()
        {
            // Clean up deposited rats when destroyed
            foreach (Rat rat in depositedRats)
            {
                if (rat != null)
                {
                    rat.IsSupportingKing = false;
                    rat.IsDeposited = false;
                    rat.CurrentRatInventory = null;
                }
            }
            depositedRats.Clear();
        }
    }
}
