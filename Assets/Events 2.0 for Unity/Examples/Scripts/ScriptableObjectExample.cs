using UnityEngine;

[CreateAssetMenu(fileName = "Scriptable Object Example", menuName = "Events 2.0 for Unity/Scriptable Object Example")]
public class ScriptableObjectExample : ScriptableObject
{
	public int intValue;

	public string stringValue;

	public EnumExample enumValue;

	public override string ToString()
	{
		return string.Format("int:{0}, string:{1}, enum:{2}", intValue, stringValue, enumValue);
	}
}