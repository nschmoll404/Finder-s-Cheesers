using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
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
        string currentHash = string.Join(",", paramNames);

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

        void RefreshDropdown()
        {
            property.serializedObject.UpdateIfRequiredOrScript();

            try
            {
                List<string> names = GetAnimatorParameterNames(property, attr.AnimatorFieldName);
                string hash = string.Join(",", names);

                if (hash != currentHash)
                {
                    currentHash = hash;
                    dropdown.choices = names;
                    if (!names.Contains(property.stringValue))
                    {
                        string newVal = names.FirstOrDefault();
                        dropdown.SetValueWithoutNotify(newVal);
                        property.stringValue = newVal;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
            }
            catch
            {
                // Animator controller is being modified, skip this frame
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
            if (animator == null || animator.runtimeAnimatorController == null)
                continue;

            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller == null)
                continue;

            foreach (AnimatorControllerParameter param in controller.parameters)
            {
                names.Add(param.name);
            }
        }

        List<string> result = names.OrderBy(n => n).ToList();
        if (result.Count == 0)
            result.Add("(No Parameters)");

        return result;
    }
}
