//Modified from Annulus Games Lucid Editor
//https://github.com/AnnulusGames/LucidEditor

//MIT License

//Copyright (c) 2023 Annulus Games

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Cainos.Common
{
    public class InspectorEditor : Editor
    {
        private const BindingFlags DeclaredMembers = BindingFlags.Instance | BindingFlags.Static |
                                                     BindingFlags.Public | BindingFlags.NonPublic |
                                                     BindingFlags.DeclaredOnly;

        private GroupNode root;
        private readonly Dictionary<string, GroupNode> groups = new Dictionary<string, GroupNode>();
        private readonly Dictionary<string, InspectorItem> items = new Dictionary<string, InspectorItem>();

        protected virtual void OnEnable()
        {
            BuildInspector();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (root == null)
            {
                BuildInspector();
            }

            DrawScriptField();
            root.Draw(false);
            serializedObject.ApplyModifiedProperties();
        }

        private void BuildInspector()
        {
            root = new GroupNode(null, string.Empty, string.Empty, target);
            groups.Clear();
            items.Clear();

            HashSet<string> serializedFields = AddSerializedFields();
            AddReflectedMembers(serializedFields);
        }

        protected void SetTooltip(string propertyName, string tooltip)
        {
            if (root == null)
            {
                BuildInspector();
            }

            if (items.TryGetValue(propertyName, out InspectorItem item))
            {
                item.tooltip = tooltip;
            }
        }

        private HashSet<string> AddSerializedFields()
        {
            HashSet<string> fieldNames = new HashSet<string>();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == "m_Script")
                {
                    continue;
                }

                FieldInfo field = FindField(target.GetType(), iterator.name);
                Attribute[] attributes = field == null
                    ? Array.Empty<Attribute>()
                    : field.GetCustomAttributes(true).OfType<Attribute>().ToArray();

                fieldNames.Add(iterator.name);
                AddItem(new SerializedFieldItem(iterator.propertyPath, serializedObject, target, attributes));
            }

            return fieldNames;
        }

        private void AddReflectedMembers(HashSet<string> serializedFields)
        {
            foreach (MemberInfo member in GetMembers(target.GetType()))
            {
                Attribute[] attributes = member.GetCustomAttributes(true).OfType<Attribute>().ToArray();
                bool showInInspector = attributes.Any(attribute => attribute is ShowInInspectorAttribute);

                if (member is FieldInfo field)
                {
                    if (showInInspector && !serializedFields.Contains(field.Name))
                    {
                        AddItem(new ReadOnlyValueItem(field, target, attributes));
                    }
                }
                else if (member is PropertyInfo property)
                {
                    if (showInInspector && property.GetIndexParameters().Length == 0 && property.GetGetMethod(true) != null)
                    {
                        AddItem(new ReflectedPropertyItem(property, target, attributes));
                    }
                }
                else if (member is MethodInfo method)
                {
                    if (showInInspector && method.ReturnType != typeof(void) && method.GetParameters().Length == 0)
                    {
                        AddItem(new ReadOnlyValueItem(method, target, attributes));
                    }

                    ButtonAttribute button = attributes.OfType<ButtonAttribute>().FirstOrDefault();
                    if (button != null)
                    {
                        AddItem(new ButtonItem(method, target, attributes, button));
                    }
                }
            }
        }

        private void AddItem(InspectorItem item)
        {
            if (!string.IsNullOrEmpty(item.name))
            {
                items[item.name] = item;
            }

            GroupNode parent = root;
            IEnumerable<PropertyGroupAttribute> groupAttributes = item.attributes
                .OfType<PropertyGroupAttribute>()
                .OrderBy(attribute => attribute.groupDepth);

            foreach (PropertyGroupAttribute attribute in groupAttributes)
            {
                string currentPath = string.Empty;
                string[] pathParts = attribute.path.Split('/');
                parent = root;

                foreach (string pathPart in pathParts)
                {
                    if (string.IsNullOrWhiteSpace(pathPart))
                    {
                        continue;
                    }

                    currentPath = string.IsNullOrEmpty(currentPath) ? pathPart : currentPath + "/" + pathPart;
                    if (!groups.TryGetValue(currentPath, out GroupNode group))
                    {
                        group = new GroupNode(attribute, currentPath, pathPart, target);
                        groups.Add(currentPath, group);
                        parent.Add(group);
                    }

                    parent = group;
                }
            }

            parent.Add(item);
        }

        private void DrawScriptField()
        {
            SerializedProperty script = serializedObject.FindProperty("m_Script");
            if (script == null)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(script);
            }
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(fieldName, DeclaredMembers);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        private static bool GetConditionValue(object owner, string memberName)
        {
            if (owner == null || string.IsNullOrEmpty(memberName))
            {
                return false;
            }

            try
            {
                for (Type current = owner.GetType(); current != null; current = current.BaseType)
                {
                    FieldInfo field = current.GetField(memberName, DeclaredMembers);
                    if (field != null)
                    {
                        return field.GetValue(field.IsStatic ? null : owner) is bool fieldValue && fieldValue;
                    }

                    PropertyInfo property = current.GetProperty(memberName, DeclaredMembers);
                    MethodInfo getter = property?.GetGetMethod(true);
                    if (getter != null && property.GetIndexParameters().Length == 0)
                    {
                        return getter.Invoke(getter.IsStatic ? null : owner, null) is bool propertyValue && propertyValue;
                    }

                    MethodInfo method = current.GetMethod(memberName, DeclaredMembers, null, Type.EmptyTypes, null);
                    if (method != null)
                    {
                        return method.Invoke(method.IsStatic ? null : owner, null) is bool methodValue && methodValue;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception.GetBaseException(), owner as UnityEngine.Object);
            }

            return false;
        }

        private static IEnumerable<MemberInfo> GetMembers(Type type)
        {
            Stack<Type> hierarchy = new Stack<Type>();
            for (Type current = type; current != null && current != typeof(UnityEngine.Object); current = current.BaseType)
            {
                hierarchy.Push(current);
            }

            while (hierarchy.Count > 0)
            {
                foreach (MemberInfo member in hierarchy.Pop().GetMembers(DeclaredMembers).OrderBy(member => member.MetadataToken))
                {
                    yield return member;
                }
            }
        }

        private abstract class InspectorItem
        {
            protected InspectorItem(string name, object owner, Attribute[] attributes)
            {
                this.name = name;
                this.owner = owner;
                this.attributes = attributes;
                order = attributes.OfType<PropertyOrderAttribute>().FirstOrDefault()?.propertyOrder ?? 0;
                tooltip = attributes.OfType<TooltipAttribute>().FirstOrDefault()?.tooltip;
            }

            internal readonly string name;
            protected readonly object owner;
            internal readonly Attribute[] attributes;
            internal readonly int order;
            internal string tooltip;

            protected bool IsVisible
            {
                get
                {
                    return attributes.OfType<ShowIfAttribute>()
                        .All(attribute => GetConditionValue(owner, attribute.condition));
                }
            }

            protected bool IsDisabled
            {
                get
                {
                    return attributes.Any(attribute => attribute is ReadOnlyAttribute) ||
                           (!Application.isPlaying && attributes.Any(attribute => attribute is DisableInEditModeAttribute));
                }
            }

            protected string GetDisplayName(string defaultName)
            {
                LabelTextAttribute label = attributes.OfType<LabelTextAttribute>().FirstOrDefault();
                return label == null ? defaultName : label.label;
            }

            internal abstract void Draw(bool inHorizontalGroup);
        }

        private sealed class GroupNode : InspectorItem
        {
            private readonly PropertyGroupAttribute attribute;
            private readonly string path;
            private readonly string label;
            private readonly string editorPrefsKey;
            private readonly List<InspectorItem> children = new List<InspectorItem>();

            internal GroupNode(PropertyGroupAttribute attribute, string path, string label, UnityEngine.Object owner)
                : base(path, owner, Array.Empty<Attribute>())
            {
                this.attribute = attribute;
                this.path = path;
                this.label = label;

                if (attribute is FoldoutGroupAttribute && owner != null)
                {
                    GlobalObjectId objectId = GlobalObjectId.GetGlobalObjectIdSlow(owner);
                    editorPrefsKey = "InspectorEditor_Foldout_" + objectId + "_" + path;
                }
            }

            internal void Add(InspectorItem item)
            {
                if (!children.Contains(item))
                {
                    children.Add(item);
                }
            }

            internal override void Draw(bool inHorizontalGroup)
            {
                if (attribute == null)
                {
                    DrawChildren(false);
                    return;
                }

                if (attribute is FoldoutGroupAttribute)
                {
                    DrawFoldout();
                    return;
                }

                if (attribute is HorizontalGroupAttribute)
                {
                    using (new LayoutIndentScope(EditorGUI.indentLevel))
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawChildren(true);
                    }
                }
            }

            private void DrawFoldout()
            {
                bool expanded = EditorPrefs.GetBool(editorPrefsKey, false);
                using (new LayoutIndentScope(EditorGUI.indentLevel))
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    bool newExpanded = DrawFoldoutHeader(expanded, label);
                    if (newExpanded != expanded)
                    {
                        expanded = newExpanded;
                        EditorPrefs.SetBool(editorPrefsKey, expanded);
                    }

                    if (expanded)
                    {
                        EditorGUILayout.Space(2f);
                        DrawChildren(false);
                    }
                }

                EditorGUILayout.Space(2f);
            }

            private static bool DrawFoldoutHeader(bool expanded, string content)
            {
                Rect position = EditorGUILayout.GetControlRect(GUILayout.MinWidth(0f));

                Rect headerRect = position;
                headerRect.yMin -= 3f;
                headerRect.yMax += 3f;
                headerRect.xMin -= 4f;
                headerRect.xMax += 4f;
                EditorGUI.LabelField(headerRect, GUIContent.none, GUI.skin.button);

                position.x += 15f;
                position.y -= 1f;

                Rect foldoutRect = position;
                foldoutRect.x -= 13.5f;
                foldoutRect.y += position.height * 0.23f;
                foldoutRect.width = 13f;
                foldoutRect.height = 13f;
                expanded = GUI.Toggle(foldoutRect, expanded, GUIContent.none, EditorStyles.foldout);

                position.x += 2f;
                position.y += 1f;
                EditorGUI.LabelField(position, content, EditorStyles.boldLabel);

                Event currentEvent = Event.current;
                if (currentEvent.type == EventType.MouseDown &&
                    currentEvent.button == 0 &&
                    position.Contains(currentEvent.mousePosition))
                {
                    expanded = !expanded;
                    currentEvent.Use();
                }

                return expanded;
            }

            private void DrawChildren(bool inHorizontalGroup)
            {
                foreach (InspectorItem child in children.OrderBy(child => child.order))
                {
                    child.Draw(inHorizontalGroup);
                }
            }
        }

        private sealed class LayoutIndentScope : IDisposable
        {
            private readonly int previousIndentLevel;

            internal LayoutIndentScope(int indentLevel)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(15f * indentLevel);

                previousIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                GUILayout.BeginVertical();
            }

            public void Dispose()
            {
                GUILayout.EndVertical();
                EditorGUI.indentLevel = previousIndentLevel;
                GUILayout.EndHorizontal();
            }
        }

        private sealed class SerializedFieldItem : InspectorItem
        {
            private readonly string propertyPath;
            private readonly SerializedObject serializedObject;

            internal SerializedFieldItem(string propertyPath, SerializedObject serializedObject,
                UnityEngine.Object targetObject, Attribute[] attributes)
                : base(propertyPath, targetObject, attributes)
            {
                this.propertyPath = propertyPath;
                this.serializedObject = serializedObject;
            }

            internal override void Draw(bool inHorizontalGroup)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyPath);
                if (property == null || !IsVisible)
                {
                    return;
                }

                string propertyTooltip = string.IsNullOrEmpty(tooltip) ? property.tooltip : tooltip;
                GUIContent label = new GUIContent(GetDisplayName(property.displayName), propertyTooltip);

                using (new EditorGUI.DisabledScope(IsDisabled))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(property, label, true, GUILayout.MinWidth(0f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }

        private sealed class ButtonItem : InspectorItem
        {
            private readonly MethodInfo method;
            private readonly UnityEngine.Object targetObject;
            private readonly string label;

            internal ButtonItem(MethodInfo method, UnityEngine.Object targetObject, Attribute[] attributes, ButtonAttribute button)
                : base(method.Name, targetObject, attributes)
            {
                this.method = method;
                this.targetObject = targetObject;
                string defaultLabel = string.IsNullOrEmpty(button.label)
                    ? ObjectNames.NicifyVariableName(method.Name)
                    : button.label;
                label = GetDisplayName(defaultLabel);
            }

            internal override void Draw(bool inHorizontalGroup)
            {
                if (!IsVisible)
                {
                    return;
                }

                bool canInvoke = method.GetParameters().Length == 0;
                string buttonTooltip = canInvoke ? tooltip : "Button methods must not have parameters.";
                using (new EditorGUI.DisabledScope(!canInvoke || IsDisabled))
                {
                    if (GUILayout.Button(new GUIContent(label, buttonTooltip),
                            GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.MinWidth(0f)))
                    {
                        Undo.RecordObject(targetObject, label);
                        try
                        {
                            method.Invoke(method.IsStatic ? null : targetObject, null);
                            EditorUtility.SetDirty(targetObject);
                        }
                        catch (TargetInvocationException exception)
                        {
                            Debug.LogException(exception.InnerException ?? exception, targetObject);
                        }
                    }
                }
            }
        }

        private sealed class ReadOnlyValueItem : InspectorItem
        {
            private readonly MemberInfo member;
            private readonly UnityEngine.Object targetObject;

            internal ReadOnlyValueItem(MemberInfo member, UnityEngine.Object targetObject, Attribute[] attributes)
                : base(member.Name, targetObject, attributes)
            {
                this.member = member;
                this.targetObject = targetObject;
            }

            internal override void Draw(bool inHorizontalGroup)
            {
                if (!IsVisible)
                {
                    return;
                }

                object value;
                try
                {
                    FieldInfo field = member as FieldInfo;
                    value = field != null
                        ? field.GetValue(field.IsStatic ? null : targetObject)
                        : ((MethodInfo)member).Invoke(((MethodInfo)member).IsStatic ? null : targetObject, null);
                }
                catch (Exception exception)
                {
                    value = exception.GetBaseException().Message;
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    GUIContent label = new GUIContent(GetDisplayName(ObjectNames.NicifyVariableName(member.Name)), tooltip);
                    EditorGUILayout.TextField(label, value == null ? "Null" : value.ToString());
                }
            }
        }

        private sealed class ReflectedPropertyItem : InspectorItem
        {
            private readonly PropertyInfo property;
            private readonly UnityEngine.Object targetObject;
            private readonly MethodInfo getter;
            private readonly MethodInfo setter;

            internal ReflectedPropertyItem(PropertyInfo property, UnityEngine.Object targetObject, Attribute[] attributes)
                : base(property.Name, targetObject, attributes)
            {
                this.property = property;
                this.targetObject = targetObject;
                getter = property.GetGetMethod(true);
                setter = property.GetSetMethod(true);
            }

            internal override void Draw(bool inHorizontalGroup)
            {
                if (!IsVisible)
                {
                    return;
                }

                object currentValue;
                try
                {
                    currentValue = getter.Invoke(getter.IsStatic ? null : targetObject, null);
                }
                catch (Exception exception)
                {
                    EditorGUILayout.HelpBox(exception.GetBaseException().Message, MessageType.Error);
                    return;
                }

                GUIContent label = new GUIContent(GetDisplayName(ObjectNames.NicifyVariableName(property.Name)), tooltip);
                bool allowSceneObjects = !attributes.Any(attribute => attribute is AssetsOnlyAttribute);
                using (new EditorGUI.DisabledScope(setter == null || IsDisabled))
                {
                    EditorGUI.BeginChangeCheck();
                    object newValue = DrawValue(property.PropertyType, label, currentValue, allowSceneObjects);
                    if (EditorGUI.EndChangeCheck() && setter != null)
                    {
                        Undo.RecordObject(targetObject, "Change " + property.Name);
                        try
                        {
                            setter.Invoke(setter.IsStatic ? null : targetObject, new[] { newValue });
                            EditorUtility.SetDirty(targetObject);
                        }
                        catch (TargetInvocationException exception)
                        {
                            Debug.LogException(exception.InnerException ?? exception, targetObject);
                        }
                    }
                }
            }

            private static object DrawValue(Type type, GUIContent label, object value, bool allowSceneObjects)
            {
                if (type == typeof(int)) return EditorGUILayout.IntField(label, (int)value, GUILayout.MinWidth(0f));
                if (type == typeof(float)) return EditorGUILayout.FloatField(label, (float)value, GUILayout.MinWidth(0f));
                if (type == typeof(bool)) return EditorGUILayout.Toggle(label, (bool)value, GUILayout.MinWidth(0f));
                if (type == typeof(string)) return EditorGUILayout.TextField(label, (string)value, GUILayout.MinWidth(0f));
                if (type == typeof(Vector2)) return EditorGUILayout.Vector2Field(label, (Vector2)value, GUILayout.MinWidth(0f));
                if (type == typeof(Vector3)) return EditorGUILayout.Vector3Field(label, (Vector3)value, GUILayout.MinWidth(0f));
                if (type == typeof(Color)) return EditorGUILayout.ColorField(label, (Color)value, GUILayout.MinWidth(0f));
                if (type.IsEnum) return EditorGUILayout.EnumPopup(label, (Enum)value, GUILayout.MinWidth(0f));

                if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                {
                    return EditorGUILayout.ObjectField(label, (UnityEngine.Object)value, type, allowSceneObjects,
                        GUILayout.MinWidth(0f));
                }

                EditorGUILayout.TextField(label, value == null ? "Null" : value.ToString(), GUILayout.MinWidth(0f));
                return value;
            }
        }
    }

    [CustomPropertyDrawer(typeof(AssetsOnlyAttribute))]
    internal sealed class AssetsOnlyAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            Type objectType = fieldInfo == null ? typeof(UnityEngine.Object) : fieldInfo.FieldType;
            property.objectReferenceValue = EditorGUI.ObjectField(position, label, property.objectReferenceValue,
                objectType, false);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.propertyType == SerializedPropertyType.ObjectReference
                ? EditorGUIUtility.singleLineHeight
                : EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}

#endif
