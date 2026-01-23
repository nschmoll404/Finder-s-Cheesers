using UnityEngine;

namespace FindersCheesers.Objectives
{
    /// <summary>
    /// Grab objective zone that attaches items to SentRats when they enter.
    /// </summary>
    [AddComponentMenu("Finder's Cheesers/Objectives/Grab Zone")]
    public class GrabZone : ObjectiveZone
    {
        [Header("Grab Settings")]
        [SerializeField] private Items.GrabItem grabItemPrefab;
        [SerializeField] private bool oneTimeUse = true;
        
        [Header("Visuals")]
        [SerializeField] private Color purpleBaseColor = new Color(0.5f, 0, 0.5f);
        [SerializeField] private Color purpleFlashColor = new Color(0.8f, 0, 0.8f);
        
        // State
        private bool hasBeenGrabbed;
        
        // Events
        public event System.Action<string> OnItemGrabbed;
        
        protected override void Awake()
        {
            baseColor = purpleBaseColor;
            flashColor = purpleFlashColor;
            hasBeenGrabbed = false;
            
            base.Awake();
        }
        
        public override void OnSentRatEnter(FindersCheesers.SentRat.SentRat sentRat)
        {
            if (hasBeenGrabbed && oneTimeUse) return;
            
            // Spawn and attach grab item
            if (grabItemPrefab != null)
            {
                Items.GrabItem grabItem = Instantiate(grabItemPrefab, transform.position, Quaternion.identity);
                sentRat.AttachItem(grabItem);
                
                hasBeenGrabbed = true;
                
                // Notify listeners
                OnItemGrabbed?.Invoke(grabItem.ItemName);
                
                Debug.Log($"[GrabZone] Item {grabItem.ItemName} grabbed by SentRat");
            }
        }
        
        /// <summary>
        /// Resets the grab zone for reuse.
        /// </summary>
        public void ResetGrabZone()
        {
            hasBeenGrabbed = false;
        }
    }
}
