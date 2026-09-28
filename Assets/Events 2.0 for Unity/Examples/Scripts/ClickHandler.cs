using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 
/// </summary>
public class ClickHandler : MonoBehaviour, IPointerClickHandler
{
	[System.Serializable]
	public class PointerEvents : UnityEvent<PointerEventData> { }

	/// <summary>
	/// 
	/// </summary>
	[SerializeField]
	private Text m_LogText = null;

	/// <summary>
	/// 
	/// </summary>
	[SerializeField]
	private UnityEvent unityEvents = null;

	/// <summary>
	/// 
	/// </summary>
	[SerializeField]
	private PointerEvents pointerEvents = null;

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
		unityEvents.Invoke();
		pointerEvents.Invoke(eventData);
	}

	public void Test()
	{
		Log("Test method. No params");
	}

	public void Test(byte b1)
	{
		Log("Test method. byte param:{0}", b1);
	}

	public void Test(sbyte sb1)
	{
		Log("Test method. sbyte param:{0}", sb1);
	}

	public void Test(short s1)
	{
		Log("Test method. short param:{0}", s1);
	}

	public void Test(ushort us1)
	{
		Log("Test method. ushort param:{0}", us1);
	}

	public void Test(int i1)
	{
		Log("Test method. int param:{0}", i1);
	}

	public void Test(uint ui1)
	{
		Log("Test method. uint param:{0}", ui1);
	}

	public void Test(long l1)
	{
		Log("Test method. long param:{0}", l1);
	}

	public void Test(float i1)
	{
		Log("Test method. float param:{0}", i1);
	}

	public void Test(double d1)
	{
		Log("Test method. double param:{0}", d1);
	}

	public void Test(string s1)
	{
		Log("Test method. string param:{0}", s1);
	}

	public void Test(GameObject go)
	{
		Log("Test method. GameObject param:{0}", go);
	}

	public void Test(PointerEventData eventData)
	{
		Log("Test method. PointerEventData param: {0}", eventData);
	}

	public void TestScriptableObjectExample(ScriptableObjectExample soe)
	{
		Log("TestScriptableObjectExample method. ScriptableObjectExample:{0}", soe.ToString());
	}

	// THE METHODS BELOW ARE NOT AVAILABLE TO CHOOSE
	public void Test(string s1, string s2)
	{
		Log("Test method. string param1:{0} - string param2:{1}", s1, s2);
	}

	public void Test(Vector2 v)
	{
		Log("Test method. Vector2 param:{0}", v);
	}

	public void Test(Vector3 v)
	{
		Log("Test method. Vector3 param:{0}", v);
	}

	public void Test(Vector4 v)
	{
		Log("Test method. Vector4 param:{0}", v);
	}
}