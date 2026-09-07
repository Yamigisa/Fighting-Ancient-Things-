using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy", menuName = "ScriptableObjects/Enemy")]
public class EnemySO : ScriptableObject
{
    [Header("Identity")]
    public string unitName;
    public Sprite sprite;
    public GameObject prefab;

    [Header("Defense")]
    public int maxHealth;

    [Header("Movement")]
    public int moveSpeed;

    [Header("Destination")]
    [Tooltip("Damage dealt to the destination when this enemy reaches it. The enemy is then removed.")]
    [Min(1)]
    public int destinationDamage = 1;

    [Header("Rewards")]
    [Min(0)] [Tooltip("Gold awarded when this enemy is removed.")]
    public int goldReward = 1;
    [Min(0)] [Tooltip("Diamonds awarded when this enemy is removed.")]
    public int diamondReward;

    [Header("Attack")]
    public int attack;

    [Tooltip("Projectile attacks can damage targets at tile range. Melee attacks only damage opposing colliders in contact.")]
    public AttackType attackType = AttackType.Projectile;
    [Tooltip("Width of the tile-based attack area. Use an odd value so the unit sits at the centre of the area.")]
    [Min(1)]
    public int attackAreaWidth = 5;

    [Tooltip("Height of the tile-based attack area. Use an odd value so the unit sits at the centre of the area.")]
    [Min(1)]
    public int attackAreaHeight = 5;

    [Tooltip("How many defenders this enemy attacks during one attack cycle.")]
    [Min(1)]
    public int maxTargets = 1;

    public float attacksPerSecond;

    [Header("Projectile")]
    [Tooltip("Optional visual prefab for this unit's projectile. A small fallback sprite is used when empty.")]
    public Projectile projectilePrefab;

    [Min(0.01f)]
    [Tooltip("World units the projectile travels per second.")]
    public float projectileSpeed = 8f;

    [Min(0.1f)]
    [Tooltip("Safety lifetime in seconds before an unhit projectile is destroyed.")]
    public float projectileLifetime = 3f;

}
