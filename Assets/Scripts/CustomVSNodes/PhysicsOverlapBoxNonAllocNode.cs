using UnityEngine;
using Unity.VisualScripting;
using System.Collections.Generic;

[UnitCategory("Physics")]
[UnitTitle("Physics Overlap Box Non Alloc")]
public class PhysicsOverlapBoxNonAllocNode : Unit
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
    public ValueInput size;

    [DoNotSerialize]
    public ValueInput rotation;

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
            var boxSize = flow.GetValue<Vector3>(size);
            var halfExt = boxSize * 0.5f; // Convert size to half extents
            var rot = flow.GetValue<Quaternion>(rotation);
            var mask = flow.GetValue<LayerMask>(layerMask);

            colliderArray = new Collider[max];
            var hitCount = Physics.OverlapBoxNonAlloc(pos, halfExt, colliderArray, rot, mask);

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
        size = ValueInput<Vector3>("Size", Vector3.one);
        rotation = ValueInput<Quaternion>("Rotation", Quaternion.identity);
        layerMask = ValueInput<LayerMask>("Layer Mask", -1);
        colliders = ValueOutput<Collider[]>("Colliders");
        hasHits = ValueOutput<bool>("Has Hits");

        Succession(input, output);
    }
}