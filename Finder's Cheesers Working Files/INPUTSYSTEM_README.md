# InputSystem Integration Guide

This document explains how to properly integrate Unity's InputSystem with PlayerInput in this project.

## Overview

This project uses Unity's new InputSystem with the `PlayerInput` component. Scripts reference input actions via `InputActionReference` and retrieve the actual `InputAction` from the `PlayerInput` component at runtime.

## Key Pattern: Getting Input Actions from PlayerInput

### Step 1: Declare InputActionReference Fields

In your script, declare serialized fields for each input action you need:

```csharp
using UnityEngine.InputSystem;

public class YourScript : MonoBehaviour
{
    [SerializeField]
    private InputActionReference yourActionReference;
    
    private InputAction yourAction;
    private PlayerInput playerInput;
}
```

### Step 2: Get PlayerInput Reference

In `Awake()`, get the PlayerInput component. There are multiple ways to do this:

#### Option A: Using PlayerInputSingleton

If your project uses the singleton pattern:

```csharp
private void Awake()
{
    playerInput = PlayerInputSingleton.Instance?.PlayerInput;
}
```

#### Option B: From Player Controller GameObject

If the script is on the same GameObject as PlayerInput (or a parent/child):

```csharp
private void Awake()
{
    // Get PlayerInput from the same GameObject
    playerInput = GetComponent<PlayerInput>();
    
    // Or get from parent (if script is on a child)
    if (playerInput == null)
    {
        playerInput = GetComponentInParent<PlayerInput>();
    }
}
```

#### Option C: From Player Reference

If you have a reference to the player GameObject:

```csharp
[SerializeField]
private GameObject player;

private void Awake()
{
    if (player != null)
    {
        playerInput = player.GetComponent<PlayerInput>();
    }
}
```

#### Option D: Find in Scene (Last Resort)

If you need to find PlayerInput anywhere in the scene:

```csharp
private void Awake()
{
    // Find first PlayerInput in scene
    playerInput = FindFirstObjectByType<PlayerInput>();
    
    if (playerInput == null)
    {
        Debug.LogError("PlayerInput component not found in scene!");
    }
}
```

**Note**: Option D should be used sparingly as it's less efficient and can be fragile if multiple PlayerInputs exist in the scene.

### Step 3: Retrieve InputAction by ID

In `OnEnable()`, retrieve the actual `InputAction` from the PlayerInput using the ID from the `InputActionReference`:

```csharp
private void OnEnable()
{
    if (playerInput != null)
    {
        // CRITICAL: Use FindAction with the ID from the InputActionReference
        yourAction = playerInput.actions.FindAction(yourActionReference.action.id);
        
        // Subscribe to input events
        if (yourAction != null)
        {
            yourAction.performed += OnYourActionPerformed;
            yourAction.started += OnYourActionStarted;
            yourAction.canceled += OnYourActionCanceled;
        }
    }
}
```

### Step 4: Unsubscribe in OnDisable

Always unsubscribe from input events in `OnDisable()`:

```csharp
private void OnDisable()
{
    if (yourAction != null)
    {
        yourAction.performed -= OnYourActionPerformed;
        yourAction.started -= OnYourActionStarted;
        yourAction.canceled -= OnYourActionCanceled;
    }
}
```

### Step 5: Handle Input Events

Implement your input handlers:

```csharp
private void OnYourActionPerformed(InputAction.CallbackContext context)
{
    // Handle the action (e.g., button press, movement)
}

private void OnYourActionStarted(InputAction.CallbackContext context)
{
    // Handle when the action starts (button down)
}

private void OnYourActionCanceled(InputAction.CallbackContext context)
{
    // Handle when the action ends (button up)
}
```

### Step 6: Read Input Values (For Continuous Actions)

For continuous actions like movement or look, read values in `Update()` or `FixedUpdate()`:

```csharp
private void Update()
{
    if (yourAction != null)
    {
        Vector2 inputValue = yourAction.ReadValue<Vector2>();
        // Use the input value
    }
}
```

## Complete Examples

### Example 1: Using PlayerInputSingleton (Current Project)

Here's a complete example from [`ObjectManipulator.cs`](Assets/Scripts/Telekinesis/ObjectManipulator.cs:99-123):

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectManipulator : MonoBehaviour
{
    [SerializeField]
    private InputActionReference interactActionReference;
    [SerializeField]
    private InputActionReference scrollActionReference;
    
    private InputAction interactAction;
    private InputAction scrollAction;
    private PlayerInput playerInput;
    
    private void Awake()
    {
        playerInput = PlayerInputSingleton.Instance?.PlayerInput;
    }
    
    private void OnEnable()
    {
        if (playerInput != null)
        {
            // Get actions using the ID from InputActionReference
            interactAction = playerInput.actions.FindAction(interactActionReference.action.id);
            scrollAction = playerInput.actions.FindAction(scrollActionReference.action.id);
            
            // Subscribe to events
            if (interactAction != null)
            {
                interactAction.performed += OnInteractPerformed;
                interactAction.started += OnInteractStarted;
                interactAction.canceled += OnInteractCanceled;
            }
            if (scrollAction != null)
            {
                scrollAction.performed += OnScrollPerformed;
            }
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
            interactAction.started -= OnInteractStarted;
            interactAction.canceled -= OnInteractCanceled;
        }
        if (scrollAction != null)
        {
            scrollAction.performed -= OnScrollPerformed;
        }
    }
    
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // Handle interact
    }
    
    private void OnScrollPerformed(InputAction.CallbackContext context)
    {
        Vector2 scrollValue = context.ReadValue<Vector2>();
        // Handle scroll
    }
}

### Example 2: Without Singleton - Get PlayerInput Directly

If your project doesn't use a singleton, you can get PlayerInput directly:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class YourScript : MonoBehaviour
{
    [SerializeField]
    private InputActionReference yourActionReference;
    
    private InputAction yourAction;
    private PlayerInput playerInput;
    
    private void Awake()
    {
        // Option 1: Get from same GameObject
        playerInput = GetComponent<PlayerInput>();
        
        // Option 2: Get from parent (if script is on child)
        if (playerInput == null)
        {
            playerInput = GetComponentInParent<PlayerInput>();
        }
        
        // Option 3: Get from player reference
        if (playerInput == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerInput = player.GetComponent<PlayerInput>();
            }
        }
        
        // Option 4: Find in scene (last resort)
        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
        }
        
        if (playerInput == null)
        {
            Debug.LogError("PlayerInput component not found!");
        }
    }
    
    private void OnEnable()
    {
        if (playerInput != null)
        {
            // Get action using the ID from InputActionReference
            yourAction = playerInput.actions.FindAction(yourActionReference.action.id);
            
            // Subscribe to events
            if (yourAction != null)
            {
                yourAction.performed += OnYourActionPerformed;
            }
        }
    }
    
    private void OnDisable()
    {
        if (yourAction != null)
        {
            yourAction.performed -= OnYourActionPerformed;
        }
    }
    
    private void OnYourActionPerformed(InputAction.CallbackContext context)
    {
        // Handle input
    }
}
```

### Example 3: Multiple Actions Without Singleton

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Input References")]
    [SerializeField] private InputActionReference moveActionReference;
    [SerializeField] private InputActionReference jumpActionReference;
    [SerializeField] private InputActionReference interactActionReference;
    
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction interactAction;
    private PlayerInput playerInput;
    
    private void Awake()
    {
        // Get PlayerInput from the same GameObject
        playerInput = GetComponent<PlayerInput>();
        
        if (playerInput == null)
        {
            Debug.LogError("PlayerInput component not found on this GameObject!");
        }
    }
    
    private void OnEnable()
    {
        if (playerInput == null) return;
        
        // Get all actions using their IDs
        moveAction = playerInput.actions.FindAction(moveActionReference.action.id);
        jumpAction = playerInput.actions.FindAction(jumpActionReference.action.id);
        interactAction = playerInput.actions.FindAction(interactActionReference.action.id);
        
        // Subscribe to events
        if (jumpAction != null)
            jumpAction.performed += OnJumpPerformed;
            
        if (interactAction != null)
        {
            interactAction.performed += OnInteractPerformed;
            interactAction.canceled += OnInteractCanceled;
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from all events
        if (jumpAction != null)
            jumpAction.performed -= OnJumpPerformed;
            
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
            interactAction.canceled -= OnInteractCanceled;
        }
    }
    
    private void Update()
    {
        // Read continuous input (movement)
        if (moveAction != null)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            // Process movement
        }
    }
    
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        // Handle jump
    }
    
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // Handle interact start
    }
    
    private void OnInteractCanceled(InputAction.CallbackContext context)
    {
        // Handle interact end
    }
}
```

## Choosing the Right Approach for Getting PlayerInput

### When to Use PlayerInputSingleton

Use the singleton pattern when:
- Multiple scripts across different GameObjects need access to PlayerInput
- You want a centralized, easy-to-access reference
- Your project has a clear player controller structure

**Example**: Current project uses [`PlayerInputSingleton`](Assets/Scripts/Player/PlayerInputSingleton.cs) for gameplay scripts

### When to Get PlayerInput Directly

Get PlayerInput directly when:
- Your script is on the same GameObject as PlayerInput
- You have a direct reference to the player GameObject
- Your project is simple and doesn't need a singleton
- You want to avoid singleton pattern complexity

**Best practice**: Use `GetComponent<PlayerInput>()` when your script is on the player GameObject.

### Comparison

| Approach | Pros | Cons | Best For |
|----------|------|------|----------|
| **Singleton** | Easy access from anywhere, centralized reference | Adds complexity, can be overkill for simple projects | Large projects with many systems |
| **GetComponent** | Simple, efficient, follows Unity best practices | Only works on same GameObject or hierarchy | Scripts on player GameObject |
| **GetComponentInParent** | Works when script is on child of player | Slightly less efficient | UI or effect scripts on player children |
| **FindFirstObjectByType** | Works anywhere in scene | Slow, fragile if multiple PlayerInputs exist | Last resort, initialization only |

## Why This Pattern?

Using `playerInput.actions.FindAction(inputActionReference.action.id)` ensures that:

1. **Correct Action Map**: You get the action from the currently active action map on the PlayerInput component
2. **Dynamic Switching**: If you switch action maps at runtime (e.g., switching between gameplay and UI controls), the reference automatically resolves to the correct action
3. **Inspector Configuration**: You can assign `InputActionReference` in the Inspector for easy configuration
4. **ID Matching**: Using the `action.id` ensures you're getting the exact action referenced, even if action names change
5. **Flexibility**: Works regardless of how you obtain the PlayerInput reference (singleton, GetComponent, etc.)

## Common Mistakes to Avoid

### ❌ Don't use InputActionReference directly for events

```csharp
// WRONG - This won't work correctly with PlayerInput
yourActionReference.action.performed += OnYourActionPerformed;
```

### ❌ Don't use action name string

```csharp
// WRONG - This is fragile and can break if action names change
yourAction = playerInput.actions["Player/YourAction"];
```

### ✅ Use the ID from InputActionReference

```csharp
// CORRECT - Robust and works with dynamic action map switching
yourAction = playerInput.actions.FindAction(yourActionReference.action.id);
```

## Input Action Event Types

- **started**: Fires when a button is pressed or action begins
- **performed**: Fires when an action is performed (can fire multiple times for continuous actions)
- **canceled**: Fires when a button is released or action ends

Use these appropriately:
- Button press: Use `started` for press, `canceled` for release
- Toggle actions: Use `performed` to toggle state
- Continuous input (movement, look): Read values in Update/FixedUpdate

## Related Files

- [`PlayerInputSingleton.cs`](Assets/Scripts/Player/PlayerInputSingleton.cs) - Singleton for accessing PlayerInput
- [`PlayerMovement.cs`](Assets/Scripts/Player/PlayerMovement.cs:112-136) - Movement input handling example
- [`ObjectManipulator.cs`](Assets/Scripts/Telekinesis/ObjectManipulator.cs:99-123) - Interaction input handling example
- [`RadialMenu.cs`](Assets/Scripts/UI/RadialMenu.cs:127-159) - UI input handling example

## Action Map Switching

For switching between different action maps (e.g., gameplay vs UI), see [`RadialMenu.cs`](Assets/Scripts/UI/RadialMenu.cs:288-348):

```csharp
// Store current action map
previousInputActionAsset = playerInput.actions;

// Switch to UI action map
playerInput.actions = uiInputActionAsset;

// Later, restore previous action map
playerInput.actions = previousInputActionAsset;
```
