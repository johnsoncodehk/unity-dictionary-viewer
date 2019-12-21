using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class DictionaryViewerWindow : EditorWindow
{

    [MenuItem("Window/Dictionary Viewer")]
    static void GetWindow()
    {
        DictionaryViewerWindow editor = (DictionaryViewerWindow)EditorWindow.GetWindow(typeof(DictionaryViewerWindow));
        editor.titleContent = new GUIContent("Dictionary Viewer");
    }

    class PropertyCacheData
    {
        public object data;
        public SerializedObject serializedObject;
        public SerializedProperty property;
        public FieldInfo field;
    }

    Vector2 m_ScrollPosition;
    Dictionary<int, bool> m_ComponentFolds = new Dictionary<int, bool>();
    Dictionary<FieldInfo, bool> m_FieldFolds = new Dictionary<FieldInfo, bool>();
    Dictionary<KeyValuePair<Component, string>, ReorderableList> m_DictionaryLists = new Dictionary<KeyValuePair<Component, string>, ReorderableList>();
    Dictionary<KeyValuePair<ReorderableList, int>, PropertyCacheData> m_ListKeyPropertys = new Dictionary<KeyValuePair<ReorderableList, int>, PropertyCacheData>();
    Dictionary<KeyValuePair<ReorderableList, int>, PropertyCacheData> m_ListValuePropertys = new Dictionary<KeyValuePair<ReorderableList, int>, PropertyCacheData>();

    void OnSelectionChange()
    {
        Repaint();
    }

    void OnGUI()
    {
        m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, new GUILayoutOption[0]);

        foreach (var gameObject in Selection.gameObjects)
            DrawGameObject(gameObject);

        EditorGUILayout.EndScrollView();
    }

    void DrawGameObject(GameObject gameObject)
    {
        var components = gameObject.GetComponents<Component>()
            .Where(HasDictionary)
            .ToList();

        if (components.Count == 0)
        {
            EditorGUILayout.LabelField("No dictionary found in " + gameObject.name);
        }
        else
        {
            foreach (Component component in components)
                DrawComponent(component);
        }
    }

    bool HasDictionary(Component component)
    {
        var fields = component.GetType().GetFields();

        foreach (var field in fields)
        {
            var dict = field.GetValue(component) as IDictionary;

            if (dict != null)
                return true;
        }

        return false;
    }

    void DrawComponent(Component component)
    {
        int instanceID = component.GetInstanceID();
        m_ComponentFolds[instanceID] = EditorGUILayout.InspectorTitlebar(GetComponentFold(instanceID), component);
        if (m_ComponentFolds[instanceID])
        {
            var fields = component.GetType().GetFields();

            foreach (var field in fields)
            {
                var dict = field.GetValue(component) as IDictionary;

                if (dict == null)
                    continue;

                m_FieldFolds[field] = EditorGUILayout.Foldout(GetFieldFold(field), field.Name);

                if (!m_FieldFolds[field])
                    continue;

                DrawReorderableList(component, field, dict);
            }
        }
    }

    void DrawReorderableList(Component component, FieldInfo field, IDictionary dict)
    {
        var id = new KeyValuePair<Component, string>(component, field.Name);
        bool addCallback = false;
        if (!m_DictionaryLists.ContainsKey(id))
        {
            addCallback = true;
            m_DictionaryLists[id] = new ReorderableList(new List<KeyValuePair<object, object>>(), typeof(KeyValuePair<object, object>), true, true, true, true);
        }
        var rList = m_DictionaryLists[id];
        rList.list.Clear();

        foreach (var key in dict.Keys.Cast<object>())
            rList.list.Add(new KeyValuePair<object, object>(key, dict[key]));

        if (addCallback)
        {
            rList.drawElementCallback = DrawItem;
            rList.onAddCallback = AddItem;
            rList.onRemoveCallback = RemoveItem;
            rList.onReorderCallback = ReorderList;
            rList.drawHeaderCallback = DrawHeader;
            rList.elementHeightCallback = GetItemHeight;
        }

        rList.DoLayoutList();

        void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, field.Name);
        }
        float GetItemHeight(int index)
        {
            var kvp = (KeyValuePair<object, object>)rList.list[index];
            object key = kvp.Key;
            object val = kvp.Value;

            GetPropertyData(key, val, rList, index, out var keyData, out var valData);

            float keyHeight = SerializedPropertyParser.GetPropertyHeight(keyData.property);
            float valHeight = SerializedPropertyParser.GetPropertyHeight(valData.property);

            return Mathf.Max(keyHeight, valHeight) + 8;
        }
        void DrawItem(Rect rect, int index, bool selected, bool focused)
        {
            var kvp = (KeyValuePair<object, object>)rList.list[index];
            object key = kvp.Key;
            object val = kvp.Value;

            float keyWidth = rect.width * 0.45f - 40;
            float valWidth = rect.width - keyWidth;

            Rect keyPosition = new Rect(rect.x, rect.y, keyWidth - 16, rect.height);
            Rect valPosition = new Rect(rect.x + keyWidth, rect.y, valWidth - 8, rect.height);

            GetPropertyData(key, val, rList, index, out var keyData, out var valData);

            EditorGUI.BeginChangeCheck();
            SerializedPropertyParser.PropertyField(keyData.serializedObject, keyData.property, keyData.field, new Rect(rect.x, rect.y, keyWidth - 16, rect.height), ref key);
            SerializedPropertyParser.PropertyField(valData.serializedObject, valData.property, valData.field, new Rect(rect.x + keyWidth, rect.y, valWidth - 8, rect.height), ref val);
            if (EditorGUI.EndChangeCheck())
            {
                rList.list[index] = new KeyValuePair<object, object>(key, val);
                ListUpdated();
            }
        }
        void AddItem(ReorderableList list)
        {
            Type[] arguments = dict.GetType().GetGenericArguments();
            Type keyType = arguments[0];
            Type valueType = arguments[1];

            dict.Add(GetDefault(keyType), GetDefault(valueType));

            object GetDefault(Type type)
            {
                if (type == null)
                    return null;
                if (type == typeof(string))
                    return "";
                return Activator.CreateInstance(type);
            }
        }
        void RemoveItem(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            ListUpdated();
        }
        void ReorderList(ReorderableList list)
        {
            ListUpdated();
        }
        void ListUpdated()
        {
            dict.Clear();
            foreach (var kvpObj in rList.list)
            {
                var kvp = (KeyValuePair<object, object>)kvpObj;
                dict.Add(kvp.Key, kvp.Value);
            }
        }
    }

    void GetPropertyData(object key, object val, ReorderableList rList, int index, out PropertyCacheData keyData, out PropertyCacheData valData)
    {
        keyData = null;
        valData = null;

        if (m_ListKeyPropertys.TryGetValue(new KeyValuePair<ReorderableList, int>(rList, index), out var key_cache))
            if (key_cache.data == key)
                keyData = key_cache;

        if (m_ListValuePropertys.TryGetValue(new KeyValuePair<ReorderableList, int>(rList, index), out var val_cache))
            if (val_cache.data == val)
                valData = val_cache;

        if (keyData == null)
        {
            var property = SerializedPropertyParser.From(key, out var serializedObject, out var field);
            keyData = new PropertyCacheData
            {
                data = key,
                serializedObject = serializedObject,
                property = property,
                field = field,
            };
            m_ListKeyPropertys[new KeyValuePair<ReorderableList, int>(rList, index)] = keyData;
        }

        if (valData == null)
        {
            var property = SerializedPropertyParser.From(val, out var serializedObject, out var field);
            valData = new PropertyCacheData
            {
                data = val,
                serializedObject = serializedObject,
                property = property,
                field = field,
            };
            m_ListValuePropertys[new KeyValuePair<ReorderableList, int>(rList, index)] = valData;
        }
    }

    bool GetComponentFold(int instanceID)
    {
        if (!m_ComponentFolds.ContainsKey(instanceID))
            return true;

        return m_ComponentFolds[instanceID];
    }

    bool GetFieldFold(FieldInfo field)
    {
        if (!m_FieldFolds.ContainsKey(field))
            return false;

        return m_FieldFolds[field];
    }

}
