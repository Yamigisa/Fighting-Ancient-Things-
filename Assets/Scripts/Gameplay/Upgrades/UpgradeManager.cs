using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    [Header("Upgrade Nodes")]
    [SerializeField] private List<UpgradeNode> upgradableNodes = new();
    [SerializeField] private UpgradeNodeUI upgradeNodeUI;

    [Header("UI Settings")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private int maximumNodesOffered = 3;
    [SerializeField] private Transform upgradeNodeUIParent;
    [SerializeField] private Button closeUpgradeUIButton;

    private HashSet<UpgradeNode> acquiredNodes = new();
    private List<UpgradeNode> currentOfferNodes = new();

    public static event Action<UpgradeNode> OnAcquiredNode;
    public static UpgradeManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameManager.PhaseChanged += HandlePhaseChanged;
        if (closeUpgradeUIButton != null)
            closeUpgradeUIButton.onClick.AddListener(CloseUpgradeUI);
    }

    private void OnDisable()
    {
        GameManager.PhaseChanged -= HandlePhaseChanged;
        if (closeUpgradeUIButton != null)
            closeUpgradeUIButton.onClick.RemoveListener(CloseUpgradeUI);
    }

    public void InitializeUpgradeNodes()
    {
        currentOfferNodes.Clear();

        for (int i = upgradeNodeUIParent.childCount - 1; i >= 0; i--)
            Destroy(upgradeNodeUIParent.GetChild(i).gameObject);

        List<UpgradeNode> eligibleNodes = GetEligibleNodes();

        for (int i = eligibleNodes.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);

            UpgradeNode temporaryNode = eligibleNodes[i];
            eligibleNodes[i] = eligibleNodes[randomIndex];
            eligibleNodes[randomIndex] = temporaryNode;
        }

        int offerCount = Mathf.Min(maximumNodesOffered, eligibleNodes.Count);

        for (int i = 0; i < offerCount; i++)
        {
            currentOfferNodes.Add(eligibleNodes[i]);
            UpgradeNodeUI newNodeUi = Instantiate(upgradeNodeUI, upgradeNodeUIParent);
            newNodeUi.Initialize(eligibleNodes[i]);
        }
    }

    private List<UpgradeNode> GetEligibleNodes()
    {
        List<UpgradeNode> eligibleNodes = new();

        foreach (UpgradeNode node in upgradableNodes)
        {
            if (CanOfferNode(node))
            {
                eligibleNodes.Add(node);
            }
        }

        return eligibleNodes;
    }

    private bool CanOfferNode(UpgradeNode node)
    {
        if (node == null)
            return false;

        if (IsAcquired(node))
            return false;

        if (node.requiredNodes == null || node.requiredNodes.Count == 0)
            return true;

        foreach (UpgradeNode requiredNode in node.requiredNodes)
        {
            if (!IsAcquired(requiredNode))
                return false;
        }

        return true;
    }

    public bool IsAcquired(UpgradeNode node)
    {
        return acquiredNodes.Contains(node);
    }

    public bool CanAcquireNode(UpgradeNode node)
    {
        if (node == null || !currentOfferNodes.Contains(node))
            return false;

        if (!CanOfferNode(node) || ResourceManager.Instance == null)
            return false;

        return ResourceManager.Instance.GetAmount(ResourceType.Diamond) >= node.diamondCost;
    }

    public bool TryAcquireNode(UpgradeNode node)
    {
        if (!CanAcquireNode(node))
            return false;

        if (!ResourceManager.Instance.TrySpend(ResourceType.Diamond, node.diamondCost))
            return false;

        acquiredNodes.Add(node);
        currentOfferNodes.Remove(node);
        OnAcquiredNode?.Invoke(node);

        return true;
    }

    private void HandlePhaseChanged(GamePhase newPhase)
    {
        if (newPhase != GamePhase.Upgrade)
            return;

        OpenUpgradeUI();
    }

    private void OpenUpgradeUI()
    {
        if (upgradePanel == null || upgradeNodeUI == null || upgradeNodeUIParent == null)
        {
            Debug.LogError("Upgrade Manager is missing an Upgrade Panel, Upgrade Node UI prefab, or Upgrade Node UI Parent.", this);
            GameManager.Instance.SetPhase(GamePhase.Build);
            return;
        }

        upgradePanel.SetActive(true);
        InitializeUpgradeNodes();
    }

    private void CloseUpgradeUI()
    {
        GameManager.Instance.SetPhase(GamePhase.Build);
        upgradePanel.SetActive(false);
    }
}
