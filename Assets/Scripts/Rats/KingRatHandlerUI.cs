using UnityEngine;
using UnityEngine.UI;

namespace FindersCheesers
{
    /// <summary>
    /// UI component that displays the icon of the throwable currently held by the KingRatHandler.
    /// Automatically updates the displayed sprite based on what throwable is being held.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/King Rat Handler UI")]
    public class KingRatHandlerUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the KingRatHandler component.")]
        [SerializeField]
        private KingRatHandler kingRatHandler;

        [Tooltip("The Image component that will display the throwable icon.")]
        [SerializeField]
        private Image iconImage;

        [Header("Default Icon")]
        [Tooltip("The icon to display when no throwable is being held.")]
        [SerializeField]
        private Sprite defaultIcon;

        [Tooltip("The color to use when no throwable is being held.")]
        [SerializeField]
        private Color defaultIconColor = new Color(1f, 1f, 1f, 0.5f);

        [Header("Display Settings")]
        [Tooltip("Whether to hide the icon when no throwable is held.")]
        [SerializeField]
        private bool hideWhenEmpty = false;

        [Tooltip("Whether to animate the icon when a throwable is grabbed.")]
        [SerializeField]
        private bool animateOnGrab = true;

        [Header("Animation Settings")]
        [Tooltip("The scale to animate to when grabbing a throwable.")]
        [SerializeField]
        private Vector3 grabScale = new Vector3(1.2f, 1.2f, 1.2f);

        [Tooltip("The speed of the grab animation.")]
        [SerializeField]
        private float grabAnimationSpeed = 5f;

        #region Private Fields

        private Vector3 originalScale;
        private bool isAnimating = false;
        private Sprite currentIcon;
        private Color currentColor;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Store original scale for animation
            if (iconImage != null)
            {
                originalScale = iconImage.transform.localScale;
            }
        }

        private void Start()
        {
            InitializeReferences();
            SubscribeToEvents();
            UpdateIconDisplay();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            // Handle grab animation
            if (isAnimating && iconImage != null)
            {
                iconImage.transform.localScale = Vector3.Lerp(
                    iconImage.transform.localScale,
                    originalScale,
                    grabAnimationSpeed * Time.deltaTime
                );

                // Stop animating when close to original scale
                if (Vector3.Distance(iconImage.transform.localScale, originalScale) < 0.01f)
                {
                    iconImage.transform.localScale = originalScale;
                    isAnimating = false;
                }
            }
        }

        #endregion

        #region Initialization

        private void InitializeReferences()
        {
            // Get KingRatHandler if not assigned
            if (kingRatHandler == null)
            {
                kingRatHandler = GetComponent<KingRatHandler>();

                if (kingRatHandler == null)
                {
                    kingRatHandler = GetComponentInParent<KingRatHandler>();
                }

                if (kingRatHandler == null)
                {
                    Debug.LogError("[KingRatHandlerUI] KingRatHandler component not found!");
                }
            }

            // Get Image component if not assigned
            if (iconImage == null)
            {
                iconImage = GetComponent<Image>();

                if (iconImage == null)
                {
                    iconImage = GetComponentInChildren<Image>();
                }

                if (iconImage == null)
                {
                    Debug.LogError("[KingRatHandlerUI] Image component not found!");
                }
            }
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            if (kingRatHandler != null)
            {
                kingRatHandler.OnKingRatGrabbed += OnThrowableGrabbed;
                kingRatHandler.OnKingRatReleased += OnThrowableReleased;
                kingRatHandler.OnKingRatThrown += OnThrowableThrown;
                kingRatHandler.OnKingRatLanded += OnThrowableLanded;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (kingRatHandler != null)
            {
                kingRatHandler.OnKingRatGrabbed -= OnThrowableGrabbed;
                kingRatHandler.OnKingRatReleased -= OnThrowableReleased;
                kingRatHandler.OnKingRatThrown -= OnThrowableThrown;
                kingRatHandler.OnKingRatLanded -= OnThrowableLanded;
            }
        }

        #endregion

        #region Event Handlers

        private void OnThrowableGrabbed()
        {
            UpdateIconDisplay();

            // Trigger grab animation
            if (animateOnGrab && iconImage != null)
            {
                iconImage.transform.localScale = grabScale;
                isAnimating = true;
            }
        }

        private void OnThrowableReleased()
        {
            UpdateIconDisplay();
        }

        private void OnThrowableThrown(Vector3 destination)
        {
            UpdateIconDisplay();
        }

        private void OnThrowableLanded(Vector3 position)
        {
            UpdateIconDisplay();
        }

        #endregion

        #region Icon Display

        private void UpdateIconDisplay()
        {
            if (iconImage == null)
            {
                return;
            }

            // Check if a throwable is being held
            if (kingRatHandler != null && kingRatHandler.IsGrabbing && kingRatHandler.KingRat != null)
            {
                // Try to get ThrowableIcon component from the held object
                ThrowableIcon throwableIcon = kingRatHandler.KingRat.GetComponent<ThrowableIcon>();

                if (throwableIcon != null && throwableIcon.Icon != null)
                {
                    // Display the throwable's icon
                    iconImage.sprite = throwableIcon.Icon;
                    iconImage.color = throwableIcon.IconColor;
                    iconImage.enabled = true;
                }
                else
                {
                    // No icon found, use default
                    DisplayDefaultIcon();
                }
            }
            else
            {
                // No throwable held
                if (hideWhenEmpty)
                {
                    iconImage.enabled = false;
                }
                else
                {
                    DisplayDefaultIcon();
                }
            }
        }

        private void DisplayDefaultIcon()
        {
            if (iconImage == null)
            {
                return;
            }

            if (defaultIcon != null)
            {
                iconImage.sprite = defaultIcon;
                iconImage.color = defaultIconColor;
                iconImage.enabled = true;
            }
            else
            {
                // No default icon, hide the image
                iconImage.enabled = false;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually updates the icon display.
        /// Call this if you need to force a refresh.
        /// </summary>
        public void RefreshIcon()
        {
            UpdateIconDisplay();
        }

        /// <summary>
        /// Sets the default icon to display when no throwable is held.
        /// </summary>
        /// <param name="icon">The default icon sprite.</param>
        public void SetDefaultIcon(Sprite icon)
        {
            defaultIcon = icon;
            UpdateIconDisplay();
        }

        /// <summary>
        /// Sets the default icon color.
        /// </summary>
        /// <param name="color">The default icon color.</param>
        public void SetDefaultIconColor(Color color)
        {
            defaultIconColor = color;
            UpdateIconDisplay();
        }

        /// <summary>
        /// Sets whether to hide the icon when no throwable is held.
        /// </summary>
        /// <param name="hide">True to hide when empty, false to show default icon.</param>
        public void SetHideWhenEmpty(bool hide)
        {
            hideWhenEmpty = hide;
            UpdateIconDisplay();
        }

        #endregion
    }
}
