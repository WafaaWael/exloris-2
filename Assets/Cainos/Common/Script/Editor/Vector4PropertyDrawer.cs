using UnityEngine;
using UnityEditor;

namespace Cainos.Common
{

    [CustomPropertyDrawer(typeof(Vector4))]
    public class Vector4PropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw "My Vector4" label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Unity-like spacing values
            float componentSpacing = 4f;      // between X | Y | Z | W blocks
            float labelFieldSpacing = 4f;     // space between "X" and the float field
            float labelWidth = 12f;           // small component label ("X", "Y", etc.)

            // Compute block widths
            float totalSpacing = componentSpacing * 3;
            float totalWidth = position.width - totalSpacing;
            float blockWidth = totalWidth / 4f;

            // Helper for block rect
            Rect Block(int index)
            {
                float x = position.x + index * (blockWidth + componentSpacing);
                return new Rect(x, position.y, blockWidth, position.height);
            }

            // X block
            Rect bx = Block(0);
            Rect lx = new Rect(bx.x, bx.y, labelWidth, bx.height);
            Rect fx = new Rect(bx.x + labelWidth + labelFieldSpacing, bx.y,
                               bx.width - (labelWidth + labelFieldSpacing), bx.height);

            // Y block
            Rect by = Block(1);
            Rect ly = new Rect(by.x, by.y, labelWidth, by.height);
            Rect fy = new Rect(by.x + labelWidth + labelFieldSpacing, by.y,
                               by.width - (labelWidth + labelFieldSpacing), by.height);

            // Z block
            Rect bz = Block(2);
            Rect lz = new Rect(bz.x, bz.y, labelWidth, bz.height);
            Rect fz = new Rect(bz.x + labelWidth + labelFieldSpacing, bz.y,
                               bz.width - (labelWidth + labelFieldSpacing), bz.height);

            // W block
            Rect bw = Block(3);
            Rect lw = new Rect(bw.x, bw.y, labelWidth, bw.height);
            Rect fw = new Rect(bw.x + labelWidth + labelFieldSpacing, bw.y,
                               bw.width - (labelWidth + labelFieldSpacing), bw.height);

            // Draw component labels
            EditorGUI.LabelField(lx, "X");
            EditorGUI.LabelField(ly, "Y");
            EditorGUI.LabelField(lz, "Z");
            EditorGUI.LabelField(lw, "W");

            // Draw fields
            EditorGUI.PropertyField(fx, property.FindPropertyRelative("x"), GUIContent.none);
            EditorGUI.PropertyField(fy, property.FindPropertyRelative("y"), GUIContent.none);
            EditorGUI.PropertyField(fz, property.FindPropertyRelative("z"), GUIContent.none);
            EditorGUI.PropertyField(fw, property.FindPropertyRelative("w"), GUIContent.none);

            EditorGUI.indentLevel = oldIndent;
            EditorGUI.EndProperty();
        }
    }
}
