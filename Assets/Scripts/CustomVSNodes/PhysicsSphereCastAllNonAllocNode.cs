using UnityEngine;
using Unity.VisualScripting;
using System.Collections.Generic;

[UnitCategory("Physics")]
[UnitTitle("Physics Sphere Cast All Non Alloc")]
public class PhysicsSphereCastAllNonAllocNode : Unit
{
    [DoNotSerialize]
    [PortLabelHidden]
    public ControlInput input;

    [DoNotSerialize]
    [PortLabelHidden]
    public ControlOutput output;

    [DoNotSerialize]
    public ValueInput maxHits;

    [DoNotSerialize]
    public ValueInput origin;

    [DoNotSerialize]
    public ValueInput radius;

    [DoNotSerialize]
    public ValueInput direction;

    [DoNotSerialize]
    public ValueInput maxDistance;

    [DoNotSerialize]
    public ValueInput layerMask;

    [DoNotSerialize]
    public ValueOutput hits;

    [DoNotSerialize]
    public ValueOutput hasHits;

    private RaycastHit[] hitArray;

    protected override void Definition()
    {
        input = ControlInput("", (flow) =>
        {
            var max = flow.GetValue<int>(maxHits);
            var orig = flow.GetValue<Vector3>(origin);
            var rad = flow.GetValue<float>(radius);
            var dir = flow.GetValue<Vector3>(direction);
            var dist = flow.GetValue<float>(maxDistance);
            var mask = flow.GetValue<LayerMask>(layerMask);

            hitArray = new RaycastHit[max];
            var hitCount = Physics.SphereCastNonAlloc(orig, rad, dir, hitArray, dist, mask);

            // Filter out invalid hits and create a new array with only valid hits
            var validHits = new RaycastHit[hitCount];
            for (int i = 0; i < hitCount; i++)
            {
                validHits[i] = hitArray[i];
            }

            flow.SetValue(hits, validHits);
            flow.SetValue(hasHits, hitCount > 0);

            return output;
        });

        output = ControlOutput("");

        maxHits = ValueInput<int>("Max Hits", 10);
        origin = ValueInput<Vector3>("Origin", Vector3.zero);
        radius = ValueInput<float>("Radius", 1f);
        direction = ValueInput<Vector3>("Direction", Vector3.forward);
        maxDistance = ValueInput<float>("Max Distance", Mathf.Infinity);
        layerMask = ValueInput<LayerMask>("Layer Mask", -1);
        hits = ValueOutput<RaycastHit[]>("Hits");
        hasHits = ValueOutput<bool>("Has Hits");

        Succession(input, output);
    }
}