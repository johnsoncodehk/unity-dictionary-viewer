using UnityEngine;
using UnityEditor;
using System.Reflection;

public static class SerializedPropertyParser
{

    static SerializedObject serializedObject = new SerializedObject(new SerializedPropertyTypes());
    static FieldInfo[] fields = typeof(SerializedPropertyTypes).GetFields();

    public static SerializedProperty From(object val, out FieldInfo valField)
    {
        valField = null;

        if (val == null)
            return null;

        foreach (var field in fields)
        {
            if (val.GetType() == field.FieldType || val.GetType().IsSubclassOf(field.FieldType))
            {
                valField = field;

                field.SetValue(serializedObject.targetObject, val);
                serializedObject.Update();
                return serializedObject.FindProperty(field.Name);
            }
        }

        return null;
    }

    public static void PropertyField(Rect position, ref object val)
    {
        if (val == null)
        {
            EditorGUI.LabelField(position, "null");
        }
        else
        {
            var property = From(val, out var field);
            if (property == null)
            {
                EditorGUI.LabelField(position, val.ToString());
            }
            else
            {
                EditorGUI.PropertyField(position, property, GUIContent.none, true);

                if (serializedObject.ApplyModifiedProperties())
                    val = field.GetValue(serializedObject.targetObject);
            }
        }
    }

    public static float GetPropertyHeight(object val)
    {
        var height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Generic, null);
        var property = SerializedPropertyParser.From(val, out var field);

        if (property != null)
            height = Mathf.Max(height, EditorGUI.GetPropertyHeight(property, GUIContent.none, true));

        return height;
    }
}
