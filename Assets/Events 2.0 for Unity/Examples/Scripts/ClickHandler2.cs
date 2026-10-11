using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 
/// </summary>
public class ClickHandler2 : MonoBehaviour, IPointerClickHandler
{
	[System.Serializable]
	public class PointerEvents2 : UnityEvent2<PointerEventData> { }

	/// <summary>
	/// 
	/// </summary>
	[SerializeField]
	private Text m_LogText = null;

	/// <summary>
	/// 
	/// </summary>
	[SerializeField]
	[Tooltip("Regular events (This tooltip will appear on Inspector)")]
	private UnityEvent2 unityEvents2 = null;

	/// <summary>
	/// 
	/// </summary>
	[SerializeField]
	[Tooltip("PointerEventData dynamic events (This tooltip will appear on Inspector)")]
	private PointerEvents2 pointerEvents2 = null;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="message"></param>
	/// <param name="args"></param>
	void Log(string message, params object[] args)
	{
		Debug.LogFormat(this, message, args);
		m_LogText.text += string.Format(message, args);
		m_LogText.text += System.Environment.NewLine;
		m_LogText.text += System.Environment.NewLine;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="eventData"></param>
	public void OnPointerClick(PointerEventData eventData)
	{
		unityEvents2.Invoke();
		pointerEvents2.Invoke(eventData);
	}

	public void Test()
	{
		Log("Test method. No params");
	}

	public void TestCharArray(char[] array)
	{
		Log("TestCharArray method. array length:{0}, values:{1}", array.Length, string.Join(",", array.Select(x => x == char.MinValue ? string.Empty : x.ToString()).ToArray()));
	}

	public void TestCharList(List<char> list)
	{
		Log("TestCharList method. list count:{0}, values:{1}", list.Count, string.Join(",", list.Select(x => x == char.MinValue ? string.Empty : x.ToString()).ToArray()));
	}

	public void Test(float f1, float f2)
	{
		Log("Test method. float param1:{0} - float param2:{1}", f1, f2);
	}

	public void Test(int int1, int int2, int int3)
	{
		Log("Test method. int param1:{0} - int param2:{1} - int param3:{2}", int1, int2, int3);
	}

	public void Test(string string1, string string2, string string3)
	{
		Log("Test method. string param1:{0} - string param2:{1} - string param3:{2}", string1, string2, string3);
	}

	[CustomInspector("TestCustomInspector")]
	public void TestCustom(int intParam, string stringParam)
	{
		Log("TestCustom method. int param:{0} - string param:{1}", intParam, stringParam);
	}

#if UNITY_EDITOR
	/// <summary>
	/// 
	/// </summary>
	/// <param name="arguments">The arguments from TestCustom method above</param>
	/// <param name="argNameRect">The rectangle for the argument name</param>
	/// <param name="argRect">The rectangle for the argument</param>
	public void TestCustomInspector(SerializedProperty arguments, Rect argNameRect, Rect argRect)
	{
		// Here, we get the first argument (aka intParam) from TestMethod
		var intParam = arguments.GetArrayElementAtIndex(0);

		// Since intParam is an integer,
		// we need to get the value through m_IntArgument field
		// Check ArgumentCache2 under UnityEvent2.cs
		intParam = intParam.FindPropertyRelative("m_IntArgument");

		EditorGUI.LabelField(argNameRect, new GUIContent("My Awesome Int:", "My awesome integer argument"));
		EditorGUI.PropertyField(argRect, intParam, GUIContent.none);

		// DON'T FORGET TO INCREASE THE Y AXIS OF BOTH RECTS for each argument
		argNameRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		argRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

		// Here, we get the second argument (aka stringParam) from TestMethod
		var stringParam = arguments.GetArrayElementAtIndex(1);

		// Since stringParam is a string,
		// we need to get the value through m_StringArgument field
		// Check ArgumentCache2 under UnityEvent2.cs
		stringParam = stringParam.FindPropertyRelative("m_StringArgument");

		EditorGUI.LabelField(argNameRect, new GUIContent("My Awesome String:", "My awesome string argument"));
		EditorGUI.PropertyField(argRect, stringParam, GUIContent.none);

		// Don't need to increase the Y axis of both rects after last argument
		//argNameRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		//argRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
	}
#endif

	public void TestCustom([CustomInspector("TestCustomInspectorForInteger")] int intParam, string stringParam, [CustomInspector("TestCustomInspectorForFloat")] float floatParam)
	{
		Log("TestCustom method. int param:{0} - string param:{1}, floatParam:{2}", intParam, stringParam, floatParam);
	}

#if UNITY_EDITOR
	/// <summary>
	/// 
	/// </summary>
	/// <param name="argument">The integer argument from TestCustom method above</param>
	/// <param name="argNameRect">The rectangle for the argument name</param>
	/// <param name="argRect">The rectangle for the argument</param>
	public void TestCustomInspectorForInteger(SerializedProperty argument, Rect argNameRect, Rect argRect)
	{
		EditorGUI.LabelField(argNameRect, new GUIContent("My Awesome Int:", "My awesome integer argument"));
		EditorGUI.PropertyField(argRect, argument, GUIContent.none);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="argument">The float argument from TestCustom method above</param>
	/// <param name="argNameRect">The rectangle for the argument name</param>
	/// <param name="argRect">The rectangle for the argument</param>
	public void TestCustomInspectorForFloat(SerializedProperty argument, Rect argNameRect, Rect argRect)
	{
		EditorGUI.LabelField(argNameRect, new GUIContent("My Awesome Float:", "My awesome float argument"));
		EditorGUI.PropertyField(argRect, argument, GUIContent.none);
	}
#endif

	public void Test(EnumExample enumExample1, EnumExample enumExample2, EnumExample enumExample3)
	{
		Log("Test method. enum param1:{0} - enum param2:{1} - enum param3:{2}", enumExample1, enumExample2, enumExample3);
	}

	public void Test(GameObject gameObject1, GameObject gameObject2)
	{
		Log("Test method. GameObject param1: {0} - GameObject param2: {1}", gameObject1, gameObject2);
	}

	public void Test(int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Log("Test method. int: {0} - string: {1} - GameObject: {2} - Enum: {3}", intParam, stringParam, gameObject, enumExample);
	}

	public void Test(Vector2 vector2)
	{
		Log("Test method. Vector2 param:{0}", vector2);
	}

	public void Test(Vector3 vector3)
	{
		Log("Test method. Vector3 param:{0}", vector3);
	}

	public void Test(Vector4 vector4)
	{
		Log("Test method. Vector4 param:{0}", vector4);
	}

	public void TestLayer([Layer] int layer)
	{
		Log("TestLayer method. layer index:{0} - name:{1}", layer, LayerMask.LayerToName(layer));
	}

	[Layer("Ignore Raycast")]
	public void TestLayer(int layer1, [Layer(0)] int layer2)
	{
		Log("TestLayer method. layer1 index:{0} - name:{1} - layer2 index:{2} - name:{3}", layer1, LayerMask.LayerToName(layer1), layer2, LayerMask.LayerToName(layer2));
	}

	public void TestColor(Color color)
	{
		Log("TestColor method. color param:{0}", color);
	}

	public void TestLayerMask(LayerMask layerMask)
	{
		List<string> layersList = new List<string>();
		for (int i = 0; i < 32; i++)
		{
			if ((layerMask.value & 1 << i) > 0 && !string.IsNullOrEmpty(LayerMask.LayerToName(i)))
				layersList.Add(LayerMask.LayerToName(i));
		}
		string layers = string.Join(",", layersList.ToArray());
		Log("TestLayerMask method. Selected layers from layerMask:{0}", layers);
	}

	public void TestQuaternion(Quaternion quaternion)
	{
		Log("Test method. Quaternion param:{0}", quaternion);
	}

	[Slider(0f, 10f)]
	public void TestSlider(float float1)
	{
		Log("TestSlider method. float param:{0}", float1);
	}

	[Slider(0f, 10f)]
	public void TestSlider(float float1, float float2)
	{
		Log("TestSlider method. float param1:{0} - float param2:{1}", float1, float2);
	}

	public void TestSlider([Slider(0f, 5f)] float float1, float float2, [Slider(0f, 10f)] float float3)
	{
		Log("TestSlider method. float param1:{0} - float param2:{1} - float param2:{2}", float1, float2, float3);
	}

	[IntSlider(0, 10)]
	public void TestIntSlider(int int1)
	{
		Log("TestIntSlider method. int param:{0}", int1);
	}

	[IntSlider(0, 10)]
	public void TestIntSlider(int int1, int int2)
	{
		Log("TestIntSlider method. int param1:{0} - int param2:{1}", int1, int2);
	}

	public void TestIntSlider([IntSlider(0, 5)] int int1, int int2, [IntSlider(0, 10)] int int3)
	{
		Log("TestIntSlider method. int param1:{0} - int param2:{1} - int param2:{2}", int1, int2, int3);
	}

	public void Test(PointerEventData eventData)
	{
		Log("Test method. Dynamic PointerEventData param:{0}", eventData);
	}

	public void Test(PointerEventData eventData, string stringParam)
	{
		Log("Test method. string param:{1} - Dynamic PointerEventData param:{0}", eventData, stringParam);
	}

	public void Test(PointerEventData eventData, int intParam)
	{
		Log("Test method. int param:{1} - Dynamic PointerEventData param:{0}", eventData, intParam);
	}

	public void Test(PointerEventData eventData, int[] array)
	{
		Log("Test method. array length:{0}, values:{1}, Dynamic PointerEventData param:{2}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()), eventData);
	}

	public void Test(PointerEventData eventData, int[] array, List<string> list)
	{
		Log("Test method. array length:{0}, values:{1}, list count:{2}, values:{3}, Dynamic PointerEventData param:{4}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()), list.Count, string.Join(",", list.ToArray()), eventData);
	}

	public void TestListInt(List<int> list)
	{
		Log("TestListInt method. list count:{0}, values:{1}", list.Count, string.Join(",", list.Select(x => x.ToString()).ToArray()));
	}

	public void TestListFloat(List<float> list)
	{
		Log("TestListFloat method. list count:{0}, values:{1}", list.Count, string.Join(",", list.Select(x => x.ToString()).ToArray()));
	}

	public void TestListString(List<string> list)
	{
		Log("TestListString method. list count:{0}, values:{1}", list.Count, string.Join(",", list.ToArray()));
	}

	public void TestListBool(List<bool> list)
	{
		Log("TestListBool method. list count:{0}, values:{1}", list.Count, string.Join(",", list.Select(x => x.ToString()).ToArray()));
	}

	public void TestListEnumExample(List<EnumExample> list)
	{
		Log("TestListEnumExample method. list count:{0}, values:{1}", list.Count, string.Join(",", list.Select(x => x.ToString()).ToArray()));
	}

	public void TestListVector2(List<Vector2> list)
	{
		Log("TestListVector2 method. list count:{0}, values:{1}", list.Count, string.Join(",", list.Select(x => x.ToString()).ToArray()));
	}

	public void TestListVector3(List<Vector3> list)
	{
		Log("TestListVector3 method. list count:{0}, values:{1}", list.Count, string.Join(",", list.Select(x => x.ToString()).ToArray()));
	}

	public void TestListVector4(List<Vector4> list)
	{
		Log("TestListVector4 method. list count:{0}, values:{1}", list.Count, string.Join(",", list.Select(x => x.ToString()).ToArray()));
	}

	public void TestListLayerMask(List<LayerMask> list)
	{
		Log("TestListLayerMask method. list count:{0}, values:{1}", list.Count, string.Join(",", list.Select(x => x.ToString()).ToArray()));
	}

	public void TestListColor(List<Color> list)
	{
		Log("TestListColor method. list count:{0}, values:{1}", list.Count, string.Join(",", list.Select(x => x.ToString()).ToArray()));
	}

	public void TestListQuaternion(List<Quaternion> list)
	{
		Log("TestListQuaternion method. list count:{0}, values:{1}", list.Count, string.Join(",", list.Select(x => x.ToString()).ToArray()));
	}

	public void TestListGameObject(List<GameObject> list)
	{
		Log("TestListGameObject method. list count:{0}, values:{1}", list.Count, string.Join(",", list.Select(x => x.name).ToArray()));
	}

	public void TestArrayInt(int[] array)
	{
		Log("TestArrayInt method. array length:{0}, values:{1}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()));
	}

	public void TestArrayFloat(float[] array)
	{
		Log("TestArrayFloat method. list count:{0}, values:{1}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()));
	}

	public void TestArrayString(string[] array)
	{
		Log("TestArrayString method. list count:{0}, values:{1}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()));
	}

	public void TestArrayBool(bool[] array)
	{
		Log("TestArrayBool method. list count:{0}, values:{1}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()));
	}

	public void TestArrayEnumExample(EnumExample[] array)
	{
		Log("TestArrayEnumExample method. list length:{0}, values:{1}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()));
	}

	public void TestArrayVector2(Vector2[] array)
	{
		Log("TestArrayVector2 method. list count:{0}, values:{1}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()));
	}

	public void TestArrayVector3(Vector3[] array)
	{
		Log("TestArrayVector3 method. list count:{0}, values:{1}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()));
	}

	public void TestArrayVector4(Vector4[] array)
	{
		Log("TestArrayVector4 method. list count:{0}, values:{1}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()));
	}

	public void TestArrayLayerMask(LayerMask[] array)
	{
		Log("TestArrayLayerMask method. list count:{0}, values:{1}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()));
	}

	public void TestArrayColor(Color[] array)
	{
		Log("TestArrayColor method. list count:{0}, values:{1}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()));
	}

	public void TestArrayQuaternion(Quaternion[] array)
	{
		Log("TestArrayQuaternion method. list count:{0}, values:{1}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()));
	}

	public void TestArrayGameObject(GameObject[] array)
	{
		Log("TestArrayGameObject method. array length:{0}, values:{1}", array.Length, string.Join(",", array.Select(x => x.name).ToArray()));
	}

	public void TestStringArrayInt(int[] array, string stringParam)
	{
		Log("TestStringArrayInt method. array length:{0}, array values:{1}, stringParam:{2}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()), stringParam);
	}

	public void TestStringArrayInt(string stringParam, int[] array)
	{
		Log("TestStringArrayInt method. stringParam:{0}, array length:{1}, array values:{2}", stringParam, array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()));
	}

	public void TestStringArrayInt(int[] array, string stringParam, List<string> list, float floatParam)
	{
		Log("TestStringArrayInt method. array length:{0}, array values:{1}, stringParam:{2}, list count:{3}, list values:{4}, floatParam:{5}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()), stringParam, list.Count, string.Join(",", list.ToArray()), floatParam);
	}

	public void TestStringArrayInt(GameObject[] array, string stringParam, List<GameObject> list, float floatParam)
	{
		Log("TestStringArrayInt method. array length:{0}, array values:{1}, stringParam:{2}, list count:{3}, list values:{4}, floatParam:{5}", array.Length, string.Join(",", array.Select(x => x.name).ToArray()), stringParam, list.Count, string.Join(",", list.Select(x => x.name).ToArray()), floatParam);
	}

	public void TestArrayIntListString(int[] array, List<string> list)
	{
		Log("TestArrayIntListString method. array length:{0}, array values:{1}, list count:{2}, list values:{3}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()), list.Count, string.Join(",", list.ToArray()));
	}

	public void TestEnumArrayEnumList(EnumExample[] array, List<EnumExample> list)
	{
		Log("TestEnumArrayEnumList method. array length:{0}, array values:{1}, list count:{2}, list values:{3}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()), list.Count, string.Join(",", list.Select(x => x.ToString()).ToArray()));
	}

	public void TestVector3ArrayVector3List(Vector3[] array, List<Vector3> list)
	{
		Log("TestVector3ArrayVector3List method. array length:{0}, array values:{1}, list count:{2}, list values:{3}", array.Length, string.Join(",", array.Select(x => x.ToString()).ToArray()), list.Count, string.Join(",", list.Select(x => x.ToString()).ToArray()));
	}

	public void TestScriptableObjectExample(ScriptableObjectExample soe1, ScriptableObjectExample soe2)
	{
		Log("TestScriptableObjectExample method. soe1:{0}, soe2:{1}", soe1, soe2);
	}

	public void TestArrayScriptableObjectExample(ScriptableObjectExample[] soes)
	{
		Log("TestArrayScriptableObjectExample method. soes length:{0}, soes values:{1}", soes.Length, string.Join(",", soes.Select(x => string.Format("name:{0}, values:{1}", x.name, x.ToString())).ToArray()));
	}

	public void TestListScriptableObjectExample(List<ScriptableObjectExample> soes)
	{
		Log("TestArrayScriptableObjectExample method. soes count:{0}, soes values:{1}", soes.Count, string.Join(",", soes.Select(x => string.Format("name:{0}, values:{1}", x.name, x.ToString())).ToArray()));
	}

	// THE METHOD BELOW IS NOT AVAILABLE TO CHOOSE
	public void TestArrayList(ArrayList list)
	{
		Log("TestArrayList method. list param:{0}", list);
	}

	// Properties created for IL2CPP Scripting Backend

	/// <summary>
	/// Created for <see cref="Test(float, float)"/>
	/// </summary>
	private UpdatableInvokableCall<float, float> _Unused0 { get; set; }

	/// <summary>
	/// Created for <see cref="Test(int, int, int)"/>
	/// </summary>
	private UpdatableInvokableCall<int, int, int> _Unused1 { get; set; }

	/// <summary>
	/// Created for <see cref="Test(string, string, string)"/>
	/// </summary>
	private UpdatableInvokableCall<string, string, string> _Unused2 { get; set; }

	/// <summary>
	/// Created for <see cref="Test(EnumExample, EnumExample, EnumExample)"/>
	/// </summary>
	private UpdatableInvokableCall<EnumExample, EnumExample, EnumExample> _Unused3 { get; set; }

	/// <summary>
	/// Created for <see cref="Test(GameObject, GameObject)"/>
	/// </summary>
	private UpdatableInvokableCall<GameObject, GameObject> _Unused4 { get; set; }

	/// <summary>
	/// Created for <see cref="Test(int, string, GameObject, EnumExample)"/>
	/// </summary>
	private UpdatableInvokableCall<int, string, GameObject, EnumExample> _Unused5 { get; set; }

	/// <summary>
	/// Created for <see cref="TestLayer(int, int)"/>
	/// </summary>
	private UpdatableInvokableCall<int, int> _Unused6 { get; set; }

	/// <summary>
	/// Created for <see cref="TestSlider(float, float)"/>
	/// </summary>
	private UpdatableInvokableCall<float, float> _Unused7 { get; set; }

	/// <summary>
	/// Created for <see cref="TestColor(Color)"/>
	/// </summary>
	private UpdatableInvokableCall<Color> _Unused8 { get; set; }

	/// <summary>
	/// Created for <see cref="TestLayerMask(LayerMask)"/>
	/// </summary>
	private UpdatableInvokableCall<LayerMask> _Unused9 { get; set; }

	/// <summary>
	/// Created for <see cref="TestLayerMask(Quaternion)"/>
	/// </summary>
	private UpdatableInvokableCall<Quaternion> _Unused10 { get; set; }

	/// <summary>
	/// Created for <see cref="Test(PointerEventData, string)"/>
	/// </summary>
	private UpdatableInvokableCall<PointerEventData, string> _Unused11 { get; set; }

	/// <summary>
	/// Created for <see cref="Test(PointerEventData, int)"/>
	/// </summary>
	private UpdatableInvokableCall<PointerEventData, int> _Unused12 { get; set; }

	/// <summary>
	/// Created for <see cref="Test(PointerEventData, int[])"/>
	/// </summary>
	private UpdatableInvokableCall<PointerEventData, int[]> _Unused13 { get; set; }

	/// <summary>
	/// Created for <see cref="Test(PointerEventData, int[], List{string})"/>
	/// </summary>
	private UpdatableInvokableCall<PointerEventData, int[], List<string>> _Unused14 { get; set; }

	/// <summary>
	/// Created for <see cref="TestSlider(float, float, float)"/>
	/// </summary>
	private UpdatableInvokableCall<float, float, float> _Unused15 { get; set; }

	/// <summary>
	/// Created for <see cref="TestCustom(int, string)"/>
	/// </summary>
	private UpdatableInvokableCall<int, string> _Unused16 { get; set; }

	/// <summary>
	/// Created for <see cref="TestCustom(int, string, float)"/>
	/// </summary>
	private UpdatableInvokableCall<int, string, float> _Unused17 { get; set; }

}