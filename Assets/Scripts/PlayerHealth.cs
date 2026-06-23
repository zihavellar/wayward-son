using System.Collections.Generic;
using UnityEngine;
using WaywardSon.SaveSystem;

namespace WaywardSon
{
    public class PlayerHealth : MonoBehaviour, ISaveable
    {
        public enum HealthState { Fine, Caution, Danger }

        [Header("Health Settings")]
        public int maxHealth = 100;
        public int currentHealth;

        // ─── ISaveable ──────────────────────────────────────────
        public string SaveID => "Player";

        public void CollectData(Dictionary<string, object> data)
        {
            data["health"] = currentHealth;
            data["maxHealth"] = maxHealth;
        }

        public void ApplyData(Dictionary<string, object> data)
        {
            if (data.TryGetValue("health", out var h))
                currentHealth = System.Convert.ToInt32(h);
            if (data.TryGetValue("maxHealth", out var mh))
                maxHealth = System.Convert.ToInt32(mh);
        }

        private void Start()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(int damage)
        {
            currentHealth -= damage;
            if (currentHealth < 0) currentHealth = 0;
            
            Debug.Log($"Player took {damage} damage! Current HP: {currentHealth}/{maxHealth} ({CurrentState})");
            
            if (currentHealth <= 0)
            {
                Debug.Log("Player Defeated! Respawning (Resetting Health)...");
                currentHealth = maxHealth;
            }
        }

        public void Heal(int healAmount)
        {
            currentHealth += healAmount;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            Debug.Log($"Player healed {healAmount}! Current HP: {currentHealth}/{maxHealth} ({CurrentState})");
        }

        public HealthState CurrentState
        {
            get
            {
                float percentage = (float)currentHealth / maxHealth;
                if (percentage > 0.60f) return HealthState.Fine;
                if (percentage >= 0.30f) return HealthState.Caution;
                return HealthState.Danger;
            }
        }

        public float SpeedMultiplier
        {
            get
            {
                switch (CurrentState)
                {
                    case HealthState.Fine:
                        return 1.0f;
                    case HealthState.Caution:
                        return 0.65f;
                    case HealthState.Danger:
                        return 0.35f;
                    default:
                        return 1.0f;
                }
            }
        }
    }
}
