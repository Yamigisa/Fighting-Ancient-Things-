using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int width, height;
    [SerializeField] private Transform mainCamera;

    [Header("Tile Settings")]
    [SerializeField] private Tile tilePrefab;

    [Header("Attack Range Preview")]
    [SerializeField] private Color attackRangeColor = new(0.3f, 0.65f, 1f, 1f);

    [Header("Enemy Destination")]
    [SerializeField] private EnemyDestination enemyDestinationPrefab;

    private Dictionary<Vector2, Tile> tiles = new Dictionary<Vector2, Tile>();
    private readonly List<Tile> attackRangePreviewTiles = new();
    private Tile previewTile;
    private bool isPlacementPreviewActive;
    private Vector2Int attackRangePreviewSourceCell;
    private Vector2Int attackRangePreviewSize;
    private Vector2Int attackRangePreviewDirection;

    public static GridManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GenerateGrid();
        SpawnEnemyDestination();
    }

    private void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile spawnedTile = Instantiate(tilePrefab, new Vector3(x, y), Quaternion.identity, transform);
                spawnedTile.name = $"Tile {x} {y}";

                tiles[new Vector2(x, y)] = spawnedTile;
            }
        }

        mainCamera.transform.position = new Vector3((float)width / 2 - 0.5f, (float)height / 2 - 0.5f, -10);
    }

    public Tile GetTilePosition(Vector2 pos)
    {
        if (tiles.TryGetValue(pos, out var tile))
        {
            return tile;
        }
        return null;
    }

    public Tile GetTileAtWorldPosition(Vector3 worldPosition)
    {
        Vector2 gridPosition = new Vector2(
            Mathf.FloorToInt(worldPosition.x + 0.5f),
            Mathf.FloorToInt(worldPosition.y + 0.5f));

        return GetTilePosition(gridPosition);
    }

    public Vector2Int GetEdgeCell(GridEdge edge, int laneIndex)
    {
        switch (edge)
        {
            case GridEdge.Top:
                return new Vector2Int(Mathf.Clamp(laneIndex, 0, width - 1), height - 1);
            case GridEdge.Left:
                return new Vector2Int(0, Mathf.Clamp(laneIndex, 0, height - 1));
            case GridEdge.Right:
                return new Vector2Int(width - 1, Mathf.Clamp(laneIndex, 0, height - 1));
            default:
                return new Vector2Int(Mathf.Clamp(laneIndex, 0, width - 1), 0);
        }
    }

    private void SpawnEnemyDestination()
    {
        Vector2Int centerCell = new(width / 2, height / 2);
        Tile centerTile = GetTilePosition(centerCell);
        if (centerTile == null)
            return;

        EnemyDestination destinationComponent = Instantiate(
            enemyDestinationPrefab,
            centerTile.transform.position,
            Quaternion.identity,
            transform);
        destinationComponent.name = "Enemy Destination";
        GameManager.Instance.InitializeEnemyDestinationHealth(destinationComponent.HealthText);
        centerTile.SetOccupied(true);
    }

    public void SetPlacementPreview(bool isActive)
    {
        isPlacementPreviewActive = isActive;

        if (!isActive)
            SetPlacementPreviewTile(null);
    }

    public void SetPlacementPreviewTile(Tile tile)
    {
        if (previewTile == tile)
            return;

        if (previewTile != null)
            previewTile.SetPlacementPreview(false);

        previewTile = isPlacementPreviewActive ? tile : null;

        if (previewTile != null)
            previewTile.SetPlacementPreview(true);
    }

    public void SetAttackRangePreview(Vector2Int sourceCell, Vector2Int areaSize, Vector2Int forwardDirection)
    {
        areaSize = new Vector2Int(Mathf.Max(1, areaSize.x), Mathf.Max(1, areaSize.y));
        if (attackRangePreviewTiles.Count > 0 && sourceCell == attackRangePreviewSourceCell &&
            areaSize == attackRangePreviewSize && forwardDirection == attackRangePreviewDirection)
        {
            return;
        }

        ClearAttackRangePreview();
        attackRangePreviewSourceCell = sourceCell;
        attackRangePreviewSize = areaSize;
        attackRangePreviewDirection = forwardDirection;

        Vector2Int sidewaysDirection = new(-forwardDirection.y, forwardDirection.x);
        int minimumWidth = -(areaSize.x / 2);

        for (int depth = 1; depth <= areaSize.y; depth++)
        {
            for (int width = minimumWidth; width < minimumWidth + areaSize.x; width++)
            {
                Vector2Int cell = sourceCell + forwardDirection * depth + sidewaysDirection * width;
                Tile tile = GetTilePosition(cell);
                if (tile == null)
                    continue;

                tile.SetAttackRangePreview(true, attackRangeColor);
                attackRangePreviewTiles.Add(tile);
            }
        }
    }

    public void ClearAttackRangePreview()
    {
        foreach (Tile tile in attackRangePreviewTiles)
        {
            if (tile != null)
                tile.SetAttackRangePreview(false, attackRangeColor);
        }

        attackRangePreviewTiles.Clear();
    }
}
