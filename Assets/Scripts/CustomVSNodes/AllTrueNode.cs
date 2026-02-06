using UnityEngine;
using Unity.VisualScripting;
using System.Collections.Generic;

[UnitCategory("Control")]
[UnitTitle("AllTrue")]
public class AllTrueNode : Unit
{
    [DoNotSerialize]
    [PortLabelHidden]
    public ControlInput input;

    [DoNotSerialize]
    [PortLabelHidden]
    public ControlOutput output;

    [DoNotSerialize]
    public ValueOutput result;

    // Inspectable field to control number of bool inputs
    [Inspectable, Serialize]
    public int boolCount = 2;

    // Runtime list of ValueInput ports
    private List<ValueInput> boolInputs = new List<ValueInput>();

    protected override void Definition()
    {
        // Clear any previous dynamic ports
        boolInputs.Clear();

        // Define control flow
        input = ControlInput("input", (flow) =>
        {
            bool allTrue = true;

            // Check each bool input
            for (int i = 0; i < boolCount; i++)
            {
                bool value = flow.GetValue<bool>(boolInputs[i]);
                if (!value)
                {
                    allTrue = false;
                    // Early exit optional, but we continue for consistency
                }
            }

            flow.SetValue(result, allTrue);
            return output;
        });

        output = ControlOutput("output");

        // Create the inspectable bool inputs
        for (int i = 0; i < boolCount; i++)
        {
            var boolInput = ValueInput<bool>($"Bool_{i}", false);
            boolInputs.Add(boolInput);
        }

        // Define output
        result = ValueOutput<bool>("All True");

        // Connect flow
        Succession(input, output);
    }
}