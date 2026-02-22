using UnityEngine;
using UnityEngine.UI;

public class CircularLayoutGroup : LayoutGroup
{
    public bool IsAnglePerElement => layoutMode == LayoutMode.AnglePerElement;
    public bool IsEndAngle => layoutMode == LayoutMode.EndAngle;

    public enum LayoutMode
    {
        AnglePerElement,
        EndAngle
    }

    // Define your variables here
    public LayoutMode layoutMode = LayoutMode.AnglePerElement;
    [Space]
    public float radius = 100f;
    public float startAngle = 0f;
    public float endAngle = 360f; // Only used if LayoutMode is EndAngle
    public float anglePerElement = 30f; // Angle in degrees for each element
    public bool lookAtCenter = true;
    public float rotationOffset = 0f; // Rotation offset in degrees

    public override void CalculateLayoutInputHorizontal() { }
    public override void CalculateLayoutInputVertical() { }

    public override void SetLayoutHorizontal()
    {
        CalculateLayout();
    }

    public override void SetLayoutVertical()
    {
        // You might not need to implement anything here for a simple layout
    }
    private void CalculateLayout()
    {
        int childCount = transform.childCount;
        Vector3 center = Vector3.zero; // Assuming the center is the local origin

        // Calculate angle difference based on the layout mode
        float angleDiff = layoutMode == LayoutMode.AnglePerElement ? anglePerElement : (endAngle - startAngle) / childCount;

        for (int i = 0; i < childCount; i++)
        {
            // Calculate the angle for the current child
            float childAngle = startAngle + angleDiff * i;
            float childAngleRad = childAngle * Mathf.Deg2Rad;
            Vector3 childPos = new Vector3(Mathf.Cos(childAngleRad), Mathf.Sin(childAngleRad), 0) * radius;

            RectTransform child = (RectTransform)transform.GetChild(i);
            child.anchoredPosition = childPos;

            // Rotate child to look at center
            if (lookAtCenter)
            {
                Vector3 lookDir = center - childPos; // Direction from child to center
                float angleToCenter = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg + rotationOffset;
                child.localRotation = Quaternion.Euler(0, 0, angleToCenter - 90); // Adjusting -90 degrees to align correctly
            }

            // Additional setup for child elements (size, rotation, etc.)
        }
    }
}
