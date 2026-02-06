using UnityEngine;
using Unity.VisualScripting;
using System.Collections.Generic;

[UnitCategory("Control")]
[UnitTitle("AllFalse")]
public class AllFalseNode : Unit
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
            bool allFalse = true;

            // Check each bool input
            for (int i = 0; i < boolCount; i++)
            {
                bool value = flow.GetValue<bool>(boolInputs[i]);
                if (value)
                {
                    allFalse = false;
                    // Early exit optional
                }
            }

            flow.SetValue(result, allFalse);
            return output;
        });

        output = ControlOutput("output");

        // Create dynamic bool inputs
        for (int i = 0; i < boolCount; i++)
        {
            var boolInput = ValueInput<bool>($"Bool_{i}", false);
            boolInputs.Add(boolInput);
        }

        // Define output
        result = ValueOutput<bool>("All False");

        // Connect flow
        Succession(input, output);
    }
}