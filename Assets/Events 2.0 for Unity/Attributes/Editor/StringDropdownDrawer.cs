/*
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class StringDropdownDrawer : OdinAttributeDrawer<StringDropdownAttribute>
{
    private List<string> options;

    /// <summary>
    /// Initializes the drawer by resolving dropdown options.
    /// </summary>
    protected override void Initialize()
    {

        var attribute = this.Attribute;

        options = new List<string>();

        // Resolve options from static lists
        if (attribute.StaticOptions != null && attribute.StaticOptions.Length > 0)
        {
            foreach (var optionSet in attribute.StaticOptions)
            {
                if (optionSet != null)
                    options.AddRange(optionSet);
            }
        }

        // Resolve options from methods
        if (attribute.MethodNames != null && attribute.MethodNames.Length > 0)
        {
            foreach (var methodName in attribute.MethodNames)
            {
                var method = this.Property.ParentType.GetMethod(methodName);
                if (method != null && method.ReturnType == typeof(string[]))
                {
                    var result = method.Invoke(this.Property.ParentValues[0], null) as string[];
                    if (result != null)
                        options.AddRange(result);
                }
                else
                {
                    Debug.LogError($"Method {methodName} must return a string[] and be non-static.");
                }
            }
        }

        // Apply sorting if enabled
        if (attribute.SortOptions)
        {
            options = options.OrderBy(option => option).ToList();
        }
    }

    /// <summary>
    /// Draws the dropdown in the Inspector.
    /// </summary>
    protected override void DrawPropertyLayout(GUIContent label)
    {
        if (options == null || options.Count == 0)
        {
            EditorGUILayout.HelpBox("No options available for the dropdown.", MessageType.Error);
            this.CallNextDrawer(label);
            return;
        }

        // Get the current value
        string currentValue = this.Property.ValueEntry.WeakSmartValue as string;
        int currentIndex = options.IndexOf(currentValue);
        if (currentIndex < 0) currentIndex = 0; // Default to the first option if current value is not found

        // Render the dropdown
        currentIndex = EditorGUILayout.Popup(label.text, currentIndex, options.ToArray());

        // Update the value if changed
        if (currentValue != options[currentIndex])
        {
            //for (int i = 0; i < this.Property.ValueEntry.Values.Count; i++)
            {
                //this.Property.ValueEntry.Values[i] = options[currentIndex];
            }
        }
    }
}
*/