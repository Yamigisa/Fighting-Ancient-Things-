using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private TextMeshProUGUI waveNumberText;
    [SerializeField] private Button startWaveButton;
    [Min(1)][SerializeField] private int diamondsPerClearedWave = 2;

    [Header("Enemy Destination Health")]
    [Min(1)][SerializeField] private int enemyDestinationMaxHealth = 20;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button retryButton;
    public TextMeshProUGUI gameOverText;

    private GamePhase currentPhase = GamePhase.Build;
    private TextMeshProUGUI enemyDestinationHealthText;
    private int currentEnemyDestinationHealth;
    public static GameManager Instance { get; private set; }
    public GamePhase CurrentPhase => currentPhase;
    public bool IsBuildPhase => currentPhase == GamePhase.Build;
    public static event Action<GamePhase> PhaseChanged;

    private void Awake()
    {
        Instance = this;
        currentEnemyDestinationHealth = enemyDestinationMaxHealth;
    }

    private void OnEnable()
    {
        startWaveButton.onClick.AddListener(StartCombat);
        PhaseChanged += HandlePhaseChangedForWaveText;
        retryButton.onClick.AddListener(RetryGame);
    }

    private void OnDisable()
    {
        startWaveButton.onClick.RemoveListener(StartCombat);
        PhaseChanged -= HandlePhaseChangedForWaveText;
        retryButton.onClick.RemoveListener(RetryGame);
    }

    private void Start()
    {
        SetPhase(GamePhase.Build);
        UpdateWaveNumberText();
    }

    public void SetPhase(GamePhase phase)
    {
        currentPhase = phase;
        PhaseChanged?.Invoke(phase);
    }

    public void StartCombat()
    {
        SetPhase(GamePhase.Combat);
        PlacementSystem.Instance?.CancelPlacement();
        startWaveButton.gameObject.SetActive(false);
    }

    public void EndCombat()
    {
        if (currentPhase == GamePhase.GameOver)
            return;

        SetPhase(GamePhase.Upgrade);
        ResourceManager.Instance?.Add(ResourceType.Diamond, diamondsPerClearedWave);
        startWaveButton.gameObject.SetActive(true);
    }

    private void HandlePhaseChangedForWaveText(GamePhase phase)
    {
        UpdateWaveNumberText();
    }

    private void UpdateWaveNumberText()
    {
        int currentWave = EnemyManager.Instance.CurrentWaveNumber;
        int totalWaves = EnemyManager.Instance.enemyWaves.Count;

        waveNumberText.text = currentWave >= totalWaves
            ? "FINAL"
            : $"WAVE {currentWave}/{totalWaves}";
    }

    public void InitializeEnemyDestinationHealth(TextMeshProUGUI healthText)
    {
        enemyDestinationHealthText = healthText;
        RefreshEnemyDestinationHealthText();
    }

    public void ReceiveEnemyDestinationDamage(int damage)
    {
        currentEnemyDestinationHealth -= damage;
        RefreshEnemyDestinationHealthText();

        if (currentEnemyDestinationHealth <= 0)
            GameOver();
    }

    private void RefreshEnemyDestinationHealthText()
    {
        enemyDestinationHealthText.text = $"Health {currentEnemyDestinationHealth}/{enemyDestinationMaxHealth}";
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        currentEnemyDestinationHealth = 0;
        SetPhase(GamePhase.GameOver);
        PlacementSystem.Instance.CancelPlacement();
        startWaveButton.gameObject.SetActive(false);
        gameOverPanel.SetActive(true);
    }
    public void RetryGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}

public enum GamePhase { Build, Combat, Upgrade, GameOver }
