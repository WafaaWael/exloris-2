using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
	[AddComponentMenu("UI/Toggle Group 2.0", 32)]
	[DisallowMultipleComponent]
	public class ToggleGroup2 : UIBehaviour
	{
		[SerializeField] private bool m_AllowSwitchOff = false;
		public bool allowSwitchOff { get { return m_AllowSwitchOff; } set { m_AllowSwitchOff = value; } }

		private List<Toggle2> m_Toggles = new List<Toggle2>();

		protected ToggleGroup2() { }

		private void ValidateToggleIsInGroup(Toggle2 toggle)
		{
			if (toggle == null || !m_Toggles.Contains(toggle))
				throw new ArgumentException(string.Format("Toggle2 {0} is not part of ToggleGroup2 {1}", new object[] { toggle, this }));
		}

		public void NotifyToggleOn(Toggle2 toggle)
		{
			ValidateToggleIsInGroup(toggle);

			// disable all toggles in the group
			for (var i = 0; i < m_Toggles.Count; i++)
			{
				if (m_Toggles[i] == toggle)
					continue;

				m_Toggles[i].isOn = false;
			}
		}

		public void UnregisterToggle(Toggle2 toggle)
		{
			if (m_Toggles.Contains(toggle))
				m_Toggles.Remove(toggle);
		}

		public void RegisterToggle(Toggle2 toggle)
		{
			if (!m_Toggles.Contains(toggle))
				m_Toggles.Add(toggle);
		}

		public bool AnyTogglesOn()
		{
			return m_Toggles.Find(x => x.isOn) != null;
		}

		public IEnumerable<Toggle2> ActiveToggles()
		{
			return m_Toggles.Where(x => x.isOn);
		}

		public void SetAllTogglesOff()
		{
			bool oldAllowSwitchOff = m_AllowSwitchOff;
			m_AllowSwitchOff = true;

			for (var i = 0; i < m_Toggles.Count; i++)
				m_Toggles[i].isOn = false;

			m_AllowSwitchOff = oldAllowSwitchOff;
		}
	}
}