using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(AnimatorVarAttribute))]
public class AnimatorVarDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        AnimatorVarAttribute attr = (AnimatorVarAttribute)attribute;
        SerializedProperty animatorProp = property.serializedObject.FindProperty(attr.AnimatorFieldName);
        VisualElement container = new VisualElement();

        List<string> paramNames = GetAnimatorParameterNames(property, attr.AnimatorFieldName);

        PopupField<string> dropdown = new PopupField<string>(
            property.displayName,
            paramNames,
            paramNames.Contains(property.stringValue) ? property.stringValue : paramNames.FirstOrDefault()
        );

        dropdown.RegisterValueChangedCallback(evt =>
        {
            property.stringValue = evt.newValue;
            property.serializedObject.ApplyModifiedProperties();
        });

        container.Add(dropdown);

        int lastAnimatorInstanceID = animatorProp != null ? animatorProp.objectReferenceInstanceIDValue : 0;

        void RefreshDropdown()
        {
            property.serializedObject.UpdateIfRequiredOrScript();
            int currentID = animatorProp?.objectReferenceInstanceIDValue ?? 0;
            if (currentID != lastAnimatorInstanceID)
            {
                lastAnimatorInstanceID = currentID;
                List<string> newNames = GetAnimatorParameterNames(property, attr.AnimatorFieldName);
                dropdown.choices = newNames;
                if (!newNames.Contains(property.stringValue))
                {
                    string newVal = newNames.FirstOrDefault();
                    dropdown.SetValueWithoutNotify(newVal);
                    property.stringValue = newVal;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        container.RegisterCallback<AttachToPanelEvent>(_ =>
        {
            EditorApplication.update += RefreshDropdown;
        });

        container.RegisterCallback<DetachFromPanelEvent>(_ =>
        {
            EditorApplication.update -= RefreshDropdown;
        });

        return container;
    }

    private List<string> GetAnimatorParameterNames(SerializedProperty property, string animatorFieldName)
    {
        Object[] targets = property.serializedObject.targetObjects;
        HashSet<string> names = new HashSet<string>();

        foreach (Object target in targets)
        {
            Component component = target as Component;
            if (component == null)
                continue;

            System.Reflection.FieldInfo field = component.GetType().GetField(
                animatorFieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
            );

            if (field == null || field.FieldType != typeof(Animator))
                continue;

            Animator animator = field.GetValue(component) as Animator;
            if (animator == null)
                continue;

            for (int i = 0; i < animator.parameterCount; i++)
            {
                names.Add(animator.GetParameter(i).name);
            }
        }

        List<string> result = names.OrderBy(n => n).ToList();
        if (result.Count == 0)
            result.Add("(No Parameters)");

        return result;
    }
}
