using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DictionaryViewerWindow : EditorWindow
{

    [MenuItem("Window/Dictionary Viewer")]
    static void GetWindow()
    {
        DictionaryViewerWindow editor = (DictionaryViewerWindow)EditorWindow.GetWindow(typeof(DictionaryViewerWindow));
        editor.titleContent = new GUIContent("Dictionary Viewer");
    }

    Vector2 m_ScrollPosition;
    Dictionary<int, bool> m_Folds = new Dictionary<int, bool>();
    Dictionary<KeyValuePair<Component, string>, ReorderableList> m_DictionaryLists = new Dictionary<KeyValuePair<Component, string>, ReorderableList>();

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
        m_Folds[instanceID] = EditorGUILayout.InspectorTitlebar(GetFold(instanceID), component);
        if (m_Folds[instanceID])
        {
            var fields = component.GetType().GetFields();

            foreach (var field in fields)
            {
                var dict = field.GetValue(component) as IDictionary;

                if (dict == null)
                    continue;

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

                    float keyHeight = SerializedPropertyParser.GetPropertyHeight(key);
                    float valHeight = SerializedPropertyParser.GetPropertyHeight(val);

                    return Mathf.Max(keyHeight, valHeight) + 2;
                }
                void DrawItem(Rect rect, int index, bool selected, bool focused)
                {
                    var kvp = (KeyValuePair<object, object>)rList.list[index];
                    object key = kvp.Key;
                    object val = kvp.Value;

                    EditorGUI.BeginChangeCheck();
                    SerializedPropertyParser.PropertyField(new Rect(rect.x, rect.y, rect.width * 0.5f, rect.height), ref key);
                    SerializedPropertyParser.PropertyField(new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.5f, rect.height), ref val);
                    if (EditorGUI.EndChangeCheck())
                    {
                        rList.list[index] = new KeyValuePair<object, object>(key, val);
                        ListUpdated();
                    }
                }
                void AddItem(ReorderableList list)
                {
                    Type keyType = null;
                    Type valType = null;
                    foreach (var kvpObj in list.list)
                    {
                        var kvp = (KeyValuePair<object, object>)kvpObj;

                        if (keyType == null && kvp.Key != null)
                            keyType = kvp.Key.GetType();

                        if (valType == null && kvp.Value != null)
                            valType = kvp.Value.GetType();

                        if (keyType != null && valType != null)
                            break;
                    }
                    dict.Add(GetDefault(keyType), GetDefault(valType));

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
        }
    }

    bool GetFold(int instanceID)
    {
        if (!m_Folds.ContainsKey(instanceID))
            return true;

        return m_Folds[instanceID];
    }

}
