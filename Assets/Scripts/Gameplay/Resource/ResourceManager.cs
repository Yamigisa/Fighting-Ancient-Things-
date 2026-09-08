using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceManager : MonoBehaviour
{
    [Header("Resources Text")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI diamondText;

    [Header("Gold Regeneration")]
    [Min(0)][SerializeField] private int startingGold = 10;
    [Min(0)][SerializeField] private int goldPerRegen = 1;
    [Min(0.01f)][SerializeField] private float goldRegenInterval = 1f;

    [Header("Diamond Regeneration")]
    [Min(0)][SerializeField] private int startingDiamond = 0;
    [Min(0)][SerializeField] private int diamondPerRegen = 0;
    [Min(0)][SerializeField] private float diamondRegenInterval = 0;

    private Dictionary<ResourceType, int> amounts = new();
    private Coroutine goldRegenCoroutine;
    private Coroutine diamondRegenCoroutine;

    public event Action<ResourceType, int> ResourceChanged;

    public int GetAmount(ResourceType type) => amounts.GetValueOrDefault(type);

    public static ResourceManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        amounts[ResourceType.Gold] = 0;
        amounts[ResourceType.Diamond] = 0;
        Add(ResourceType.Gold, startingGold);
        Add(ResourceType.Diamond, startingDiamond);
    }

    private void OnEnable()
    {
        ResourceChanged += UpdateResourceText;
        GameManager.PhaseChanged += HandlePhaseChanged;
        RefreshResourceTexts();
    }

    private void OnDisable()
    {
        ResourceChanged -= UpdateResourceText;

        StopCoroutine(goldRegenCoroutine);
        goldRegenCoroutine = null;

        GameManager.PhaseChanged -= HandlePhaseChanged;
    }

    public void Add(ResourceType type, int amount)
    {
        amounts[type] = GetAmount(type) + amount;
        ResourceChanged?.Invoke(type, amounts[type]);
    }

    public bool TrySpend(ResourceType type, int amount)
    {
        if (GetAmount(type) < amount)
            return false;

        amounts[type] -= amount;
        ResourceChanged?.Invoke(type, amounts[type]);

        return true;
    }

    private void RefreshResourceTexts()
    {
        UpdateResourceText(ResourceType.Gold, GetAmount(ResourceType.Gold));
        UpdateResourceText(ResourceType.Diamond, GetAmount(ResourceType.Diamond));
    }

    private void UpdateResourceText(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Gold:
                if (goldText != null)
                    goldText.text = amount.ToString();
                break;

            case ResourceType.Diamond:
                if (diamondText != null)
                    diamondText.text = amount.ToString();
                break;
        }
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.Combat)
        {
            ResourceRegeneration(ResourceType.Gold, true);
        }
        else
        {
            ResourceRegeneration(ResourceType.Gold, false);
        }
    }

    private void ResourceRegeneration(ResourceType type = ResourceType.Gold, bool isActive = true)
    {
        if (isActive)
        {
            switch (type)
            {
                case ResourceType.Gold:
                    goldRegenCoroutine = StartCoroutine(RegenerateResource(type));
                    break;

                case ResourceType.Diamond:
                    diamondRegenCoroutine = StartCoroutine(RegenerateResource(type));
                    break;
            }
        }
        else
        {
            switch (type)
            {
                case ResourceType.Gold:
                    if (goldRegenCoroutine != null)
                    {
                        StopCoroutine(goldRegenCoroutine);
                        goldRegenCoroutine = null;
                    }
                    break;

                case ResourceType.Diamond:
                    if (diamondRegenCoroutine != null)
                    {
                        StopCoroutine(diamondRegenCoroutine);
                        diamondRegenCoroutine = null;
                    }
                    break;
            }
        }
    }

    private IEnumerator RegenerateResource(ResourceType type)
    {
        while (true)
        {
            switch (type)
            {
                case ResourceType.Gold:
                    yield return new WaitForSeconds(goldRegenInterval);
                    Add(ResourceType.Gold, goldPerRegen);
                    break;

                case ResourceType.Diamond:
                    yield return new WaitForSeconds(diamondRegenInterval);
                    Add(ResourceType.Diamond, diamondPerRegen);
                    break;

                default:
                    yield break;
            }
        }
    }
}

public enum ResourceType
{
    Gold,
    Diamond
}
