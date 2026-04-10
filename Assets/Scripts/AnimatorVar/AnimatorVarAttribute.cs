using UnityEngine;

public class AnimatorVarAttribute : PropertyAttribute
{
    public string AnimatorFieldName { get; }

    public AnimatorVarAttribute(string animatorFieldName)
    {
        AnimatorFieldName = animatorFieldName;
    }
}
