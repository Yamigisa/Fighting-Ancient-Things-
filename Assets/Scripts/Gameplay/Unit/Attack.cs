using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class Attack : MonoBehaviour
{
    private Health owner;
    private int damage;
    private Vector2Int areaSize;
    private int maxTargets;
    private float attacksPerSecond;
    private Projectile projectilePrefab;
    private float projectileSpeed;
    private float projectileLifetime;
    private AttackType attackType;
    private float nextAttackTime;
    private readonly List<Health> contactTargets = new();
    private Health priorityTarget;
    private Vector2 facingDirectionOverride;
    private bool hasFacingOverride;

    public bool HasAttackArea => damage > 0 && areaSize.x > 0 && areaSize.y > 0;

    public void Initialize(Health newOwner, int newDamage, Vector2Int newAreaSize,
        int newMaxTargets, float newAttacksPerSecond, Projectile newProjectilePrefab,
        float newProjectileSpeed, float newProjectileLifetime, AttackType newAttackType)
    {
        owner = newOwner;
        damage = newDamage;
        areaSize = new Vector2Int(Mathf.Max(1, newAreaSize.x), Mathf.Max(1, newAreaSize.y));
        maxTargets = Mathf.Max(1, newMaxTargets);
        attacksPerSecond = newAttacksPerSecond;
        projectilePrefab = newProjectilePrefab;
        projectileSpeed = newProjectileSpeed;
        projectileLifetime = newProjectileLifetime;
        attackType = newAttackType;
        nextAttackTime = attacksPerSecond > 0f ? Time.time + 1f / attacksPerSecond : Time.time;
    }

    private void Update() => TryAttack();

    public void SetRangePreviewVisible(bool isVisible)
    {
        if (GridManager.Instance == null)
            return;

        if (!isVisible || !HasAttackArea)
        {
            GridManager.Instance.ClearAttackRangePreview();
            return;
        }

        GridManager.Instance.SetAttackRangePreview(
            WorldToGridCell(transform.position),
            areaSize,
            GetAttackDirection());
    }

    public void SetPriorityTarget(Health target)
    {
        priorityTarget = target;
    }

    /// <summary>
    /// Overrides the facing direction used for attack-range calculations.
    /// Pass Vector2.zero to clear the override and revert to transform.up (used by player units).
    /// </summary>
    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.0001f)
        {
            facingDirectionOverride = direction.normalized;
            hasFacingOverride = true;
        }
        else
        {
            hasFacingOverride = false;
        }
    }

    public void AddDamage(int amount)
    {
        damage = Mathf.Max(0, damage + amount);
    }

    public void AddAttackSpeed(float amount)
    {
        attacksPerSecond = Mathf.Max(0f, attacksPerSecond + amount);
    }

    private void TryAttack()
    {
        if (owner == null || owner.IsDead || !HasAttackArea || attacksPerSecond <= 0f || Time.time < nextAttackTime)
            return;

        List<Health> targets = attackType == AttackType.Melee
            ? FindContactTargets()
            : FindTargetsInRange();
        if (targets.Count == 0)
            return;

        foreach (Health target in targets)
        {
            if (attackType == AttackType.Melee)
                target.TakeDamage(damage);
            else
                LaunchProjectile(target);
        }

        nextAttackTime = Time.time + 1f / attacksPerSecond;
    }

    private List<Health> FindTargetsInRange()
    {
        if (priorityTarget != null && !priorityTarget.IsDead && IsValidTarget(priorityTarget) &&
            IsTargetInGridRange(priorityTarget.transform.position))
            return new List<Health> { priorityTarget };

        List<Health> targets = new();
        foreach (Health possibleTarget in FindObjectsByType<Health>(FindObjectsSortMode.None))
        {
            if (possibleTarget == owner || possibleTarget.IsDead || !IsValidTarget(possibleTarget) ||
                !IsTargetInGridRange(possibleTarget.transform.position))
                continue;

            targets.Add(possibleTarget);
        }

        targets.Sort((first, second) =>
            ((Vector2)(first.transform.position - transform.position)).sqrMagnitude.CompareTo(
            ((Vector2)(second.transform.position - transform.position)).sqrMagnitude));

        if (targets.Count > maxTargets)
            targets.RemoveRange(maxTargets, targets.Count - maxTargets);

        return targets;
    }

    private List<Health> FindContactTargets()
    {
        if (priorityTarget != null && !priorityTarget.IsDead && IsValidTarget(priorityTarget) &&
            IsTargetInGridRange(priorityTarget.transform.position))
            return new List<Health> { priorityTarget };

        contactTargets.RemoveAll(target => target == null || target.IsDead || !IsValidTarget(target) ||
            !IsTargetInGridRange(target.transform.position));
        List<Health> targets = new(contactTargets);
        targets.Sort((first, second) =>
            ((Vector2)(first.transform.position - transform.position)).sqrMagnitude.CompareTo(
            ((Vector2)(second.transform.position - transform.position)).sqrMagnitude));

        if (targets.Count > maxTargets)
            targets.RemoveRange(maxTargets, targets.Count - maxTargets);

        return targets;
    }

    private void OnCollisionEnter2D(Collision2D collision) => RegisterContact(collision.collider);
    private void OnCollisionExit2D(Collision2D collision) => UnregisterContact(collision.collider);

    private void RegisterContact(Collider2D other)
    {
        if (attackType != AttackType.Melee)
            return;

        Health target = other.GetComponentInParent<Health>();
        if (target != null && target != owner && IsValidTarget(target) && !contactTargets.Contains(target))
            contactTargets.Add(target);
    }

    private void UnregisterContact(Collider2D other)
    {
        Health target = other.GetComponentInParent<Health>();
        if (target != null)
            contactTargets.Remove(target);
    }

    private void LaunchProjectile(Health target)
    {
        Projectile projectile = projectilePrefab != null
            ? Instantiate(projectilePrefab, transform.position, Quaternion.identity)
            : CreateFallbackProjectile();

        projectile.Initialize(target, damage, projectileSpeed, projectileLifetime);
    }

    private Projectile CreateFallbackProjectile()
    {
        GameObject projectileObject = new GameObject($"{name} Projectile");
        projectileObject.transform.position = transform.position;
        projectileObject.transform.localScale = Vector3.one * 0.25f;

        SpriteRenderer projectileRenderer = projectileObject.AddComponent<SpriteRenderer>();
        SpriteRenderer ownerRenderer = GetComponentInChildren<SpriteRenderer>();
        if (ownerRenderer != null)
        {
            projectileRenderer.sprite = ownerRenderer.sprite;
            projectileRenderer.sharedMaterial = ownerRenderer.sharedMaterial;
            projectileRenderer.sortingLayerID = ownerRenderer.sortingLayerID;
            projectileRenderer.sortingOrder = ownerRenderer.sortingOrder + 10;
        }

        projectileRenderer.color = Color.yellow;
        return projectileObject.AddComponent<Projectile>();
    }

    private bool IsTargetInGridRange(Vector3 targetPosition)
    {
        Vector2Int sourceCell = WorldToGridCell(transform.position);
        Vector2Int targetCell = WorldToGridCell(targetPosition);
        Vector2Int offset = targetCell - sourceCell;
        Vector2Int forwardDirection = GetAttackDirection();
        Vector2Int sidewaysDirection = new(-forwardDirection.y, forwardDirection.x);

        int forwardDistance = offset.x * forwardDirection.x + offset.y * forwardDirection.y;
        int sidewaysDistance = offset.x * sidewaysDirection.x + offset.y * sidewaysDirection.y;
        int minimumWidth = -(areaSize.x / 2);

        return forwardDistance >= 1 && forwardDistance <= areaSize.y &&
            sidewaysDistance >= minimumWidth && sidewaysDistance < minimumWidth + areaSize.x;
    }

    private bool IsValidTarget(Health target)
    {
        return GetComponent<EnemyObject>() != null
            ? target.GetComponent<UnitObject>() != null
            : target.GetComponent<EnemyObject>() != null;
    }

    private Vector2Int GetAttackDirection()
    {
        Vector2 direction = hasFacingOverride ? facingDirectionOverride : (Vector2)transform.up;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return new Vector2Int(direction.x > 0f ? 1 : -1, 0);

        return new Vector2Int(0, direction.y > 0f ? 1 : -1);
    }

    private static Vector2Int WorldToGridCell(Vector3 worldPosition) =>
        new(Mathf.FloorToInt(worldPosition.x + 0.5f), Mathf.FloorToInt(worldPosition.y + 0.5f));
}

public enum AttackType
{
    Projectile,
    Melee
}
