using UnityEngine;

[RequireComponent(typeof(Health), typeof(Attack))]
[RequireComponent(typeof(Movement))]
public class EnemyObject : MonoBehaviour
{
    public event System.Action<EnemyObject> Destroyed;
    [SerializeField] private EnemySO enemySO;
    [SerializeField] private Vector2 initialMoveDirection = Vector2.left;

    private Health blockingUnit;
    private Attack attack;
    private Movement movement;
    private int destinationDamage;
    private bool hasCustomPath;
    private int goldReward;
    private int diamondReward;
    private bool rewardsDropped;

    private void Awake()
    {
        Collider2D bodyCollider = GetComponent<Collider2D>() ?? gameObject.AddComponent<CircleCollider2D>();
        bodyCollider.isTrigger = false;

        Health health = GetComponent<Health>() ?? gameObject.AddComponent<Health>();
        attack = GetComponent<Attack>() ?? gameObject.AddComponent<Attack>();
        movement = GetComponent<Movement>() ?? gameObject.AddComponent<Movement>();

        if (enemySO == null)
        {
            Debug.LogError($"{name} is missing its EnemySO.", this);
            enabled = false;
            return;
        }

        health.Initialize(enemySO.maxHealth);
        attack.Initialize(health, enemySO.attack, new Vector2Int(enemySO.attackAreaWidth, enemySO.attackAreaHeight),
            enemySO.maxTargets, enemySO.attacksPerSecond, enemySO.projectilePrefab, enemySO.projectileSpeed, enemySO.projectileLifetime,
            enemySO.attackType);
        movement.Initialize(enemySO.moveSpeed, initialMoveDirection);
        destinationDamage = Mathf.Max(1, enemySO.destinationDamage);
        goldReward = Mathf.Max(0, enemySO.goldReward);
        diamondReward = Mathf.Max(0, enemySO.diamondReward);
    }

    private void Update()
    {
        if (!hasCustomPath && EnemyDestination.Instance != null)
            movement.SetDestination(EnemyDestination.Instance.transform);

        if (HasReachedDestinationTile())
        {
            EnemyDestination.Instance.ReceiveEnemy(destinationDamage);
            Destroy(gameObject);
            return;
        }

        if (blockingUnit != null && !blockingUnit.IsDead)
            return;

        blockingUnit = null;
        attack.SetPriorityTarget(null);
        movement.SetBlocked(false);
    }

    public void ConfigurePath(GridEdge spawnEdge, EnemyPathMode pathMode)
    {
        hasCustomPath = pathMode == EnemyPathMode.StraightThenTurn;
        if (!hasCustomPath || movement == null || EnemyDestination.Instance == null)
            return;

        Vector2 destinationPosition = EnemyDestination.Instance.transform.position;
        Vector2 firstCorner = spawnEdge is GridEdge.Bottom or GridEdge.Top
            ? new Vector2(transform.position.x, destinationPosition.y)
            : new Vector2(destinationPosition.x, transform.position.y);

        movement.SetWaypoints(firstCorner, destinationPosition);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (blockingUnit != null)
            return;

        Health unit = collision.collider.GetComponentInParent<Health>();
        if (unit == null || unit.GetComponent<UnitObject>() == null)
            return;

        blockingUnit = unit;
        attack.SetPriorityTarget(unit);
        movement.SetBlocked(true);
    }

    private bool HasReachedDestinationTile()
    {
        if (EnemyDestination.Instance == null || GridManager.Instance == null)
            return false;

        Tile enemyTile = GridManager.Instance.GetTileAtWorldPosition(transform.position);
        Tile destinationTile = GridManager.Instance.GetTileAtWorldPosition(EnemyDestination.Instance.transform.position);

        return enemyTile != null && enemyTile == destinationTile;
    }

    private void OnDestroy()
    {
        DropRewards();
        Destroyed?.Invoke(this);
    }

    private void DropRewards()
    {
        if (rewardsDropped)
            return;

        rewardsDropped = true;
        if (ResourceManager.Instance == null)
            return;

        if (goldReward > 0)
            ResourceManager.Instance.CollectFromWorld(ResourceType.Gold, goldReward, transform.position);
        if (diamondReward > 0)
            ResourceManager.Instance.CollectFromWorld(ResourceType.Diamond, diamondReward, transform.position);
    }
}
