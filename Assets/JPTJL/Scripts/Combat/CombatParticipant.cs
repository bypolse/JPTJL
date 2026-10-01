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
        [SerializeField] private int currentMana;
        [SerializeField] private int currentShield;

        public CharacterStatsData StatsData => statsData;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => statsData != null ? statsData.MaxHealth : 100;
        public int CurrentMana => currentMana;
        public int MaxMana => statsData != null ? statsData.MaxMana : 50;
        public int CurrentShield => currentShield;
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
            currentMana = stats != null ? stats.MaxMana : 50;
            currentShield = 0;

            CombatEvents.TriggerHealthChanged(this, currentHealth, MaxHealth);
            CombatEvents.TriggerManaChanged(this, currentMana, MaxMana);
            CombatEvents.TriggerShieldChanged(this, currentShield);
        }

        public bool HasMana(int amount)
        {
            return currentMana >= amount;
        }

        public bool ConsumeMana(int amount)
        {
            if (amount <= 0) return true;
            if (!HasMana(amount)) return false;

            currentMana = Mathf.Max(0, currentMana - amount);
            CombatEvents.TriggerManaChanged(this, currentMana, MaxMana);
            return true;
        }

        public void RestoreMana(int amount)
        {
            if (amount <= 0) return;
            currentMana = Mathf.Min(MaxMana, currentMana + amount);
            CombatEvents.TriggerManaChanged(this, currentMana, MaxMana);
        }

        public void AddShield(int amount)
        {
            if (amount <= 0) return;
            currentShield += amount;
            CombatEvents.TriggerShieldChanged(this, currentShield);
        }

        public void ClearShield()
        {
            if (currentShield <= 0) return;
            currentShield = 0;
            CombatEvents.TriggerShieldChanged(this, currentShield);
        }

        public void TakeDamage(int damage)
        {
            if (!IsAlive) return;

            int actualDamage = Mathf.Max(0, damage);
            int damageToHp = actualDamage;

            // Shield absorbs incoming damage first before reducing HP
            if (currentShield > 0)
            {
                if (currentShield >= damageToHp)
                {
                    currentShield -= damageToHp;
                    damageToHp = 0;
                }
                else
                {
                    damageToHp -= currentShield;
                    currentShield = 0;
                }
                CombatEvents.TriggerShieldChanged(this, currentShield);
            }

            if (damageToHp > 0)
            {
                currentHealth = Mathf.Max(0, currentHealth - damageToHp);
                CombatEvents.TriggerHealthChanged(this, currentHealth, MaxHealth);
            }

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
