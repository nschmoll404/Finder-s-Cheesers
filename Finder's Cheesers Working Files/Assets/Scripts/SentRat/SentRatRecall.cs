using UnityEngine;
using UnityEngine.InputSystem;

namespace FindersCheesers.SentRat
{
    /// <summary>
    /// Handles recalling all active SentRats with spacebar.
    /// </summary>
    [AddComponentMenu("Finder's Cheesers/SentRat/SentRat Recall")]
    public class SentRatRecall : MonoBehaviour
    {
        [Header("Input References")]
        [SerializeField] private InputActionReference recallActionReference;
        
        // Components
        private PlayerInput playerInput;
        private InputAction recallAction;
        private SentRatLauncher sentRatLauncher;
        
        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            sentRatLauncher = GetComponent<SentRatLauncher>();
        }
        
        private void OnEnable()
        {
            if (playerInput != null && recallActionReference != null)
            {
                recallAction = playerInput.actions.FindAction(recallActionReference.action.id);
                if (recallAction != null)
                {
                    recallAction.performed += OnRecallPerformed;
                }
            }
        }
        
        private void OnDisable()
        {
            if (recallAction != null)
            {
                recallAction.performed -= OnRecallPerformed;
            }
        }
        
        private void OnRecallPerformed(InputAction.CallbackContext context)
        {
            RecallAllSentRats();
        }
        
        private void RecallAllSentRats()
        {
            if (sentRatLauncher != null)
            {
                foreach (SentRat sentRat in sentRatLauncher.GetActiveSentRats())
                {
                    if (sentRat != null)
                    {
                        sentRat.Recall();
                    }
                }
            }
        }
    }
}
