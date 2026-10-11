using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI
{
	/// <summary>
	///   <para>Custom Editor for the Button Component.</para>
	/// </summary>
	[CanEditMultipleObjects]
	[CustomEditor(typeof(Button2), true)]
	public class Button2Editor : SelectableEditor
	{
		SerializedProperty m_UsePointerEventDataProperty;

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			serializedObject.Update();

			EditorGUILayout.Space();

			m_UsePointerEventDataProperty = serializedObject.FindProperty("m_UsePointerEventData");

			EditorGUILayout.PropertyField(m_UsePointerEventDataProperty, new GUIContent("Use payload ?", "Use event payload associated with pointer (mouse / touch) events as a dynamic parameter ?"));

			if (m_UsePointerEventDataProperty.boolValue)
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnClick2"));
			else
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnClick"));

			serializedObject.ApplyModifiedProperties();
		}
	}
}