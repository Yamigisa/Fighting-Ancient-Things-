using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum GamePhase { Build, Combat }

public class GamePhaseManager : MonoBehaviour
{
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Button startWaveButton;
    [SerializeField] private TextMeshProUGUI startWaveButtonText;

    [Header("Wave Rewards")]
    [Min(0)] [SerializeField] private int diamondsPerClearedWave = 2;

    [Header("Build-Phase Spawn Indicators")]
    [SerializeField] private Color spawnIndicatorColor = new(1f, 0.3f, 0.1f, 0.85f);
    [Min(0.1f)] [SerializeField] private float indicatorFlashSpeed = 2f;
    [Min(0.1f)] [SerializeField] private float indicatorTravelSpeed = 0.35f;

    private readonly List<SpawnRouteIndicator> spawnIndicators = new();
    private Sprite indicatorSprite;
    private bool spawnIndicatorsInitialized;

    private class SpawnRouteIndicator
    {
        public GameObject root;
        public SpriteRenderer pulse;
        public Vector3 start;
        public Vector3 end;
        public float offset;
    }

    public static GamePhaseManager Instance { get; private set; }
    public GamePhase CurrentPhase { get; private set; } = GamePhase.Build;
    public bool IsBuildPhase => CurrentPhase == GamePhase.Build;
    public event Action<GamePhase> PhaseChanged;

    private void Awake()
    {
        Instance = this;
        if (enemySpawner == null)
            enemySpawner = GetComponent<EnemySpawner>();
    }

    private void Start()
    {
        CreateFallbackStartWaveButton();

        if (startWaveButton != null)
            startWaveButton.onClick.AddListener(StartCombat);

        SetPhase(GamePhase.Build);
        RefreshStartWaveButton();
    }

    private void OnDestroy()
    {
        if (startWaveButton != null)
            startWaveButton.onClick.RemoveListener(StartCombat);
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!IsBuildPhase)
            return;

        if (!spawnIndicatorsInitialized && EnemyDestination.Instance != null)
            RefreshSpawnIndicators();

        foreach (SpawnRouteIndicator indicator in spawnIndicators)
        {
            if (indicator.pulse == null)
                continue;

            float progress = Mathf.Repeat(Time.time * indicatorTravelSpeed + indicator.offset, 1f);
            float alpha = Mathf.Lerp(0.25f, spawnIndicatorColor.a,
                (Mathf.Sin((Time.time + indicator.offset) * indicatorFlashSpeed * Mathf.PI * 2f) + 1f) * 0.5f);
            indicator.pulse.transform.position = Vector3.Lerp(indicator.start, indicator.end, progress);
            indicator.pulse.color = new Color(spawnIndicatorColor.r, spawnIndicatorColor.g, spawnIndicatorColor.b, alpha);
        }
    }

    public void StartCombat()
    {
        if (!IsBuildPhase || enemySpawner == null || !enemySpawner.HasRemainingWaves)
            return;

        PlacementSystem.Instance?.CancelPlacement();
        SetPhase(GamePhase.Combat);
        enemySpawner.StartNextWave();
        RefreshStartWaveButton();
    }

    public void EndCombat()
    {
        SetPhase(GamePhase.Build);
        RefreshStartWaveButton();
        SkillTreeManager.Instance?.Show();
    }

    public void HandleWaveCleared()
    {
        if (diamondsPerClearedWave <= 0 || ResourceManager.Instance == null)
            return;

        Vector3 rewardOrigin = EnemyDestination.Instance != null
            ? EnemyDestination.Instance.transform.position
            : Vector3.zero;
        ResourceManager.Instance.CollectFromWorld(ResourceType.Diamond, diamondsPerClearedWave, rewardOrigin);
    }

    private void SetPhase(GamePhase phase)
    {
        CurrentPhase = phase;
        ResourceManager.Instance?.SetGoldRegenerationActive(phase == GamePhase.Combat);
        RefreshSpawnIndicators();
        PhaseChanged?.Invoke(phase);
    }

    private void RefreshSpawnIndicators()
    {
        foreach (SpawnRouteIndicator indicator in spawnIndicators)
        {
            if (indicator.root != null)
            {
                indicator.root.SetActive(false);
                Destroy(indicator.root);
            }
        }
        spawnIndicators.Clear();
        spawnIndicatorsInitialized = false;

        if (!IsBuildPhase || enemySpawner == null || GridManager.Instance == null ||
            enemySpawner.CurrentWaveGroups == null || EnemyDestination.Instance == null)
        {
            return;
        }

        Vector3 destinationPosition = EnemyDestination.Instance.transform.position;
        int routeIndex = 0;
        foreach (WaveEnemyGroup group in enemySpawner.CurrentWaveGroups)
        {
            Vector2Int cell = GridManager.Instance.GetEdgeCell(group.spawnEdge, group.laneIndex);
            Vector3 spawnPosition = new(cell.x, cell.y, -0.1f);
            GameObject routeObject = new($"Spawn Route ({group.spawnEdge}, Lane {group.laneIndex})", typeof(LineRenderer));
            LineRenderer line = routeObject.GetComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, spawnPosition);
            line.SetPosition(1, destinationPosition);
            line.startWidth = 0.06f;
            line.endWidth = 0.06f;
            line.startColor = new Color(spawnIndicatorColor.r, spawnIndicatorColor.g, spawnIndicatorColor.b, 0.25f);
            line.endColor = line.startColor;

            GameObject pulseObject = new("Traveling Pulse", typeof(SpriteRenderer));
            pulseObject.transform.SetParent(routeObject.transform);
            pulseObject.transform.position = spawnPosition;
            pulseObject.transform.localScale = Vector3.one * 0.35f;

            SpriteRenderer pulse = pulseObject.GetComponent<SpriteRenderer>();
            pulse.sprite = GetIndicatorSprite();
            Tile tile = GridManager.Instance.GetTilePosition(cell);
            SpriteRenderer tileRenderer = tile != null ? tile.GetComponent<SpriteRenderer>() : null;
            if (tileRenderer != null)
            {
                line.sortingLayerID = tileRenderer.sortingLayerID;
                line.sortingOrder = tileRenderer.sortingOrder + 9;
                pulse.sortingLayerID = tileRenderer.sortingLayerID;
                pulse.sortingOrder = tileRenderer.sortingOrder + 10;
            }
            else
            {
                line.sortingOrder = 19;
                pulse.sortingOrder = 20;
            }

            spawnIndicators.Add(new SpawnRouteIndicator
            {
                root = routeObject,
                pulse = pulse,
                start = spawnPosition,
                end = destinationPosition,
                offset = routeIndex++ * 0.2f
            });
        }

        spawnIndicatorsInitialized = true;
    }

    private Sprite GetIndicatorSprite()
    {
        if (indicatorSprite != null)
            return indicatorSprite;

        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        indicatorSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return indicatorSprite;
    }

    private void CreateFallbackStartWaveButton()
    {
        if (startWaveButton != null)
            return;

        GameObject canvasObject = new("Wave Controls", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        GameObject buttonObject = new("Start Wave Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvasObject.transform, false);
        RectTransform buttonTransform = buttonObject.GetComponent<RectTransform>();
        buttonTransform.anchorMin = new Vector2(1f, 0f);
        buttonTransform.anchorMax = new Vector2(1f, 0f);
        buttonTransform.pivot = new Vector2(1f, 0f);
        buttonTransform.anchoredPosition = new Vector2(-30f, 30f);
        buttonTransform.sizeDelta = new Vector2(220f, 65f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.15f, 0.5f, 0.22f, 1f);
        startWaveButton = buttonObject.GetComponent<Button>();
        startWaveButton.targetGraphic = buttonImage;

        GameObject textObject = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textTransform = textObject.GetComponent<RectTransform>();
        textTransform.anchorMin = Vector2.zero;
        textTransform.anchorMax = Vector2.one;
        textTransform.offsetMin = Vector2.zero;
        textTransform.offsetMax = Vector2.zero;
        startWaveButtonText = textObject.GetComponent<TextMeshProUGUI>();
        startWaveButtonText.font = TMP_Settings.defaultFontAsset;
        startWaveButtonText.alignment = TextAlignmentOptions.Center;
        startWaveButtonText.color = Color.white;
        startWaveButtonText.fontSize = 24f;
    }

    private void RefreshStartWaveButton()
    {
        if (startWaveButton != null)
            startWaveButton.interactable = IsBuildPhase && enemySpawner != null && enemySpawner.HasRemainingWaves;

        if (startWaveButtonText != null)
            startWaveButtonText.text = enemySpawner != null && enemySpawner.HasRemainingWaves
                ? $"Start Wave {enemySpawner.CurrentWaveNumber}"
                : "All Waves Complete";
    }
}
