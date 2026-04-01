using UnityEngine;
using UnityEngine.UI;

public class ContentHeightFitter : MonoBehaviour
{
    [Header("Grid Layout Settings")]
    [SerializeField] private float extraPadding = 0f;

    [Header("Scrollbar Settings")]
    [SerializeField] private Scrollbar targetScrollbar;

    private GridLayoutGroup gridLayout;
    private RectTransform rectTransform;
    private int lastChildCount;

    private void Awake()
    {
        gridLayout = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        lastChildCount = transform.childCount;
        UpdateContentHeight();
    }

    private void OnTransformChildrenChanged()
    {
        int newChildCount = transform.childCount;
        if (newChildCount != lastChildCount)
        {
            lastChildCount = newChildCount;
            UpdateContentHeight();
        }
    }

    public void UpdateContentHeight()
    {
        if (gridLayout == null || rectTransform == null)
            return;

        int childCount = gridLayout.transform.childCount;
        if (childCount == 0)
            return;

        Vector2 cellSize = gridLayout.cellSize;
        Vector2 spacing = gridLayout.spacing;
        int constraintCount = gridLayout.constraintCount;
        GridLayoutGroup.Constraint constraint = gridLayout.constraint;

        float totalHeight;

        if (constraint == GridLayoutGroup.Constraint.Flexible)
        {
            int rows = Mathf.CeilToInt((float)childCount / constraintCount);
            totalHeight = rows * (cellSize.y + spacing.y) - spacing.y + extraPadding;
        }
        else
        {
            int columns = constraintCount;
            int rows = Mathf.CeilToInt((float)childCount / columns);
            totalHeight = rows * (cellSize.y + spacing.y) - spacing.y + extraPadding;
        }

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
    }
}
