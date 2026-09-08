using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Upgrade Node", menuName = "ScriptableObjects/Upgrade Node")]
public class UpgradeNode : ScriptableObject
{
    [Header("Identity")]
    public string nodeName;
    public string description;
    public Sprite icon;
    [SerializeField] private string nodeId;
    public string NodeId => nodeId;

    [Header("Category and Effect")]
    public GeneralSkillEffect generalSkillEffect;
    public float effectAmount;

    [Header("Costs and Requirements")]
    public int diamondCost;
    public List<UpgradeNode> requiredNodes = new();
}

public enum GeneralSkillEffect { Health, Attack, AttackSpeed }
