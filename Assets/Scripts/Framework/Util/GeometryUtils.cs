using System.Collections.Generic;
using UnityEngine;

public static class GeometryUtils
{
    // Calculates the intersection point of two lines if it exists
    public static bool LineIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = new Vector2();

        float s1_x = p2.x - p1.x;
        float s1_y = p2.y - p1.y;
        float s2_x = p4.x - p3.x;
        float s2_y = p4.y - p3.y;

        float s, t;
        s = (-s1_y * (p1.x - p3.x) + s1_x * (p1.y - p3.y)) / (-s2_x * s1_y + s1_x * s2_y);
        t = (s2_x * (p1.y - p3.y) - s2_y * (p1.x - p3.x)) / (-s2_x * s1_y + s1_x * s2_y);

        if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
        {
            // Collision detected
            intersection.x = p1.x + (t * s1_x);
            intersection.y = p1.y + (t * s1_y);
            return true;
        }

        return false; // No collision
    }

    // Main method to find intersection points between two trapezoids
    public static List<Vector2> FindTrapezoidIntersections(Vector2[] trapezoid1, Vector2[] trapezoid2)
    {
        List<Vector2> intersections = new List<Vector2>();

        for (int i = 0; i < trapezoid1.Length; i++)
        {
            Vector2 start1 = trapezoid1[i];
            Vector2 end1 = trapezoid1[(i + 1) % trapezoid1.Length];

            for (int j = 0; j < trapezoid2.Length; j++)
            {
                Vector2 start2 = trapezoid2[j];
                Vector2 end2 = trapezoid2[(j + 1) % trapezoid2.Length];

                if (LineIntersect(start1, end1, start2, end2, out Vector2 intersection))
                {
                    intersections.Add(intersection);
                }
            }
        }

        return intersections;
    }
}
