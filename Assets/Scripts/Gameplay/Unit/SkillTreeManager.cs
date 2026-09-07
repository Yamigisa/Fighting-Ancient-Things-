using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GeneralSkillEffect { Health, Attack, AttackSpeed }
public enum GeneralSkillCategory { Vitality, Offense, Tempo }

[Serializable]
public class GeneralSkillNode
{
    [Header("Identity")]
    [Tooltip("A unique ID used by other nodes as a prerequisite. Example: vitality_1")]
    public string nodeId = "new_node";
    public string title = "New Skill";
    [TextArea] public string description;
    [Min(1)] public int diamondCost = 2;
    [Header("Category and Bonus")]
    public GeneralSkillCategory category;
    public GeneralSkillEffect effect;
    [Min(0.1f)] public float amount = 1f;
    [Header("Prerequisites")]
    [Tooltip("All listed node IDs must be unlocked before this node can be bought.")]
    public List<string> requiredNodeIds = new();
    [HideInInspector] public bool unlocked;
}

public class SkillTreeManager : MonoBehaviour
{
    [Header("General Skill Tree")]
    [Tooltip("Nodes unlock in order. Edit their cost and bonus in the Inspector.")]
    [SerializeField] private List<GeneralSkillNode> generalNodes = new()
    {
        new GeneralSkillNode { nodeId = "vitality_1", title = "Reinforced Frames", description = "+2 health for every unit.", diamondCost = 2, category = GeneralSkillCategory.Vitality, effect = GeneralSkillEffect.Health, amount = 2 },
        new GeneralSkillNode { nodeId = "offense_1", title = "Sharpened Weapons", description = "+1 attack damage for every unit.", diamondCost = 2, category = GeneralSkillCategory.Offense, effect = GeneralSkillEffect.Attack, amount = 1 },
        new GeneralSkillNode { nodeId = "tempo_1", title = "Rapid Training", description = "+0.25 attacks per second for every unit.", diamondCost = 3, category = GeneralSkillCategory.Tempo, effect = GeneralSkillEffect.AttackSpeed, amount = 0.25f, requiredNodeIds = new() { "vitality_1", "offense_1" } },
        new GeneralSkillNode { nodeId = "vitality_2", title = "Fortified Core", description = "+4 health for every unit.", diamondCost = 4, category = GeneralSkillCategory.Vitality, effect = GeneralSkillEffect.Health, amount = 4, requiredNodeIds = new() { "vitality_1" } },
        new GeneralSkillNode { nodeId = "offense_2", title = "Lethal Rounds", description = "+2 attack damage for every unit.", diamondCost = 4, category = GeneralSkillCategory.Offense, effect = GeneralSkillEffect.Attack, amount = 2, requiredNodeIds = new() { "offense_1" } },
        new GeneralSkillNode { nodeId = "tempo_2", title = "Rapid Fire", description = "+0.35 attacks per second for every unit.", diamondCost = 5, category = GeneralSkillCategory.Tempo, effect = GeneralSkillEffect.AttackSpeed, amount = 0.35f, requiredNodeIds = new() { "tempo_1" } }
    };

    private GameObject panel;
    private readonly List<SkillNodeView> nodeViews = new();
    private Sprite uiSprite;
    private Sprite nodeSprite;
    private TextMeshProUGUI diamondBalanceText;

    private class SkillNodeView
    {
        public Button button;
        public Image image;
        public TextMeshProUGUI title;
        public TextMeshProUGUI cost;
        public TextMeshProUGUI symbol;
    }

    public static SkillTreeManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (panel == null)
            CreateUi();

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.ResourceChanged += HandleResourceChanged;
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.ResourceChanged -= HandleResourceChanged;
        if (Instance == this)
            Instance = null;
    }

    public void Show()
    {
        if (GamePhaseManager.Instance != null && !GamePhaseManager.Instance.IsBuildPhase)
            return;

        if (panel == null)
            CreateUi();

        panel.SetActive(true);
        RefreshUi();
    }

    public void Close()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void ApplyBonusesTo(UnitObject unit)
    {
        foreach (GeneralSkillNode node in generalNodes)
        {
            if (node.unlocked)
                unit.ApplySkillBonus(node.effect, node.amount);
        }
    }

    private void BuyNode(int index)
    {
        if (index < 0 || index >= generalNodes.Count || !CanBuy(index))
            return;

        GeneralSkillNode node = generalNodes[index];
        if (!ResourceManager.Instance.TrySpend(ResourceType.Diamond, node.diamondCost))
            return;

        node.unlocked = true;
        foreach (UnitObject unit in FindObjectsByType<UnitObject>(FindObjectsSortMode.None))
            unit.ApplySkillBonus(node.effect, node.amount);

        RefreshUi();
    }

    private bool CanBuy(int index)
    {
        if (generalNodes[index].unlocked || ResourceManager.Instance == null ||
            ResourceManager.Instance.GetAmount(ResourceType.Diamond) < generalNodes[index].diamondCost)
        {
            return false;
        }

        foreach (string requiredId in generalNodes[index].requiredNodeIds)
        {
            GeneralSkillNode requiredNode = generalNodes.Find(node => node.nodeId == requiredId);
            if (requiredNode == null || !requiredNode.unlocked)
                return false;
        }

        return true;
    }

    private void HandleResourceChanged(ResourceType type, int amount)
    {
        if (type == ResourceType.Diamond && panel != null && panel.activeSelf)
            RefreshUi();
    }

    private void CreateUi()
    {
        GameObject canvasObject = new("Skill Tree UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        panel = new GameObject("Skill Tree Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelTransform = panel.GetComponent<RectTransform>();
        panelTransform.anchorMin = new Vector2(0.5f, 0.5f);
        panelTransform.anchorMax = new Vector2(0.5f, 0.5f);
        panelTransform.pivot = new Vector2(0.5f, 0.5f);
        panelTransform.sizeDelta = new Vector2(960f, 620f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.sprite = GetUiSprite();
        panelImage.color = new Color(0.05f, 0.08f, 0.16f, 0.96f);

        CreateText(panel.transform, "Title", "GENERAL MASTERY TREE", new Vector2(0f, 255f), 34, Color.white);
        CreateText(panel.transform, "Subtitle", "Unlock permanent unit upgrades with diamonds", new Vector2(0f, 215f), 18, new Color(0.65f, 0.78f, 0.95f));
        diamondBalanceText = CreateText(panel.transform, "Diamond Balance", string.Empty, new Vector2(-365f, 255f), 22, new Color(0.35f, 0.85f, 1f));
        diamondBalanceText.alignment = TextAlignmentOptions.Left;

        foreach (GeneralSkillCategory category in Enum.GetValues(typeof(GeneralSkillCategory)))
        {
            Vector2 headerPosition = new(GetCategoryColumn(category), 165f);
            CreateText(panel.transform, $"{category} Header", category.ToString().ToUpper(), headerPosition, 19, GetCategoryColor(category));
        }

        for (int index = 0; index < generalNodes.Count; index++)
        {
            foreach (string requiredId in generalNodes[index].requiredNodeIds)
            {
                int parentIndex = generalNodes.FindIndex(node => node.nodeId == requiredId);
                if (parentIndex >= 0)
                    CreateConnector(panel.transform, GetNodePosition(parentIndex), GetNodePosition(index));
            }
        }

        for (int index = 0; index < generalNodes.Count; index++)
        {
            int capturedIndex = index;
            SkillNodeView nodeView = CreateTreeNode(panel.transform, GetNodePosition(index), generalNodes[index]);
            nodeView.button.onClick.AddListener(() => BuyNode(capturedIndex));
            nodeViews.Add(nodeView);
        }

        Button closeButton = CreateButton(panel.transform, new Vector2(395f, 255f), new Vector2(105f, 42f));
        closeButton.onClick.AddListener(Close);
        closeButton.GetComponent<Image>().color = new Color(0.5f, 0.16f, 0.16f, 1f);
        closeButton.GetComponentInChildren<TextMeshProUGUI>().text = "Close";
        panel.SetActive(false);
    }

    private void RefreshUi()
    {
        if (diamondBalanceText != null && ResourceManager.Instance != null)
            diamondBalanceText.text = $"♦ {ResourceManager.Instance.GetAmount(ResourceType.Diamond)}";

        for (int index = 0; index < generalNodes.Count; index++)
        {
            GeneralSkillNode node = generalNodes[index];
            bool canBuy = CanBuy(index);
            SkillNodeView view = nodeViews[index];
            view.button.interactable = canBuy;
            view.symbol.text = GetEffectSymbol(node.effect);
            view.title.text = node.title;
            string requirements = node.requiredNodeIds.Count == 0
                ? "Root node"
                : $"Requires: {string.Join(" + ", node.requiredNodeIds)}";
            view.cost.text = node.unlocked
                ? "UNLOCKED"
                : $"{node.diamondCost} ♦  •  {requirements}\n{node.description}";
            view.image.color = node.unlocked
                ? new Color(0.18f, 0.72f, 0.4f, 1f)
                : canBuy ? new Color(0.25f, 0.56f, 0.92f, 1f) : new Color(0.16f, 0.2f, 0.3f, 1f);
            view.cost.color = node.unlocked ? new Color(0.45f, 1f, 0.6f) : canBuy ? Color.white : new Color(0.5f, 0.55f, 0.65f);
        }
    }

    private Vector2 GetNodePosition(int index)
    {
        GeneralSkillNode node = generalNodes[index];
        int tier = 0;
        for (int previousIndex = 0; previousIndex < index; previousIndex++)
        {
            if (generalNodes[previousIndex].category == node.category)
                tier++;
        }

        return new Vector2(GetCategoryColumn(node.category), 65f - tier * 180f);
    }

    private static float GetCategoryColumn(GeneralSkillCategory category) => category switch
    {
        GeneralSkillCategory.Vitality => -300f,
        GeneralSkillCategory.Offense => 0f,
        GeneralSkillCategory.Tempo => 300f,
        _ => 0f
    };

    private static Color GetCategoryColor(GeneralSkillCategory category) => category switch
    {
        GeneralSkillCategory.Vitality => new Color(0.45f, 0.9f, 0.55f),
        GeneralSkillCategory.Offense => new Color(1f, 0.5f, 0.35f),
        GeneralSkillCategory.Tempo => new Color(0.4f, 0.75f, 1f),
        _ => Color.white
    };

    private void CreateConnector(Transform parent, Vector2 from, Vector2 to)
    {
        GameObject connectorObject = new("Skill Path", typeof(RectTransform), typeof(Image));
        connectorObject.transform.SetParent(parent, false);
        RectTransform rect = connectorObject.GetComponent<RectTransform>();
        Vector2 direction = to - from;
        rect.sizeDelta = new Vector2(direction.magnitude, 5f);
        rect.anchoredPosition = (from + to) * 0.5f;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        Image image = connectorObject.GetComponent<Image>();
        image.sprite = GetUiSprite();
        image.color = new Color(0.35f, 0.53f, 0.77f, 0.75f);
    }

    private SkillNodeView CreateTreeNode(Transform parent, Vector2 position, GeneralSkillNode node)
    {
        GameObject nodeObject = new("Skill Node", typeof(RectTransform), typeof(Image), typeof(Button));
        nodeObject.transform.SetParent(parent, false);
        RectTransform rect = nodeObject.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(92f, 92f);
        Image image = nodeObject.GetComponent<Image>();
        image.sprite = GetNodeSprite();
        Button button = nodeObject.GetComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI symbol = CreateText(nodeObject.transform, "Symbol", string.Empty, Vector2.zero, 24, Color.white);
        symbol.rectTransform.anchorMin = Vector2.zero;
        symbol.rectTransform.anchorMax = Vector2.one;
        symbol.rectTransform.offsetMin = Vector2.zero;
        symbol.rectTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI title = CreateText(nodeObject.transform, "Name", node.title, new Vector2(0f, -66f), 17, Color.white);
        title.fontStyle = FontStyles.Bold;
        TextMeshProUGUI cost = CreateText(nodeObject.transform, "Cost", string.Empty, new Vector2(0f, -99f), 13, Color.white);
        cost.enableWordWrapping = true;
        cost.rectTransform.sizeDelta = new Vector2(190f, 50f);

        return new SkillNodeView { button = button, image = image, title = title, cost = cost, symbol = symbol };
    }

    private static string GetEffectSymbol(GeneralSkillEffect effect) => effect switch
    {
        GeneralSkillEffect.Health => "HP",
        GeneralSkillEffect.Attack => "ATK",
        GeneralSkillEffect.AttackSpeed => "SPD",
        _ => "?"
    };

    private Button CreateButton(Transform parent, Vector2 position, Vector2? size = null)
    {
        GameObject buttonObject = new("Skill Node", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size ?? new Vector2(620f, 72f);
        Image image = buttonObject.GetComponent<Image>();
        image.sprite = GetUiSprite();
        image.color = new Color(0.14f, 0.23f, 0.38f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", string.Empty, Vector2.zero, 18, Color.white);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 4f);
        textRect.offsetMax = new Vector2(-14f, -4f);
        text.alignment = TextAlignmentOptions.Left;
        return button;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string value, Vector2 position, float size, Color color)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.text = value;
        RectTransform rect = text.rectTransform;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(650f, 50f);
        return text;
    }

    private Sprite GetUiSprite()
    {
        if (uiSprite != null)
            return uiSprite;

        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        uiSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return uiSprite;
    }

    private Sprite GetNodeSprite()
    {
        if (nodeSprite != null)
            return nodeSprite;

        const int size = 64;
        Texture2D texture = new(size, size);
        Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = distance <= radius ? 1f : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        nodeSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return nodeSprite;
    }
}
