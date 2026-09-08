using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Enemy Object Prefab")]
    [SerializeField] private EnemyObject enemyObjectPrefab;

    [Header("Enemy wave")]
    public List<EnemyInWave> enemyWaves = new();

    private Coroutine waveCoroutine;
    private readonly HashSet<EnemyObject> activeEnemies = new();

    private int currentWaveIndex;
    public int CurrentWaveNumber => currentWaveIndex + 1;
    public EnemyInWave CurrentWave => enemyWaves[currentWaveIndex];

    public bool HasRemainingWaves => currentWaveIndex < enemyWaves.Count;

    public static EnemyManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameManager.PhaseChanged += HandlePhaseCombat;
    }

    private void OnDisable()
    {
        GameManager.PhaseChanged -= HandlePhaseCombat;
    }

    private void HandlePhaseCombat(GamePhase phase)
    {
        if (phase == GamePhase.Combat)
        {
            StartWave();
        }
    }

    private void StartWave()
    {
        waveCoroutine = StartCoroutine(SpawnWave());
    }

    private IEnumerator SpawnWave()
    {
        EnemyInWave wave = enemyWaves[currentWaveIndex];

        for (int enemyIndex = 0; enemyIndex < wave.count; enemyIndex++)
        {
            SpawnEnemy(wave);

            if (wave.spawnInterval > 0f && enemyIndex < wave.count - 1)
                yield return new WaitForSeconds(wave.spawnInterval);
        }

        yield return new WaitUntil(() => activeEnemies.Count == 0);

        currentWaveIndex++;
        waveCoroutine = null;

        GameManager.Instance.EndCombat();
    }

    private void SpawnEnemy(EnemyInWave group)
    {
        Vector2Int cell = GridManager.Instance.GetEdgeCell(group.spawnEdge, group.laneIndex);
        Vector3 spawnPosition = new Vector3(cell.x, cell.y, 0f);

        EnemySO enemyData = group.enemyList[UnityEngine.Random.Range(0, group.enemyList.Count)];

        EnemyObject enemy = Instantiate(enemyObjectPrefab, spawnPosition, Quaternion.identity);

        enemy.Initialize(enemyData);

        enemy.ConfigurePath(group.spawnEdge);
        activeEnemies.Add(enemy);
        enemy.Destroyed += HandleEnemyDestroyed;
    }

    private void HandleEnemyDestroyed(EnemyObject enemy)
    {
        enemy.Destroyed -= HandleEnemyDestroyed;

        activeEnemies.Remove(enemy);
    }
}

public enum GridEdge { Bottom, Top, Left, Right }

[Serializable]
public class EnemyInWave
{
    public List<EnemySO> enemyList = new();
    [Min(1)] public int count = 1;
    [Min(0f)] public float spawnInterval = 1f;
    public GridEdge spawnEdge = GridEdge.Bottom;
    [Tooltip("Bottom/Top: 0 is the leftmost column. Left/Right: 0 is the bottom row.")]
    [Min(0)] public int laneIndex;
}
