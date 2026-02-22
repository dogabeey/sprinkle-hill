using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorExtensions
{
    // Extension method that converts a Vector3 array to a Vector2 array
    public static Vector2[] ToVector2Array(this Vector3[] vector3Array)
    {
        Vector2[] vector2Array = new Vector2[vector3Array.Length];

        for (int i = 0; i < vector3Array.Length; i++)
        {
            vector2Array[i] = vector3Array[i];
        }

        return vector2Array;
    }

    // Extension method that converts a Vector2 array to a Vector3 array
    public static Vector3[] ToVector3Array(this Vector2[] vector2Array)
    {
        Vector3[] vector3Array = new Vector3[vector2Array.Length];

        for (int i = 0; i < vector2Array.Length; i++)
        {
            vector3Array[i] = vector2Array[i];
        }

        return vector3Array;
    }

    // Extension method that converts Transform array to Vector2 array
    public static Vector2[] ToVector2Array(this Transform[] transformArray)
    {
        Vector2[] vector2Array = new Vector2[transformArray.Length];

        for (int i = 0; i < transformArray.Length; i++)
        {
            vector2Array[i] = transformArray[i].position;
        }

        return vector2Array;
    }
}
