using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRouting : MonoBehaviour
{
    [Header("Routing References")]
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private GridManager gridManager;

    [Header("Build-Phase Spawn Indicators")]
    [SerializeField] private Color spawnIndicatorColor = new(1f, 0.3f, 0.1f, 0.85f);
    [Min(0.1f)][SerializeField] private float indicatorFlashSpeed = 2f;
    [Min(0.1f)][SerializeField] private float indicatorTravelSpeed = 0.35f;
    [Min(0.01f)][SerializeField] private float lineWidth = 0.06f;
    [Min(0.01f)][SerializeField] private float pulseScale = 0.35f;
    [Min(0)][SerializeField] private int lineSortingOrderOffset = 9;
    [Min(0)][SerializeField] private int pulseSortingOrderOffset = 10;

    private readonly List<SpawnRouteIndicator> spawnIndicators = new();
    private Sprite indicatorSprite;

    private class SpawnRouteIndicator
    {
        public GameObject root;
        public SpriteRenderer pulse;
        public Vector3 start;
        public Vector3 corner;
        public Vector3 end;
        public float offset;
    }

    private void Awake()
    {
        indicatorSprite = CreateIndicatorSprite();
    }

    private void OnEnable()
    {
        StartCoroutine(InitializeRouting());
    }

    private void OnDisable()
    {
        GameManager.PhaseChanged -= HandlePhaseChanged;
        ClearSpawnIndicators();
    }

    private IEnumerator InitializeRouting()
    {
        yield return null;
        GameManager.PhaseChanged += HandlePhaseChanged;
        HandlePhaseChanged(GameManager.Instance.CurrentPhase);
    }

    private void Update()
    {
        if (!GameManager.Instance.IsBuildPhase)
            return;

        foreach (SpawnRouteIndicator indicator in spawnIndicators)
        {
            float progress = Mathf.Repeat(Time.time * indicatorTravelSpeed + indicator.offset, 1f);
            float alpha = Mathf.Lerp(0.25f, spawnIndicatorColor.a,
                (Mathf.Sin((Time.time + indicator.offset) * indicatorFlashSpeed * Mathf.PI * 2f) + 1f) * 0.5f);

            float firstPathLength = Vector3.Distance(indicator.start, indicator.corner);
            float secondPathLength = Vector3.Distance(indicator.corner, indicator.end);
            float firstPathProgress = firstPathLength / (firstPathLength + secondPathLength);

            if (secondPathLength == 0f)
                indicator.pulse.transform.position = Vector3.Lerp(indicator.start, indicator.corner, progress);
            else if (firstPathLength == 0f)
                indicator.pulse.transform.position = Vector3.Lerp(indicator.corner, indicator.end, progress);
            else if (progress <= firstPathProgress)
                indicator.pulse.transform.position = Vector3.Lerp(indicator.start, indicator.corner, progress / firstPathProgress);
            else
                indicator.pulse.transform.position = Vector3.Lerp(indicator.corner, indicator.end,
                    (progress - firstPathProgress) / (1f - firstPathProgress));

            indicator.pulse.color = new Color(spawnIndicatorColor.r, spawnIndicatorColor.g, spawnIndicatorColor.b, alpha);
        }
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.Build && enemyManager.HasRemainingWaves)
            RefreshSpawnIndicators();
        else
            ClearSpawnIndicators();
    }

    private void RefreshSpawnIndicators()
    {
        ClearSpawnIndicators();

        Vector3 destinationPosition = EnemyDestination.Instance.transform.position;
        int routeIndex = 0;

        EnemyInWave wave = enemyManager.CurrentWave;
        Vector2Int cell = gridManager.GetEdgeCell(wave.spawnEdge, wave.laneIndex);
        Vector3 spawnPosition = new(cell.x, cell.y, -0.1f);

        Vector3 firstCorner = wave.spawnEdge is GridEdge.Bottom or GridEdge.Top
            ? new Vector3(spawnPosition.x, destinationPosition.y, destinationPosition.z)
            : new Vector3(destinationPosition.x, spawnPosition.y, destinationPosition.z);

        GameObject routeObject = new($"Spawn Route ({wave.spawnEdge}, Lane {wave.laneIndex})", typeof(LineRenderer));
        LineRenderer line = routeObject.GetComponent<LineRenderer>();
        line.positionCount = firstCorner == destinationPosition ? 2 : 3;
        line.SetPosition(0, spawnPosition);
        line.SetPosition(1, firstCorner);
        if (line.positionCount == 3)
            line.SetPosition(2, destinationPosition);
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.startColor = new Color(spawnIndicatorColor.r, spawnIndicatorColor.g, spawnIndicatorColor.b, 0.25f);
        line.endColor = line.startColor;

        GameObject pulseObject = new("Traveling Pulse", typeof(SpriteRenderer));
        pulseObject.transform.SetParent(routeObject.transform);
        pulseObject.transform.position = spawnPosition;
        pulseObject.transform.localScale = Vector3.one * pulseScale;

        SpriteRenderer pulse = pulseObject.GetComponent<SpriteRenderer>();
        pulse.sprite = indicatorSprite;

        Tile tile = gridManager.GetTilePosition(cell);
        SpriteRenderer tileRenderer = tile.GetComponent<SpriteRenderer>();
        line.sortingLayerID = tileRenderer.sortingLayerID;
        line.sortingOrder = tileRenderer.sortingOrder + lineSortingOrderOffset;
        pulse.sortingLayerID = tileRenderer.sortingLayerID;
        pulse.sortingOrder = tileRenderer.sortingOrder + pulseSortingOrderOffset;

        spawnIndicators.Add(new SpawnRouteIndicator
        {
            root = routeObject,
            pulse = pulse,
            start = spawnPosition,
            corner = firstCorner,
            end = destinationPosition,
            offset = routeIndex++ * 0.2f
        });
    }

    private void ClearSpawnIndicators()
    {
        foreach (SpawnRouteIndicator indicator in spawnIndicators)
            Destroy(indicator.root);

        spawnIndicators.Clear();
    }

    private Sprite CreateIndicatorSprite()
    {
        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
