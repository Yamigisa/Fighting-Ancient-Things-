using UnityEngine;

public class Health : MonoBehaviour
{
    private int currentHealth;
    private bool isDead;

    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    public void Initialize(int maxHealth)
    {
        currentHealth = Mathf.Max(1, maxHealth);
        isDead = false;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            isDead = true;
            Destroy(gameObject);
        }
    }

    public void AddHealth(int amount)
    {
        if (!isDead && amount > 0)
            currentHealth += amount;
    }
}
