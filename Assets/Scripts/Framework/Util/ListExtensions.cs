using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System;

public static class ListExtensions
{
    private static System.Random rng = new System.Random();

    public static void Shuffle<T>(this List<T> list, int? seed = null)
    {
        int n = list.Count;
        System.Random rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
    public static T GetRandomElement<T>(this List<T> list, int? seed = null)
    {
        if (list.Count == 0 || list.TrueForAll(e => e == null))
        {
            throw new InvalidOperationException("The list is empty.");
        }

        System.Random rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

        int randomIndex = rng.Next(list.Count);
        return list[randomIndex];
    }

    public static List<T> GetRandomElements<T>(this List<T> list, int count, int? seed = null)
    {
        if (count <= 0)
        {
            throw new ArgumentException("Count must be greater than zero.");
        }

        int listCount = list.Count;
        if (count >= listCount)
        {
            // If count is greater than or equal to the list count, return a copy of the entire list.
            return new List<T>(list);
        }

        // Initialize the random number generator with the provided seed or use a random seed if none is provided.
        System.Random rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

        List<T> randomElements = new List<T>(count);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = rng.Next(listCount);

            // Add the random element to the result list and remove it from the original list to avoid duplicates.
            randomElements.Add(list[randomIndex]);
            list.RemoveAt(randomIndex);
            listCount--;
        }

        return randomElements;
    }

    
}
