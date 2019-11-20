using UnityEngine;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

[Serializable]
public class SerializedPropertyTypes : ScriptableObject
{
    public int i;
    public float f;
    public double d;
    public string s;
    public bool b;
    public Color color;
    public Object obj;
    public AnimationCurve animationCurve;
    public Gradient gradient;
    public Vector2 vector2;
    public Vector3 vector3;
    public Vector4 vector4;
    public Vector2Int vector2Int;
    public Vector3Int vector3Int;
    public Rect rect;
    public RectInt rectInt;
    public Bounds bounds;
    public BoundsInt boundsInt;

    public int[] arr_i;
    public float[] arr_f;
    public double[] arr_d;
    public string[] arr_s;
    public bool[] arr_b;
    public Color[] arr_color;
    public Object[] arr_obj;
    public AnimationCurve[] arr_animationCurve;
    public Gradient[] arr_gradient;
    public Vector2[] arr_vector2;
    public Vector3[] arr_vector3;
    public Vector4[] arr_vector4;
    public Vector2Int[] arr_vector2Int;
    public Vector3Int[] arr_vector3Int;
    public Rect[] arr_rect;
    public RectInt[] arr_rectInt;
    public Bounds[] arr_bounds;
    public BoundsInt[] arr_boundsInt;

    public List<int> list_i;
    public List<float> list_f;
    public List<double> list_d;
    public List<string> list_s;
    public List<bool> list_b;
    public List<Color> list_color;
    public List<Object> list_obj;
    public List<AnimationCurve> list_animationCurve;
    public List<Gradient> list_gradient;
    public List<Vector2> list_vector2;
    public List<Vector3> list_vector3;
    public List<Vector4> list_vector4;
    public List<Vector2Int> list_vector2Int;
    public List<Vector3Int> list_vector3Int;
    public List<Rect> list_rect;
    public List<RectInt> list_rectInt;
    public List<Bounds> list_bounds;
    public List<BoundsInt> list_boundsInt;
}
