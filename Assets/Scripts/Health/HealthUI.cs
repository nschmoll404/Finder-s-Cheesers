using UnityEngine;
using UnityEngine.UIElements;

namespace FindersCheesers
{
    /// <summary>
    /// A UI component that displays health using a UIDocument.
    /// Automatically searches for a ProgressBar element in the root visual element.
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Health UI")]
    public class HealthUI : MonoBehaviour
    {
        #region Settings

        [Header("UI Settings")]
        [Tooltip("The UIDocument component to use for displaying health")]
        [SerializeField]
        private UIDocument uiDocument;

        [Tooltip("The name of the ProgressBar element to search for in the root visual element")]
        [SerializeField]
        private string progressBarElementName = "ProgressBar";

        [Tooltip("Whether to automatically find the UIDocument component if not assigned")]
        [SerializeField]
        private bool autoFindUIDocument = true;

        [Tooltip("Whether to automatically find the Health component on the same GameObject")]
        [SerializeField]
        private bool autoFindHealth = true;

        [Header("Visual Settings")]
        [Tooltip("Whether to show the health value text")]
        [SerializeField]
        private bool showHealthText = true;

        [Tooltip("Format string for the health text (use {0} for current, {1} for max)")]
        [SerializeField]
        private string healthTextFormat = "{0}/{1}";

        [Tooltip("Color for high health (above 50%)")]
        [SerializeField]
        private Color highHealthColor = Color.green;

        [Tooltip("Color for medium health (25-50%)")]
        [SerializeField]
        private Color mediumHealthColor = Color.yellow;

        [Tooltip("Color for low health (below 25%)")]
        [SerializeField]
        private Color lowHealthColor = Color.red;

        [Header("Debug")]
        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        #endregion

        #region Component References

        private Health healthComponent;
        private ProgressBar progressBar;
        private Label healthTextLabel;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeUIDocument();
            InitializeHealthComponent();
        }

        private void OnEnable()
        {
            // Subscribe to health events
            if (healthComponent != null)
            {
                healthComponent.OnHealthChanged += HandleHealthChanged;
                healthComponent.OnDeath += HandleDeath;
            }

            // Find UI elements when enabled
            FindUIElements();
            
            // Update UI with initial health
            UpdateHealthUI();
        }

        private void OnDisable()
        {
            // Unsubscribe from health events
            if (healthComponent != null)
            {
                healthComponent.OnHealthChanged -= HandleHealthChanged;
                healthComponent.OnDeath -= HandleDeath;
            }
        }

        private void OnDestroy()
        {
            // Clean up references
            progressBar = null;
            healthTextLabel = null;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the UIDocument reference.
        /// </summary>
        private void InitializeUIDocument()
        {
            if (uiDocument == null && autoFindUIDocument)
            {
                uiDocument = GetComponent<UIDocument>();

                if (uiDocument == null)
                {
                    uiDocument = GetComponentInParent<UIDocument>();
                }

                if (uiDocument == null)
                {
                    Debug.LogError("[HealthUI] UIDocument component not found! Please assign it manually or ensure it exists on this GameObject or a parent.");
                }
                else if (debugMode)
                {
                    Debug.Log("[HealthUI] UIDocument component found automatically.");
                }
            }
            else if (uiDocument != null && debugMode)
            {
                Debug.Log("[HealthUI] UIDocument component assigned.");
            }
        }

        /// <summary>
        /// Initializes the Health component reference.
        /// </summary>
        private void InitializeHealthComponent()
        {
            if (healthComponent == null && autoFindHealth)
            {
                healthComponent = GetComponent<Health>();

                if (healthComponent == null)
                {
                    healthComponent = GetComponentInParent<Health>();
                }

                if (healthComponent == null)
                {
                    Debug.LogError("[HealthUI] Health component not found! Please assign it manually or ensure it exists on this GameObject or a parent.");
                }
                else if (debugMode)
                {
                    Debug.Log("[HealthUI] Health component found automatically.");
                }
            }
            else if (healthComponent != null && debugMode)
            {
                Debug.Log("[HealthUI] Health component assigned.");
            }
        }

        /// <summary>
        /// Finds the ProgressBar and optional Label elements in the UIDocument.
        /// </summary>
        private void FindUIElements()
        {
            if (uiDocument == null)
            {
                Debug.LogError("[HealthUI] UIDocument is not assigned!");
                return;
            }

            VisualElement rootVisualElement = uiDocument.rootVisualElement;

            if (rootVisualElement == null)
            {
                Debug.LogError("[HealthUI] UIDocument rootVisualElement is null!");
                return;
            }

            // Find the ProgressBar by name
            progressBar = rootVisualElement.Q<ProgressBar>(progressBarElementName);

            if (progressBar == null)
            {
                Debug.LogError($"[HealthUI] ProgressBar element with name '{progressBarElementName}' not found in the UIDocument!");
            }
            else if (debugMode)
            {
                Debug.Log($"[HealthUI] ProgressBar element '{progressBarElementName}' found successfully.");
            }

            // Try to find a Label element for displaying health text
            // Look for common label names
            string[] possibleLabelNames = { "HealthText", "HealthLabel", "HealthValue", "Text" };
            foreach (string labelName in possibleLabelNames)
            {
                healthTextLabel = rootVisualElement.Q<Label>(labelName);
                if (healthTextLabel != null)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[HealthUI] Label element '{labelName}' found successfully.");
                    }
                    break;
                }
            }

            if (healthTextLabel == null && debugMode)
            {
                Debug.Log("[HealthUI] No Label element found for health text display.");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles health change events.
        /// </summary>
        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            UpdateHealthUI();

            if (debugMode)
            {
                Debug.Log($"[HealthUI] Health changed: {currentHealth}/{maxHealth}");
            }
        }

        /// <summary>
        /// Handles death events.
        /// </summary>
        private void HandleDeath()
        {
            UpdateHealthUI();

            if (debugMode)
            {
                Debug.Log("[HealthUI] Entity died!");
            }
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// Updates the health UI elements.
        /// </summary>
        private void UpdateHealthUI()
        {
            if (healthComponent == null)
            {
                return;
            }

            UpdateProgressBar();
            UpdateHealthText();
            UpdateProgressBarColor();
        }

        /// <summary>
        /// Updates the ProgressBar value.
        /// </summary>
        private void UpdateProgressBar()
        {
            if (progressBar == null)
            {
                return;
            }

            float healthPercentage = healthComponent.HealthPercentage;
            progressBar.value = healthPercentage;

            if (debugMode)
            {
                Debug.Log($"[HealthUI] ProgressBar updated to {healthPercentage:P0}");
            }
        }

        /// <summary>
        /// Updates the health text label.
        /// </summary>
        private void UpdateHealthText()
        {
            if (healthTextLabel == null || !showHealthText)
            {
                return;
            }

            string healthText = string.Format(healthTextFormat, healthComponent.CurrentHealth, healthComponent.MaxHealth);
            healthTextLabel.text = healthText;

            if (debugMode)
            {
                Debug.Log($"[HealthUI] Health text updated to: {healthText}");
            }
        }

        /// <summary>
        /// Updates the ProgressBar color based on health percentage.
        /// </summary>
        private void UpdateProgressBarColor()
        {
            if (progressBar == null)
            {
                return;
            }

            float healthPercentage = healthComponent.HealthPercentage;
            Color targetColor;

            if (healthPercentage > 0.5f)
            {
                targetColor = highHealthColor;
            }
            else if (healthPercentage > 0.25f)
            {
                targetColor = mediumHealthColor;
            }
            else
            {
                targetColor = lowHealthColor;
            }

            // Apply color to the progress bar's background or title
            progressBar.style.backgroundColor = new StyleColor(targetColor);

            if (debugMode)
            {
                Debug.Log($"[HealthUI] ProgressBar color updated to {targetColor}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets the UIDocument to use.
        /// </summary>
        /// <param name="document">The UIDocument to use.</param>
        public void SetUIDocument(UIDocument document)
        {
            uiDocument = document;
            FindUIElements();
            UpdateHealthUI();
        }

        /// <summary>
        /// Sets the Health component to monitor.
        /// </summary>
        /// <param name="health">The Health component to monitor.</param>
        public void SetHealthComponent(Health health)
        {
            // Unsubscribe from old component
            if (healthComponent != null)
            {
                healthComponent.OnHealthChanged -= HandleHealthChanged;
                healthComponent.OnDeath -= HandleDeath;
            }

            healthComponent = health;

            // Subscribe to new component
            if (healthComponent != null)
            {
                healthComponent.OnHealthChanged += HandleHealthChanged;
                healthComponent.OnDeath += HandleDeath;
                UpdateHealthUI();
            }
        }

        /// <summary>
        /// Sets the name of the ProgressBar element to search for.
        /// </summary>
        /// <param name="elementName">The name of the ProgressBar element.</param>
        public void SetProgressBarElementName(string elementName)
        {
            progressBarElementName = elementName;
            FindUIElements();
            UpdateHealthUI();
        }

        /// <summary>
        /// Sets whether to show the health text.
        /// </summary>
        /// <param name="show">Whether to show the health text.</param>
        public void SetShowHealthText(bool show)
        {
            showHealthText = show;
            UpdateHealthText();
        }

        /// <summary>
        /// Sets the format string for the health text.
        /// </summary>
        /// <param name="format">The format string (use {0} for current, {1} for max).</param>
        public void SetHealthTextFormat(string format)
        {
            healthTextFormat = format;
            UpdateHealthText();
        }

        /// <summary>
        /// Sets the color thresholds for the health bar.
        /// </summary>
        /// <param name="high">Color for high health.</param>
        /// <param name="medium">Color for medium health.</param>
        /// <param name="low">Color for low health.</param>
        public void SetHealthColors(Color high, Color medium, Color low)
        {
            highHealthColor = high;
            mediumHealthColor = medium;
            lowHealthColor = low;
            UpdateProgressBarColor();
        }

        /// <summary>
        /// Forces a refresh of the UI elements.
        /// </summary>
        public void RefreshUI()
        {
            FindUIElements();
            UpdateHealthUI();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the ProgressBar element.
        /// </summary>
        public ProgressBar ProgressBar => progressBar;

        /// <summary>
        /// Gets the health text Label element (null if not found).
        /// </summary>
        public Label HealthTextLabel => healthTextLabel;

        /// <summary>
        /// Gets whether the UI is properly initialized.
        /// </summary>
        public bool IsInitialized => uiDocument != null && progressBar != null;

        #endregion
    }
}
