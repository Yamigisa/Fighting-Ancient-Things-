using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    [Header("Buyable Units")]
    [SerializeField] private List<UnitSO> availableUnits = new List<UnitSO>();
    [SerializeField] private GameObject unitPrefab;

    [Header("Unit Shop")]
    [SerializeField] private UnitUICard unitUICard;
    [SerializeField] private Transform unitUIShopParent;

    private void Start()
    {
        InitializeUnitUICards();
    }

    private void InitializeUnitUICards()
    {
        foreach (UnitSO unit in availableUnits)
        {
            UnitUICard card = Instantiate(unitUICard, unitUIShopParent.transform);
            card.gameObject.SetActive(true);
            card.SetUnit(unit, unitPrefab);
        }
    }
}
