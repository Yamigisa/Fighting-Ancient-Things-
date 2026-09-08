using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Placement Preview Colors")]
    [SerializeField] private Color validPlacementColor = new(0.35f, 0.9f, 0.35f, 1f);
    [SerializeField] private Color invalidPlacementColor = new(0.9f, 0.35f, 0.35f, 1f);

    private bool isOccupied = false;
    private bool isShowingPlacementPreview;
    private bool isShowingAttackRangePreview;
    private Color defaultColor;
    private Color attackRangeColor;

    public bool IsOccupied => isOccupied;

    private void Awake()
    {
        defaultColor = spriteRenderer.color;
    }

    public void SetPlacementPreview(bool isActive)
    {
        isShowingPlacementPreview = isActive;
        RefreshColor();
    }

    public void SetAttackRangePreview(bool isActive, Color color)
    {
        isShowingAttackRangePreview = isActive;
        attackRangeColor = color;
        RefreshColor();
    }

    public void SetOccupied(bool value)
    {
        isOccupied = value;
        RefreshColor();
    }

    private void RefreshColor()
    {
        if (spriteRenderer == null)
            return;

        if (isShowingPlacementPreview)
        {
            spriteRenderer.color = isOccupied ? invalidPlacementColor : validPlacementColor;
            return;
        }

        if (isShowingAttackRangePreview)
        {
            Color previewColor = Color.Lerp(defaultColor, attackRangeColor, attackRangeColor.a);
            previewColor.a = defaultColor.a;
            spriteRenderer.color = previewColor;
            return;
        }

        spriteRenderer.color = defaultColor;
    }
}
