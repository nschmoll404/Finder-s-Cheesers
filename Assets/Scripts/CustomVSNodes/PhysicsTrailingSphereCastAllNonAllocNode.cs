using UnityEngine;
using Unity.VisualScripting;
using System.Collections.Generic;

[UnitCategory("Physics")]
[UnitTitle("Physics Trailing Sphere Cast All Non Alloc")]
public class PhysicsTrailingSphereCastAllNonAllocNode : Unit
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
    public ValueInput layerMask;

    [DoNotSerialize]
    public ValueOutput hits;
    
    [DoNotSerialize]
    public ValueOutput firstHit;

    [DoNotSerialize]
    public ValueOutput firstGameObject;

    [DoNotSerialize]
    public ValueOutput firstLayer;

    [DoNotSerialize]
    public ValueOutput firstTag;

    [DoNotSerialize]
    public ValueOutput hasHits;

    private Vector3 lastPos;
    private RaycastHit[] hitArray;

    protected override void Definition()
    {
        input = ControlInput("", (flow) =>
        {
            var max = flow.GetValue<int>(maxHits);
            var orig = flow.GetValue<Vector3>(origin);
            var rad = flow.GetValue<float>(radius);
            var mask = flow.GetValue<LayerMask>(layerMask);

            if (lastPos == Vector3.zero)
            {
                lastPos = orig;
            }

            var dir = (orig - lastPos).normalized;
            lastPos = orig;
            var dist = Vector3.Distance(orig, lastPos);

            hitArray = new RaycastHit[max];
            var hitCount = Physics.SphereCastNonAlloc(orig, rad, dir, hitArray, dist, mask);

            // Filter out invalid hits and create a new array with only valid hits
            var validHits = new RaycastHit[hitCount];
            bool hasHit = false;
            RaycastHit firstHit = default;
            GameObject firstGO = null;
            int firstLayer = -1;
            string firstTag = null;

            for (int i = 0; i < hitCount; i++)
            {
                validHits[i] = hitArray[i];
            }

            hasHit = hitCount > 0;
            if (hasHit)
            {
                firstHit = hitArray[0];
                firstGO = firstHit.collider.gameObject;
                firstLayer = firstHit.collider.gameObject.layer;
                firstTag = firstHit.collider.tag;
            }

            flow.SetValue(hits, validHits);
            flow.SetValue(hasHits, hasHit);
            flow.SetValue(this.firstHit, firstHit);
            flow.SetValue(this.firstGameObject, firstGO);
            flow.SetValue(this.firstLayer, firstLayer);
            flow.SetValue(this.firstTag, firstTag);

            return output;
        });

        output = ControlOutput("");

        maxHits = ValueInput<int>("Max Hits", 10);
        origin = ValueInput<Vector3>("Origin", Vector3.zero);
        radius = ValueInput<float>("Radius", 1f);
        layerMask = ValueInput<LayerMask>("Layer Mask", -1);
        hits = ValueOutput<RaycastHit[]>("Hits");
        hasHits = ValueOutput<bool>("Has Hits");
        firstHit = ValueOutput<RaycastHit>("First Hit");
        firstGameObject = ValueOutput<GameObject>("First GO");
        firstLayer = ValueOutput<int>("First Layer");
        firstTag = ValueOutput<string>("First Tag");

        Succession(input, output);
    }
}