using UnityEngine;

namespace FindersCheesers
{
    /// <summary>
    /// Component that stores icon data for throwable objects.
    /// Attach this to any throwable prefab to provide an icon for UI display.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Throwable Icon")]
    public class ThrowableIcon : MonoBehaviour
    {
        [Header("Icon Settings")]
        [Tooltip("The icon/sprite to display in the UI when this throwable is being held.")]
        [SerializeField]
        private Sprite icon;

        [Tooltip("The color to tint the icon in the UI.")]
        [SerializeField]
        private Color iconColor = Color.white;

        [Header("Display Settings")]
        [Tooltip("The name of this throwable type for display purposes.")]
        [SerializeField]
        private string displayName = "Throwable";

        #region Properties

        /// <summary>
        /// Gets the icon sprite for this throwable.
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// Gets the color to tint the icon.
        /// </summary>
        public Color IconColor => iconColor;

        /// <summary>
        /// Gets the display name of this throwable.
        /// </summary>
        public string DisplayName => displayName;

        #endregion

        #region Public API

        /// <summary>
        /// Sets the icon sprite.
        /// </summary>
        /// <param name="newIcon">The new icon sprite.</param>
        public void SetIcon(Sprite newIcon)
        {
            icon = newIcon;
        }

        /// <summary>
        /// Sets the icon color.
        /// </summary>
        /// <param name="newColor">The new color.</param>
        public void SetIconColor(Color newColor)
        {
            iconColor = newColor;
        }

        /// <summary>
        /// Sets the display name.
        /// </summary>
        /// <param name="name">The new display name.</param>
        public void SetDisplayName(string name)
        {
            displayName = name;
        }

        #endregion
    }
}
