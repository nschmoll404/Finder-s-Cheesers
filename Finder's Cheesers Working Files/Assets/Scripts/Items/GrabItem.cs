using UnityEngine;

namespace FindersCheesers.Items
{
    /// <summary>
    /// Base class for items that can be grabbed by SentRats.
    /// </summary>
    [AddComponentMenu("Finder's Cheesers/Items/Grab Item")]
    public class GrabItem : MonoBehaviour
    {
        [Header("Item Settings")]
        [SerializeField] private string itemName;
        [SerializeField] private Sprite icon;
        
        [Header("Visuals")]
        [SerializeField] private GameObject itemModel;
        
        // State
        private bool isAttached;
        private Transform sentRatTransform;
        
        /// <summary>
        /// Gets item name (should have "_grab" suffix).
        /// </summary>
        public string ItemName => itemName;
        
        /// <summary>
        /// Gets item icon for UI display.
        /// </summary>
        public Sprite Icon => icon;
        
        /// <summary>
        /// Attaches this item to a SentRat.
        /// </summary>
        public void AttachToSentRat(Transform sentRatTransform)
        {
            this.sentRatTransform = sentRatTransform;
            isAttached = true;
            
            // Parent item model to SentRat
            if (itemModel != null)
            {
                itemModel.transform.SetParent(sentRatTransform);
                itemModel.transform.localPosition = Vector3.zero;
            }
        }
        
        protected virtual void Update()
        {
            if (isAttached && sentRatTransform != null)
            {
                transform.position = sentRatTransform.position;
            }
        }
    }
}
