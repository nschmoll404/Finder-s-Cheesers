using UnityEngine;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

// Define the custom node class, inheriting from Unit (base class for Visual Scripting nodes)
[UnitTitle("Input Action Read Vector2")]
[UnitCategory("Input")]
[TypeIcon(typeof(Vector2))] // Icon for the node in the graph
public class InputActionReadVector2Node : Unit
{
    // Define the trigger input port
    [DoNotSerialize]
    [PortLabelHidden]
    public ControlInput inputTrigger { get; private set; }

    // Define the trigger output port
    [DoNotSerialize]
    [PortLabelHidden]
    public ControlOutput outputTrigger { get; private set; }

    // Define the InputAction input port
    [DoNotSerialize]
    public ValueInput inputAction { get; private set; }

    // Define the Vector2 output port
    [DoNotSerialize]
    public ValueOutput vector2Output { get; private set; }

    // Initialize the node's ports
    protected override void Definition()
    {
        // Input trigger to start execution (name matches variable: inputTrigger)
        inputTrigger = ControlInput("inputTrigger", (flow) => Trigger(flow));

        // Output trigger to continue the flow (name matches variable: outputTrigger)
        outputTrigger = ControlOutput("outputTrigger");

        // Input for the InputAction
        inputAction = ValueInput<InputAction>("inputAction", null);

        // Output for the Vector2 value
        vector2Output = ValueOutput<Vector2>("vector2", (flow) => GetVector2(flow));
        Succession(inputTrigger, outputTrigger);
    }

    // Logic to execute when the trigger is activated
    private ControlOutput Trigger(Flow flow)
    {
        // Ensure the flow continues to the output trigger
        return outputTrigger;
    }

    // Logic to retrieve the Vector2 value from the InputAction
    private Vector2 GetVector2(Flow flow)
    {
        // Get the InputAction
        var action = flow.GetValue<InputAction>(inputAction);

        // Check for valid input
        if (action != null)
        {
            // Read the Vector2 value from the action
            return action.ReadValue<Vector2>();
        }

        // Return zero vector if the action is invalid
        return Vector2.zero;
    }
}