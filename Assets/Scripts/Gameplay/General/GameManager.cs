using System;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Wave UI")]
    [SerializeField] private Button startWaveButton;

    [Header("Wave Rewards")]
    [Min(1)][SerializeField] private int diamondsPerClearedWave = 2;

    private GamePhase currentPhase = GamePhase.Build;
    public static GameManager Instance { get; private set; }
    public GamePhase CurrentPhase => currentPhase;
    public bool IsBuildPhase => currentPhase == GamePhase.Build;
    public static event Action<GamePhase> PhaseChanged;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        startWaveButton.onClick.AddListener(StartCombat);
    }

    private void OnDisable()
    {
        startWaveButton.onClick.RemoveListener(StartCombat);
    }

    private void Start()
    {
        SetPhase(GamePhase.Build);
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
        SetPhase(GamePhase.Upgrade);
        ResourceManager.Instance?.Add(ResourceType.Diamond, diamondsPerClearedWave);
        startWaveButton.gameObject.SetActive(true);
    }
}

public enum GamePhase { Build, Combat, Upgrade }
