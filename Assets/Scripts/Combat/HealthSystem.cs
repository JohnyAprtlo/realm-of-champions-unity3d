using System;
using UnityEngine;

namespace MobaGame.Combat
{
    public class HealthSystem : MonoBehaviour
    {
        [Header("Health Settings")]
        public float maxHealth = 1000f;
        public float currentHealth;
        public float currentShield = 0f;

        [Header("Status")]
        public bool isDead = false;

        public event Action<float, float> OnHealthChanged; // (current, max)
        public event Action<float, bool> OnDamaged;         // (amount, isCritical)
        public event Action OnDeath;

        private Renderer[] renderers;
        private Color[] originalColors;

        private void Awake()
        {
            currentHealth = maxHealth;
            renderers = GetComponentsInChildren<Renderer>();
        }

        public void TakeDamage(float damage, GameObject attacker = null)
        {
            if (isDead || damage <= 0) return;

            bool isCritical = damage > 250f;

            // Shield absorption
            if (currentShield > 0)
            {
                if (currentShield >= damage)
                {
                    currentShield -= damage;
                    damage = 0;
                }
                else
                {
                    damage -= currentShield;
                    currentShield = 0;
                }
            }

            if (damage > 0)
            {
                currentHealth -= damage;
                OnDamaged?.Invoke(damage, isCritical);
                StartCoroutine(HitFlashEffect());
            }

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                isDead = true;
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                OnDeath?.Invoke();
            }
            else
            {
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }

        public void AddShield(float shieldAmount)
        {
            currentShield += shieldAmount;
        }

        public void Heal(float amount)
        {
            if (isDead) return;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Restores an entity after death with an optionally scaled max health.
        /// </summary>
        public void Revive(float newMaxHealth = -1f)
        {
            if (newMaxHealth > 0f)
            {
                maxHealth = newMaxHealth;
            }
            isDead = false;
            currentShield = 0f;
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private System.Collections.IEnumerator HitFlashEffect()
        {
            if (renderers == null || renderers.Length == 0) yield break;

            foreach (var r in renderers)
            {
                if (r != null && r.material != null)
                {
                    if (r.material.HasProperty("_Color")) r.material.color = Color.white;
                    if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", Color.white);
                }
            }

            yield return new WaitForSeconds(0.12f);

            // Revert color will be handled or re-applied
        }
    }
}
