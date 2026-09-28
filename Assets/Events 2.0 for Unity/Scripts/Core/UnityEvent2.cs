using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Serialization;

namespace UnityEngine.Events
{
	/// <summary>
	/// The mode that a listener is operating in.
	/// </summary>
	public enum PersistentListenerMode2
	{
		/// <summary>
		/// The listener will use the function binding specified by the event.
		/// </summary>
		EventDefined,
		/// <summary>
		/// The listener will bind to zero argument functions.
		/// </summary>
		Void,
		/// <summary>
		/// The listener will bind to an Object type argument functions.
		/// </summary>
		Object,
		/// <summary>
		/// The listener will bind to an char (\0 ~ \uffff) type argument functions.
		/// </summary>
		Char,
		/// <summary>
		/// The listener will bind to an byte (0 ~ 255) argument functions.
		/// </summary>
		Byte,
		/// <summary>
		/// The listener will bind to an sbyte (-128 ~ 127) argument functions.
		/// </summary>
		SByte,
		/// <summary>
		/// The listener will bind to an short (-32,768 ~ 32,767) argument functions.
		/// </summary>
		Short,
		/// <summary>
		/// The listener will bind to an ushort (0 ~ 65,535) argument functions.
		/// </summary>
		UShort,
		/// <summary>
		/// The listener will bind to an int (-2,147,483,648 ~ 2,147,483,647) argument functions.
		/// </summary>
		Int,
		/// <summary>
		/// The listener will bind to an uint (0 ~ 4,294,967,295) argument functions.
		/// </summary>
		UInt,
		/// <summary>
		/// The listener will bind to an long (-9,223,372,036,854,775,808 ~ 9,223,372,036,854,775,807) argument functions.
		/// </summary>
		Long,
		/// <summary>
		/// The listener will bind to a float (±1.5 x 10e-45 ~ ±3.4 x 10e38) argument functions.
		/// </summary>
		Float,
		/// <summary>
		/// The listener will bind to a double (±5.0 × 10e−324 ~ ±1.7 × 10e308) argument functions.
		/// </summary>
		Double,
		/// <summary>
		/// The listener will bind to a string argument functions.
		/// </summary>
		String,
		/// <summary>
		/// The listener will bind to a bool argument functions.
		/// </summary>
		Bool,
		/// <summary>
		/// The listener will bind to an enum argument functions.
		/// </summary>
		Enum,
		/// <summary>
		/// The listener will bind to a Vector2 argument functions.
		/// </summary>
		Vector2,
		/// <summary>
		/// The listener will bind to a Vector2Int argument functions.
		/// </summary>
		Vector2Int,
		/// <summary>
		/// The listener will bind to a Vector3 argument functions.
		/// </summary>
		Vector3,
		/// <summary>
		/// The listener will bind to a Vector3Int argument functions.
		/// </summary>
		Vector3Int,
		/// <summary>
		/// The listener will bind to a Vector4 argument functions.
		/// </summary>
		Vector4,
		/// <summary>
		/// The listener will bind to a LayerMask argument functions.
		/// </summary>
		LayerMask,
		/// <summary>
		/// The listener will bind to a Color argument functions.
		/// </summary>
		Color,
		/// <summary>
		/// The listener will bind to a Quaternion argument functions.
		/// </summary>
		Quaternion,
		/// <summary>
		/// The listener will bind to an Array or Generic List argument functions.
		/// </summary>
		Array
	}

	/// <summary>
	/// Use this attribute to turn an integer parameter into a layer enum.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
	public class LayerAttribute : PropertyAttribute
	{
		/// <summary>
		/// The default value to display.
		/// </summary>
		public readonly int defaultValue = 0;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="layerIndex">The layer index</param>
		public LayerAttribute(int layerIndex = 0)
		{
			if (string.IsNullOrEmpty(LayerMask.LayerToName(layerIndex)))
				layerIndex = 0;

			defaultValue = layerIndex;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="layerName">The layer name</param>
		public LayerAttribute(string layerName)
		{
			int layer = LayerMask.NameToLayer(layerName);
			defaultValue = layer > -1 ? layer : 0;
		}
	}

	/// <summary>
	/// Use this attribute to turn a float parameter into a slider
	/// with min and max values.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
	public class SliderAttribute : PropertyAttribute
	{
		/// <summary>
		/// The min value.
		/// </summary>
		public readonly float minValue = 0f;

		/// <summary>
		/// The max value.
		/// </summary>
		public readonly float maxValue = 0f;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="min">The min value</param>
		/// <param name="max">The max value</param>
		public SliderAttribute(float min, float max)
		{
			if (max < min)
				max = min;

			minValue = min;
			maxValue = max;
		}
	}

	/// <summary>
	/// Use this attribute to turn an integer parameter into a slider
	/// with min and max values.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
	public class IntSliderAttribute : PropertyAttribute
	{
		/// <summary>
		/// The min value.
		/// </summary>
		public readonly int minValue = 0;

		/// <summary>
		/// The max value.
		/// </summary>
		public readonly int maxValue = 0;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="min">The min value</param>
		/// <param name="max">The max value</param>
		public IntSliderAttribute(int min, int max)
		{
			if (max < min)
				max = min;

			minValue = min;
			maxValue = max;
		}
	}

	/// <summary>
	/// Use this attribute to trigger a custom method to build
	/// the arguments on the inspector.
	/// Note that the method specified by "methodName" must have signature like below:
	/// <code>
	/// For methods:
	/// public void ExampleMethod(SerializedProperty arguments, Rect argNameRect, Rect argRect) { }
	/// </code>
	/// <code>
	/// For parameters:
	/// public void ExampleMethod(SerializedProperty argument, Rect argNameRect, Rect argRect) { }
	/// </code>
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
	public class CustomInspectorAttribute : PropertyAttribute
	{
		public readonly string methodName = null;

		public CustomInspectorAttribute(string methodName)
		{
			this.methodName = methodName;
		}
	}

	[Serializable]
	class ArgumentCache2 : ISerializationCallbackReceiver
	{
		[SerializeField]
		[FormerlySerializedAs("objectArgument")]
		private Object m_ObjectArgument;
		[SerializeField]
		[FormerlySerializedAs("objectArgumentAssemblyTypeName")]
		private string m_ObjectArgumentAssemblyTypeName;
		[SerializeField]
		[FormerlySerializedAs("charArgument")]
		private char m_CharArgument;
		[SerializeField]
		[FormerlySerializedAs("byteArgument")]
		private byte m_ByteArgument;
		[SerializeField]
		[FormerlySerializedAs("sbyteArgument")]
		private sbyte m_SByteArgument;
		[SerializeField]
		[FormerlySerializedAs("shortArgument")]
		private short m_ShortArgument;
		[SerializeField]
		[FormerlySerializedAs("ushortArgument")]
		private ushort m_UShortArgument;
		[SerializeField]
		[FormerlySerializedAs("intArgument")]
		private int m_IntArgument;
		[SerializeField]
		[FormerlySerializedAs("uintArgument")]
		private uint m_UIntArgument;
		[SerializeField]
		[FormerlySerializedAs("longArgument")]
		private long m_LongArgument;
		[SerializeField]
		[FormerlySerializedAs("floatArgument")]
		private float m_FloatArgument;
		[SerializeField]
		[FormerlySerializedAs("doubleArgument")]
		private double m_DoubleArgument;
		[SerializeField]
		[FormerlySerializedAs("stringArgument")]
		private string m_StringArgument;
		[SerializeField]
		[FormerlySerializedAs("boolArgument")]
		private bool m_BoolArgument;
		[SerializeField]
		[FormerlySerializedAs("enumArgument")]
		private Enum m_EnumArgument;
		[SerializeField]
		[FormerlySerializedAs("vector2Argument")]
		private Vector2 m_Vector2Argument;
#if UNITY_2017_2_OR_NEWER
		[SerializeField]
		[FormerlySerializedAs("vector2IntArgument")]
		private Vector2Int m_Vector2IntArgument;
#endif
		[SerializeField]
		[FormerlySerializedAs("vector3Argument")]
		private Vector3 m_Vector3Argument;
#if UNITY_2017_2_OR_NEWER
		[SerializeField]
		[FormerlySerializedAs("vector3IntArgument")]
		private Vector3Int m_Vector3IntArgument;
#endif
		[SerializeField]
		[FormerlySerializedAs("vector4Argument")]
		private Vector4 m_Vector4Argument;
		[SerializeField]
		[FormerlySerializedAs("layerMaskArgument")]
		private LayerMask m_LayerMaskArgument;
		[SerializeField]
		[FormerlySerializedAs("colorArgument")]
		private Color m_ColorArgument;
		[SerializeField]
		[FormerlySerializedAs("quaternionArgument")]
		private Quaternion m_QuaternionArgument;
		[SerializeField]
		[FormerlySerializedAs("objectArrayArgument")]
		private List<Object> m_ObjectArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("charArrayArgument")]
		private List<char> m_CharArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("byteArrayArgument")]
		private List<byte> m_ByteArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("sbyteArrayArgument")]
		private List<sbyte> m_SByteArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("shortArrayArgument")]
		private List<short> m_ShortArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("ushortArrayArgument")]
		private List<ushort> m_UShortArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("intArrayArgument")]
		private List<int> m_IntArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("uintArrayArgument")]
		private List<uint> m_UIntArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("longArrayArgument")]
		private List<long> m_LongArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("floatArrayArgument")]
		private List<float> m_FloatArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("doubleArrayArgument")]
		private List<double> m_DoubleArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("stringArrayArgument")]
		private List<string> m_StringArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("boolArrayArgument")]
		private List<bool> m_BoolArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("enumArrayArgument")]
		private List<Enum> m_EnumArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("vector2ArrayArgument")]
		private List<Vector2> m_Vector2ArrayArgument;
#if UNITY_2017_2_OR_NEWER
		[SerializeField]
		[FormerlySerializedAs("vector2IntArrayArgument")]
		private List<Vector2Int> m_Vector2IntArrayArgument;
#endif
		[SerializeField]
		[FormerlySerializedAs("vector3ArrayArgument")]
		private List<Vector3> m_Vector3ArrayArgument;
#if UNITY_2017_2_OR_NEWER
		[SerializeField]
		[FormerlySerializedAs("vector3IntArrayArgument")]
		private List<Vector3Int> m_Vector3IntArrayArgument;
#endif
		[SerializeField]
		[FormerlySerializedAs("vector4ArrayArgument")]
		private List<Vector4> m_Vector4ArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("layerMaskArrayArgument")]
		private List<LayerMask> m_LayerMaskArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("colorArrayArgument")]
		private List<Color> m_ColorArrayArgument;
		[SerializeField]
		[FormerlySerializedAs("quaternionArrayArgument")]
		private List<Quaternion> m_QuaternionArrayArgument;

		public Object unityObjectArgument
		{
			get { return m_ObjectArgument; }
			set
			{
				m_ObjectArgument = value;
				m_ObjectArgumentAssemblyTypeName = value == null ? string.Empty : value.GetType().AssemblyQualifiedName;
			}
		}

		public string unityObjectArgumentAssemblyTypeName
		{
			get { return m_ObjectArgumentAssemblyTypeName; }
		}

		public char charArgument
		{
			get { return m_CharArgument; }
			set
			{
				m_CharArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(char).AssemblyQualifiedName;
			}
		}

		public byte byteArgument
		{
			get { return m_ByteArgument; }
			set
			{
				m_ByteArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(byte).AssemblyQualifiedName;
			}
		}

		public sbyte sbyteArgument
		{
			get { return m_SByteArgument; }
			set
			{
				m_SByteArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(sbyte).AssemblyQualifiedName;
			}
		}

		public short shortArgument
		{
			get { return m_ShortArgument; }
			set
			{
				m_ShortArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(short).AssemblyQualifiedName;
			}
		}

		public ushort ushortArgument
		{
			get { return m_UShortArgument; }
			set
			{
				m_UShortArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(ushort).AssemblyQualifiedName;
			}
		}

		public int intArgument
		{
			get { return m_IntArgument; }
			set
			{
				m_IntArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(int).AssemblyQualifiedName;
			}
		}

		public uint uintArgument
		{
			get { return m_UIntArgument; }
			set
			{
				m_UIntArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(uint).AssemblyQualifiedName;
			}
		}

		public long longArgument
		{
			get { return m_LongArgument; }
			set
			{
				m_LongArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(long).AssemblyQualifiedName;
			}
		}

		public float floatArgument
		{
			get { return m_FloatArgument; }
			set
			{
				m_FloatArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(float).AssemblyQualifiedName;
			}
		}

		public double doubleArgument
		{
			get { return m_DoubleArgument; }
			set
			{
				m_DoubleArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(double).AssemblyQualifiedName;
			}
		}

		public string stringArgument
		{
			get { return m_StringArgument; }
			set
			{
				m_StringArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(string).AssemblyQualifiedName;
			}
		}

		public bool boolArgument
		{
			get { return m_BoolArgument; }
			set
			{
				m_BoolArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(bool).AssemblyQualifiedName;
			}
		}

		public Enum enumArgument
		{
			get { return m_EnumArgument; }
			set
			{
				m_EnumArgument = value;
				m_ObjectArgumentAssemblyTypeName = value == null ? string.Empty : value.GetType().AssemblyQualifiedName;
			}
		}

		public Vector2 vector2Argument
		{
			get { return m_Vector2Argument; }
			set
			{
				m_Vector2Argument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(Vector2).AssemblyQualifiedName;
			}
		}
#if UNITY_2017_2_OR_NEWER
		public Vector2Int vector2IntArgument
		{
			get { return m_Vector2IntArgument; }
			set
			{
				m_Vector2IntArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(Vector2Int).AssemblyQualifiedName;
			}
		}
#endif

		public Vector3 vector3Argument
		{
			get { return m_Vector3Argument; }
			set
			{
				m_Vector3Argument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(Vector3).AssemblyQualifiedName;
			}
		}
#if UNITY_2017_2_OR_NEWER
		public Vector3Int vector3IntArgument
		{
			get { return m_Vector3IntArgument; }
			set
			{
				m_Vector3IntArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(Vector3Int).AssemblyQualifiedName;
			}
		}
#endif

		public Vector4 vector4Argument
		{
			get { return m_Vector4Argument; }
			set
			{
				m_Vector4Argument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(Vector4).AssemblyQualifiedName;
			}
		}

		public LayerMask layerMaskArgument
		{
			get { return m_LayerMaskArgument; }
			set
			{
				m_LayerMaskArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(LayerMask).AssemblyQualifiedName;
			}
		}

		public Color colorArgument
		{
			get { return m_ColorArgument; }
			set
			{
				m_ColorArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(Color).AssemblyQualifiedName;
			}
		}

		public Quaternion quaternionArgument
		{
			get { return m_QuaternionArgument; }
			set
			{
				m_QuaternionArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(Quaternion).AssemblyQualifiedName;
			}
		}

		public List<Object> unityObjectArrayArgument
		{
			get { return m_ObjectArrayArgument; }
			set
			{
				m_ObjectArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = value == null ? string.Empty : value.GetType().AssemblyQualifiedName;
			}
		}

		public List<char> charArrayArgument
		{
			get { return m_CharArrayArgument; }
			set
			{
				m_CharArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<char>).AssemblyQualifiedName;
			}
		}

		public List<byte> byteArrayArgument
		{
			get { return m_ByteArrayArgument; }
			set
			{
				m_ByteArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<byte>).AssemblyQualifiedName;
			}
		}

		public List<sbyte> sbyteArrayArgument
		{
			get { return m_SByteArrayArgument; }
			set
			{
				m_SByteArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<sbyte>).AssemblyQualifiedName;
			}
		}

		public List<short> shortArrayArgument
		{
			get { return m_ShortArrayArgument; }
			set
			{
				m_ShortArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<short>).AssemblyQualifiedName;
			}
		}

		public List<ushort> ushortArrayArgument
		{
			get { return m_UShortArrayArgument; }
			set
			{
				m_UShortArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<ushort>).AssemblyQualifiedName;
			}
		}

		public List<int> intArrayArgument
		{
			get { return m_IntArrayArgument; }
			set
			{
				m_IntArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<int>).AssemblyQualifiedName;
			}
		}

		public List<uint> uintArrayArgument
		{
			get { return m_UIntArrayArgument; }
			set
			{
				m_UIntArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<uint>).AssemblyQualifiedName;
			}
		}

		public List<long> longArrayArgument
		{
			get { return m_LongArrayArgument; }
			set
			{
				m_LongArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<long>).AssemblyQualifiedName;
			}
		}

		public List<float> floatArrayArgument
		{
			get { return m_FloatArrayArgument; }
			set
			{
				m_FloatArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<float>).AssemblyQualifiedName;
			}
		}

		public List<double> doubleArrayArgument
		{
			get { return m_DoubleArrayArgument; }
			set
			{
				m_DoubleArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<double>).AssemblyQualifiedName;
			}
		}

		public List<string> stringArrayArgument
		{
			get { return m_StringArrayArgument; }
			set
			{
				m_StringArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<string>).AssemblyQualifiedName;
			}
		}

		public List<bool> boolArrayArgument
		{
			get { return m_BoolArrayArgument; }
			set
			{
				m_BoolArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<bool>).AssemblyQualifiedName;
			}
		}

		public List<Enum> enumArrayArgument
		{
			get { return m_EnumArrayArgument; }
			set
			{
				m_EnumArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = value == null ? string.Empty : value.GetType().AssemblyQualifiedName;
			}
		}

		public List<Vector2> vector2ArrayArgument
		{
			get { return m_Vector2ArrayArgument; }
			set
			{
				m_Vector2ArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<Vector2>).AssemblyQualifiedName;
			}
		}
#if UNITY_2017_2_OR_NEWER
		public List<Vector2Int> vector2IntArrayArgument
		{
			get { return m_Vector2IntArrayArgument; }
			set
			{
				m_Vector2IntArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<Vector2Int>).AssemblyQualifiedName;
			}
		}
#endif

		public List<Vector3> vector3ArrayArgument
		{
			get { return m_Vector3ArrayArgument; }
			set
			{
				m_Vector3ArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<Vector3>).AssemblyQualifiedName;
			}
		}
#if UNITY_2017_2_OR_NEWER
		public List<Vector3Int> vector3IntArrayArgument
		{
			get { return m_Vector3IntArrayArgument; }
			set
			{
				m_Vector3IntArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<Vector3Int>).AssemblyQualifiedName;
			}
		}
#endif

		public List<Vector4> vector4ArrayArgument
		{
			get { return m_Vector4ArrayArgument; }
			set
			{
				m_Vector4ArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<Vector4>).AssemblyQualifiedName;
			}
		}

		public List<LayerMask> layerMaskArrayArgument
		{
			get { return m_LayerMaskArrayArgument; }
			set
			{
				m_LayerMaskArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<LayerMask>).AssemblyQualifiedName;
			}
		}

		public List<Color> colorArrayArgument
		{
			get { return m_ColorArrayArgument; }
			set
			{
				m_ColorArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<Color>).AssemblyQualifiedName;
			}
		}

		public List<Quaternion> quaternionArrayArgument
		{
			get { return m_QuaternionArrayArgument; }
			set
			{
				m_QuaternionArrayArgument = value;
				m_ObjectArgumentAssemblyTypeName = typeof(List<Quaternion>).AssemblyQualifiedName;
			}
		}

		private void TidyAssemblyTypeName()
		{
			if (string.IsNullOrEmpty(m_ObjectArgumentAssemblyTypeName))
				return;

			int num = int.MaxValue;
			int val1_1 = m_ObjectArgumentAssemblyTypeName.IndexOf(", Version=");
			if (val1_1 != -1)
				num = Math.Min(val1_1, num);
			int val1_2 = m_ObjectArgumentAssemblyTypeName.IndexOf(", Culture=");
			if (val1_2 != -1)
				num = Math.Min(val1_2, num);
			int val1_3 = m_ObjectArgumentAssemblyTypeName.IndexOf(", PublicKeyToken=");
			if (val1_3 != -1)
				num = Math.Min(val1_3, num);
			if (num == int.MaxValue)
				return;

			m_ObjectArgumentAssemblyTypeName = m_ObjectArgumentAssemblyTypeName.Substring(0, num);

			int val1_4 = m_ObjectArgumentAssemblyTypeName.IndexOf("[[");
			if (val1_4 != -1)
				m_ObjectArgumentAssemblyTypeName += "]]";
		}

		public void OnBeforeSerialize()
		{
			TidyAssemblyTypeName();
		}

		public void OnAfterDeserialize()
		{
			TidyAssemblyTypeName();
		}
	}

	public abstract class BaseInvokableCall2
	{
		protected string targetAssembly = null;
		protected string methodName = null;

		protected BaseInvokableCall2()
		{
		}

		protected BaseInvokableCall2(object target, MethodInfo function)
		{
			if (target == null)
				throw new ArgumentNullException("target");
			if (function == null)
				throw new ArgumentNullException("function");

			targetAssembly = target.GetType().AssemblyQualifiedName;
			methodName = function.Name;
		}

		public abstract void Invoke(object[] args);

		protected static void ThrowOnInvalidArg<T>(object arg, string methodName, string targetAssembly)
		{
			if (arg != null && !(arg is T))
				throw new ArgumentException(string.Format("Passed argument 'arg' on method '{0}' for target '{1}' is of the wrong type. Type:{2} Expected:{3}", methodName, targetAssembly, arg.GetType(), typeof(T)));
		}

		protected static bool AllowInvoke(Delegate @delegate)
		{
			object target = @delegate.Target;
			if (target == null)
				return true;
			Object @object = target as Object;
			if (!ReferenceEquals(@object, null))
				return @object != null;
			return true;
		}

		public abstract bool Find(object targetObj, MethodInfo method);
	}

	public class InvokableCall2 : BaseInvokableCall2
	{
		private event UnityAction Delegate;

		public InvokableCall2(object target, MethodInfo theFunction) : base(target, theFunction)
		{
			Delegate += (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), target, theFunction);
		}

		public InvokableCall2(UnityAction action)
		{
			Delegate += action;
		}

		public override bool Find(object targetObj, MethodInfo method)
		{
			return Delegate.Target == targetObj && Delegate.Method.Equals(method);
		}

		public override void Invoke(object[] args)
		{
			if (AllowInvoke(Delegate))
				Delegate();
		}

		public void Invoke()
		{
			if (AllowInvoke(Delegate))
				Delegate();
		}
	}

	public class InvokableCall2<T0> : BaseInvokableCall2
	{
		protected event UnityAction<T0> Delegate;

		public InvokableCall2(object target, MethodInfo theFunction) : base(target, theFunction)
		{
			Delegate += (UnityAction<T0>)System.Delegate.CreateDelegate(typeof(UnityAction<T0>), target, theFunction);
		}

		public InvokableCall2(UnityAction<T0> action)
		{
			Delegate += action;
		}

		public override void Invoke(object[] args)
		{
			if (args.Length != 1)
				throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 1");
			ThrowOnInvalidArg<T0>(args[0], methodName, targetAssembly);
			if (!AllowInvoke(Delegate))
				return;
			Delegate((T0)args[0]);
		}

		public virtual void Invoke(T0 args0)
		{
			if (AllowInvoke(Delegate))
				Delegate(args0);
		}

		public override bool Find(object targetObj, MethodInfo method)
		{
			return Delegate.Target == targetObj && Delegate.Method.Equals(method);
		}
	}

	public class InvokableCall2<T0, T1> : BaseInvokableCall2
	{
		protected event UnityAction<T0, T1> Delegate;

		public InvokableCall2(object target, MethodInfo theFunction) : base(target, theFunction)
		{
			Delegate += (UnityAction<T0, T1>)System.Delegate.CreateDelegate(typeof(UnityAction<T0, T1>), target, theFunction);
		}

		public InvokableCall2(UnityAction<T0, T1> action)
		{
			Delegate += action;
		}

		public override void Invoke(object[] args)
		{
			if (args.Length != 2)
				throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 2");
			ThrowOnInvalidArg<T0>(args[0], methodName, targetAssembly);
			ThrowOnInvalidArg<T1>(args[1], methodName, targetAssembly);
			if (!AllowInvoke(Delegate))
				return;
			Delegate((T0)args[0], (T1)args[1]);
		}

		public virtual void Invoke(T0 args0, T1 args1)
		{
			if (AllowInvoke(Delegate))
				Delegate(args0, args1);
		}

		public override bool Find(object targetObj, MethodInfo method)
		{
			return Delegate.Target == targetObj && Delegate.Method.Equals(method);
		}
	}

	public class InvokableCall2<T0, T1, T2> : BaseInvokableCall2
	{
		protected event UnityAction<T0, T1, T2> Delegate;

		public InvokableCall2(object target, MethodInfo theFunction) : base(target, theFunction)
		{
			Delegate += (UnityAction<T0, T1, T2>)System.Delegate.CreateDelegate(typeof(UnityAction<T0, T1, T2>), target, theFunction);
		}

		public InvokableCall2(UnityAction<T0, T1, T2> action)
		{
			Delegate += action;
		}

		public override void Invoke(object[] args)
		{
			if (args.Length != 3)
				throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 3");
			ThrowOnInvalidArg<T0>(args[0], methodName, targetAssembly);
			ThrowOnInvalidArg<T1>(args[1], methodName, targetAssembly);
			ThrowOnInvalidArg<T2>(args[2], methodName, targetAssembly);
			if (!AllowInvoke(Delegate))
				return;
			Delegate((T0)args[0], (T1)args[1], (T2)args[2]);
		}

		public virtual void Invoke(T0 args0, T1 args1, T2 args2)
		{
			if (AllowInvoke(Delegate))
				Delegate(args0, args1, args2);
		}

		public override bool Find(object targetObj, MethodInfo method)
		{
			return Delegate.Target == targetObj && Delegate.Method.Equals(method);
		}
	}

	public class InvokableCall2<T0, T1, T2, T3> : BaseInvokableCall2
	{
		protected event UnityAction<T0, T1, T2, T3> Delegate;

		public InvokableCall2(object target, MethodInfo theFunction) : base(target, theFunction)
		{
			Delegate += (UnityAction<T0, T1, T2, T3>)System.Delegate.CreateDelegate(typeof(UnityAction<T0, T1, T2, T3>), target, theFunction);
		}

		public InvokableCall2(UnityAction<T0, T1, T2, T3> action)
		{
			Delegate += action;
		}

		public override void Invoke(object[] args)
		{
			if (args.Length != 4)
				throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 4");
			ThrowOnInvalidArg<T0>(args[0], methodName, targetAssembly);
			ThrowOnInvalidArg<T1>(args[1], methodName, targetAssembly);
			ThrowOnInvalidArg<T2>(args[2], methodName, targetAssembly);
			ThrowOnInvalidArg<T3>(args[3], methodName, targetAssembly);
			if (!AllowInvoke(Delegate))
				return;
			Delegate((T0)args[0], (T1)args[1], (T2)args[2], (T3)args[3]);
		}

		public virtual void Invoke(T0 args0, T1 args1, T2 args2, T3 args3)
		{
			if (AllowInvoke(Delegate))
				Delegate(args0, args1, args2, args3);
		}

		public override bool Find(object targetObj, MethodInfo method)
		{
			return Delegate.Target == targetObj && Delegate.Method.Equals(method);
		}
	}

	public class InvokableCall2<T0, T1, T2, T3, T4> : BaseInvokableCall2
	{
		protected event UnityAction<T0, T1, T2, T3, T4> Delegate;

		public InvokableCall2(object target, MethodInfo theFunction) : base(target, theFunction)
		{
			Delegate += (UnityAction<T0, T1, T2, T3, T4>)System.Delegate.CreateDelegate(typeof(UnityAction<T0, T1, T2, T3, T4>), target, theFunction);
		}

		public InvokableCall2(UnityAction<T0, T1, T2, T3, T4> action)
		{
			Delegate += action;
		}

		public override void Invoke(object[] args)
		{
			if (args.Length != 5)
				throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 5");
			ThrowOnInvalidArg<T0>(args[0], methodName, targetAssembly);
			ThrowOnInvalidArg<T1>(args[1], methodName, targetAssembly);
			ThrowOnInvalidArg<T2>(args[2], methodName, targetAssembly);
			ThrowOnInvalidArg<T3>(args[3], methodName, targetAssembly);
			ThrowOnInvalidArg<T4>(args[4], methodName, targetAssembly);
			if (!AllowInvoke(Delegate))
				return;
			Delegate((T0)args[0], (T1)args[1], (T2)args[2], (T3)args[3], (T4)args[4]);
		}

		public virtual void Invoke(T0 args0, T1 args1, T2 args2, T3 args3, T4 args4)
		{
			if (AllowInvoke(Delegate))
				Delegate(args0, args1, args2, args3, args4);
		}

		public override bool Find(object targetObj, MethodInfo method)
		{
			return Delegate.Target == targetObj && Delegate.Method.Equals(method);
		}
	}

	public class InvokableCall2<T0, T1, T2, T3, T4, T5> : BaseInvokableCall2
	{
		protected event UnityAction<T0, T1, T2, T3, T4, T5> Delegate;

		public InvokableCall2(object target, MethodInfo theFunction) : base(target, theFunction)
		{
			Delegate += (UnityAction<T0, T1, T2, T3, T4, T5>)System.Delegate.CreateDelegate(typeof(UnityAction<T0, T1, T2, T3, T4, T5>), target, theFunction);
		}

		public InvokableCall2(UnityAction<T0, T1, T2, T3, T4, T5> action)
		{
			Delegate += action;
		}

		public override void Invoke(object[] args)
		{
			if (args.Length != 6)
				throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 6");
			ThrowOnInvalidArg<T0>(args[0], methodName, targetAssembly);
			ThrowOnInvalidArg<T1>(args[1], methodName, targetAssembly);
			ThrowOnInvalidArg<T2>(args[2], methodName, targetAssembly);
			ThrowOnInvalidArg<T3>(args[3], methodName, targetAssembly);
			ThrowOnInvalidArg<T4>(args[4], methodName, targetAssembly);
			ThrowOnInvalidArg<T5>(args[5], methodName, targetAssembly);
			if (!AllowInvoke(Delegate))
				return;
			Delegate((T0)args[0], (T1)args[1], (T2)args[2], (T3)args[3], (T4)args[4], (T5)args[5]);
		}

		public virtual void Invoke(T0 args0, T1 args1, T2 args2, T3 args3, T4 args4, T5 args5)
		{
			if (AllowInvoke(Delegate))
				Delegate(args0, args1, args2, args3, args4, args5);
		}

		public override bool Find(object targetObj, MethodInfo method)
		{
			return Delegate.Target == targetObj && Delegate.Method.Equals(method);
		}
	}

	public class InvokableCall2<T0, T1, T2, T3, T4, T5, T6> : BaseInvokableCall2
	{
		protected event UnityAction<T0, T1, T2, T3, T4, T5, T6> Delegate;

		public InvokableCall2(object target, MethodInfo theFunction) : base(target, theFunction)
		{
			Delegate += (UnityAction<T0, T1, T2, T3, T4, T5, T6>)System.Delegate.CreateDelegate(typeof(UnityAction<T0, T1, T2, T3, T4, T5, T6>), target, theFunction);
		}

		public InvokableCall2(UnityAction<T0, T1, T2, T3, T4, T5, T6> action)
		{
			Delegate += action;
		}

		public override void Invoke(object[] args)
		{
			if (args.Length != 7)
				throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 7");
			ThrowOnInvalidArg<T0>(args[0], methodName, targetAssembly);
			ThrowOnInvalidArg<T1>(args[1], methodName, targetAssembly);
			ThrowOnInvalidArg<T2>(args[2], methodName, targetAssembly);
			ThrowOnInvalidArg<T3>(args[3], methodName, targetAssembly);
			ThrowOnInvalidArg<T4>(args[4], methodName, targetAssembly);
			ThrowOnInvalidArg<T5>(args[5], methodName, targetAssembly);
			ThrowOnInvalidArg<T6>(args[6], methodName, targetAssembly);
			if (!AllowInvoke(Delegate))
				return;
			Delegate((T0)args[0], (T1)args[1], (T2)args[2], (T3)args[3], (T4)args[4], (T5)args[5], (T6)args[6]);
		}

		public virtual void Invoke(T0 args0, T1 args1, T2 args2, T3 args3, T4 args4, T5 args5, T6 args6)
		{
			if (AllowInvoke(Delegate))
				Delegate(args0, args1, args2, args3, args4, args5, args6);
		}

		public override bool Find(object targetObj, MethodInfo method)
		{
			return Delegate.Target == targetObj && Delegate.Method.Equals(method);
		}
	}

	public class InvokableCall2<T0, T1, T2, T3, T4, T5, T6, T7> : BaseInvokableCall2
	{
		protected event UnityAction<T0, T1, T2, T3, T4, T5, T6, T7> Delegate;

		public InvokableCall2(object target, MethodInfo theFunction) : base(target, theFunction)
		{
			Delegate += (UnityAction<T0, T1, T2, T3, T4, T5, T6, T7>)System.Delegate.CreateDelegate(typeof(UnityAction<T0, T1, T2, T3, T4, T5, T6, T7>), target, theFunction);
		}

		public InvokableCall2(UnityAction<T0, T1, T2, T3, T4, T5, T6, T7> action)
		{
			Delegate += action;
		}

		public override void Invoke(object[] args)
		{
			if (args.Length != 8)
				throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 8");
			ThrowOnInvalidArg<T0>(args[0], methodName, targetAssembly);
			ThrowOnInvalidArg<T1>(args[1], methodName, targetAssembly);
			ThrowOnInvalidArg<T2>(args[2], methodName, targetAssembly);
			ThrowOnInvalidArg<T3>(args[3], methodName, targetAssembly);
			ThrowOnInvalidArg<T4>(args[4], methodName, targetAssembly);
			ThrowOnInvalidArg<T5>(args[5], methodName, targetAssembly);
			ThrowOnInvalidArg<T6>(args[6], methodName, targetAssembly);
			ThrowOnInvalidArg<T7>(args[7], methodName, targetAssembly);
			if (!AllowInvoke(Delegate))
				return;
			Delegate((T0)args[0], (T1)args[1], (T2)args[2], (T3)args[3], (T4)args[4], (T5)args[5], (T6)args[6], (T7)args[7]);
		}

		public virtual void Invoke(T0 args0, T1 args1, T2 args2, T3 args3, T4 args4, T5 args5, T6 args6, T7 args7)
		{
			if (AllowInvoke(Delegate))
				Delegate(args0, args1, args2, args3, args4, args5, args6, args7);
		}

		public override bool Find(object targetObj, MethodInfo method)
		{
			return Delegate.Target == targetObj && Delegate.Method.Equals(method);
		}
	}

	public class InvokableCall2<T0, T1, T2, T3, T4, T5, T6, T7, T8> : BaseInvokableCall2
	{
		protected event UnityAction<T0, T1, T2, T3, T4, T5, T6, T7, T8> Delegate;

		public InvokableCall2(object target, MethodInfo theFunction) : base(target, theFunction)
		{
			Delegate += (UnityAction<T0, T1, T2, T3, T4, T5, T6, T7, T8>)System.Delegate.CreateDelegate(typeof(UnityAction<T0, T1, T2, T3, T4, T5, T6, T7, T8>), target, theFunction);
		}

		public InvokableCall2(UnityAction<T0, T1, T2, T3, T4, T5, T6, T7, T8> action)
		{
			Delegate += action;
		}

		public override void Invoke(object[] args)
		{
			if (args.Length != 9)
				throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 9");
			ThrowOnInvalidArg<T0>(args[0], methodName, targetAssembly);
			ThrowOnInvalidArg<T1>(args[1], methodName, targetAssembly);
			ThrowOnInvalidArg<T2>(args[2], methodName, targetAssembly);
			ThrowOnInvalidArg<T3>(args[3], methodName, targetAssembly);
			ThrowOnInvalidArg<T4>(args[4], methodName, targetAssembly);
			ThrowOnInvalidArg<T5>(args[5], methodName, targetAssembly);
			ThrowOnInvalidArg<T6>(args[6], methodName, targetAssembly);
			ThrowOnInvalidArg<T7>(args[7], methodName, targetAssembly);
			ThrowOnInvalidArg<T8>(args[8], methodName, targetAssembly);
			if (!AllowInvoke(Delegate))
				return;
			Delegate((T0)args[0], (T1)args[1], (T2)args[2], (T3)args[3], (T4)args[4], (T5)args[5], (T6)args[6], (T7)args[7], (T8)args[8]);
		}

		public virtual void Invoke(T0 args0, T1 args1, T2 args2, T3 args3, T4 args4, T5 args5, T6 args6, T7 args7, T8 args8)
		{
			if (AllowInvoke(Delegate))
				Delegate(args0, args1, args2, args3, args4, args5, args6, args7, args8);
		}

		public override bool Find(object targetObj, MethodInfo method)
		{
			return Delegate.Target == targetObj && Delegate.Method.Equals(method);
		}
	}

	public class InvokableCall2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : BaseInvokableCall2
	{
		protected event UnityAction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> Delegate;

		public InvokableCall2(object target, MethodInfo theFunction) : base(target, theFunction)
		{
			Delegate += (UnityAction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>)System.Delegate.CreateDelegate(typeof(UnityAction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>), target, theFunction);
		}

		public InvokableCall2(UnityAction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
		{
			Delegate += action;
		}

		public override void Invoke(object[] args)
		{
			if (args.Length != 10)
				throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 10");
			ThrowOnInvalidArg<T0>(args[0], methodName, targetAssembly);
			ThrowOnInvalidArg<T1>(args[1], methodName, targetAssembly);
			ThrowOnInvalidArg<T2>(args[2], methodName, targetAssembly);
			ThrowOnInvalidArg<T3>(args[3], methodName, targetAssembly);
			ThrowOnInvalidArg<T4>(args[4], methodName, targetAssembly);
			ThrowOnInvalidArg<T5>(args[5], methodName, targetAssembly);
			ThrowOnInvalidArg<T6>(args[6], methodName, targetAssembly);
			ThrowOnInvalidArg<T7>(args[7], methodName, targetAssembly);
			ThrowOnInvalidArg<T8>(args[8], methodName, targetAssembly);
			ThrowOnInvalidArg<T9>(args[9], methodName, targetAssembly);
			if (!AllowInvoke(Delegate))
				return;
			Delegate((T0)args[0], (T1)args[1], (T2)args[2], (T3)args[3], (T4)args[4], (T5)args[5], (T6)args[6], (T7)args[7], (T8)args[8], (T9)args[9]);
		}

		public virtual void Invoke(T0 args0, T1 args1, T2 args2, T3 args3, T4 args4, T5 args5, T6 args6, T7 args7, T8 args8, T9 args9)
		{
			if (AllowInvoke(Delegate))
				Delegate(args0, args1, args2, args3, args4, args5, args6, args7, args8, args9);
		}

		public override bool Find(object targetObj, MethodInfo method)
		{
			return Delegate.Target == targetObj && Delegate.Method.Equals(method);
		}
	}

	public class UpdatableInvokableCall<T0> : InvokableCall2<T0>
	{
		private readonly object[] m_Args = new object[1];

		private bool m_IsEventDefined = false;

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined, T0 arg) : base(target, theFunction)
		{
			m_Args[0] = arg;
			m_IsEventDefined = isEventDefined;
		}

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined) : base(target, theFunction)
		{
			m_IsEventDefined = isEventDefined;
		}

		public override void Invoke(object[] args)
		{
			if (m_IsEventDefined)
				for (int i = 0; i < args.Length; i++)
					m_Args[i] = args[i];
			base.Invoke(m_Args);
		}

		public override void Invoke(T0 args0)
		{
			if (m_IsEventDefined)
			{
				m_Args[0] = args0;
			}

			base.Invoke((T0)m_Args[0]);
		}
	}

	public class UpdatableInvokableCall<T0, T1> : InvokableCall2<T0, T1>
	{
		private readonly object[] m_Args = new object[2];

		private bool m_IsEventDefined = false;

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined, T0 arg0, T1 arg1) : base(target, theFunction)
		{
			m_Args[0] = arg0;
			m_Args[1] = arg1;
			m_IsEventDefined = isEventDefined;
		}

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined) : base(target, theFunction)
		{
			m_IsEventDefined = isEventDefined;
		}

		public override void Invoke(object[] args)
		{
			if (m_IsEventDefined)
				for (int i = 0; i < args.Length; i++)
					m_Args[i] = args[i];
			base.Invoke(m_Args);
		}

		public override void Invoke(T0 args0, T1 args1)
		{
			if (m_IsEventDefined)
			{
				m_Args[0] = args0;
				m_Args[1] = args1;
			}

			base.Invoke((T0)m_Args[0], (T1)m_Args[1]);
		}
	}

	public class UpdatableInvokableCall<T0, T1, T2> : InvokableCall2<T0, T1, T2>
	{
		private readonly object[] m_Args = new object[3];

		private bool m_IsEventDefined = false;

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined, T0 arg0, T1 arg1, T2 arg2) : base(target, theFunction)
		{
			m_Args[0] = arg0;
			m_Args[1] = arg1;
			m_Args[2] = arg2;
			m_IsEventDefined = isEventDefined;
		}

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined) : base(target, theFunction)
		{
			m_IsEventDefined = isEventDefined;
		}

		public override void Invoke(object[] args)
		{
			if (m_IsEventDefined)
				for (int i = 0; i < args.Length; i++)
					m_Args[i] = args[i];
			base.Invoke(m_Args);
		}

		public override void Invoke(T0 args0, T1 args1, T2 args2)
		{
			if (m_IsEventDefined)
			{
				m_Args[0] = args0;
				m_Args[1] = args1;
				m_Args[2] = args2;
			}

			base.Invoke((T0)m_Args[0], (T1)m_Args[1], (T2)m_Args[2]);
		}
	}

	public class UpdatableInvokableCall<T0, T1, T2, T3> : InvokableCall2<T0, T1, T2, T3>
	{
		private readonly object[] m_Args = new object[4];

		private bool m_IsEventDefined = false;

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined, T0 arg0, T1 arg1, T2 arg2, T3 arg3) : base(target, theFunction)
		{
			m_Args[0] = arg0;
			m_Args[1] = arg1;
			m_Args[2] = arg2;
			m_Args[3] = arg3;
			m_IsEventDefined = isEventDefined;
		}

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined) : base(target, theFunction)
		{
			m_IsEventDefined = isEventDefined;
		}

		public override void Invoke(object[] args)
		{
			if (m_IsEventDefined)
				for (int i = 0; i < args.Length; i++)
					m_Args[i] = args[i];
			base.Invoke(m_Args);
		}

		public override void Invoke(T0 args0, T1 args1, T2 args2, T3 args3)
		{
			if (m_IsEventDefined)
			{
				m_Args[0] = args0;
				m_Args[1] = args1;
				m_Args[2] = args2;
				m_Args[3] = args3;
			}

			base.Invoke((T0)m_Args[0], (T1)m_Args[1], (T2)m_Args[2], (T3)m_Args[3]);
		}
	}

	public class UpdatableInvokableCall<T0, T1, T2, T3, T4> : InvokableCall2<T0, T1, T2, T3, T4>
	{
		private readonly object[] m_Args = new object[5];

		private bool m_IsEventDefined = false;

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) : base(target, theFunction)
		{
			m_Args[0] = arg0;
			m_Args[1] = arg1;
			m_Args[2] = arg2;
			m_Args[3] = arg3;
			m_Args[4] = arg4;
			m_IsEventDefined = isEventDefined;
		}

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined) : base(target, theFunction)
		{
			m_IsEventDefined = isEventDefined;
		}

		public override void Invoke(object[] args)
		{
			if (m_IsEventDefined)
				for (int i = 0; i < args.Length; i++)
					m_Args[i] = args[i];
			base.Invoke(m_Args);
		}

		public override void Invoke(T0 args0, T1 args1, T2 args2, T3 args3, T4 args4)
		{
			if (m_IsEventDefined)
			{
				m_Args[0] = args0;
				m_Args[1] = args1;
				m_Args[2] = args2;
				m_Args[3] = args3;
				m_Args[4] = args4;
			}

			base.Invoke((T0)m_Args[0], (T1)m_Args[1], (T2)m_Args[2], (T3)m_Args[3], (T4)m_Args[4]);
		}
	}

	public class UpdatableInvokableCall<T0, T1, T2, T3, T4, T5> : InvokableCall2<T0, T1, T2, T3, T4, T5>
	{
		private readonly object[] m_Args = new object[6];

		private bool m_IsEventDefined = false;

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) : base(target, theFunction)
		{
			m_Args[0] = arg0;
			m_Args[1] = arg1;
			m_Args[2] = arg2;
			m_Args[3] = arg3;
			m_Args[4] = arg4;
			m_Args[5] = arg5;
			m_IsEventDefined = isEventDefined;
		}

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined) : base(target, theFunction)
		{
			m_IsEventDefined = isEventDefined;
		}

		public override void Invoke(object[] args)
		{
			if (m_IsEventDefined)
				for (int i = 0; i < args.Length; i++)
					m_Args[i] = args[i];
			base.Invoke(m_Args);
		}

		public override void Invoke(T0 args0, T1 args1, T2 args2, T3 args3, T4 args4, T5 args5)
		{
			if (m_IsEventDefined)
			{
				m_Args[0] = args0;
				m_Args[1] = args1;
				m_Args[2] = args2;
				m_Args[3] = args3;
				m_Args[4] = args4;
				m_Args[5] = args5;
			}

			base.Invoke((T0)m_Args[0], (T1)m_Args[1], (T2)m_Args[2], (T3)m_Args[3], (T4)m_Args[4], (T5)m_Args[5]);
		}
	}

	public class UpdatableInvokableCall<T0, T1, T2, T3, T4, T5, T6> : InvokableCall2<T0, T1, T2, T3, T4, T5, T6>
	{
		private readonly object[] m_Args = new object[7];

		private bool m_IsEventDefined = false;

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) : base(target, theFunction)
		{
			m_Args[0] = arg0;
			m_Args[1] = arg1;
			m_Args[2] = arg2;
			m_Args[3] = arg3;
			m_Args[4] = arg4;
			m_Args[5] = arg5;
			m_Args[6] = arg6;
			m_IsEventDefined = isEventDefined;
		}

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined) : base(target, theFunction)
		{
			m_IsEventDefined = isEventDefined;
		}

		public override void Invoke(object[] args)
		{
			if (m_IsEventDefined)
				for (int i = 0; i < args.Length; i++)
					m_Args[i] = args[i];
			base.Invoke(m_Args);
		}

		public override void Invoke(T0 args0, T1 args1, T2 args2, T3 args3, T4 args4, T5 args5, T6 args6)
		{
			if (m_IsEventDefined)
			{
				m_Args[0] = args0;
				m_Args[1] = args1;
				m_Args[2] = args2;
				m_Args[3] = args3;
				m_Args[4] = args4;
				m_Args[5] = args5;
				m_Args[6] = args6;
			}

			base.Invoke((T0)m_Args[0], (T1)m_Args[1], (T2)m_Args[2], (T3)m_Args[3], (T4)m_Args[4], (T5)m_Args[5], (T6)m_Args[6]);
		}
	}

	public class UpdatableInvokableCall<T0, T1, T2, T3, T4, T5, T6, T7> : InvokableCall2<T0, T1, T2, T3, T4, T5, T6, T7>
	{
		private readonly object[] m_Args = new object[8];

		private bool m_IsEventDefined = false;

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) : base(target, theFunction)
		{
			m_Args[0] = arg0;
			m_Args[1] = arg1;
			m_Args[2] = arg2;
			m_Args[3] = arg3;
			m_Args[4] = arg4;
			m_Args[5] = arg5;
			m_Args[6] = arg6;
			m_Args[7] = arg7;
			m_IsEventDefined = isEventDefined;
		}

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined) : base(target, theFunction)
		{
			m_IsEventDefined = isEventDefined;
		}

		public override void Invoke(object[] args)
		{
			if (m_IsEventDefined)
				for (int i = 0; i < args.Length; i++)
					m_Args[i] = args[i];
			base.Invoke(m_Args);
		}

		public override void Invoke(T0 args0, T1 args1, T2 args2, T3 args3, T4 args4, T5 args5, T6 args6, T7 args7)
		{
			if (m_IsEventDefined)
			{
				m_Args[0] = args0;
				m_Args[1] = args1;
				m_Args[2] = args2;
				m_Args[3] = args3;
				m_Args[4] = args4;
				m_Args[5] = args5;
				m_Args[6] = args6;
				m_Args[7] = args7;
			}

			base.Invoke((T0)m_Args[0], (T1)m_Args[1], (T2)m_Args[2], (T3)m_Args[3], (T4)m_Args[4], (T5)m_Args[5], (T6)m_Args[6], (T7)m_Args[7]);
		}
	}

	public class UpdatableInvokableCall<T0, T1, T2, T3, T4, T5, T6, T7, T8> : InvokableCall2<T0, T1, T2, T3, T4, T5, T6, T7, T8>
	{
		private readonly object[] m_Args = new object[9];

		private bool m_IsEventDefined = false;

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) : base(target, theFunction)
		{
			m_Args[0] = arg0;
			m_Args[1] = arg1;
			m_Args[2] = arg2;
			m_Args[3] = arg3;
			m_Args[4] = arg4;
			m_Args[5] = arg5;
			m_Args[6] = arg6;
			m_Args[7] = arg7;
			m_Args[8] = arg8;
			m_IsEventDefined = isEventDefined;
		}

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined) : base(target, theFunction)
		{
			m_IsEventDefined = isEventDefined;
		}

		public override void Invoke(object[] args)
		{
			if (m_IsEventDefined)
				for (int i = 0; i < args.Length; i++)
					m_Args[i] = args[i];
			base.Invoke(m_Args);
		}

		public override void Invoke(T0 args0, T1 args1, T2 args2, T3 args3, T4 args4, T5 args5, T6 args6, T7 args7, T8 args8)
		{
			if (m_IsEventDefined)
			{
				m_Args[0] = args0;
				m_Args[1] = args1;
				m_Args[2] = args2;
				m_Args[3] = args3;
				m_Args[4] = args4;
				m_Args[5] = args5;
				m_Args[6] = args6;
				m_Args[7] = args7;
				m_Args[8] = args8;
			}

			base.Invoke((T0)m_Args[0], (T1)m_Args[1], (T2)m_Args[2], (T3)m_Args[3], (T4)m_Args[4], (T5)m_Args[5], (T6)m_Args[6], (T7)m_Args[7], (T8)m_Args[8]);
		}
	}

	public class UpdatableInvokableCall<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : InvokableCall2<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
	{
		private readonly object[] m_Args = new object[10];

		private bool m_IsEventDefined = false;

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) : base(target, theFunction)
		{
			m_Args[0] = arg0;
			m_Args[1] = arg1;
			m_Args[2] = arg2;
			m_Args[3] = arg3;
			m_Args[4] = arg4;
			m_Args[5] = arg5;
			m_Args[6] = arg6;
			m_Args[7] = arg7;
			m_Args[8] = arg8;
			m_Args[9] = arg9;
			m_IsEventDefined = isEventDefined;
		}

		public UpdatableInvokableCall(Object target, MethodInfo theFunction, bool isEventDefined) : base(target, theFunction)
		{
			m_IsEventDefined = isEventDefined;
		}

		public override void Invoke(object[] args)
		{
			if (m_IsEventDefined)
				for (int i = 0; i < args.Length; i++)
					m_Args[i] = args[i];
			base.Invoke(m_Args);
		}

		public override void Invoke(T0 args0, T1 args1, T2 args2, T3 args3, T4 args4, T5 args5, T6 args6, T7 args7, T8 args8, T9 args9)
		{
			if (m_IsEventDefined)
			{
				m_Args[0] = args0;
				m_Args[1] = args1;
				m_Args[2] = args2;
				m_Args[3] = args3;
				m_Args[4] = args4;
				m_Args[5] = args5;
				m_Args[6] = args6;
				m_Args[7] = args7;
				m_Args[8] = args8;
				m_Args[9] = args9;
			}

			base.Invoke((T0)m_Args[0], (T1)m_Args[1], (T2)m_Args[2], (T3)m_Args[3], (T4)m_Args[4], (T5)m_Args[5], (T6)m_Args[6], (T7)m_Args[7], (T8)m_Args[8], (T9)m_Args[9]);
		}
	}

	[Serializable]
	class PersistentCall2
	{
		[FormerlySerializedAs("arguments")]
		[SerializeField]
		private List<ArgumentCache2> m_Arguments = new List<ArgumentCache2>();
		[SerializeField]
		[FormerlySerializedAs("enabled")]
		[FormerlySerializedAs("m_Enabled")]
		private UnityEventCallState m_CallState = UnityEventCallState.RuntimeOnly;
		[SerializeField]
		[FormerlySerializedAs("instance")]
		private Object m_Target;
		[FormerlySerializedAs("methodName")]
		[SerializeField]
		private string m_MethodName;
		[SerializeField]
		[FormerlySerializedAs("modes")]
		private List<PersistentListenerMode2> m_Modes = new List<PersistentListenerMode2>();

		public Object target
		{
			get { return m_Target; }
		}

		public string methodName
		{
			get { return m_MethodName; }
		}

		public List<ArgumentCache2> arguments
		{
			get { return m_Arguments; }
		}

		public UnityEventCallState callState
		{
			get { return m_CallState; }
			set { m_CallState = value; }
		}

		public List<PersistentListenerMode2> modes
		{
			get { return m_Modes; }
			set { m_Modes = value; }
		}

		public bool IsValid()
		{
			if (target != null)
				return !string.IsNullOrEmpty(methodName);
			return false;
		}

		public BaseInvokableCall2 GetRuntimeCall(UnityEventBase2 theEvent, object[] parameters)
		{
			if (m_CallState == UnityEventCallState.RuntimeOnly && !Application.isPlaying)
				return null;
			if (m_CallState == UnityEventCallState.Off || theEvent == null)
				return null;
			if (m_Modes.Count == 0)
				return null;

			MethodInfo method = theEvent.FindMethod(this);
			if (method == null)
				return null;

			if (m_Modes.Count == 1)
			{
				switch (m_Modes[0])
				{
					case PersistentListenerMode2.Void:
						return new InvokableCall2(target, method);
					case PersistentListenerMode2.Char:
						return new UpdatableInvokableCall<char>(target, method, false, m_Arguments[0].charArgument);
					case PersistentListenerMode2.Byte:
						return new UpdatableInvokableCall<byte>(target, method, false, m_Arguments[0].byteArgument);
					case PersistentListenerMode2.SByte:
						return new UpdatableInvokableCall<sbyte>(target, method, false, m_Arguments[0].sbyteArgument);
					case PersistentListenerMode2.Short:
						return new UpdatableInvokableCall<short>(target, method, false, m_Arguments[0].shortArgument);
					case PersistentListenerMode2.UShort:
						return new UpdatableInvokableCall<ushort>(target, method, false, m_Arguments[0].ushortArgument);
					case PersistentListenerMode2.Int:
						return new UpdatableInvokableCall<int>(target, method, false, m_Arguments[0].intArgument);
					case PersistentListenerMode2.UInt:
						return new UpdatableInvokableCall<uint>(target, method, false, m_Arguments[0].uintArgument);
					case PersistentListenerMode2.Long:
						return new UpdatableInvokableCall<long>(target, method, false, m_Arguments[0].intArgument);
					case PersistentListenerMode2.Float:
						return new UpdatableInvokableCall<float>(target, method, false, m_Arguments[0].floatArgument);
					case PersistentListenerMode2.Double:
						return new UpdatableInvokableCall<double>(target, method, false, m_Arguments[0].floatArgument);
					case PersistentListenerMode2.String:
						return new UpdatableInvokableCall<string>(target, method, false, m_Arguments[0].stringArgument);
					case PersistentListenerMode2.Bool:
						return new UpdatableInvokableCall<bool>(target, method, false, m_Arguments[0].boolArgument);
					case PersistentListenerMode2.Vector2:
						return new UpdatableInvokableCall<Vector2>(target, method, false, m_Arguments[0].vector2Argument);
					case PersistentListenerMode2.Vector3:
						return new UpdatableInvokableCall<Vector3>(target, method, false, m_Arguments[0].vector3Argument);
#if UNITY_2017_2_OR_NEWER
					case PersistentListenerMode2.Vector2Int:
						return new UpdatableInvokableCall<Vector2Int>(target, method, false, m_Arguments[0].vector2IntArgument);
					case PersistentListenerMode2.Vector3Int:
						return new UpdatableInvokableCall<Vector3Int>(target, method, false, m_Arguments[0].vector3IntArgument);
#endif
					case PersistentListenerMode2.Vector4:
						return new UpdatableInvokableCall<Vector4>(target, method, false, m_Arguments[0].vector4Argument);
					case PersistentListenerMode2.LayerMask:
						return new UpdatableInvokableCall<LayerMask>(target, method, false, m_Arguments[0].layerMaskArgument);
					case PersistentListenerMode2.Color:
						return new UpdatableInvokableCall<Color>(target, method, false, m_Arguments[0].colorArgument);
					case PersistentListenerMode2.Quaternion:
						return new UpdatableInvokableCall<Quaternion>(target, method, false, m_Arguments[0].quaternionArgument);
					case PersistentListenerMode2.Array:
						return GetArrayCall(target, method, m_Arguments[0]);
					case PersistentListenerMode2.Object:
						return GetObjectCall(target, method, m_Arguments[0]);
					default:
						break;
				}
			}

			return GetInvokableCall(target, method, m_Modes, m_Arguments.ToArray(), parameters);
		}

		private BaseInvokableCall2 GetObjectCall(Object target, MethodInfo method, ArgumentCache2 argument)
		{
			Type type = typeof(Object);
			if (!string.IsNullOrEmpty(argument.unityObjectArgumentAssemblyTypeName))
				type = Type.GetType(argument.unityObjectArgumentAssemblyTypeName, false) ?? typeof(Object);
			ConstructorInfo constructor = typeof(UpdatableInvokableCall<>).MakeGenericType(type).GetConstructor(new Type[] { typeof(Object), typeof(MethodInfo), typeof(bool), type });
			Object @object = argument.unityObjectArgument;
			if (@object != null && !type.IsAssignableFrom(@object.GetType()))
				@object = null;

			return constructor.Invoke(new object[] { target, method, false, @object }) as BaseInvokableCall2;
		}

		private BaseInvokableCall2 GetArrayCall(Object target, MethodInfo method, ArgumentCache2 argument)
		{
			Type type = typeof(List<Object>);
			if (!string.IsNullOrEmpty(argument.unityObjectArgumentAssemblyTypeName))
				type = Type.GetType(argument.unityObjectArgumentAssemblyTypeName, false) ?? typeof(List<Object>);

			ConstructorInfo constructor = typeof(UpdatableInvokableCall<>).MakeGenericType(type).GetConstructor(new Type[] { typeof(Object), typeof(MethodInfo), typeof(bool), type });

			object arrayArgument = GetValue(type, argument);

			return constructor.Invoke(new object[] { target, method, false, arrayArgument }) as BaseInvokableCall2;
		}

		private BaseInvokableCall2 GetInvokableCall(Object target, MethodInfo method, List<PersistentListenerMode2> modes, ArgumentCache2[] arguments, object[] informedParameters)
		{
			Type[] typeArguments = new Type[arguments.Length];
			Type[] types = new Type[arguments.Length + 3];
			object[] parameters = new object[arguments.Length + 3];

			types[0] = typeof(Object);
			types[1] = typeof(MethodInfo);
			types[2] = typeof(bool);

			parameters[0] = target;
			parameters[1] = method;
			parameters[2] = false;

			for (int i = 0; i < typeArguments.Length; i++)
			{
				typeArguments[i] = Type.GetType(arguments[i].unityObjectArgumentAssemblyTypeName, false) ?? typeof(Object);
				types[i + 3] = typeArguments[i];

				if (modes[i] == PersistentListenerMode2.EventDefined)
				{
					parameters[2] = true;
					parameters[i + 3] = informedParameters[i];
				}
				else
				{
					parameters[i + 3] = GetValue(typeArguments[i], arguments[i]);
				}
			}

			Type invokableType = typeof(InvokableCall2);

			if (typeArguments.Length == 1)
				invokableType = typeof(UpdatableInvokableCall<>);
			else if (typeArguments.Length == 2)
				invokableType = typeof(UpdatableInvokableCall<,>);
			else if (typeArguments.Length == 3)
				invokableType = typeof(UpdatableInvokableCall<,,>);
			else if (typeArguments.Length == 4)
				invokableType = typeof(UpdatableInvokableCall<,,,>);
			else if (typeArguments.Length == 5)
				invokableType = typeof(UpdatableInvokableCall<,,,,>);
			else if (typeArguments.Length == 6)
				invokableType = typeof(UpdatableInvokableCall<,,,,,>);
			else if (typeArguments.Length == 7)
				invokableType = typeof(UpdatableInvokableCall<,,,,,,>);
			else if (typeArguments.Length == 8)
				invokableType = typeof(UpdatableInvokableCall<,,,,,,,>);
			else if (typeArguments.Length == 9)
				invokableType = typeof(UpdatableInvokableCall<,,,,,,,,>);
			else if (typeArguments.Length == 10)
				invokableType = typeof(UpdatableInvokableCall<,,,,,,,,,>);

			ConstructorInfo constructor = invokableType.MakeGenericType(typeArguments).GetConstructor(types);

			return constructor == null ? null : constructor.Invoke(parameters) as BaseInvokableCall2;
		}

		private object GetValue(Type type, ArgumentCache2 argument)
		{
			if (type == typeof(char))
				return argument.charArgument;
			else if (type == typeof(byte))
				return argument.byteArgument;
			else if (type == typeof(sbyte))
				return argument.sbyteArgument;
			else if (type == typeof(short))
				return argument.shortArgument;
			else if (type == typeof(ushort))
				return argument.ushortArgument;
			else if (type == typeof(int))
				return argument.intArgument;
			else if (type == typeof(uint))
				return argument.uintArgument;
			else if (type.IsEnum)
				return Enum.GetValues(type).GetValue(argument.intArgument);
			else if (type == typeof(long))
				return argument.longArgument;
			else if (type == typeof(float))
				return argument.floatArgument;
			else if (type == typeof(double))
				return argument.doubleArgument;
			else if (type == typeof(string))
				return argument.stringArgument;
			else if (type == typeof(bool))
				return argument.boolArgument;
			else if (type == typeof(Vector2))
				return argument.vector2Argument;
			else if (type == typeof(Vector3))
				return argument.vector3Argument;
#if UNITY_2017_2_OR_NEWER
			else if (type == typeof(Vector2Int))
				return argument.vector2IntArgument;
			else if (type == typeof(Vector3Int))
				return argument.vector3IntArgument;
#endif
			else if (type == typeof(Vector4))
				return argument.vector4Argument;
			else if (type == typeof(LayerMask))
				return argument.layerMaskArgument;
			else if (type == typeof(Color))
				return argument.colorArgument;
			else if (type == typeof(Quaternion))
				return argument.quaternionArgument;
			else if (type.IsArray)
			{
				if (type == typeof(char[]))
					return argument.charArrayArgument.ToArray();
				else if (type == typeof(byte[]))
					return argument.byteArrayArgument.ToArray();
				else if (type == typeof(sbyte[]))
					return argument.sbyteArrayArgument.ToArray();
				else if (type == typeof(short[]))
					return argument.shortArrayArgument.ToArray();
				else if (type == typeof(ushort[]))
					return argument.ushortArrayArgument.ToArray();
				else if (type == typeof(int[]))
					return argument.intArrayArgument.ToArray();
				else if (type == typeof(uint[]))
					return argument.uintArrayArgument.ToArray();
				else if (type == typeof(long[]))
					return argument.longArrayArgument.ToArray();
				else if (type == typeof(float[]))
					return argument.floatArrayArgument.ToArray();
				else if (type == typeof(double[]))
					return argument.doubleArrayArgument.ToArray();
				else if (type == typeof(string[]))
					return argument.stringArrayArgument.ToArray();
				else if (type == typeof(bool[]))
					return argument.boolArrayArgument.ToArray();
				else if (type == typeof(Vector2[]))
					return argument.vector2ArrayArgument.ToArray();
#if UNITY_2017_2_OR_NEWER
				else if (type == typeof(Vector2Int[]))
					return argument.vector2IntArrayArgument.ToArray();
#endif
				else if (type == typeof(Vector3[]))
					return argument.vector3ArrayArgument.ToArray();
#if UNITY_2017_2_OR_NEWER
				else if (type == typeof(Vector3Int[]))
					return argument.vector3IntArrayArgument.ToArray();
#endif
				else if (type == typeof(Vector4[]))
					return argument.vector4ArrayArgument.ToArray();
				else if (type == typeof(LayerMask[]))
					return argument.layerMaskArrayArgument.ToArray();
				else if (type == typeof(Color[]))
					return argument.colorArrayArgument.ToArray();
				else if (type == typeof(Quaternion[]))
					return argument.quaternionArrayArgument.ToArray();
				else if (type.GetElementType().IsEnum)
				{
					Type listType = typeof(List<>).MakeGenericType(type.GetElementType());
					var instance = Activator.CreateInstance(listType);

					for (int i = 0; i < argument.intArrayArgument.Count; i++)
						listType.GetMethod("Add").Invoke(instance, new object[] { argument.intArrayArgument[i] });

					return listType.GetMethod("ToArray").Invoke(instance, null);
				}
				else if (type == typeof(ScriptableObject[]))
					return Array.ConvertAll(argument.unityObjectArrayArgument.ToArray(), x => (ScriptableObject)x);
				else if (type == typeof(GameObject[]))
					return Array.ConvertAll(argument.unityObjectArrayArgument.ToArray(), x => (GameObject)x);
				else
				{
					Type listType = typeof(List<>).MakeGenericType(type.GetElementType());
					var instance = Activator.CreateInstance(listType);

					for (int i = 0; i < argument.unityObjectArrayArgument.Count; i++)
						listType.GetMethod("Add").Invoke(instance, new object[] { Convert.ChangeType(argument.unityObjectArrayArgument[i], type.GetElementType()) });

					return listType.GetMethod("ToArray").Invoke(instance, null);
				}
			}
			else if (IsValidListType(type))
			{
				if (type == typeof(List<char>))
					return argument.charArrayArgument;
				else if (type == typeof(List<byte>))
					return argument.byteArrayArgument;
				else if (type == typeof(List<sbyte>))
					return argument.sbyteArrayArgument;
				else if (type == typeof(List<short>))
					return argument.shortArrayArgument;
				else if (type == typeof(List<ushort>))
					return argument.ushortArrayArgument;
				else if (type == typeof(List<int>))
					return argument.intArrayArgument;
				else if (type == typeof(List<uint>))
					return argument.uintArrayArgument;
				else if (type == typeof(List<long>))
					return argument.longArrayArgument;
				else if (type == typeof(List<float>))
					return argument.floatArrayArgument;
				else if (type == typeof(List<double>))
					return argument.doubleArrayArgument;
				else if (type == typeof(List<string>))
					return argument.stringArrayArgument;
				else if (type == typeof(List<bool>))
					return argument.boolArrayArgument;
				else if (type == typeof(List<Vector2>))
					return argument.vector2ArrayArgument;
#if UNITY_2017_2_OR_NEWER
				else if (type == typeof(List<Vector2Int>))
					return argument.vector2IntArrayArgument;
#endif
				else if (type == typeof(List<Vector3>))
					return argument.vector3ArrayArgument;
#if UNITY_2017_2_OR_NEWER
				else if (type == typeof(List<Vector3Int>))
					return argument.vector3IntArrayArgument;
#endif
				else if (type == typeof(List<Vector4>))
					return argument.vector4ArrayArgument;
				else if (type == typeof(List<LayerMask>))
					return argument.layerMaskArrayArgument;
				else if (type == typeof(List<Color>))
					return argument.colorArrayArgument;
				else if (type == typeof(List<Quaternion>))
					return argument.quaternionArrayArgument;
				else if (type.GetGenericArguments()[0].IsEnum)
				{
					Type listType = typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]);
					var instance = Activator.CreateInstance(listType);

					for (int i = 0; i < argument.intArrayArgument.Count; i++)
						listType.GetMethod("Add").Invoke(instance, new object[] { argument.intArrayArgument[i] });

					return instance;
				}
				else
				{
					Type listType = typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]);
					var instance = Activator.CreateInstance(listType);

					for (int i = 0; i < argument.unityObjectArrayArgument.Count; i++)
						listType.GetMethod("Add").Invoke(instance, new object[] { Convert.ChangeType(argument.unityObjectArrayArgument[i], type.GetGenericArguments()[0]) });

					return instance;
				}
			}

			if (argument.unityObjectArgument != null && !type.IsAssignableFrom(argument.unityObjectArgument.GetType()))
				return null;

			return argument.unityObjectArgument;
		}

		public void RegisterPersistentListener(Object target, string methodName)
		{
			m_Target = target;
			m_MethodName = methodName;
		}

		public void UnregisterPersistentListener()
		{
			m_Target = null;
			m_MethodName = string.Empty;
		}

		private bool IsValidListType(Type type)
		{
			var genericArguments = type.GetGenericArguments();

			return type.IsGenericType
					&& genericArguments.Length == 1
					&& typeof(List<>).MakeGenericType(genericArguments[0]).IsAssignableFrom(type);
		}
	}

	[Serializable]
	class PersistentCallGroup2
	{
		[SerializeField]
		[FormerlySerializedAs("m_Listeners")]
		private List<PersistentCall2> m_Calls;

		public int Count
		{
			get { return m_Calls.Count; }
		}

		public PersistentCallGroup2()
		{
			m_Calls = new List<PersistentCall2>();
		}

		public PersistentCall2 GetListener(int index)
		{
			return m_Calls[index];
		}

		public IEnumerable<PersistentCall2> GetListeners()
		{
			return m_Calls;
		}

		public void AddListener()
		{
			m_Calls.Add(new PersistentCall2());
		}

		public void AddListener(PersistentCall2 call)
		{
			m_Calls.Add(call);
		}

		public void RemoveListener(int index)
		{
			m_Calls.RemoveAt(index);
		}

		public void Clear()
		{
			m_Calls.Clear();
		}

		public void RegisterEventPersistentListener(int index, Object targetObj, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.EventDefined);
		}

		public void RegisterVoidPersistentListener(int index, Object targetObj, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Void);
		}

		public void RegisterObjectPersistentListener(int index, Object targetObj, Object argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Object);
			listener.arguments.Add(new ArgumentCache2() { unityObjectArgument = argument });
		}

		public void RegisterCharPersistentListener(int index, Object targetObj, char argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Char);
			listener.arguments.Add(new ArgumentCache2() { charArgument = argument });
		}

		public void RegisterBytePersistentListener(int index, Object targetObj, byte argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Byte);
			listener.arguments.Add(new ArgumentCache2() { byteArgument = argument });
		}

		public void RegisterSBytePersistentListener(int index, Object targetObj, sbyte argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.SByte);
			listener.arguments.Add(new ArgumentCache2() { sbyteArgument = argument });
		}

		public void RegisterShortPersistentListener(int index, Object targetObj, short argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Short);
			listener.arguments.Add(new ArgumentCache2() { shortArgument = argument });
		}

		public void RegisterUShortPersistentListener(int index, Object targetObj, ushort argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.UShort);
			listener.arguments.Add(new ArgumentCache2() { ushortArgument = argument });
		}

		public void RegisterIntPersistentListener(int index, Object targetObj, int argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Int);
			listener.arguments.Add(new ArgumentCache2() { intArgument = argument });
		}

		public void RegisterUIntPersistentListener(int index, Object targetObj, uint argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.UInt);
			listener.arguments.Add(new ArgumentCache2() { uintArgument = argument });
		}

		public void RegisterLongPersistentListener(int index, Object targetObj, long argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Long);
			listener.arguments.Add(new ArgumentCache2() { longArgument = argument });
		}

		public void RegisterFloatPersistentListener(int index, Object targetObj, float argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Float);
			listener.arguments.Add(new ArgumentCache2() { floatArgument = argument });
		}

		public void RegisterDoublePersistentListener(int index, Object targetObj, double argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Double);
			listener.arguments.Add(new ArgumentCache2() { doubleArgument = argument });
		}

		public void RegisterStringPersistentListener(int index, Object targetObj, string argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.String);
			listener.arguments.Add(new ArgumentCache2() { stringArgument = argument });
		}

		public void RegisterBoolPersistentListener(int index, Object targetObj, bool argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Bool);
			listener.arguments.Add(new ArgumentCache2() { boolArgument = argument });
		}

		public void RegisterEnumPersistentListener(int index, Object targetObj, Enum argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Enum);
			listener.arguments.Add(new ArgumentCache2() { enumArgument = argument });
		}

		public void RegisterVector2PersistentListener(int index, Object targetObj, Vector2 argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Vector2);
			listener.arguments.Add(new ArgumentCache2() { vector2Argument = argument });
		}
#if UNITY_2017_2_OR_NEWER
		public void RegisterVector2IntPersistentListener(int index, Object targetObj, Vector2Int argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Vector2Int);
			listener.arguments.Add(new ArgumentCache2() { vector2IntArgument = argument });
		}
#endif

		public void RegisterVector3PersistentListener(int index, Object targetObj, Vector3 argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Vector3);
			listener.arguments.Add(new ArgumentCache2() { vector3Argument = argument });
		}
#if UNITY_2017_2_OR_NEWER
		public void RegisterVector3IntPersistentListener(int index, Object targetObj, Vector3Int argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Vector3Int);
			listener.arguments.Add(new ArgumentCache2() { vector3IntArgument = argument });
		}
#endif

		public void RegisterVector4PersistentListener(int index, Object targetObj, Vector4 argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Vector4);
			listener.arguments.Add(new ArgumentCache2() { vector4Argument = argument });
		}

		public void RegisterLayerMaskPersistentListener(int index, Object targetObj, LayerMask argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.LayerMask);
			listener.arguments.Add(new ArgumentCache2() { layerMaskArgument = argument });
		}

		public void RegisterColorPersistentListener(int index, Object targetObj, Color argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Color);
			listener.arguments.Add(new ArgumentCache2() { colorArgument = argument });
		}

		public void RegisterQuaternionPersistentListener(int index, Object targetObj, Quaternion argument, string methodName)
		{
			PersistentCall2 listener = GetListener(index);
			listener.RegisterPersistentListener(targetObj, methodName);
			listener.modes.Add(PersistentListenerMode2.Quaternion);
			listener.arguments.Add(new ArgumentCache2() { quaternionArgument = argument });
		}

		public void UnregisterPersistentListener(int index)
		{
			GetListener(index).UnregisterPersistentListener();
		}

		public void RemoveListeners(Object target, string methodName)
		{
			List<PersistentCall2> persistentCallList = new List<PersistentCall2>();
			for (int index = 0; index < m_Calls.Count; ++index)
			{
				if (m_Calls[index].target == target && m_Calls[index].methodName == methodName)
					persistentCallList.Add(m_Calls[index]);
			}
			m_Calls.RemoveAll(new Predicate<PersistentCall2>(persistentCallList.Contains));
		}

		public void Initialize(InvokableCallList2 invokableList, UnityEventBase2 unityEventBase, object[] parameters)
		{
			foreach (var persistentCall in m_Calls)
			{
				if (!persistentCall.IsValid())
					continue;

				var call = persistentCall.GetRuntimeCall(unityEventBase, parameters);
				if (call != null)
					invokableList.AddPersistentInvokableCall(call);
			}
		}
	}

	class InvokableCallList2
	{
		private readonly List<BaseInvokableCall2> m_PersistentCalls = new List<BaseInvokableCall2>();
		private readonly List<BaseInvokableCall2> m_RuntimeCalls = new List<BaseInvokableCall2>();

		private readonly List<BaseInvokableCall2> m_ExecutingCalls = new List<BaseInvokableCall2>();

		private bool m_NeedsUpdate = true;

		public int Count
		{
			get { return m_PersistentCalls.Count + m_RuntimeCalls.Count; }
		}

		public void AddPersistentInvokableCall(BaseInvokableCall2 call)
		{
			m_PersistentCalls.Add(call);
			m_NeedsUpdate = true;
		}

		public void AddListener(BaseInvokableCall2 call)
		{
			m_RuntimeCalls.Add(call);
			m_NeedsUpdate = true;
		}

		public void RemoveListener(object targetObj, MethodInfo method)
		{
			List<BaseInvokableCall2> baseInvokableCallList = new List<BaseInvokableCall2>();
			for (int index = 0; index < m_RuntimeCalls.Count; ++index)
			{
				if (m_RuntimeCalls[index].Find(targetObj, method))
					baseInvokableCallList.Add(m_RuntimeCalls[index]);
			}
			m_RuntimeCalls.RemoveAll(new Predicate<BaseInvokableCall2>(baseInvokableCallList.Contains));
			m_NeedsUpdate = true;
		}

		public void Clear()
		{
			m_RuntimeCalls.Clear();
			m_NeedsUpdate = true;
		}

		public void ClearPersistent()
		{
			m_PersistentCalls.Clear();
			m_NeedsUpdate = true;
		}

		public List<BaseInvokableCall2> PrepareInvoke()
		{
			if (m_NeedsUpdate)
			{
				m_ExecutingCalls.Clear();
				m_ExecutingCalls.AddRange(m_PersistentCalls);
				m_ExecutingCalls.AddRange(m_RuntimeCalls);
				m_NeedsUpdate = false;
			}

			return m_ExecutingCalls;
		}

		public void Invoke(object[] parameters)
		{
			if (m_NeedsUpdate)
			{
				m_ExecutingCalls.Clear();
				m_ExecutingCalls.AddRange(m_PersistentCalls);
				m_ExecutingCalls.AddRange(m_RuntimeCalls);
				m_NeedsUpdate = false;
			}
			for (int index = 0; index < m_ExecutingCalls.Count; ++index)
				m_ExecutingCalls[index].Invoke(parameters);
		}
	}

	[Serializable]
	public abstract class UnityEventBase2 : ISerializationCallbackReceiver
	{
		private bool m_CallsDirty = true;
		private InvokableCallList2 m_Calls;

		[SerializeField]
		[FormerlySerializedAs("m_PersistentListeners")]
		private PersistentCallGroup2 m_PersistentCalls = null;

#pragma warning disable 414 //used by serialized properties
		//[SerializeField]
		//private string m_TypeName;
#pragma warning restore 414

		protected UnityEventBase2()
		{
			//m_TypeName = GetType().AssemblyQualifiedName;
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			DirtyPersistentCalls();
			//m_TypeName = GetType().AssemblyQualifiedName;
		}

		protected abstract MethodInfo FindMethod_Impl(object targetObj, string name);

		internal abstract BaseInvokableCall2 GetDelegate(object target, MethodInfo theFunction);

		internal MethodInfo FindMethod(PersistentCall2 call)
		{
			Type[] argumentTypes = new Type[call.arguments.Count];

			for (int i = 0; i < argumentTypes.Length; i++)
				argumentTypes[i] = Type.GetType(call.arguments[i].unityObjectArgumentAssemblyTypeName, false) ?? typeof(Object);

			return GetValidMethodInfo(call.target, call.methodName, argumentTypes);
		}

		internal MethodInfo FindMethod(object listener, string name, PersistentListenerMode2 mode, Type argumentType)
		{
			switch (mode)
			{
				case PersistentListenerMode2.EventDefined:
					return FindMethod_Impl(listener, name);
				case PersistentListenerMode2.Void:
					return GetValidMethodInfo(listener, name, new Type[0]);
				case PersistentListenerMode2.Object:
					return GetValidMethodInfo(listener, name, new Type[1] { argumentType ?? typeof(Object) });
				case PersistentListenerMode2.Char:
					return GetValidMethodInfo(listener, name, new Type[1] { typeof(char) });
				case PersistentListenerMode2.Byte:
					return GetValidMethodInfo(listener, name, new Type[1] { typeof(byte) });
				case PersistentListenerMode2.SByte:
					return GetValidMethodInfo(listener, name, new Type[1] { typeof(sbyte) });
				case PersistentListenerMode2.Short:
					return GetValidMethodInfo(listener, name, new Type[1] { typeof(short) });
				case PersistentListenerMode2.UShort:
					return GetValidMethodInfo(listener, name, new Type[1] { typeof(ushort) });
				case PersistentListenerMode2.Int:
					return GetValidMethodInfo(listener, name, new Type[1] { typeof(int) });
				case PersistentListenerMode2.UInt:
					return GetValidMethodInfo(listener, name, new Type[1] { typeof(uint) });
				case PersistentListenerMode2.Long:
					return GetValidMethodInfo(listener, name, new Type[1] { typeof(long) });
				case PersistentListenerMode2.Float:
					return GetValidMethodInfo(listener, name, new Type[1] { typeof(float) });
				case PersistentListenerMode2.Double:
					return GetValidMethodInfo(listener, name, new Type[1] { typeof(double) });
				case PersistentListenerMode2.String:
					return GetValidMethodInfo(listener, name, new Type[1] { typeof(string) });
				case PersistentListenerMode2.Bool:
					return GetValidMethodInfo(listener, name, new Type[1] { typeof(bool) });
				case PersistentListenerMode2.Enum:
					return GetValidMethodInfo(listener, name, new Type[1] { typeof(Enum) });
				default:
					return null;
			}
		}

		public MethodInfo FindMethod(object listener, string name, PersistentListenerMode2 mode, Type[] argumentTypes = null)
		{
			if (mode == PersistentListenerMode2.EventDefined)
				return FindMethod_Impl(listener, name);

			return GetValidMethodInfo(listener, name, argumentTypes);
		}

		/// <summary>
		///   <para>Get the number of registered persistent listeners.</para>
		/// </summary>
		public int GetPersistentEventCount()
		{
			return m_PersistentCalls.Count;
		}

		/// <summary>
		///   <para>Get the target component of the listener at index index.</para>
		/// </summary>
		/// <param name="index">Index of the listener to query.</param>
		public Object GetPersistentTarget(int index)
		{
			PersistentCall2 listener = m_PersistentCalls.GetListener(index);
			if (listener != null)
				return listener.target;
			return null;
		}

		/// <summary>
		///   <para>Get the target method name of the listener at index index.</para>
		/// </summary>
		/// <param name="index">Index of the listener to query.</param>
		public string GetPersistentMethodName(int index)
		{
			PersistentCall2 listener = m_PersistentCalls.GetListener(index);
			if (listener != null)
				return listener.methodName;
			return string.Empty;
		}

		private void DirtyPersistentCalls()
		{
			Calls.ClearPersistent();
			m_CallsDirty = true;
		}

		private void RebuildPersistentCallsIfNeeded(object[] parameters)
		{
			if (!m_CallsDirty) return;

			m_PersistentCalls.Initialize(Calls, this, parameters);
			m_CallsDirty = false;
		}

		/// <summary>
		///   <para>Modify the execution state of a persistent listener.</para>
		/// </summary>
		/// <param name="index">Index of the listener to query.</param>
		/// <param name="state">State to set.</param>
		public void SetPersistentListenerState(int index, UnityEventCallState state)
		{
			PersistentCall2 listener = m_PersistentCalls.GetListener(index);
			if (listener != null)
				listener.callState = state;
			DirtyPersistentCalls();
		}

		protected void AddListener(object targetObj, MethodInfo method)
		{
			Calls.AddListener(GetDelegate(targetObj, method));
		}

		internal void AddCall(BaseInvokableCall2 call)
		{
			Calls.AddListener(call);
		}

		protected void RemoveListener(object targetObj, MethodInfo method)
		{
			Calls.RemoveListener(targetObj, method);
		}

		/// <summary>
		/// Remove all listeners from the event.
		/// </summary>
		public void RemoveAllListeners()
		{
			Calls.Clear();
		}

		protected List<BaseInvokableCall2> PrepareInvoke(params object[] parameters)
		{
			RebuildPersistentCallsIfNeeded(parameters);
			return Calls.PrepareInvoke();
		}

		protected void Invoke(object[] parameters)
		{
			List<BaseInvokableCall2> calls = PrepareInvoke(parameters);

			for (var i = 0; i < calls.Count; i++)
				calls[i].Invoke(parameters);
		}

		public override string ToString()
		{
			return base.ToString() + " " + GetType().FullName;
		}

		/// <summary>
		///   <para>Given an object, function name, and a list of argument types; find the method that matches.</para>
		/// </summary>
		/// <param name="obj">Object to search for the method.</param>
		/// <param name="functionName">Function name to search for.</param>
		/// <param name="argumentTypes">Argument types for the function.</param>
		public static MethodInfo GetValidMethodInfo(object obj, string functionName, Type[] argumentTypes)
		{
			//if (functionName == "TestGameObjectList" || functionName == "TestGameObjectArray")
			//	Debug.LogFormat("Target:{0}, functioName:{1}, argumentType:{2}", obj, functionName, argumentTypes[0]);
			for (Type type = obj.GetType(); type != typeof(object) && type != null; type = type.BaseType)
			{
				MethodInfo method = type.GetMethod(functionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, argumentTypes, null);
				if (method != null)
				{
					ParameterInfo[] parameters = method.GetParameters();

					if (parameters.Length != argumentTypes.Length)
						continue;

					bool flag = true;
					int index = 0;
					foreach (ParameterInfo parameterInfo in parameters)
					{
						flag = argumentTypes[index].IsPrimitive == parameterInfo.ParameterType.IsPrimitive;
						if (flag)
							++index;
						else
							break;
					}
					if (flag)
						return method;
				}
			}
			return null;
		}

		protected bool ValidateRegistration(MethodInfo method, object targetObj, PersistentListenerMode2 mode)
		{
			return ValidateRegistration(method, targetObj, mode, typeof(Object));
		}

		protected bool ValidateRegistration(MethodInfo method, object targetObj, PersistentListenerMode2 mode, Type argumentType)
		{
			if (method == null)
				throw new ArgumentNullException("method", string.Format("Can not register null method on {0} for callback!", targetObj));
			Object @object = targetObj as Object;
			if (@object == null || @object.GetEntityId() == EntityId.None)
				throw new ArgumentException(string.Format("Could not register callback {0} on {1}. The class {2} does not derive from UnityEngine.Object", method.Name, targetObj, (targetObj != null ? targetObj.GetType().ToString() : "null")));
			if (method.IsStatic)
				throw new ArgumentException(string.Format("Could not register listener {0} on {1} static functions are not supported.", method, GetType()));
			if (FindMethod(targetObj, method.Name, mode, argumentType) != null)
				return true;
			Debug.LogWarning(string.Format("Could not register listener {0}.{1} on {2} the method could not be found.", targetObj, method, GetType()));
			return false;
		}

		internal void AddPersistentListener()
		{
			m_PersistentCalls.AddListener();
		}

		protected void RegisterPersistentListener(int index, object targetObj, MethodInfo method)
		{
			if (!ValidateRegistration(method, targetObj, PersistentListenerMode2.EventDefined))
				return;
			m_PersistentCalls.RegisterEventPersistentListener(index, targetObj as Object, method.Name);
			DirtyPersistentCalls();
		}

        internal void RemovePersistentListener(Object target, MethodInfo method)
        {
            if (method == null || method.IsStatic || (target == null || target.GetEntityId() == EntityId.None))
                return;
            m_PersistentCalls.RemoveListeners(target, method.Name);
            DirtyPersistentCalls();
        }

		internal void RemovePersistentListener(int index)
		{
			m_PersistentCalls.RemoveListener(index);
			DirtyPersistentCalls();
		}

		internal void UnregisterPersistentListener(int index)
		{
			m_PersistentCalls.UnregisterPersistentListener(index);
			DirtyPersistentCalls();
		}

		internal void AddVoidPersistentListener(UnityAction call)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterVoidPersistentListener(persistentEventCount, call);
		}

		internal void RegisterVoidPersistentListener(int index, UnityAction call)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Void))
					return;
				m_PersistentCalls.RegisterVoidPersistentListener(index, call.Target as Object, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddCharPersistentListener(UnityAction<char> call, char argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterCharPersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterCharPersistentListener(int index, UnityAction<char> call, char argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Char))
					return;
				m_PersistentCalls.RegisterCharPersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddBytePersistentListener(UnityAction<byte> call, byte argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterBytePersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterBytePersistentListener(int index, UnityAction<byte> call, byte argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Byte))
					return;
				m_PersistentCalls.RegisterBytePersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddSBytePersistentListener(UnityAction<SByte> call, SByte argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterSBytePersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterSBytePersistentListener(int index, UnityAction<SByte> call, SByte argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Int))
					return;
				m_PersistentCalls.RegisterSBytePersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddShortPersistentListener(UnityAction<short> call, short argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterShortPersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterShortPersistentListener(int index, UnityAction<short> call, short argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Short))
					return;
				m_PersistentCalls.RegisterShortPersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddUShortPersistentListener(UnityAction<ushort> call, ushort argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterUShortPersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterUShortPersistentListener(int index, UnityAction<ushort> call, ushort argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.UShort))
					return;
				m_PersistentCalls.RegisterUShortPersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddIntPersistentListener(UnityAction<int> call, int argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterIntPersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterIntPersistentListener(int index, UnityAction<int> call, int argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Int))
					return;
				m_PersistentCalls.RegisterIntPersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddUIntPersistentListener(UnityAction<uint> call, uint argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterUIntPersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterUIntPersistentListener(int index, UnityAction<uint> call, uint argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.UInt))
					return;
				m_PersistentCalls.RegisterUIntPersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddLongPersistentListener(UnityAction<long> call, long argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterLongPersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterLongPersistentListener(int index, UnityAction<long> call, long argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Long))
					return;
				m_PersistentCalls.RegisterLongPersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddFloatPersistentListener(UnityAction<float> call, float argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterFloatPersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterFloatPersistentListener(int index, UnityAction<float> call, float argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Float))
					return;
				m_PersistentCalls.RegisterFloatPersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddDoublePersistentListener(UnityAction<double> call, double argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterDoublePersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterDoublePersistentListener(int index, UnityAction<double> call, double argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Double))
					return;
				m_PersistentCalls.RegisterDoublePersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddBoolPersistentListener(UnityAction<bool> call, bool argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterBoolPersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterBoolPersistentListener(int index, UnityAction<bool> call, bool argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Bool))
					return;
				m_PersistentCalls.RegisterBoolPersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddEnumPersistentListener(UnityAction<Enum> call, Enum argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterEnumPersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterEnumPersistentListener(int index, UnityAction<Enum> call, Enum argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Enum))
					return;
				m_PersistentCalls.RegisterEnumPersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddStringPersistentListener(UnityAction<string> call, string argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterStringPersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterStringPersistentListener(int index, UnityAction<string> call, string argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.String))
					return;
				m_PersistentCalls.RegisterStringPersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddObjectPersistentListener<T>(UnityAction<T> call, T argument) where T : Object
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterObjectPersistentListener<T>(persistentEventCount, call, argument);
		}

		internal void RegisterObjectPersistentListener<T>(int index, UnityAction<T> call, T argument) where T : Object
		{
			if (call == null)
				throw new ArgumentNullException("call", "Registering a Listener requires a non null call");
			if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Object, !(argument == null) ? argument.GetType() : typeof(Object)))
				return;
			m_PersistentCalls.RegisterObjectPersistentListener(index, call.Target as Object, argument, call.Method.Name);
			DirtyPersistentCalls();
		}

		internal void AddVector2PersistentListener(UnityAction<Vector2> call, Vector2 argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterVector2PersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterVector2PersistentListener(int index, UnityAction<Vector2> call, Vector2 argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Vector2))
					return;
				m_PersistentCalls.RegisterVector2PersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddVector3PersistentListener(UnityAction<Vector3> call, Vector3 argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterVector3PersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterVector3PersistentListener(int index, UnityAction<Vector3> call, Vector3 argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Vector3))
					return;
				m_PersistentCalls.RegisterVector3PersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

#if UNITY_2017_2_OR_NEWER
		internal void AddVector2IntPersistentListener(UnityAction<Vector2Int> call, Vector2Int argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterVector2IntPersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterVector2IntPersistentListener(int index, UnityAction<Vector2Int> call, Vector2Int argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Vector2Int))
					return;
				m_PersistentCalls.RegisterVector2PersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddVector3IntPersistentListener(UnityAction<Vector3Int> call, Vector3Int argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterVector3IntPersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterVector3IntPersistentListener(int index, UnityAction<Vector3Int> call, Vector3Int argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Vector3Int))
					return;
				m_PersistentCalls.RegisterVector3IntPersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}
#endif

		internal void AddVector4PersistentListener(UnityAction<Vector4> call, Vector4 argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterVector4PersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterVector4PersistentListener(int index, UnityAction<Vector4> call, Vector4 argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Vector4))
					return;
				m_PersistentCalls.RegisterVector4PersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddLayerMaskPersistentListener(UnityAction<LayerMask> call, LayerMask argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterLayerMaskPersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterLayerMaskPersistentListener(int index, UnityAction<LayerMask> call, LayerMask argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.LayerMask))
					return;
				m_PersistentCalls.RegisterLayerMaskPersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddColorPersistentListener(UnityAction<Color> call, Color argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterColorPersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterColorPersistentListener(int index, UnityAction<Color> call, Color argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Color))
					return;
				m_PersistentCalls.RegisterColorPersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		internal void AddQuaternionPersistentListener(UnityAction<Quaternion> call, Quaternion argument)
		{
			int persistentEventCount = GetPersistentEventCount();
			AddPersistentListener();
			RegisterQuaternionPersistentListener(persistentEventCount, call, argument);
		}

		internal void RegisterQuaternionPersistentListener(int index, UnityAction<Quaternion> call, Quaternion argument)
		{
			if (call == null)
			{
				Debug.LogWarning("Registering a Listener requires an action");
			}
			else
			{
				if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode2.Quaternion))
					return;
				m_PersistentCalls.RegisterQuaternionPersistentListener(index, call.Target as Object, argument, call.Method.Name);
				DirtyPersistentCalls();
			}
		}

		private InvokableCallList2 Calls
		{
			get
			{
				if (m_Calls == null)
					m_Calls = new InvokableCallList2();

				return m_Calls;
			}
		}
	}
}