using UnityEngine;
using UnityEngine.InputSystem;

namespace FindersCheesers
{
    /// <summary>
    /// Singleton for accessing the PlayerInput component across the project.
    /// This provides a centralized reference to the player's input system.
    /// 
    /// Usage:
    /// - Attach this component to your player GameObject along with PlayerInput
    /// - Access the PlayerInput from anywhere using PlayerInputSingleton.Instance.PlayerInput
    /// </summary>
    [AddComponentMenu("Finders Cheesers/Player Input Singleton")]
    public class PlayerInputSingleton : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Should this singleton persist across scene changes?")]
        [SerializeField]
        private bool dontDestroyOnLoad = true;

        [Tooltip("Show debug information in the console")]
        [SerializeField]
        private bool debugMode = false;

        // Singleton instance
        private static PlayerInputSingleton instance;

        // PlayerInput component reference
        private PlayerInput playerInput;

        /// <summary>
        /// Gets the singleton instance of PlayerInputSingleton.
        /// </summary>
        public static PlayerInputSingleton Instance
        {
            get
            {
                if (instance == null)
                {
                    // Try to find an existing instance in the scene
                    instance = FindFirstObjectByType<PlayerInputSingleton>();
                    
                    if (instance == null)
                    {
                        Debug.LogWarning("[PlayerInputSingleton] No instance found in the scene!");
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Gets the PlayerInput component from the singleton.
        /// </summary>
        public PlayerInput PlayerInput
        {
            get
            {
                if (playerInput == null && instance != null)
                {
                    playerInput = instance.GetComponent<PlayerInput>();
                }
                return playerInput;
            }
        }

        private void Awake()
        {
            // Check for duplicate instances
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[PlayerInputSingleton] Multiple instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            // Set this as the singleton instance
            instance = this;

            // Cache the PlayerInput component
            playerInput = GetComponent<PlayerInput>();

            if (playerInput == null)
            {
                Debug.LogError("[PlayerInputSingleton] PlayerInput component not found on this GameObject!");
            }

            // Persist across scene loads if enabled
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (debugMode)
            {
                Debug.Log("[PlayerInputSingleton] Singleton initialized successfully.");
            }
        }

        private void OnDestroy()
        {
            // Clear the instance reference if this is the active instance
            if (instance == this)
            {
                instance = null;
                playerInput = null;

                if (debugMode)
                {
                    Debug.Log("[PlayerInputSingleton] Singleton destroyed.");
                }
            }
        }

        /// <summary>
        /// Gets an InputAction by its reference ID.
        /// </summary>
        /// <param name="actionReference">The InputActionReference containing the action ID.</param>
        /// <returns>The InputAction if found, null otherwise.</returns>
        public InputAction GetAction(InputActionReference actionReference)
        {
            if (actionReference == null)
            {
                Debug.LogWarning("[PlayerInputSingleton] Action reference is null.");
                return null;
            }

            if (PlayerInput == null)
            {
                Debug.LogWarning("[PlayerInputSingleton] PlayerInput is not available.");
                return null;
            }

            InputAction action = PlayerInput.actions.FindAction(actionReference.action.id);

            if (action == null && debugMode)
            {
                Debug.LogWarning($"[PlayerInputSingleton] Action with ID {actionReference.action.id} not found.");
            }

            return action;
        }

        /// <summary>
        /// Gets an InputAction by its name.
        /// </summary>
        /// <param name="actionName">The name of the action to find.</param>
        /// <returns>The InputAction if found, null otherwise.</returns>
        public InputAction GetAction(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                Debug.LogWarning("[PlayerInputSingleton] Action name is null or empty.");
                return null;
            }

            if (PlayerInput == null)
            {
                Debug.LogWarning("[PlayerInputSingleton] PlayerInput is not available.");
                return null;
            }

            InputAction action = PlayerInput.actions.FindAction(actionName);

            if (action == null && debugMode)
            {
                Debug.LogWarning($"[PlayerInputSingleton] Action '{actionName}' not found.");
            }

            return action;
        }

        /// <summary>
        /// Checks if the singleton is properly initialized.
        /// </summary>
        /// <returns>True if the singleton and PlayerInput are available, false otherwise.</returns>
        public static bool IsInitialized()
        {
            return instance != null && instance.PlayerInput != null;
        }

        /// <summary>
        /// Switches the current action map.
        /// </summary>
        /// <param name="actionMapName">The name of the action map to switch to.</param>
        public void SwitchActionMap(string actionMapName)
        {
            if (PlayerInput == null)
            {
                Debug.LogWarning("[PlayerInputSingleton] Cannot switch action map - PlayerInput is not available.");
                return;
            }

            InputActionMap actionMap = PlayerInput.actions.FindActionMap(actionMapName);

            if (actionMap != null)
            {
                PlayerInput.currentActionMap = actionMap;

                if (debugMode)
                {
                    Debug.Log($"[PlayerInputSingleton] Switched to action map: {actionMapName}");
                }
            }
            else
            {
                Debug.LogWarning($"[PlayerInputSingleton] Action map '{actionMapName}' not found.");
            }
        }

        /// <summary>
        /// Gets the name of the currently active action map.
        /// </summary>
        /// <returns>The name of the current action map, or null if not available.</returns>
        public string GetCurrentActionMapName()
        {
            if (PlayerInput == null || PlayerInput.currentActionMap == null)
            {
                return null;
            }

            return PlayerInput.currentActionMap.name;
        }
    }
}
