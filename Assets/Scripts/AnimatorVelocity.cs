using UnityEngine;

namespace FindersCheesers
{
    [AddComponentMenu("Finders Cheesers/Animator Velocity")]
    public class AnimatorVelocity : MonoBehaviour
    {
        public enum VelocitySpace
        {
            World,
            Local
        }

        public enum UpdateMode
        {
            Update,
            FixedUpdate,
            LateUpdate
        }

        [Header("Target")]
        [Tooltip("The Animator to send velocity data to. If null, will try to find one on this GameObject.")]
        [SerializeField]
        private Animator animator;

        [Tooltip("The Transform to track velocity from. If null, uses this GameObject's Transform.")]
        [SerializeField]
        private Transform trackedTransform;

        [Header("Velocity Settings")]
        [Tooltip("When to calculate and send velocity to the Animator.")]
        [SerializeField]
        private UpdateMode updateMode = UpdateMode.LateUpdate;

        [Tooltip("Whether to use world or local velocity.")]
        [SerializeField]
        private VelocitySpace velocitySpace = VelocitySpace.Local;

        [Tooltip("Smoothing time in seconds. Lower values respond faster, higher values smooth out spikes.")]
        [SerializeField]
        private float smoothingTime = 0.1f;

        [Tooltip("Round velocity values to the nearest increment (0 = no rounding).")]
        [SerializeField]
        private float roundToNearest = 0.01f;

        [Header("Animator Parameter Names")]
        [Tooltip("Name of the float parameter in the Animator for X velocity.")]
        [SerializeField, AnimatorVar("animator")]
        private string xVelocityParam = "VelocityX";

        [Tooltip("Name of the float parameter in the Animator for Y velocity.")]
        [SerializeField, AnimatorVar("animator")]
        private string yVelocityParam = "VelocityY";

        [Tooltip("Name of the float parameter in the Animator for Z velocity.")]
        [SerializeField, AnimatorVar("animator")]
        private string zVelocityParam = "VelocityZ";

        private Vector3 previousPosition;
        private Vector3 smoothVelocity;
        private float smoothVelocityX;
        private float smoothVelocityY;
        private float smoothVelocityZ;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (trackedTransform == null)
            {
                trackedTransform = transform;
            }
        }

        private void Start()
        {
            previousPosition = trackedTransform.position;
            smoothVelocity = Vector3.zero;
        }

        private void Update()
        {
            if (updateMode == UpdateMode.Update)
            {
                CalculateVelocity(Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (updateMode == UpdateMode.FixedUpdate)
            {
                CalculateVelocity(Time.fixedDeltaTime);
            }
        }

        private void LateUpdate()
        {
            if (updateMode == UpdateMode.LateUpdate)
            {
                CalculateVelocity(Time.deltaTime);
            }
        }

        private void CalculateVelocity(float deltaTime)
        {
            if (animator == null || trackedTransform == null || deltaTime <= 0f)
            {
                return;
            }

            Vector3 currentPosition = trackedTransform.position;
            Vector3 delta = currentPosition - previousPosition;
            Vector3 rawVelocity = delta / deltaTime;

            if (velocitySpace == VelocitySpace.Local)
            {
                rawVelocity = trackedTransform.InverseTransformDirection(rawVelocity);
            }

            smoothVelocityX = Mathf.SmoothDamp(smoothVelocityX, rawVelocity.x, ref smoothVelocity.x, smoothingTime);
            smoothVelocityY = Mathf.SmoothDamp(smoothVelocityY, rawVelocity.y, ref smoothVelocity.y, smoothingTime);
            smoothVelocityZ = Mathf.SmoothDamp(smoothVelocityZ, rawVelocity.z, ref smoothVelocity.z, smoothingTime);

            if (roundToNearest > 0f)
            {
                smoothVelocityX = Mathf.Round(smoothVelocityX / roundToNearest) * roundToNearest;
                smoothVelocityY = Mathf.Round(smoothVelocityY / roundToNearest) * roundToNearest;
                smoothVelocityZ = Mathf.Round(smoothVelocityZ / roundToNearest) * roundToNearest;
            }

            if (roundToNearest > 0f)
            {
                smoothVelocityX = Mathf.Round(smoothVelocityX / roundToNearest) * roundToNearest;
                smoothVelocityY = Mathf.Round(smoothVelocityY / roundToNearest) * roundToNearest;
                smoothVelocityZ = Mathf.Round(smoothVelocityZ / roundToNearest) * roundToNearest;
            }

            if (!string.IsNullOrEmpty(xVelocityParam))
            {
                animator.SetFloat(xVelocityParam, smoothVelocityX);
            }

            if (!string.IsNullOrEmpty(yVelocityParam))
            {
                animator.SetFloat(yVelocityParam, smoothVelocityY);
            }

            if (!string.IsNullOrEmpty(zVelocityParam))
            {
                animator.SetFloat(zVelocityParam, smoothVelocityZ);
            }

            previousPosition = currentPosition;
        }

        /// <summary>
        /// Gets the current smoothed velocity vector.
        /// </summary>
        public Vector3 GetSmoothedVelocity()
        {
            return new Vector3(smoothVelocityX, smoothVelocityY, smoothVelocityZ);
        }
    }
}
