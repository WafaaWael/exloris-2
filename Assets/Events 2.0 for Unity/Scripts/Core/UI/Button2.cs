using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
	/// <summary>
	/// 
	/// </summary>
	[AddComponentMenu("UI/Button 2.0", 30)]
	public class Button2 : Selectable, IEventSystemHandler, IPointerClickHandler, ISubmitHandler
	{
		[FormerlySerializedAs("onClick")]
		[SerializeField]
		[Tooltip("Events to trigger when this button is clicked")]
		private Button2ClickedEvent m_OnClick = new Button2ClickedEvent();

		[SerializeField]
		private bool m_UsePointerEventData = false;

		[FormerlySerializedAs("onClick2")]
		[SerializeField]
		[Tooltip("Events to trigger when this button is clicked")]
		private PointerDataEvent m_OnClick2 = new PointerDataEvent();

		protected Button2() { }

		private void Press(PointerEventData eventData = null)
		{
			if (!IsActive() || !IsInteractable())
				return;

			if (m_UsePointerEventData && eventData != null)
				m_OnClick2.Invoke(eventData);
			else
				m_OnClick.Invoke();
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (!m_UsePointerEventData && eventData.button != PointerEventData.InputButton.Left)
				return;

			Press(eventData);
		}

		public void OnSubmit(BaseEventData eventData)
		{
			Press();

			// if we get set disabled during the press
			// don't run the coroutine.
			if (!IsActive() || !IsInteractable())
				return;

			DoStateTransition(SelectionState.Pressed, false);
			StartCoroutine(OnFinishSubmit());
		}

		[DebuggerHidden]
		private IEnumerator OnFinishSubmit()
		{
			var fadeTime = colors.fadeDuration;
			var elapsedTime = 0f;

			while (elapsedTime < fadeTime)
			{
				elapsedTime += Time.unscaledDeltaTime;
				yield return null;
			}

			DoStateTransition(currentSelectionState, false);
		}

		public Button2ClickedEvent onClick
		{
			get { return m_OnClick; }
			set { m_OnClick = value; }
		}

		public PointerDataEvent onClick2
		{
			get { return m_OnClick2; }
			set { m_OnClick2 = value; }
		}

		public bool usePayload
		{
			get { return m_UsePointerEventData; }
			set { m_UsePointerEventData = value; }
		}

		/// <summary>
		/// 
		/// </summary>
		[Serializable]
		public class Button2ClickedEvent : UnityEvent2 { }

		/// <summary>
		/// 
		/// </summary>
		[Serializable]
		public class PointerDataEvent : UnityEvent2<PointerEventData> { }
	}
}