using UnityEngine; using Game.EventManagement;
using UnityEngine.UI;

public class FlexibleGridLayoutGroup : GridLayoutGroup
{
    public enum AxisCalculationMode
    {
        Percent,
        Flat
    }

    [SerializeField]
    private AxisCalculationMode widthCalculationMode = AxisCalculationMode.Percent;

    [SerializeField]
    private AxisCalculationMode heightCalculationMode = AxisCalculationMode.Percent;

    [SerializeField]
    private Vector2 percentSize = new(1f, 1f);

    [SerializeField]
    private Vector2 flatSize = new(100f, 100f);

    [SerializeField]
    private Vector2 percentSpacing = Vector2.zero;

    [SerializeField]
    private Vector2 flatSpacing = Vector2.zero;

    [SerializeField]
    private Vector4 percentPadding = Vector4.zero;

    [SerializeField]
    private Vector4 flatPadding = Vector4.zero;

    public Vector2 PercentSize
    {
        get => percentSize;
        set
        {
            percentSize = ClampMinZero(value);
            UpdateLayoutValuesFromParent();
            SetDirty();
        }
    }

    public Vector2 FlatSize
    {
        get => flatSize;
        set
        {
            flatSize = ClampMinZero(value);
            UpdateLayoutValuesFromParent();
            SetDirty();
        }
    }

    public Vector2 PercentSpacing
    {
        get => percentSpacing;
        set
        {
            percentSpacing = ClampMinZero(value);
            UpdateLayoutValuesFromParent();
            SetDirty();
        }
    }

    public Vector2 FlatSpacing
    {
        get => flatSpacing;
        set
        {
            flatSpacing = ClampMinZero(value);
            UpdateLayoutValuesFromParent();
            SetDirty();
        }
    }

    public Vector4 PercentPadding
    {
        get => percentPadding;
        set
        {
            percentPadding = ClampMinZero(value);
            UpdateLayoutValuesFromParent();
            SetDirty();
        }
    }

    public Vector4 FlatPadding
    {
        get => flatPadding;
        set
        {
            flatPadding = ClampMinZero(value);
            UpdateLayoutValuesFromParent();
            SetDirty();
        }
    }

    public override void CalculateLayoutInputHorizontal()
    {
        UpdateLayoutValuesFromParent();
        base.CalculateLayoutInputHorizontal();
    }

    public override void CalculateLayoutInputVertical()
    {
        UpdateLayoutValuesFromParent();
        base.CalculateLayoutInputVertical();
    }

    public override void SetLayoutHorizontal()
    {
        UpdateLayoutValuesFromParent();
        base.SetLayoutHorizontal();
    }

    public override void SetLayoutVertical()
    {
        UpdateLayoutValuesFromParent();
        base.SetLayoutVertical();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        NormalizeValues();
        UpdateLayoutValuesFromParent();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        NormalizeValues();
        UpdateLayoutValuesFromParent();
    }
#endif

    private void UpdateLayoutValuesFromParent()
    {
        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        Vector2 parentSize = parentRect.rect.size;

        float spacingX = widthCalculationMode == AxisCalculationMode.Percent
            ? parentSize.x * percentSpacing.x
            : flatSpacing.x;

        float spacingY = heightCalculationMode == AxisCalculationMode.Percent
            ? parentSize.y * percentSpacing.y
            : flatSpacing.y;

        spacing = new Vector2(spacingX, spacingY);

        int paddingLeft = widthCalculationMode == AxisCalculationMode.Percent
            ? Mathf.RoundToInt(parentSize.x * percentPadding.x)
            : Mathf.RoundToInt(flatPadding.x);

        int paddingRight = widthCalculationMode == AxisCalculationMode.Percent
            ? Mathf.RoundToInt(parentSize.x * percentPadding.y)
            : Mathf.RoundToInt(flatPadding.y);

        int paddingTop = heightCalculationMode == AxisCalculationMode.Percent
            ? Mathf.RoundToInt(parentSize.y * percentPadding.z)
            : Mathf.RoundToInt(flatPadding.z);

        int paddingBottom = heightCalculationMode == AxisCalculationMode.Percent
            ? Mathf.RoundToInt(parentSize.y * percentPadding.w)
            : Mathf.RoundToInt(flatPadding.w);

        padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);

        int childCount = Mathf.Max(1, GetActiveLayoutChildCount());
        GetGridDimensions(childCount, out int columns, out int rows);

        float availableWidth = Mathf.Max(0f, parentSize.x - padding.horizontal - (spacing.x * (columns - 1)));
        float availableHeight = Mathf.Max(0f, parentSize.y - padding.vertical - (spacing.y * (rows - 1)));

        float cellWidth = widthCalculationMode == AxisCalculationMode.Percent
            ? availableWidth * (percentSize.x / Mathf.Max(1, columns))
            : flatSize.x;

        float cellHeight = heightCalculationMode == AxisCalculationMode.Percent
            ? availableHeight * (percentSize.y / Mathf.Max(1, rows))
            : flatSize.y;

        cellSize = new Vector2(Mathf.Max(0f, cellWidth), Mathf.Max(0f, cellHeight));
    }

    private int GetActiveLayoutChildCount()
    {
        int count = 0;
        for (int i = 0; i < rectTransform.childCount; i++)
        {
            if (!(rectTransform.GetChild(i) is RectTransform child) || !child.gameObject.activeInHierarchy)
            {
                continue;
            }

            ILayoutIgnorer layoutIgnorer = child.GetComponent<ILayoutIgnorer>();
            if (layoutIgnorer != null && layoutIgnorer.ignoreLayout)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private void GetGridDimensions(int childCount, out int columns, out int rows)
    {
        switch (constraint)
        {
            case Constraint.FixedColumnCount:
                columns = Mathf.Max(1, constraintCount);
                rows = Mathf.Max(1, Mathf.CeilToInt((float)childCount / columns));
                break;

            case Constraint.FixedRowCount:
                rows = Mathf.Max(1, constraintCount);
                columns = Mathf.Max(1, Mathf.CeilToInt((float)childCount / rows));
                break;

            default:
                if (startAxis == Axis.Horizontal)
                {
                    columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(childCount)));
                    rows = Mathf.Max(1, Mathf.CeilToInt((float)childCount / columns));
                }
                else
                {
                    rows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(childCount)));
                    columns = Mathf.Max(1, Mathf.CeilToInt((float)childCount / rows));
                }
                break;
        }
    }

    private void NormalizeValues()
    {
        percentSize = ClampMinZero(percentSize);
        flatSize = ClampMinZero(flatSize);
        percentSpacing = ClampMinZero(percentSpacing);
        flatSpacing = ClampMinZero(flatSpacing);
        percentPadding = ClampMinZero(percentPadding);
        flatPadding = ClampMinZero(flatPadding);
    }

    private static Vector2 ClampMinZero(Vector2 value)
    {
        return new Vector2(Mathf.Max(0f, value.x), Mathf.Max(0f, value.y));
    }

    private static Vector4 ClampMinZero(Vector4 value)
    {
        return new Vector4(
            Mathf.Max(0f, value.x),
            Mathf.Max(0f, value.y),
            Mathf.Max(0f, value.z),
            Mathf.Max(0f, value.w));
    }
}
