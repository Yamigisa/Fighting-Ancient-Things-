using TMPro;
using UnityEngine;

[RequireComponent(typeof(Health), typeof(CircleCollider2D))]
public class EnemyDestination : MonoBehaviour
{
    public static EnemyDestination Instance { get; private set; }

    private Health health;
    private int maxHealth;
    private TextMeshProUGUI healthText;

    public int CurrentHealth => health != null ? health.CurrentHealth : 0;

    public void Initialize(int maxHealth)
    {
        this.maxHealth = Mathf.Max(1, maxHealth);
        health = GetComponent<Health>();
        health.Initialize(this.maxHealth);

        CircleCollider2D trigger = GetComponent<CircleCollider2D>();
        trigger.isTrigger = true;

        CreateHealthUi();
        RefreshHealthUi();
    }

    public void ReceiveEnemy(int damage)
    {
        if (health != null && !health.IsDead)
        {
            health.TakeDamage(damage);
            RefreshHealthUi();
        }
    }

    private void CreateHealthUi()
    {
        GameObject canvasObject = new("Destination Health Canvas", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        GameObject textObject = new("Destination Health", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvas.transform, false);
        RectTransform textTransform = textObject.GetComponent<RectTransform>();
        textTransform.anchorMin = new Vector2(0.5f, 1f);
        textTransform.anchorMax = new Vector2(0.5f, 1f);
        textTransform.pivot = new Vector2(0.5f, 1f);
        textTransform.anchoredPosition = new Vector2(0f, -24f);
        textTransform.sizeDelta = new Vector2(260f, 50f);

        healthText = textObject.GetComponent<TextMeshProUGUI>();
        healthText.font = TMP_Settings.defaultFontAsset;
        healthText.fontSize = 30f;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.color = Color.white;
    }

    private void RefreshHealthUi()
    {
        if (healthText != null)
            healthText.text = $"Health {CurrentHealth}/{maxHealth}";
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
