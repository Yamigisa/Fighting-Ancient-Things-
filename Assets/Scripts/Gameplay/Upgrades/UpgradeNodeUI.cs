using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeNodeUI : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private TextMeshProUGUI nodeName;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private Image nodeIcon;

    [Header("Cost")]
    [SerializeField] private TextMeshProUGUI diamondCostText;

    [Header("Button")]
    [SerializeField] private Button acquireButton;
    private UpgradeNode upgradeNode;

    private void Awake()
    {
        if (acquireButton == null)
            acquireButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.ResourceChanged += HandleResourceChanged;
    }

    private void OnDisable()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.ResourceChanged -= HandleResourceChanged;
    }

    public void Initialize(UpgradeNode node)
    {
        if (node == null)
            return;

        upgradeNode = node;

        nodeName.text = node.nodeName;
        description.text = node.description;
        nodeIcon.sprite = node.icon;
        diamondCostText.text = $"Cost: {node.diamondCost} Diamonds";

        acquireButton.onClick.RemoveListener(BuyUpgrade);
        acquireButton.onClick.AddListener(BuyUpgrade);

        RefreshPurchaseState();
    }

    private void BuyUpgrade()
    {
        bool wasPurchased = UpgradeManager.Instance.TryAcquireNode(upgradeNode);

        if (!wasPurchased)
            return;

        acquireButton.interactable = false;
        Destroy(gameObject);
    }

    private void HandleResourceChanged(ResourceType type, int amount)
    {
        if (type == ResourceType.Diamond)
            RefreshPurchaseState();
    }

    private void RefreshPurchaseState()
    {
        if (acquireButton == null)
            return;

        acquireButton.interactable = UpgradeManager.Instance != null && UpgradeManager.Instance.CanAcquireNode(upgradeNode);
    }
}
