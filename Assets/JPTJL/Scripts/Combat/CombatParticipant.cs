using System;
using UnityEngine;
using JPTJL.Data;

namespace JPTJL.Combat
{
    /// <summary>
    /// Pure combatant entity managing runtime stats, health, and combat status.
    /// Free of visual references.
    /// </summary>
    [Serializable]
    public class CombatParticipant
    {
        [SerializeField] private CharacterStatsData statsData;
        [SerializeField] private int currentHealth;

        public CharacterStatsData StatsData => statsData;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => statsData != null ? statsData.MaxHealth : 100;
        public bool IsAlive => currentHealth > 0;
        public bool IsPlayer => statsData != null && statsData.IsPlayer;
        public string Name => statsData != null ? statsData.CharacterName : "Unknown";

        public CombatParticipant(CharacterStatsData stats)
        {
            Initialize(stats);
        }

        public void Initialize(CharacterStatsData stats)
        {
            statsData = stats;
            currentHealth = stats != null ? stats.MaxHealth : 100;
            CombatEvents.TriggerHealthChanged(this, currentHealth, MaxHealth);
        }

        public void TakeDamage(int damage)
        {
            if (!IsAlive) return;

            int actualDamage = Mathf.Max(0, damage);
            currentHealth = Mathf.Max(0, currentHealth - actualDamage);

            CombatEvents.TriggerHealthChanged(this, currentHealth, MaxHealth);

            if (currentHealth <= 0)
            {
                CombatEvents.TriggerParticipantDefeated(this);
            }
        }

        public void Heal(int amount)
        {
            if (!IsAlive) return;

            int actualHeal = Mathf.Max(0, amount);
            currentHealth = Mathf.Min(MaxHealth, currentHealth + actualHeal);

            CombatEvents.TriggerHealthChanged(this, currentHealth, MaxHealth);
        }
    }
}
