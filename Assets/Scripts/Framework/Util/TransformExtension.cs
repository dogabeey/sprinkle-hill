using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExtension 
{
    // Function to remove all children from a transform
    public static void Clear(this Transform transform)
    {
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
    public static bool IsChildOrGrandchildOf(this Transform child, Transform parent)
    {
        // Check if the child is null to prevent infinite loops in case of an error.
        while (child != null)
        {
            // If the child's parent is the parent we're looking for, return true.
            if (child == parent)
            {
                return true;
            }

            // Move up to the next parent.
            child = child.parent;
        }

        // If we get here, the child is not a child or grandchild of the parent.
        return false;
    }
}
