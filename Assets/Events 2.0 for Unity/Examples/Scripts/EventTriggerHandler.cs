using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EventTriggerHandler : MonoBehaviour
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

	public void OnPointerEnter(BaseEventData eventData)
	{
		Log("OnPointerEnter - eventData: {0}", (PointerEventData)eventData);
	}

	public void OnPointerExit(BaseEventData eventData)
	{
		Log("OnPointerExit - eventData: {0}", (PointerEventData)eventData);
	}

	public void OnPointerDown(BaseEventData eventData)
	{
		Log("OnPointerDown - eventData: {0}", (PointerEventData)eventData);
	}

	public void OnPointerUp(BaseEventData eventData)
	{
		Log("OnPointerDown - eventData: {0}", (PointerEventData)eventData);
	}

	public void OnPointerClick(BaseEventData eventData)
	{
		Log("OnPointerClick - eventData: {0}", (PointerEventData)eventData);
	}

	public void OnDrag(BaseEventData eventData)
	{
		Log("OnDrag - eventData: {0}", (PointerEventData)eventData);
	}

	public void OnDrop(BaseEventData eventData)
	{
		Log("OnDrop - eventData: {0}", (PointerEventData)eventData);
	}

	public void OnScroll(BaseEventData eventData)
	{
		Log("OnScroll - eventData: {0}", (PointerEventData)eventData);
	}

	public void OnUpdateSelected(BaseEventData eventData)
	{
		Log("OnUpdateSelected - eventData: {0}", eventData);
	}

	public void OnSelect(BaseEventData eventData)
	{
		Log("OnSelect - eventData: {0}", eventData);
	}

	public void OnDeselect(BaseEventData eventData)
	{
		Log("OnDeselect - eventData: {0}", eventData);
	}

	public void OnMove(BaseEventData eventData)
	{
		Log("OnMove - eventData: {0}", (AxisEventData)eventData);
	}

	public void OnInitializePotentialDrag(BaseEventData eventData)
	{
		Log("OnInitializePotentialDrag - eventData: {0}", (PointerEventData)eventData);
	}

	public void OnBeginDrag(BaseEventData eventData)
	{
		Log("OnBeginDrag - eventData: {0}", (PointerEventData)eventData);
	}

	public void OnEndDrag(BaseEventData eventData)
	{
		Log("OnEndDrag - eventData: {0}", eventData);
	}

	public void OnSubmit(BaseEventData eventData)
	{
		Log("OnSubmit - eventData: {0}", eventData);
	}

	public void OnCancel(BaseEventData eventData)
	{
		Log("OnCancel - eventData: {0}", eventData);
	}
}