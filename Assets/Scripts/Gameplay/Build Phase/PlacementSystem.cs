using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlacementSystem : MonoBehaviour
{
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform placedUnitsParent;

    [Header("Unit Ghost Preview")]
    [Range(0f, 1f)][SerializeField] private float previewAlpha = 0.5f;

    private UnitSO selectedUnit;
    private GameObject ghostUnit;
    private UnitObject ghostUnitObject;
    private UnitObject movingUnit;
    private Tile movingUnitOriginTile;
    private Tile hoveredTile;
    private readonly Dictionary<SpriteRenderer, Color> ghostSpriteColors = new();
    private TextMeshProUGUI moveControlsText;

    public static PlacementSystem Instance { get; private set; }
    public bool IsPlacementMode => ghostUnit != null;
    public bool IsMovingUnit => movingUnit != null;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (ghostUnit == null)
            return;

        UpdateGhostPosition();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (IsMovingUnit)
                RotateMovingGhost();
            else
                CancelPlacement();

            return;
        }

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
            TryPlaceSelectedUnit();
    }

    public void StartPlacement(UnitSO unit)
    {
        if (GamePhaseManager.Instance != null && !GamePhaseManager.Instance.IsBuildPhase)
            return;

        CancelPlacement();

        selectedUnit = unit;
        ghostUnit = Instantiate(selectedUnit.prefab);
        ConfigureGhost(ghostUnit);
        ghostUnitObject = ghostUnit.GetComponent<UnitObject>();
        if (ghostUnitObject != null)
            ghostUnitObject.SetAttackRangeVisible(true);

        gridManager.SetPlacementPreview(true);
    }

    public void StartMovingUnit(UnitObject unit)
    {
        if (unit == null || unit.UnitData == null ||
            (GamePhaseManager.Instance != null && !GamePhaseManager.Instance.IsBuildPhase))
        {
            return;
        }

        CancelPlacement();
        movingUnit = unit;
        movingUnitOriginTile = gridManager.GetTileAtWorldPosition(unit.transform.position);
        if (movingUnitOriginTile == null)
        {
            movingUnit = null;
            return;
        }

        selectedUnit = unit.UnitData;
        movingUnitOriginTile.SetOccupied(false);
        unit.gameObject.SetActive(false);

        ghostUnit = Instantiate(selectedUnit.prefab, unit.transform.position, unit.transform.rotation);
        ConfigureGhost(ghostUnit);
        ghostUnitObject = ghostUnit.GetComponent<UnitObject>();
        if (ghostUnitObject != null)
            ghostUnitObject.SetAttackRangeVisible(true);

        gridManager.SetPlacementPreview(true);
        SetMoveControlsVisible(true);
    }

    public void CancelPlacement()
    {
        if (gridManager != null)
            gridManager.SetPlacementPreview(false);

        if (ghostUnitObject != null)
            ghostUnitObject.SetAttackRangeVisible(false);

        if (ghostUnit != null)
            Destroy(ghostUnit);

        if (movingUnit != null)
        {
            movingUnit.gameObject.SetActive(true);
            if (movingUnitOriginTile != null)
                movingUnitOriginTile.SetOccupied(true);
        }

        ghostSpriteColors.Clear();
        ghostUnit = null;
        ghostUnitObject = null;
        movingUnit = null;
        movingUnitOriginTile = null;
        selectedUnit = null;
        hoveredTile = null;
        SetMoveControlsVisible(false);
    }

    private void UpdateGhostPosition()
    {
        Vector3 mouseWorldPosition = sceneCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;

        hoveredTile = gridManager.GetTileAtWorldPosition(mouseWorldPosition);
        gridManager.SetPlacementPreviewTile(hoveredTile);

        if (hoveredTile == null)
        {
            // Keep the preview following the cursor outside the grid, but mark it
            // invalid so it cannot be mistaken for a placeable position.
            ghostUnit.transform.position = mouseWorldPosition;
            SetGhostColor(false);

            if (ghostUnitObject != null)
                ghostUnitObject.SetAttackRangeVisible(false);

            return;
        }

        ghostUnit.transform.position = hoveredTile.transform.position;
        SetGhostColor(!hoveredTile.IsOccupied);

        if (ghostUnitObject != null)
            ghostUnitObject.SetAttackRangeVisible(true);
    }

    private void TryPlaceSelectedUnit()
    {
        if (GamePhaseManager.Instance != null && !GamePhaseManager.Instance.IsBuildPhase)
            return;

        if (hoveredTile == null || hoveredTile.IsOccupied || selectedUnit == null)
            return;

        if (IsMovingUnit)
        {
            PlaceMovedUnit();
            return;
        }

        if (ResourceManager.Instance == null ||
            !ResourceManager.Instance.TrySpend(ResourceType.Gold, selectedUnit.cost))
        {
            return;
        }

        Instantiate(selectedUnit.prefab, hoveredTile.transform.position, Quaternion.identity, placedUnitsParent);
        hoveredTile.SetOccupied(true);
        CancelPlacement();
    }

    private void PlaceMovedUnit()
    {
        movingUnit.transform.SetPositionAndRotation(hoveredTile.transform.position, ghostUnit.transform.rotation);
        movingUnit.gameObject.SetActive(true);
        hoveredTile.SetOccupied(true);

        if (ghostUnitObject != null)
            ghostUnitObject.SetAttackRangeVisible(false);
        Destroy(ghostUnit);
        ghostSpriteColors.Clear();
        ghostUnit = null;
        ghostUnitObject = null;
        movingUnit = null;
        movingUnitOriginTile = null;
        selectedUnit = null;
        hoveredTile = null;
        gridManager.SetPlacementPreview(false);
        SetMoveControlsVisible(false);
    }

    private void RotateMovingGhost()
    {
        if (ghostUnit != null)
            ghostUnit.transform.Rotate(0f, 0f, -90f);
    }

    private void ConfigureGhost(GameObject preview)
    {
        ghostSpriteColors.Clear();

        foreach (Collider2D collider in preview.GetComponentsInChildren<Collider2D>())
            collider.enabled = false;

        foreach (MonoBehaviour behaviour in preview.GetComponentsInChildren<MonoBehaviour>())
            behaviour.enabled = false;

        foreach (SpriteRenderer spriteRenderer in preview.GetComponentsInChildren<SpriteRenderer>())
        {
            ghostSpriteColors.Add(spriteRenderer, spriteRenderer.color);
        }

        SetGhostColor(true);
    }

    private void SetGhostColor(bool canPlace)
    {
        foreach (KeyValuePair<SpriteRenderer, Color> sprite in ghostSpriteColors)
        {
            if (sprite.Key == null)
                continue;

            Color previewColor = canPlace ? sprite.Value : Color.red;
            previewColor.a = previewAlpha;
            sprite.Key.color = previewColor;
        }
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void SetMoveControlsVisible(bool isVisible)
    {
        if (moveControlsText == null && isVisible)
            CreateMoveControls();

        if (moveControlsText != null)
            moveControlsText.transform.parent.gameObject.SetActive(isVisible);
    }

    private void CreateMoveControls()
    {
        GameObject canvasObject = new("Move Controls", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        GameObject textObject = new("Move Instructions", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvasObject.transform, false);
        RectTransform textTransform = textObject.GetComponent<RectTransform>();
        textTransform.anchorMin = new Vector2(0.5f, 0f);
        textTransform.anchorMax = new Vector2(0.5f, 0f);
        textTransform.pivot = new Vector2(0.5f, 0f);
        textTransform.anchoredPosition = new Vector2(0f, 28f);
        textTransform.sizeDelta = new Vector2(500f, 50f);

        moveControlsText = textObject.GetComponent<TextMeshProUGUI>();
        moveControlsText.font = TMP_Settings.defaultFontAsset;
        moveControlsText.fontSize = 24f;
        moveControlsText.alignment = TextAlignmentOptions.Center;
        moveControlsText.color = Color.white;
        moveControlsText.text = "LMB: Place    RMB: Rotate    Esc: Cancel";
    }
}
