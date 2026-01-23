using UnityEngine;

namespace FindersCheesers.SentRat
{
    /// <summary>
    /// SentRat character that can be launched, interact with objectives, and be recalled.
    /// </summary>
    [AddComponentMenu("Finder's Cheesers/SentRat/SentRat")]
    public class SentRat : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Rigidbody rb;
        
        [Header("Settings")]
        [SerializeField] private float recallSpeed = 20f;
        [SerializeField] private float stopDistance = 0.5f;
        
        // State
        private Transform returnTarget;
        private Vector3 initialVelocity;
        private bool isRecalled;
        private bool isMovingToTarget;
        private Items.GrabItem attachedItem;
        
        // References
        private SentRatLauncher launcher;
        private Player.KingRatController kingRatController;
        private Player.RatInventory ratInventory;
        
        /// <summary>
        /// Initializes SentRat with launcher and target references.
        /// </summary>
        public void Initialize(SentRatLauncher launcher, Transform returnTarget, Vector3 velocity)
        {
            this.launcher = launcher;
            this.returnTarget = returnTarget;
            this.initialVelocity = velocity;
            
            // Get references
            kingRatController = returnTarget.GetComponent<Player.KingRatController>();
            ratInventory = returnTarget.GetComponent<Player.RatInventory>();
            
            // Apply initial velocity
            if (rb != null)
            {
                rb.linearVelocity = initialVelocity;
            }
            
            // Stop after a short time
            Invoke(nameof(StopMovement), 0.5f);
        }
        
        private void StopMovement()
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        
        private void Update()
        {
            if (isRecalled && isMovingToTarget)
            {
                MoveToReturnTarget();
            }
        }
        
        private void MoveToReturnTarget()
        {
            if (returnTarget == null) return;
            
            Vector3 direction = (returnTarget.position - transform.position).normalized;
            transform.position += direction * recallSpeed * Time.deltaTime;
            
            // Check if reached target
            if (Vector3.Distance(transform.position, returnTarget.position) < stopDistance)
            {
                OnReachedReturnTarget();
            }
        }
        
        private void OnReachedReturnTarget()
        {
            // Handle attached item
            if (attachedItem != null && ratInventory != null)
            {
                ratInventory.AddItem(attachedItem.ItemName, attachedItem.Icon);
                
                // Check if it's a cheese item for extra rat
                Items.CheeseGrabItem cheeseItem = attachedItem.GetComponent<Items.CheeseGrabItem>();
                if (cheeseItem != null && kingRatController != null)
                {
                    kingRatController.IncreaseRats();
                }
            }
            
            // Increase rat count
            if (kingRatController != null)
            {
                kingRatController.IncreaseRats();
            }
            
            // Notify launcher
            if (launcher != null)
            {
                launcher.OnSentRatRecalled(this);
            }
            
            // Destroy SentRat
            Destroy(gameObject);
        }
        
        /// <summary>
        /// Recalls SentRat back to KingRat.
        /// </summary>
        public void Recall()
        {
            isRecalled = true;
            isMovingToTarget = true;
            
            // Disable physics
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
        
        /// <summary>
        /// Attaches a grab item to this SentRat.
        /// </summary>
        public void AttachItem(Items.GrabItem item)
        {
            attachedItem = item;
            item.AttachToSentRat(transform);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Check for objective zones
            Objectives.ObjectiveZone objectiveZone = other.GetComponent<Objectives.ObjectiveZone>();
            if (objectiveZone != null)
            {
                objectiveZone.OnSentRatEnter(this);
            }
        }
    }
}
