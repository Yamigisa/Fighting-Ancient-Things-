using UnityEngine;

[RequireComponent(typeof(Health), typeof(Attack))]
[RequireComponent(typeof(GoldProducer))]
public class UnitObject : MonoBehaviour
{
    [SerializeField] private UnitSO unitSO;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Health health;
    private Attack attack;

    public UnitSO UnitData => unitSO;

    private void Awake()
    {
        health = GetComponent<Health>();
        attack = GetComponent<Attack>();
        GoldProducer goldProducer = GetComponent<GoldProducer>();

        health.Initialize(unitSO.maxHealth);

        attack.Initialize(
            health,
            unitSO.attack,
            new Vector2Int(unitSO.attackAreaWidth, unitSO.attackAreaHeight),
            unitSO.maxTargets,
            unitSO.attacksPerSecond,
            unitSO.projectilePrefab,
            unitSO.projectileSpeed,
            unitSO.projectileLifetime,
            unitSO.attackType
        );

        goldProducer.Initialize(unitSO.goldProduced, unitSO.goldProductionInterval);

        spriteRenderer.sprite = unitSO.sprite;
    }

    private void OnEnable()
    {
        UpgradeManager.OnAcquiredNode += HandleUpgradeAcquired;
    }

    private void OnDisable()
    {
        attack.SetRangePreviewVisible(false);
        UpgradeManager.OnAcquiredNode -= HandleUpgradeAcquired;
    }

    private void OnMouseEnter()
    {
        attack.SetRangePreviewVisible(true);
    }

    private void OnMouseExit()
    {
        attack.SetRangePreviewVisible(false);
    }

    private void OnMouseDown()
    {
        if (!GamePhaseManager.Instance.IsBuildPhase)
            return;

        if (!PlacementSystem.Instance.IsPlacementMode)
            PlacementSystem.Instance.StartMovingUnit(this);
    }

    public void SetAttackRangeVisible(bool isVisible)
    {
        attack.SetRangePreviewVisible(isVisible);
    }

    private void ApplySkillBonus(GeneralSkillEffect effect, float amount)
    {
        switch (effect)
        {
            case GeneralSkillEffect.Health:
                health.AddHealth(Mathf.RoundToInt(amount));
                break;
            case GeneralSkillEffect.Attack:
                attack.AddDamage(Mathf.RoundToInt(amount));
                break;
            case GeneralSkillEffect.AttackSpeed:
                attack.AddAttackSpeed(amount);
                break;
        }
    }

    private void HandleUpgradeAcquired(UpgradeNode node)
    {
        ApplySkillBonus(node.generalSkillEffect, node.effectAmount);
    }
}
