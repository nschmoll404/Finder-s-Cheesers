using Unity.VisualScripting;
using UnityEngine;

[UnitTitle("Is False")]
[UnitCategory("Boolean")]
public class IsFalseNode : Unit
{
    [DoNotSerialize]
    public ValueInput inputBool { get; private set; }

    [DoNotSerialize]
    public ValueOutput outputBool { get; private set; }

    protected override void Definition()
    {
        inputBool = ValueInput<bool>("Bool", false);
        outputBool = ValueOutput<bool>("IsFalse", (flow) => !flow.GetValue<bool>(inputBool));

        Requirement(inputBool, outputBool);
    }
}