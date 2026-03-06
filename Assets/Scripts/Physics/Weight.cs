using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// A component that stores the weight of a GameObject.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Physics/Weight")]
    public class Weight : MonoBehaviour
    {
        #region Settings

        [Header("Weight Settings")]
        [Tooltip("The weight value of the object")]
        [SerializeField]
        private float weight = 1f;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the weight value.
        /// </summary>
        public float WeightValue => weight;

        #endregion

        #region Editor

        /// <summary>
        /// Validates the weight value in the editor.
        /// </summary>
        private void OnValidate()
        {
            // Ensure weight is non-negative
            weight = Mathf.Max(0f, weight);
        }

        #endregion
    }
}
