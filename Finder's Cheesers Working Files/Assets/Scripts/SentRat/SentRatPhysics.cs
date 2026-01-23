using UnityEngine;

namespace FindersCheesers.SentRat
{
    /// <summary>
    /// Handles physics interactions for SentRat character.
    /// </summary>
    [AddComponentMenu("Finder's Cheesers/SentRat/SentRat Physics")]
    public class SentRatPhysics : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Collider sentRatCollider;
        
        [Header("Physics Settings")]
        [SerializeField] private float friction = 0.5f;
        [SerializeField] private float drag = 0.1f;
        [SerializeField] private float bounciness = 0.3f;
        
        [Header("Collision Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 0.1f;
        
        // State
        private bool isGrounded;
        
        private void Awake()
        {
            // Get components if not assigned
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }
            
            if (sentRatCollider == null)
            {
                sentRatCollider = GetComponent<Collider>();
            }
            
            // Configure physics material
            SetupPhysicsMaterial();
        }
        
        private void SetupPhysicsMaterial()
        {
            // Create and assign physics material
            PhysicsMaterial physicsMaterial = new PhysicsMaterial
            {
                dynamicFriction = friction,
                staticFriction = friction,
                bounciness = bounciness,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Average
            };
            
            if (sentRatCollider != null)
            {
                sentRatCollider.material = physicsMaterial;
            }
        }
        
        private void FixedUpdate()
        {
            CheckGrounded();
            ApplyDrag();
        }
        
        private void CheckGrounded()
        {
            // Raycast downwards to check if grounded
            RaycastHit hit;
            isGrounded = Physics.Raycast(
                transform.position + Vector3.up * 0.1f,
                Vector3.down,
                out hit,
                groundCheckDistance,
                groundLayer
            );
        }
        
        private void ApplyDrag()
        {
            // Apply drag when grounded to slow down
            if (isGrounded && rb != null)
            {
                rb.linearDamping = drag;
            }
            else if (rb != null)
            {
                rb.linearDamping = 0f;
            }
        }
        
        /// <summary>
        /// Gets whether SentRat is currently grounded.
        /// </summary>
        public bool IsGrounded() => isGrounded;
        
        /// <summary>
        /// Applies an impulse force to the SentRat.
        /// </summary>
        public void ApplyImpulse(Vector3 impulse)
        {
            if (rb != null)
            {
                rb.AddForce(impulse, ForceMode.Impulse);
            }
        }
        
        /// <summary>
        /// Sets the velocity of the SentRat directly.
        /// </summary>
        public void SetVelocity(Vector3 velocity)
        {
            if (rb != null)
            {
                rb.linearVelocity = velocity;
            }
        }
        
        /// <summary>
        /// Stops all movement of the SentRat.
        /// </summary>
        public void StopMovement()
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            // Optional: Add collision effects or sounds here
            Debug.Log($"[SentRatPhysics] Collided with: {collision.gameObject.name}");
        }
        
        private void OnCollisionExit(Collision collision)
        {
            // Optional: Handle collision exit
        }
    }
}
