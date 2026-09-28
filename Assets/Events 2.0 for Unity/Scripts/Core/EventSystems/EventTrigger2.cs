using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace UnityEngine.EventSystems
{
	/// <summary>
	/// Receives events from the EventSystem and calls registered functions for each event.
	/// </summary>
	[AddComponentMenu("Event/Event Trigger 2.0")]
	public class EventTrigger2 : MonoBehaviour, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IScrollHandler, IUpdateSelectedHandler, ISelectHandler, IDeselectHandler, IMoveHandler, ISubmitHandler, ICancelHandler
	{
		[SerializeField]
		[FormerlySerializedAs("delegates")]
		private List<Entry> m_Delegates;

		/// <summary>
		/// Called by the EventSystem when the pointer enters the object associated with
		/// this EventTrigger.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnPointerEnter(PointerEventData eventData)
		{
			Execute(EventTriggerType.PointerEnter, eventData);
		}

		/// <summary>
		/// Called by the EventSystem when the pointer exits the object associated with this 
		/// EventTrigger.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnPointerExit(PointerEventData eventData)
		{
			Execute(EventTriggerType.PointerExit, eventData);
		}

		/// <summary>
		/// Called by the EventSystem when a PointerDown event occurs.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnPointerDown(PointerEventData eventData)
		{
			Execute(EventTriggerType.PointerDown, eventData);
		}

		/// <summary>
		/// Called by the EventSystem when a PointerUp event occurs.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnPointerUp(PointerEventData eventData)
		{
			Execute(EventTriggerType.PointerUp, eventData);
		}

		/// <summary>
		/// Called by the EventSystem when a Click event occurs.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnPointerClick(PointerEventData eventData)
		{
			Execute(EventTriggerType.PointerClick, eventData);
		}

		/// <summary>
		/// Called by the EventSystem when a drag has been found, but before it is valid
		/// to begin the drag.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnInitializePotentialDrag(PointerEventData eventData)
		{
			Execute(EventTriggerType.InitializePotentialDrag, eventData);
		}

		/// <summary>
		/// Called before a drag is started.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnBeginDrag(PointerEventData eventData)
		{
			Execute(EventTriggerType.BeginDrag, eventData);
		}

		/// <summary>
		/// Called by the EventSystem every time the pointer is moved during dragging.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnDrag(PointerEventData eventData)
		{
			Execute(EventTriggerType.Drag, eventData);
		}

		/// <summary>
		/// Called by the EventSystem once dragging ends.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnEndDrag(PointerEventData eventData)
		{
			Execute(EventTriggerType.EndDrag, eventData);
		}

		/// <summary>
		/// Called by the EventSystem when an object accepts a drop.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnDrop(PointerEventData eventData)
		{
			Execute(EventTriggerType.Drop, eventData);
		}

		/// <summary>
		/// Called by the EventSystem when a Scroll event occurs.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnScroll(PointerEventData eventData)
		{
			Execute(EventTriggerType.Scroll, eventData);
		}

		/// <summary>
		/// Called by the EventSystem when the object associated with this EventTrigger is 
		/// updated.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnUpdateSelected(BaseEventData eventData)
		{
			Execute(EventTriggerType.UpdateSelected, eventData);
		}

		/// <summary>
		/// Called by the EventSystem when a Select event occurs.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnSelect(BaseEventData eventData)
		{
			Execute(EventTriggerType.Select, eventData);
		}

		/// <summary>
		/// Called by the EventSystem when a new object is being selected.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnDeselect(BaseEventData eventData)
		{
			Execute(EventTriggerType.Deselect, eventData);
		}

		/// <summary>
		/// Called by the EventSystem when a Move event occurs.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnMove(AxisEventData eventData)
		{
			Execute(EventTriggerType.Move, eventData);
		}

		/// <summary>
		/// Called by the EventSystem when a Submit event occurs.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnSubmit(BaseEventData eventData)
		{
			Execute(EventTriggerType.Submit, eventData);
		}

		/// <summary>
		/// Called by the EventSystem when a Cancel event occurs.
		/// </summary>
		/// <param name="eventData">Current event data.</param>
		public virtual void OnCancel(BaseEventData eventData)
		{
			Execute(EventTriggerType.Cancel, eventData);
		}

		/// <summary>
		/// All the functions registered in this EventTrigger.
		/// </summary>
		public List<Entry> triggers
		{
			get
			{
				if (m_Delegates == null)
					m_Delegates = new List<Entry>();

				return m_Delegates;
			}
			set
			{
				m_Delegates = value;
			}
		}

		private void Execute(EventTriggerType id, BaseEventData eventData)
		{
			for (int index = 0; index < triggers.Count; ++index)
			{
				Entry trigger = triggers[index];
				if (trigger.eventID == id && trigger.callback != null)
					trigger.callback.Invoke(eventData);
			}
		}

		/// <summary>
		/// UnityEvent class for Triggers.
		/// </summary>
		[Serializable]
		public class TriggerEvent : UnityEvent2<BaseEventData> { }

		/// <summary>
		/// An Entry in the EventSystem delegates list.
		/// </summary>
		[Serializable]
		public class Entry
		{
			/// <summary>
			/// What type of event is the associated callback listening for.
			/// </summary>
			public EventTriggerType eventID;

			/// <summary>
			/// The desired TriggerEvent to be Invoked.
			/// </summary>
			public TriggerEvent callback;
		}
	}
}