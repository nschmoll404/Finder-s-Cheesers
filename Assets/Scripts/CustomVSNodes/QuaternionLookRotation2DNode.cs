using UnityEngine;
using Unity.VisualScripting;

// Unit class for the QuaternionLookRotation2D node
[UnitTitle("Quaternion Look Rotation 2D")]
[UnitCategory("Math/Quaternion")]
public class QuaternionLookRotation2DNode : Unit
{
    [DoNotSerialize]
    public ValueInput sourcePosition { get; private set; } // Input for source position

    [DoNotSerialize]
    public ValueInput targetPosition { get; private set; } // Input for target position

    [DoNotSerialize]
    public ValueOutput rotation { get; private set; } // Output for the resulting Quaternion

    protected override void Definition()
    {
        // Define inputs
        sourcePosition = ValueInput<Vector3>("Source Position", Vector3.zero);
        targetPosition = ValueInput<Vector3>("Target Position", Vector3.zero);

        // Define output
        rotation = ValueOutput<Quaternion>("Rotation", ComputeRotation);
    }

    private Quaternion ComputeRotation(Flow flow)
    {
        // Get input values
        Vector3 source = flow.GetValue<Vector3>(sourcePosition);
        Vector3 target = flow.GetValue<Vector3>(targetPosition);

        // Calculate direction vector in 2D (XY plane)
        Vector2 direction = new Vector2(target.x - source.x, target.y - source.y);

        // If direction is zero, return identity quaternion to avoid invalid rotation
        if (direction == Vector2.zero)
        {
            return Quaternion.identity;
        }

        // Calculate the angle to rotate the X-axis (right vector) toward the target
        // In 2D, the X-axis (right) is at 0 degrees in Unity's coordinate system
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Create a Quaternion rotation around the Z-axis
        // No offset needed since we want the X-axis (0 degrees) to point at the target
        return Quaternion.Euler(0f, 0f, angle);
    }
}

//// Optional: Node descriptor for better documentation in the Visual Scripting graph
//[Descriptor(typeof(QuaternionLookRotation2DNode))]
//public class QuaternionLookRotation2DDescriptor : UnitDescriptor<QuaternionLookRotation2DNode>
//{
//    public QuaternionLookRotation2DDescriptor(QuaternionLookRotation2DNode unit) : base(unit) { }

//    protected override string DefinedSummary()
//    {
//        return "Calculates a Quaternion that rotates a GameObject in 2D so its X-axis points toward the target position.";
//    }

//    protected override void DefinedPort(IUnitPort port, UnitPortDescription description)
//    {
//        base.DefinedPort(port, description);
//        if (port.key == nameof(unit.sourcePosition))
//            description.summary = "The position of the GameObject (in world space).";
//        else if (port.key == nameof(unit.targetPosition))
//            description.summary = "The target position to look at (in world space).";
//        else if (port.key == nameof(unit.rotation))
//            description.summary = "The Quaternion rotation that aligns the X-axis toward the target.";
//    }
//}