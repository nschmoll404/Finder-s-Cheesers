using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Syncs the carrying and throwing state from a KingRatThrowable to an Animator.
    /// Mirrors the style of AnimatorKingRatHandler - attach alongside or near a
    /// KingRatThrowable and assign matching Bool parameters in the Animator Controller.
    ///
    /// Uses KingRatThrowable's public events (OnPickedUp, OnDropped, OnThrown,
    /// OnLanded, OnThrowEnd) for reliable, frame-perfect state changes.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Animator King Rat Throwable")]
    public class AnimatorKingRatThrowable : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The Animator to send state data to. If null, will try to find one on this GameObject.")]
        [SerializeField]
        private Animator animator;

        [Tooltip("The KingRatThrowable to read state from. If null, will try to find one on this GameObject.")]
        [SerializeField]
        private KingRatThrowable kingRatThrowable;

        [Header("Animator Parameter Names")]
        [Tooltip("Name of the Bool parameter in the Animator for the carrying state.")]
        [SerializeField, AnimatorVar("animator")]
        private string carryingParam = "IsCarrying";

        [Tooltip("Name of the Bool parameter in the Animator for the throwing state.")]
        [SerializeField, AnimatorVar("animator")]
        private string throwingParam = "IsThrowing";

        [Tooltip("Name of the Bool parameter in the Animator for the landed state.")]
        [SerializeField, AnimatorVar("animator")]
        private string landedParam = "IsLanded";

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (kingRatThrowable == null)
            {
                kingRatThrowable = GetComponent<KingRatThrowable>();
            }
        }

        private void OnEnable()
        {
            if (kingRatThrowable == null)
            {
                return;
            }

            kingRatThrowable.OnPickedUp += OnPickedUp;
            kingRatThrowable.OnDropped += OnDropped;
            kingRatThrowable.OnThrown += OnThrown;
            kingRatThrowable.OnLanded += OnLanded;
            kingRatThrowable.OnThrowEnd += OnThrowEnd;
        }

        private void OnDisable()
        {
            if (kingRatThrowable == null)
            {
                return;
            }

            kingRatThrowable.OnPickedUp -= OnPickedUp;
            kingRatThrowable.OnDropped -= OnDropped;
            kingRatThrowable.OnThrown -= OnThrown;
            kingRatThrowable.OnLanded -= OnLanded;
            kingRatThrowable.OnThrowEnd -= OnThrowEnd;
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void OnPickedUp()
        {
            SetCarryingParam(true);
            SetThrowingParam(false);
            SetLandedParam(false);
        }

        private void OnDropped()
        {
            SetCarryingParam(false);
            SetThrowingParam(false);
            SetLandedParam(false);
        }

        private void OnThrown(Vector3 destination)
        {
            SetCarryingParam(false);
            SetLandedParam(false);
            SetThrowingParam(true);
        }

        private void OnLanded(Vector3 landPosition)
        {
            SetThrowingParam(false);
            SetLandedParam(true);
        }

        private void OnThrowEnd()
        {
            SetThrowingParam(false);
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

        private void SetLandedParam(bool value)
        {
            if (animator == null || string.IsNullOrEmpty(landedParam))
            {
                return;
            }

            animator.SetBool(landedParam, value);
        }
    }
}
