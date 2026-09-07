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
    [Min(0)] [SerializeField] private int startingGold = 10;
    [Min(0)] [SerializeField] private int goldPerRegen = 1;
    [Min(0.01f)] [SerializeField] private float goldRegenInterval = 1f;

    private Dictionary<ResourceType, int> amounts = new();
    private Coroutine goldRegenCoroutine;
    private Canvas pickupCanvas;
    private Sprite pickupSprite;

    public event Action<ResourceType, int> ResourceChanged;

    public int GetAmount(ResourceType type) => amounts.GetValueOrDefault(type);

    public static ResourceManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        amounts[ResourceType.Gold] = 0;
        amounts[ResourceType.Diamond] = 0;
        Add(ResourceType.Gold, startingGold);
    }

    private void OnEnable()
    {
        ResourceChanged += UpdateResourceText;
        RefreshResourceTexts();
    }

    private void OnDisable()
    {
        ResourceChanged -= UpdateResourceText;

        if (goldRegenCoroutine != null)
        {
            StopCoroutine(goldRegenCoroutine);
            goldRegenCoroutine = null;
        }
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

    public void CollectFromWorld(ResourceType type, int amount, Vector3 worldPosition)
    {
        if (amount <= 0)
            return;

        TextMeshProUGUI targetText = type == ResourceType.Gold ? goldText : diamondText;
        if (targetText == null)
        {
            Add(type, amount);
            return;
        }

        StartCoroutine(AnimateCollection(type, amount, worldPosition, targetText));
    }

    public void SetGoldRegenerationActive(bool isActive)
    {
        if (isActive && goldRegenCoroutine == null)
            goldRegenCoroutine = StartCoroutine(RegenerateGold());
        else if (!isActive && goldRegenCoroutine != null)
        {
            StopCoroutine(goldRegenCoroutine);
            goldRegenCoroutine = null;
        }
    }

    private void RefreshResourceTexts()
    {
        UpdateResourceText(ResourceType.Gold, GetAmount(ResourceType.Gold));
        UpdateResourceText(ResourceType.Diamond, GetAmount(ResourceType.Diamond));
    }

    private IEnumerator RegenerateGold()
    {
        while (true)
        {
            yield return new WaitForSeconds(goldRegenInterval);
            Add(ResourceType.Gold, goldPerRegen);
        }
    }

    private IEnumerator AnimateCollection(ResourceType type, int amount, Vector3 worldPosition, TextMeshProUGUI targetText)
    {
        EnsurePickupCanvas();
        GameObject pickupObject = new($"{type} Pickup", typeof(RectTransform), typeof(Image));
        pickupObject.transform.SetParent(pickupCanvas.transform, false);
        RectTransform pickupTransform = pickupObject.GetComponent<RectTransform>();
        pickupTransform.sizeDelta = new Vector2(24f, 24f);

        Image pickupImage = pickupObject.GetComponent<Image>();
        pickupImage.sprite = GetPickupSprite();
        pickupImage.color = type == ResourceType.Gold ? new Color(1f, 0.8f, 0.1f) : new Color(0.3f, 0.8f, 1f);

        Vector2 start = RectTransformUtility.WorldToScreenPoint(null, worldPosition);
        Vector2 end = RectTransformUtility.WorldToScreenPoint(null, targetText.transform.position);
        float duration = 0.55f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = elapsed / duration;
            pickupTransform.position = Vector2.Lerp(start, end, progress * progress);
            pickupTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, progress);
            yield return null;
        }

        Add(type, amount);
        Destroy(pickupObject);
    }

    private void EnsurePickupCanvas()
    {
        if (pickupCanvas != null)
            return;

        GameObject canvasObject = new("Resource Pickup Effects", typeof(Canvas), typeof(CanvasScaler));
        pickupCanvas = canvasObject.GetComponent<Canvas>();
        pickupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        pickupCanvas.sortingOrder = 100;
    }

    private Sprite GetPickupSprite()
    {
        if (pickupSprite != null)
            return pickupSprite;

        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        pickupSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return pickupSprite;
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
}

public enum ResourceType
{
    Gold,
    Diamond
}
