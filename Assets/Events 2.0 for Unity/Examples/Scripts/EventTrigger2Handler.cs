using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EventTrigger2Handler : MonoBehaviour
{
	/// <summary>
	/// 
	/// </summary>
	[SerializeField]
	private Text m_LogText = null;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="message"></param>
	/// <param name="args"></param>
	void Log(string message, params object[] args)
	{
		Debug.LogFormat(message, args);
		m_LogText.text += string.Format(message, args);
		m_LogText.text += System.Environment.NewLine;
		m_LogText.text += System.Environment.NewLine;
	}

	public void OnPointerEnter(BaseEventData eventData, string stringParam)
	{
		Log("OnPointerEnter - string: {1}, eventData: {0}", (PointerEventData)eventData, stringParam);
	}

	public void OnPointerExit(BaseEventData eventData, string stringParam)
	{
		Log("OnPointerExit - string: {1}, eventData: {0}", (PointerEventData)eventData, stringParam);
	}

	public void OnPointerDown(BaseEventData eventData, string stringParam)
	{
		Log("OnPointerDown - string: {1}, eventData: {0}", (PointerEventData)eventData, stringParam);
	}

	public void OnPointerUp(BaseEventData eventData, string stringParam)
	{
		Log("OnPointerDown - string: {1}, eventData: {0}", (PointerEventData)eventData, stringParam);
	}

	public void OnPointerClick(BaseEventData eventData, string stringParam)
	{
		Log("OnPointerClick - string: {1}, eventData: {0}", (PointerEventData)eventData, stringParam);
	}

	public void OnDrag(BaseEventData eventData, string stringParam)
	{
		Log("OnDrag - string: {1}, eventData: {0}", (PointerEventData)eventData, stringParam);
	}

	public void OnDrop(BaseEventData eventData, string stringParam)
	{
		Log("OnDrop - string: {1}, eventData: {0}", (PointerEventData)eventData, stringParam);
	}

	public void OnScroll(BaseEventData eventData, string stringParam)
	{
		Log("OnScroll - string: {1}, eventData: {0}", (PointerEventData)eventData, stringParam);
	}

	public void OnUpdateSelected(BaseEventData eventData, string stringParam)
	{
		Log("OnUpdateSelected - string: {1}, eventData: {0}", eventData, stringParam);
	}

	public void OnSelect(BaseEventData eventData, string stringParam)
	{
		Log("OnSelect - string: {1}, eventData: {0}", eventData, stringParam);
	}

	public void OnDeselect(BaseEventData eventData, string stringParam)
	{
		Log("OnDeselect - string: {1}, eventData: {0}", eventData, stringParam);
	}

	public void OnMove(BaseEventData eventData, string stringParam)
	{
		Log("OnMove - string: {1}, eventData: {0}", (AxisEventData)eventData, stringParam);
	}

	public void OnInitializePotentialDrag(BaseEventData eventData, string stringParam)
	{
		Log("OnInitializePotentialDrag - string: {1}, eventData: {0}", (PointerEventData)eventData, stringParam);
	}

	public void OnBeginDrag(BaseEventData eventData, string stringParam)
	{
		Log("OnBeginDrag - string: {1}, eventData: {0}", (PointerEventData)eventData, stringParam);
	}

	public void OnEndDrag(BaseEventData eventData, string stringParam)
	{
		Log("OnEndDrag - string: {1}, eventData: {0}", eventData, stringParam);
	}

	public void OnSubmit(BaseEventData eventData, string stringParam)
	{
		Log("OnSubmit - string: {1}, eventData: {0}", eventData, stringParam);
	}

	public void OnCancel(BaseEventData eventData, string stringParam)
	{
		Log("OnCancel - string: {1}, eventData: {0}", eventData, stringParam);
	}
}