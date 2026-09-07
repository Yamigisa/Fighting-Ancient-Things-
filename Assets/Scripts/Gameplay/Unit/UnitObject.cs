using UnityEngine;

[RequireComponent(typeof(Health), typeof(Attack))]
[RequireComponent(typeof(GoldProducer))]
public class UnitObject : MonoBehaviour
{
    [SerializeField] private UnitSO unitSO;
    private Attack attack;

    public UnitSO UnitData => unitSO;

    private void Awake()
    {
        Health health = GetComponent<Health>() ?? gameObject.AddComponent<Health>();
        attack = GetComponent<Attack>() ?? gameObject.AddComponent<Attack>();
        GoldProducer goldProducer = GetComponent<GoldProducer>() ?? gameObject.AddComponent<GoldProducer>();
        if (unitSO == null)
        {
            Debug.LogError($"{name} is missing its UnitSO.", this);
            enabled = false;
            return;
        }

        health.Initialize(unitSO.maxHealth);
        attack.Initialize(health, unitSO.attack, new Vector2Int(unitSO.attackAreaWidth, unitSO.attackAreaHeight),
            unitSO.maxTargets, unitSO.attacksPerSecond, unitSO.projectilePrefab, unitSO.projectileSpeed, unitSO.projectileLifetime,
            unitSO.attackType);
        goldProducer.Initialize(unitSO.goldProduced, unitSO.goldProductionInterval);
        SkillTreeManager.Instance?.ApplyBonusesTo(this);
    }

    private void OnDisable()
    {
        if (attack != null)
            attack.SetRangePreviewVisible(false);
    }

    private void OnMouseEnter()
    {
        if (attack != null)
            attack.SetRangePreviewVisible(true);
    }

    private void OnMouseExit()
    {
        if (attack != null)
            attack.SetRangePreviewVisible(false);
    }

    private void OnMouseDown()
    {
        if (GamePhaseManager.Instance != null && !GamePhaseManager.Instance.IsBuildPhase)
            return;

        if (PlacementSystem.Instance != null && !PlacementSystem.Instance.IsPlacementMode)
            PlacementSystem.Instance.StartMovingUnit(this);
    }

    public void SetAttackRangeVisible(bool isVisible)
    {
        if (attack != null)
            attack.SetRangePreviewVisible(isVisible);
    }

    public void ApplySkillBonus(GeneralSkillEffect effect, float amount)
    {
        switch (effect)
        {
            case GeneralSkillEffect.Health:
                GetComponent<Health>()?.AddHealth(Mathf.RoundToInt(amount));
                break;
            case GeneralSkillEffect.Attack:
                attack?.AddDamage(Mathf.RoundToInt(amount));
                break;
            case GeneralSkillEffect.AttackSpeed:
                attack?.AddAttackSpeed(amount);
                break;
        }
    }
}
