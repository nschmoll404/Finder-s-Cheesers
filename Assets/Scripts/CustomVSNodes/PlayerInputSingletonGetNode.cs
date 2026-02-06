using UnityEngine;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using FindersCheesers;

/// <summary>
/// Custom Visual Scripting node that retrieves the PlayerInput component
/// from the PlayerInputSingleton.
/// </summary>
[UnitTitle("Player Input Singleton Get")]
[UnitCategory("Input")]
[TypeIcon(typeof(PlayerInput))]
public class PlayerInputSingletonGetNode : Unit
{
    [DoNotSerialize]
    [PortLabelHidden]
    public ControlInput inputTrigger { get; private set; }

    [DoNotSerialize]
    [PortLabelHidden]
    public ControlOutput outputTrigger { get; private set; }

    [DoNotSerialize]
    public ValueOutput playerInput { get; private set; }

    [DoNotSerialize]
    public ValueOutput isInitialized { get; private set; }

    [DoNotSerialize]
    public ValueOutput singletonInstance { get; private set; }

    protected override void Definition()
    {
        inputTrigger = ControlInput("inputTrigger", (flow) => Trigger(flow));
        outputTrigger = ControlOutput("outputTrigger");

        playerInput = ValueOutput<PlayerInput>("Player Input", (flow) => GetPlayerInput(flow));
        isInitialized = ValueOutput<bool>("Is Initialized", (flow) => GetIsInitialized(flow));
        singletonInstance = ValueOutput<PlayerInputSingleton>("Singleton Instance", (flow) => GetSingletonInstance(flow));

        Succession(inputTrigger, outputTrigger);
    }

    private ControlOutput Trigger(Flow flow)
    {
        return outputTrigger;
    }

    private PlayerInput GetPlayerInput(Flow flow)
    {
        if (PlayerInputSingleton.Instance != null)
        {
            return PlayerInputSingleton.Instance.PlayerInput;
        }
        return null;
    }

    private bool GetIsInitialized(Flow flow)
    {
        return PlayerInputSingleton.IsInitialized();
    }

    private PlayerInputSingleton GetSingletonInstance(Flow flow)
    {
        return PlayerInputSingleton.Instance;
    }
}
