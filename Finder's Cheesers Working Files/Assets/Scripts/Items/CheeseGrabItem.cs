using UnityEngine;

namespace FindersCheesers.Items
{
    /// <summary>
    /// Special grab item that increases rat count when recalled.
    /// </summary>
    [AddComponentMenu("Finder's Cheesers/Items/Cheese Grab Item")]
    public class CheeseGrabItem : GrabItem
    {
        [Header("Cheese Settings")]
        [SerializeField] private int ratsAdded = 1;
        
        [Header("Visuals")]
        [SerializeField] private Color cheeseColor = Color.yellow;
        [SerializeField] private float sphereRadius = 0.5f;
        
        /// <summary>
        /// Gets number of rats added when this cheese is recalled.
        /// </summary>
        public int RatsAdded => ratsAdded;
        
        private void OnDrawGizmos()
        {
            // Draw cheese sphere for debugging
            Gizmos.color = cheeseColor;
            Gizmos.DrawWireSphere(transform.position, sphereRadius);
        }
    }
}
