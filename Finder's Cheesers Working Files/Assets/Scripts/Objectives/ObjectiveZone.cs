using UnityEngine;

namespace FindersCheesers.Objectives
{
    /// <summary>
    /// Base class for objective zones that SentRats can interact with.
    /// </summary>
    [AddComponentMenu("Finder's Cheesers/Objectives/Objective Zone")]
    public abstract class ObjectiveZone : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private float flashSpeed = 1f;
        [SerializeField] protected Color baseColor;
        [SerializeField] protected Color flashColor;
        
        [Header("Trigger")]
        [SerializeField] private Collider triggerCollider;
        
        // Components
        private Renderer zoneRenderer;
        private Material zoneMaterial;
        
        // State
        private float flashTimer;
        
        protected virtual void Awake()
        {
            zoneRenderer = GetComponent<Renderer>();
            if (zoneRenderer != null)
            {
                zoneMaterial = zoneRenderer.material;
            }
            
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider>();
            }
        }
        
        protected virtual void Update()
        {
            UpdateFlashAnimation();
        }
        
        private void UpdateFlashAnimation()
        {
            if (zoneMaterial == null) return;
            
            flashTimer += Time.deltaTime * flashSpeed;
            float lerpValue = (Mathf.Sin(flashTimer) + 1f) * 0.5f;
            zoneMaterial.color = Color.Lerp(baseColor, flashColor, lerpValue);
        }
        
        /// <summary>
        /// Called when a SentRat enters the objective zone.
        /// </summary>
        public abstract void OnSentRatEnter(FindersCheesers.SentRat.SentRat sentRat);
    }
}
