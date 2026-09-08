using TMPro;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class EnemyDestination : MonoBehaviour
{
    public static EnemyDestination Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI healthText;

    public TextMeshProUGUI HealthText => healthText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
