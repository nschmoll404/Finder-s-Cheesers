using UnityEngine;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

// Define the custom node class, inheriting from Unit (base class for Visual Scripting nodes)
[UnitTitle("Player Input Find Action")]
[UnitCategory("Input")]
[TypeIcon(typeof(InputAction))] // Icon for the node in the graph
public class PlayerInputFindActionNode : Unit
{
    // Define the trigger input port
    [DoNotSerialize]
    [PortLabelHidden]
    public ControlInput inputTrigger { get; private set; }

    // Define the trigger output port
    [DoNotSerialize]
    [PortLabelHidden]
    public ControlOutput outputTrigger { get; private set; }

    // Define the PlayerInput component input port
    [DoNotSerialize]
    public ValueInput playerInput { get; private set; }

    // Define the InputActionReference input port
    [DoNotSerialize]
    public ValueInput actionReference { get; private set; }

    // Define the InputAction output port
    [DoNotSerialize]
    public ValueOutput inputActionOutput { get; private set; }

    // Initialize the node's ports
    protected override void Definition()
    {
        // Input trigger to start execution (name matches variable: inputTrigger)
        inputTrigger = ControlInput("inputTrigger", (flow) => Trigger(flow));

        // Output trigger to continue the flow (name matches variable: outputTrigger)
        outputTrigger = ControlOutput("outputTrigger");

        // Input for the PlayerInput component
        playerInput = ValueInput<PlayerInput>("playerInput", null);

        // Input for the InputActionReference
        actionReference = ValueInput<InputActionReference>("actionReference", null);

        // Output for the InputAction
        inputActionOutput = ValueOutput<InputAction>("inputAction", (flow) => GetInputAction(flow));
        Succession(inputTrigger, outputTrigger);
    }

    // Logic to execute when the trigger is activated
    private ControlOutput Trigger(Flow flow)
    {
        // Ensure the flow continues to the output trigger
        return outputTrigger;
    }

    // Logic to retrieve the InputAction from the PlayerInput component
    private InputAction GetInputAction(Flow flow)
    {
        // Get the PlayerInput component
        var playerInputComponent = flow.GetValue<PlayerInput>(playerInput);

        // Get the InputActionReference
        var actionRef = flow.GetValue<InputActionReference>(actionReference);

        // Check for valid inputs
        if (playerInputComponent != null && actionRef != null && actionRef.action != null)
        {
            // Find the action in the PlayerInput's action collection using the action ID
            var action = playerInputComponent.actions.FindAction(actionRef.action.id);
            if (action != null)
            {
                return action;
            }
        }

        // Return null if inputs are invalid or action is not found
        return null;
    }
}