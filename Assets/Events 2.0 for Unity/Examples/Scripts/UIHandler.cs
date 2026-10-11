using UnityEngine;
using UnityEngine.EventSystems;

public class UIHandler : MonoBehaviour
{
	public enum EnumExample
	{
		FIRST,
		SECOND,
		THIRD
	}

	public void ButtonClick()
	{
		Debug.Log("Button clicked. No params");
	}

	public void ButtonClick(string string1, string string2)
	{
		Debug.LogFormat("Button clicked {0} - {1}", string1, string2);
	}

	public void ButtonClick(int int1, int int2, int int3)
	{
		Debug.LogFormat("Button clicked {0} - {1} - {2}", int1, int2, int3);
	}

	public void ButtonClick(int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Debug.LogFormat("Button clicked {0} - {1} - {2} - {3}", intParam, stringParam, gameObject, enumExample);
	}

	public void ButtonClick(PointerEventData eventData)
	{
		Debug.LogFormat("Button clicked {0}", eventData);
	}

	public void ButtonClick(PointerEventData eventData, int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Debug.LogFormat("Button clicked {0} - {1} - {2} - {3} - {4}", intParam, stringParam, gameObject, enumExample, eventData);
	}

	public void ToggleChanged(bool toggle)
	{
		Debug.LogFormat("Toggle changed {0}", toggle);
	}

	public void ToggleChanged(bool toggle, int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Debug.LogFormat("Toggle changed {0} - {1} - {2} - {3} - {4}", toggle, intParam, stringParam, gameObject, enumExample);
	}

	public void SliderChanged(float slider)
	{
		Debug.LogFormat("Slider changed {0}", slider);
	}

	public void SliderChanged(float slider, int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Debug.LogFormat("Slider changed {0} - {1} - {2} - {3} - {4}", slider, intParam, stringParam, gameObject, enumExample);
	}

	public void ScrollbarChanged(float scrollbar)
	{
		Debug.LogFormat("Scrollbar changed {0}", scrollbar);
	}

	public void ScrollbarChanged(float scrollbar, int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Debug.LogFormat("Scrollbar changed {0} - {1} - {2} - {3} - {4}", scrollbar, intParam, stringParam, gameObject, enumExample);
	}

	public void DropdownChanged(int index)
	{
		Debug.LogFormat("Dropdown changed {0}", index);
	}

	public void DropdownChanged(int index, int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Debug.LogFormat("Dropdown changed {0} - {1} - {2} - {3} - {4}", index, intParam, stringParam, gameObject, enumExample);
	}

	public void DropdownChanged(string label)
	{
		Debug.LogFormat("Dropdown changed {0}", label);
	}

	public void DropdownChanged(string label, int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Debug.LogFormat("Dropdown changed {0} - {1} - {2} - {3} - {4}", label, intParam, stringParam, gameObject, enumExample);
	}

	public void DropdownChanged(Sprite sprite)
	{
		Debug.LogFormat("Dropdown changed {0}", sprite);
	}

	public void DropdownChanged(Sprite sprite, int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Debug.LogFormat("Dropdown changed {0} - {1} - {2} - {3} - {4}", sprite, intParam, stringParam, gameObject, enumExample);
	}

	public void DropdownChanged(int index, string label)
	{
		Debug.LogFormat("Dropdown changed {0} - {1}", index, label);
	}

	public void DropdownChanged(int index, string label, int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Debug.LogFormat("Dropdown changed {0} - {1} - {2} - {3} - {4} - {5}", index, label, intParam, stringParam, gameObject, enumExample);
	}

	public void DropdownChanged(int index, Sprite sprite)
	{
		Debug.LogFormat("Dropdown changed {0} - {1}", index, sprite);
	}

	public void DropdownChanged(int index, Sprite sprite, int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Debug.LogFormat("Dropdown changed {0} - {1} - {2} - {3} - {4} - {5}", index, sprite, intParam, stringParam, gameObject, enumExample);
	}

	public void DropdownChanged(string label, Sprite sprite)
	{
		Debug.LogFormat("Dropdown changed {0} - {1}", label, sprite);
	}

	public void DropdownChanged(string label, Sprite sprite, int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Debug.LogFormat("Dropdown changed {0} - {1} - {2} - {3} - {4} - {5}", label, sprite, intParam, stringParam, gameObject, enumExample);
	}

	public void DropdownChanged(int index, string label, Sprite sprite)
	{
		Debug.LogFormat("Dropdown changed {0} - {1} - {2}", index, label, sprite);
	}

	public void DropdownChanged(int index, string label, Sprite sprite, int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Debug.LogFormat("Dropdown changed {0} - {1} - {2} - {3} - {4} - {5} - {6}", index, label, sprite, intParam, stringParam, gameObject, enumExample);
	}

	public void InputFieldChanged(string value)
	{
		Debug.LogFormat("InputField changed {0}", value);
	}

	public void InputFieldChanged(string value, int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Debug.LogFormat("InputField changed {0} - {1} - {2} - {3} - {4}", value, intParam, stringParam, gameObject, enumExample);
	}

	public void InputFieldEndEdit(string value)
	{
		Debug.LogFormat("InputField end edit {0}", value);
	}

	public void InputFieldEndEdit(string value, int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Debug.LogFormat("InputField end edit {0} - {1} - {2} - {3} - {4}", value, intParam, stringParam, gameObject, enumExample);
	}

	public void ScrollViewChanged(Vector2 vector2)
	{
		Debug.LogFormat("ScrollView changed {0}", vector2);
	}

	public void ScrollViewChanged(Vector2 vector2, int intParam, string stringParam, GameObject gameObject, EnumExample enumExample)
	{
		Debug.LogFormat("ScrollView changed {0} - {1} - {2} - {3} - {4}", vector2, intParam, stringParam, gameObject, enumExample);
	}
}