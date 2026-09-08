using System.Collections;
using UnityEngine;

public class GoldProducer : MonoBehaviour
{
    private int goldPerInterval;
    private float interval;
    private Coroutine productionCoroutine;

    public void Initialize(int newGoldPerInterval, float newInterval)
    {
        goldPerInterval = newGoldPerInterval;
        interval = newInterval;
    }

    private void OnEnable()
    {
        GamePhaseManager.PhaseChanged += HandlePhaseChanged;

        SetProductionActive(GamePhaseManager.Instance != null && !GamePhaseManager.Instance.IsBuildPhase);
    }

    private void OnDisable()
    {
        GamePhaseManager.PhaseChanged -= HandlePhaseChanged;

        SetProductionActive(false);
    }

    private void HandlePhaseChanged(GamePhase phase) => SetProductionActive(phase == GamePhase.Combat);

    private void SetProductionActive(bool isActive)
    {
        if (isActive && productionCoroutine == null && goldPerInterval > 0 && interval > 0f)
            productionCoroutine = StartCoroutine(ProduceGold());
        else if (!isActive && productionCoroutine != null)
        {
            StopCoroutine(productionCoroutine);
            productionCoroutine = null;
        }
    }

    private IEnumerator ProduceGold()
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            if (ResourceManager.Instance != null)
                ResourceManager.Instance.Add(ResourceType.Gold, goldPerInterval);
        }
    }
}
