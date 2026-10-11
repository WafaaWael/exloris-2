using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI
{
	[CustomEditor(typeof(Dropdown2), true)]
	[CanEditMultipleObjects]
	public class Dropdown2Editor : SelectableEditor
	{
		SerializedProperty m_Template;
		SerializedProperty m_CaptionText;
		SerializedProperty m_CaptionImage;
		SerializedProperty m_ItemText;
		SerializedProperty m_ItemImage;
		SerializedProperty m_Value;
		SerializedProperty m_Options;
		SerializedProperty m_CallbackType;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_Template = serializedObject.FindProperty("m_Template");
			m_CaptionText = serializedObject.FindProperty("m_CaptionText");
			m_CaptionImage = serializedObject.FindProperty("m_CaptionImage");
			m_ItemText = serializedObject.FindProperty("m_ItemText");
			m_ItemImage = serializedObject.FindProperty("m_ItemImage");
			m_Value = serializedObject.FindProperty("m_Value");
			m_Options = serializedObject.FindProperty("m_Options");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			serializedObject.Update();

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(m_Template);
			EditorGUILayout.PropertyField(m_CaptionText);
			EditorGUILayout.PropertyField(m_CaptionImage);
			EditorGUILayout.PropertyField(m_ItemText);
			EditorGUILayout.PropertyField(m_ItemImage);
			EditorGUILayout.PropertyField(m_Value);
			EditorGUILayout.PropertyField(m_Options);

			m_CallbackType = serializedObject.FindProperty("m_CallbackType");

			EditorGUILayout.PropertyField(m_CallbackType, new GUIContent("Callback Type", "The type of callback when dropdown value changes"));

			Dropdown2.CallbackType callbackType = (Dropdown2.CallbackType)m_CallbackType.intValue;

			switch (callbackType)
			{
				case Dropdown2.CallbackType.INDEX:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnValueChanged"));
					break;
				case Dropdown2.CallbackType.LABEL:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnLabelChanged"));
					break;
				case Dropdown2.CallbackType.SPRITE:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnSpriteChanged"));
					break;
				case Dropdown2.CallbackType.INDEX_LABEL:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnValueLabelChanged"));
					break;
				case Dropdown2.CallbackType.INDEX_SPRITE:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnValueSpriteChanged"));
					break;
				case Dropdown2.CallbackType.LABEL_SPRITE:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnLabelSpriteChanged"));
					break;
				case Dropdown2.CallbackType.ALL:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnValueLabelSpriteChanged"));
					break;
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}