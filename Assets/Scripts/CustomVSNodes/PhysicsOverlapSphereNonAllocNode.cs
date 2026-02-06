using UnityEngine;
using Unity.VisualScripting;
using System.Collections.Generic;

[UnitCategory("Physics")]
[UnitTitle("Physics Overlap Sphere Non Alloc")]
public class PhysicsOverlapSphereNonAllocNode : Unit
{
    [DoNotSerialize]
    [PortLabelHidden]
    public ControlInput input;

    [DoNotSerialize]
    [PortLabelHidden]
    public ControlOutput output;

    [DoNotSerialize]
    public ValueInput maxColliders;

    [DoNotSerialize]
    public ValueInput center;

    [DoNotSerialize]
    public ValueInput radius;

    [DoNotSerialize]
    public ValueInput layerMask;

    [DoNotSerialize]
    public ValueOutput colliders;

    [DoNotSerialize]
    public ValueOutput hasHits;

    private Collider[] colliderArray;

    protected override void Definition()
    {
        input = ControlInput("", (flow) =>
        {
            var max = flow.GetValue<int>(maxColliders);
            var pos = flow.GetValue<Vector3>(center);
            var rad = flow.GetValue<float>(radius);
            var mask = flow.GetValue<LayerMask>(layerMask);

            colliderArray = new Collider[max];
            var hitCount = Physics.OverlapSphereNonAlloc(pos, rad, colliderArray, mask);

            // Filter out null colliders and create a new array with only valid hits
            var validColliders = new Collider[hitCount];
            for (int i = 0; i < hitCount; i++)
            {
                validColliders[i] = colliderArray[i];
            }

            flow.SetValue(colliders, validColliders);
            flow.SetValue(hasHits, hitCount > 0);

            return output;
        });

        output = ControlOutput("");

        maxColliders = ValueInput<int>("Max Colliders", 10);
        center = ValueInput<Vector3>("Center", Vector3.zero);
        radius = ValueInput<float>("Radius", 1f);
        layerMask = ValueInput<LayerMask>("Layer Mask", -1);
        colliders = ValueOutput<Collider[]>("Colliders");
        hasHits = ValueOutput<bool>("Has Hits");

        Succession(input, output);
    }
}