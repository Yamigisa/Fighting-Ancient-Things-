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
    private GameObject selectedUnitPrefab;
    private GameObject ghostUnit;
    private UnitObject ghostUnitObject;
    private UnitObject movingUnit;
    private Tile movingUnitOriginTile;
    private Tile hoveredTile;
    private bool waitingForMoveSelectionClickRelease;
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
        {
            if (Input.GetMouseButtonDown(0))
                TryStartMovingClickedUnit();

            return;
        }

        UpdateGhostPosition();

        if (waitingForMoveSelectionClickRelease)
        {
            if (!Input.GetMouseButton(0))
                waitingForMoveSelectionClickRelease = false;

            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            RotateGhost();
            return;
        }

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
            TryPlaceSelectedUnit();
    }

    public void StartPlacement(UnitSO unit, GameObject unitPrefab)
    {
        if (!GameManager.Instance.IsBuildPhase)
            return;

        if (unit == null || unitPrefab == null || !unitPrefab.TryGetComponent<UnitObject>(out _))
        {
            Debug.LogError("Placement needs a UnitSO and a unit prefab that contains UnitObject.", unitPrefab);
            return;
        }

        CancelPlacement();

        selectedUnit = unit;
        selectedUnitPrefab = unitPrefab;
        ghostUnit = Instantiate(selectedUnitPrefab);
        ghostUnit.GetComponent<UnitObject>()?.SetUnitData(selectedUnit);
        ConfigureGhost(ghostUnit);
        ghostUnitObject = ghostUnit.GetComponent<UnitObject>();
        if (ghostUnitObject != null)
            ghostUnitObject.SetAttackRangeVisible(true);

        gridManager.SetPlacementPreview(true);
        SetMoveControlsVisible(true);
    }

    public void StartMovingUnit(UnitObject unit)
    {
        if (unit == null || unit.UnitData == null ||
            !GameManager.Instance.IsBuildPhase)
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
        selectedUnitPrefab = unit.gameObject;
        movingUnitOriginTile.SetOccupied(false);

        ghostUnit = Instantiate(selectedUnitPrefab, unit.transform.position, unit.transform.rotation);
        unit.gameObject.SetActive(false);
        ConfigureGhost(ghostUnit);
        ghostUnitObject = ghostUnit.GetComponent<UnitObject>();
        if (ghostUnitObject != null)
            ghostUnitObject.SetAttackRangeVisible(true);

        gridManager.SetPlacementPreview(true);
        waitingForMoveSelectionClickRelease = true;
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
        selectedUnitPrefab = null;
        hoveredTile = null;
        waitingForMoveSelectionClickRelease = false;
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
        if (!GameManager.Instance.IsBuildPhase)
            return;

        if (hoveredTile == null || hoveredTile.IsOccupied || selectedUnit == null || selectedUnitPrefab == null)
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

        GameObject placedUnit = Instantiate(selectedUnitPrefab, hoveredTile.transform.position, Quaternion.identity, placedUnitsParent);
        placedUnit.GetComponent<UnitObject>()?.SetUnitData(selectedUnit);
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
        selectedUnitPrefab = null;
        hoveredTile = null;
        waitingForMoveSelectionClickRelease = false;
        gridManager.SetPlacementPreview(false);
        SetMoveControlsVisible(false);
    }

    private void TryStartMovingClickedUnit()
    {
        if (!GameManager.Instance.IsBuildPhase ||
            sceneCamera == null)
        {
            return;
        }

        Vector3 mouseWorldPosition = sceneCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;
        Tile clickedTile = gridManager != null
            ? gridManager.GetTileAtWorldPosition(mouseWorldPosition)
            : null;

        if (clickedTile == null)
            return;

        foreach (UnitObject unit in FindObjectsByType<UnitObject>(FindObjectsSortMode.None))
        {
            if (unit == null || !unit.isActiveAndEnabled)
                continue;

            Tile unitTile = gridManager.GetTileAtWorldPosition(unit.transform.position);
            if (unitTile == clickedTile)
            {
                StartMovingUnit(unit);
                return;
            }
        }
    }

    private void RotateGhost()
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
