using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "ScriptableObjects/Unit")]
public class UnitSO : ScriptableObject
{
    [Header("Identity")]
    public string unitName;
    public Sprite sprite;

    [Header("Defense")]
    public int maxHealth;

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

    [Tooltip("How many enemies this unit attacks during one attack cycle.")]
    [Min(1)]
    public int maxTargets = 1;

    [Tooltip("Number of attacks this unit can perform per second.")]
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

    [Header("Economy")]
    [Tooltip("Gold produced each time this unit's production timer completes.")]
    [Min(0)]
    public int goldProduced;

    [Tooltip("Seconds between Gold production. Units with no Gold production ignore this value.")]
    [Min(0.01f)]
    public float goldProductionInterval;

    [Header("Cost")]
    public int cost;

}
