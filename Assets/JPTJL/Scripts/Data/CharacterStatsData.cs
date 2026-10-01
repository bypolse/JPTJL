using UnityEngine;

namespace JPTJL.Data
{
    /// <summary>
    /// Base ScriptableObject defining character attributes and stats for combat calculations.
    /// Designed for consumption by pure combat logic and extension by UI/Data team.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterStats", menuName = "JPTJL/Combat/Character Stats Data")]
    public class CharacterStatsData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string characterName = "Character";
        [SerializeField] private bool isPlayer = true;

        [Header("Core Attributes")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int maxMana = 50;
        [SerializeField] private int baseAttack = 15;
        [SerializeField] private int baseDefense = 5;
        [SerializeField] private float speed = 10f;
        [SerializeField] private float criticalChance = 0.05f;
        [SerializeField] private float criticalMultiplier = 1.5f;

        public string CharacterName => characterName;
        public bool IsPlayer => isPlayer;
        public int MaxHealth => maxHealth;
        public int MaxMana => maxMana;
        public int BaseAttack => baseAttack;
        public int BaseDefense => baseDefense;
        public float Speed => speed;
        public float CriticalChance => criticalChance;
        public float CriticalMultiplier => criticalMultiplier;
    }
}
