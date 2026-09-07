using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GridEdge { Bottom, Top, Left, Right }
public enum EnemyPathMode { Direct, StraightThenTurn }

[Serializable]
public class WaveEnemyGroup
{
    public EnemyObject enemyPrefab;
    [Min(1)] public int count = 1;
    [Min(0f)] public float spawnInterval = 1f;
    [Tooltip("The edge from which this line enters the grid.")]
    public GridEdge spawnEdge = GridEdge.Bottom;
    [Tooltip("Bottom/Top: 0 is the leftmost column. Left/Right: 0 is the bottom row.")]
    [Min(0)] public int laneIndex;
    [Tooltip("Direct moves diagonally toward the destination. Straight Then Turn moves inward along the grid first, then makes one 90-degree turn.")]
    public EnemyPathMode pathMode = EnemyPathMode.StraightThenTurn;
}

[Serializable]
public class Wave
{
    public string waveName = "Wave 1";
    [Min(0f)] public float delayBeforeWave = 1f;
    [Tooltip("Groups are processed in order. Add groups with different lane indices for multiple lines.")]
    public List<WaveEnemyGroup> enemyGroups = new();
}

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private List<Wave> waves = new();
    private readonly HashSet<EnemyObject> activeEnemies = new();
    private Coroutine waveCoroutine;
    private int currentWaveIndex;

    public int CurrentWaveNumber => currentWaveIndex + 1;
    public bool HasRemainingWaves => currentWaveIndex < waves.Count;
    public IReadOnlyList<WaveEnemyGroup> CurrentWaveGroups =>
        HasRemainingWaves ? waves[currentWaveIndex].enemyGroups : null;

    public void StartNextWave()
    {
        if (waveCoroutine == null && HasRemainingWaves)
            waveCoroutine = StartCoroutine(SpawnWave());
    }

    private IEnumerator SpawnWave()
    {
        Wave wave = waves[currentWaveIndex];
        if (wave.delayBeforeWave > 0f)
            yield return new WaitForSeconds(wave.delayBeforeWave);

        foreach (WaveEnemyGroup group in wave.enemyGroups)
        {
            if (group.enemyPrefab == null)
            {
                Debug.LogWarning($"EnemySpawner: {wave.waveName} has a group without an enemy prefab.", this);
                continue;
            }

            for (int enemyIndex = 0; enemyIndex < group.count; enemyIndex++)
            {
                SpawnEnemy(group);
                if (group.spawnInterval > 0f && enemyIndex < group.count - 1)
                    yield return new WaitForSeconds(group.spawnInterval);
            }
        }

        yield return new WaitUntil(() => activeEnemies.Count == 0);
        currentWaveIndex++;
        waveCoroutine = null;
        GamePhaseManager.Instance?.HandleWaveCleared();
        GamePhaseManager.Instance?.EndCombat();
    }

    private void SpawnEnemy(WaveEnemyGroup group)
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("EnemySpawner requires a GridManager.", this);
            return;
        }

        Vector2Int cell = GridManager.Instance.GetEdgeCell(group.spawnEdge, group.laneIndex);
        Vector3 spawnPosition = new Vector3(cell.x, cell.y, 0f);
        EnemyObject enemy = Instantiate(group.enemyPrefab, spawnPosition, Quaternion.identity);
        enemy.ConfigurePath(group.spawnEdge, group.pathMode);
        activeEnemies.Add(enemy);
        enemy.Destroyed += HandleEnemyDestroyed;
    }

    private void HandleEnemyDestroyed(EnemyObject enemy)
    {
        if (enemy != null)
            enemy.Destroyed -= HandleEnemyDestroyed;
        activeEnemies.Remove(enemy);
    }
}
