using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Syncs the carrying and throwing state from a KingRatHandler to an Animator.
    /// Mirrors the style of AnimatorVelocity - attach alongside or near a KingRatHandler
    /// and assign matching Bool parameters in the Animator Controller.
    ///
    /// Uses KingRatHandler's public events (OnKingRatGrabbed, OnKingRatReleased,
    /// OnKingRatThrown, OnKingRatLanded) for reliable, frame-perfect state changes.
    /// A minimum hold time on the throwing param ensures the Animator always sees it.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Animator King Rat Handler")]
    public class AnimatorKingRatHandler : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The Animator to send state data to. If null, will try to find one on this GameObject.")]
        [SerializeField]
        private Animator animator;

        [Tooltip("The KingRatHandler to read state from. If null, will try to find one on this GameObject.")]
        [SerializeField]
        private KingRatHandler kingRatHandler;

        [Header("Animator Parameter Names")]
        [Tooltip("Name of the Bool parameter in the Animator for the carrying (IsGrabbing) state.")]
        [SerializeField, AnimatorVar("animator")]
        private string carryingParam = "IsCarrying";

        [Tooltip("Name of the Bool parameter in the Animator for the throwing (IsThrowing) state.")]
        [SerializeField, AnimatorVar("animator")]
        private string throwingParam = "IsThrowing";

        [Header("Throwing Hold Settings")]
        [Tooltip("Minimum number of seconds the throwing bool stays true after a throw begins. " +
                 "Increase this if the Animator transition isn't triggering reliably.")]
        [SerializeField, Min(0f)]
        private float throwingHoldTime = 0.15f;

        private float throwingHoldTimer = 0f;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (kingRatHandler == null)
            {
                kingRatHandler = GetComponent<KingRatHandler>();
            }
        }

        private void OnEnable()
        {
            if (kingRatHandler == null)
            {
                return;
            }

            kingRatHandler.OnKingRatGrabbed += OnGrabbed;
            kingRatHandler.OnKingRatReleased += OnReleased;
            kingRatHandler.OnKingRatThrown += OnThrown;
            kingRatHandler.OnKingRatLanded += OnLanded;
        }

        private void OnDisable()
        {
            if (kingRatHandler == null)
            {
                return;
            }

            kingRatHandler.OnKingRatGrabbed -= OnGrabbed;
            kingRatHandler.OnKingRatReleased -= OnReleased;
            kingRatHandler.OnKingRatThrown -= OnThrown;
            kingRatHandler.OnKingRatLanded -= OnLanded;
        }

        private void Update()
        {
            if (throwingHoldTimer <= 0f)
            {
                return;
            }

            throwingHoldTimer -= Time.deltaTime;

            if (throwingHoldTimer <= 0f)
            {
                throwingHoldTimer = 0f;
                SetThrowingParam(false);
            }
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void OnGrabbed()
        {
            SetCarryingParam(true);
        }

        private void OnReleased()
        {
            SetCarryingParam(false);
        }

        private void OnThrown(Vector3 destination)
        {
            throwingHoldTimer = throwingHoldTime;
            SetThrowingParam(true);
        }

        private void OnLanded(Vector3 landPosition)
        {
            // Only clear if the hold timer has already expired; otherwise let it
            // count down naturally so the minimum hold time is always respected.
            if (throwingHoldTimer <= 0f)
            {
                SetThrowingParam(false);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SetCarryingParam(bool value)
        {
            if (animator == null || string.IsNullOrEmpty(carryingParam))
            {
                return;
            }

            animator.SetBool(carryingParam, value);
        }

        private void SetThrowingParam(bool value)
        {
            if (animator == null || string.IsNullOrEmpty(throwingParam))
            {
                return;
            }

            animator.SetBool(throwingParam, value);
        }
    }
}
