using System;
using UnityEditor;

namespace UnityEngine.EventSystems
{
	/// <summary>
	/// Custom Editor for the EventTrigger2 Component.
	/// </summary>
	[CustomEditor(typeof(EventTrigger2), true)]
	public class EventTrigger2Editor : Editor
	{
		SerializedProperty m_ScriptProp;

		private SerializedProperty m_DelegatesProperty;
		private GUIContent m_IconToolbarMinus;
		private GUIContent m_EventIDName;
		private GUIContent[] m_EventTypes;
		private GUIContent m_AddButtonContent;

		protected virtual void OnEnable()
		{
			m_ScriptProp = serializedObject.FindProperty("m_Script");

			m_DelegatesProperty = serializedObject.FindProperty("m_Delegates");

			m_AddButtonContent = new GUIContent("Add New Event Type");
			m_EventIDName = new GUIContent(string.Empty);
			m_IconToolbarMinus = new GUIContent(EditorGUIUtility.IconContent("Toolbar Minus"));
			m_IconToolbarMinus.tooltip = "Remove this event handler.";

			string[] names = Enum.GetNames(typeof(EventTriggerType));
			m_EventTypes = new GUIContent[names.Length];
			for (int index = 0; index < names.Length; index++)
				m_EventTypes[index] = new GUIContent(names[index]);
		}

		public override void OnInspectorGUI()
		{
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
			serializedObject.Update();

			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(m_ScriptProp);
			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.Space();

			int toBeRemovedEntry = -1;
			Vector2 vector2 = GUIStyle.none.CalcSize(m_IconToolbarMinus);

			for (int index = 0; index < m_DelegatesProperty.arraySize; ++index)
			{
				SerializedProperty arrayElementAtIndex = m_DelegatesProperty.GetArrayElementAtIndex(index);

				SerializedProperty eventIDProp = arrayElementAtIndex.FindPropertyRelative("eventID");
				SerializedProperty callbackProp = arrayElementAtIndex.FindPropertyRelative("callback");

				m_EventIDName.text = eventIDProp.enumDisplayNames[eventIDProp.enumValueIndex];

				EditorGUILayout.PropertyField(callbackProp, m_EventIDName);

				Rect lastRect = GUILayoutUtility.GetLastRect();
				if (GUI.Button(new Rect(lastRect.xMax - vector2.x - 20.0f, lastRect.y + 1f, vector2.x, vector2.y), m_IconToolbarMinus, GUIStyle.none))
					toBeRemovedEntry = index;

				EditorGUILayout.Space();
			}

			if (toBeRemovedEntry > -1)
				RemoveEntry(toBeRemovedEntry);

			Rect rect = GUILayoutUtility.GetRect(m_AddButtonContent, GUI.skin.button);
			rect.x = rect.x + ((rect.width - 200.0f) / 2.0f);
			rect.width = 200f;

			if (GUI.Button(rect, m_AddButtonContent))
				ShowAddTriggerMenu();

			// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
			serializedObject.ApplyModifiedProperties();
		}

		private void RemoveEntry(int toBeRemovedEntry)
		{
			m_DelegatesProperty.DeleteArrayElementAtIndex(toBeRemovedEntry);
		}

		private void ShowAddTriggerMenu()
		{
			GenericMenu genericMenu = new GenericMenu();
			for (int index1 = 0; index1 < m_EventTypes.Length; ++index1)
			{
				bool flag = true;
				for (int index2 = 0; index2 < m_DelegatesProperty.arraySize; ++index2)
				{
					if (m_DelegatesProperty.GetArrayElementAtIndex(index2).FindPropertyRelative("eventID").enumValueIndex == index1)
						flag = false;
				}
				if (flag)
					genericMenu.AddItem(m_EventTypes[index1], false, new GenericMenu.MenuFunction2(OnAddNewSelected), (object)index1);
				else
					genericMenu.AddDisabledItem(m_EventTypes[index1]);
			}

			genericMenu.ShowAsContext();

			Event.current.Use();
		}

		private void OnAddNewSelected(object index)
		{
			int num = (int)index;

			m_DelegatesProperty.GetArrayElementAtIndex(++m_DelegatesProperty.arraySize - 1).FindPropertyRelative("eventID").enumValueIndex = num;

			serializedObject.ApplyModifiedProperties();
		}
	}
}