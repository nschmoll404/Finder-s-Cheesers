using UnityEngine;

namespace FindersCheesers.Objectives
{
    /// <summary>
    /// Toggle objective zone that changes boolean values when a SentRat enters.
    /// </summary>
    [AddComponentMenu("Finder's Cheesers/Objectives/Toggle Zone")]

   

    

    public class ToggleZone : ObjectiveZone
    {
        [Header("Toggle Settings")]
        [SerializeField] private string toggleID;
        [SerializeField] private bool defaultValue = false;
        
        [Header("Visuals")]
        [SerializeField] private Color tealBaseColor = new Color(0, 0.5f, 0.5f);
        [SerializeField] private Color tealFlashColor = new Color(0, 0.8f, 0.8f);
        
        // State
        private bool currentToggleValue;

        public GameObject targetObject;
        public Animator targetAnimator;
        private bool isAnimating = false;


        // Events
        public event System.Action<string, bool> OnToggleChanged;
        
        protected override void Awake()
        {
            baseColor = tealBaseColor;
            flashColor = tealFlashColor;
            currentToggleValue = defaultValue;
            
            base.Awake();
        }
        
        public override void OnSentRatEnter(FindersCheesers.SentRat.SentRat sentRat)
        {
            // Toggle boolean value
            currentToggleValue = !currentToggleValue;
            
            // Notify listeners
            OnToggleChanged?.Invoke(toggleID, currentToggleValue);
            
            Debug.Log($"[ToggleZone] {toggleID} toggled to {currentToggleValue}");

            if (defaultValue == true)
            {
                targetAnimator.Play("DoorLift");
            }

           




        }






        /// <summary>
        /// Gets current toggle value.
        /// </summary>
        public bool GetToggleValue() => currentToggleValue;
        
        /// <summary>
        /// Sets toggle value directly.
        /// </summary>
        public void SetToggleValue(bool value)
        {
            currentToggleValue = value;

           
        }

        public void Update()
        {
            
        }

        public void Start()
        {
            targetObject = GameObject.FindWithTag("DOOR");
            targetAnimator = targetObject.GetComponent<Animator>();

            if (targetAnimator == null)
            {
                Debug.LogError("You need an Animator on the target GameObject");
            }


            
        } 

    }


}
