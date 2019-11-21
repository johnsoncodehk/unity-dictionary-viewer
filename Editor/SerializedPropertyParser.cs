using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

public static class SerializedPropertyParser
{

    static FieldInfo[] fields = typeof(SerializedPropertyTypes).GetFields();
    static Dictionary<System.Type, FieldInfo> fieldMatchingCache = new Dictionary<System.Type, FieldInfo>();

    public static SerializedProperty From(object val, out SerializedObject serializedObject, out FieldInfo field)
    {
        field = null;

        serializedObject = new SerializedObject(new SerializedPropertyTypes());

        if (!serializedObject.targetObject || val == null)
            return null;

        field = FindMatchingField(val.GetType());
        if (field != null)
        {
            field.SetValue(serializedObject.targetObject, val);
            serializedObject.Update();
            return serializedObject.FindProperty(field.Name);
        }

        return null;
    }

    static FieldInfo FindMatchingField(System.Type valType)
    {
        if (fieldMatchingCache.ContainsKey(valType))
            return fieldMatchingCache[valType];

        foreach (var field in fields)
        {
            if (valType == field.FieldType || valType.IsSubclassOf(field.FieldType))
            {
                fieldMatchingCache[valType] = field;
                return field;
            }
        }

        fieldMatchingCache[valType] = null;
        return null;
    }

    public static void PropertyField(SerializedObject serializedObject, SerializedProperty property, FieldInfo field, Rect position, ref object val)
    {
        if (val == null)
        {
            EditorGUI.LabelField(position, "null");
        }
        else
        {
            if (property == null)
            {
                EditorGUI.LabelField(position, val.ToString());
            }
            else
            {
                EditorGUI.PropertyField(position, property, GUIContent.none, true);

                if (serializedObject.targetObject && serializedObject.ApplyModifiedProperties())
                    val = field.GetValue(serializedObject.targetObject);
            }
        }
    }

    public static float GetPropertyHeight(SerializedProperty property)
    {
        var height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Generic, null);

        if (property != null)
            height = Mathf.Max(height, EditorGUI.GetPropertyHeight(property, GUIContent.none, true));

        return height;
    }
}
