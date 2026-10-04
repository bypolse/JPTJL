using UnityEngine;
using JPTJL.Combat.States;
using JPTJL.Data;
using JPTJL.FSM;
using JPTJL.Timing;

namespace JPTJL.Combat
{
    /// <summary>
    /// Central coordinator for Turn-Based Combat Gameplay Core.
    /// Strictly decoupled from UI and visuals. Manages the FSM, Timing Engine, and participants.
    /// </summary>
    public class CombatController : MonoBehaviour
    {
        [Header("Initial Data Config (Optional Editor Hook)")]
        [SerializeField] private CharacterStatsData defaultPlayerStats;
        [SerializeField] private CharacterStatsData defaultEnemyStats;
        [SerializeField] private SkillData defaultPlayerSkill;
        [SerializeField] private SkillData defaultEnemySkill;
        [SerializeField] private SkillData[] playerSkills;
        [SerializeField] private bool autoStartBattle = true;

        public SkillData[] PlayerSkills => playerSkills;

        // Core Components
        public StateMachine StateMachine { get; private set; }
        public ITimingEngine TimingEngine { get; private set; }
        public KeyQTEEngine KeyQTEEngine { get; private set; }

        // Combatants
        public CombatParticipant Player { get; private set; }
        public CombatParticipant Enemy { get; private set; }

        // Turn & Action Runtime Context
        public bool IsPlayerTurn { get; internal set; }
        public SkillData CurrentSkill { get; internal set; }
        public SkillData EnemyDefaultSkill { get; internal set; }
        public TimingResult OffensiveTimingResult { get; internal set; }
        public System.Collections.Generic.List<QTEHitResult> LastQTEResults { get; internal set; } = new System.Collections.Generic.List<QTEHitResult>();
        public DefenseType ActiveDefenseType { get; internal set; }
        public TimingResult DefensiveTimingResult { get; internal set; }
        public TimingWindowConfig ActiveDefenseConfig { get; internal set; }
        public bool BattleWon { get; internal set; }

        /// <summary>
        /// Direct mapping to the high-level 4 turn states required by the game design.
        /// </summary>
        public CombatTurnState CurrentTurnState
        {
            get
            {
                if (StateMachine?.CurrentState is BattleEndState)
                    return CombatTurnState.FinCombate;
                if (StateMachine?.CurrentState is QTEExecutionState)
                    return CombatTurnState.EjecutandoQTE;
                if (StateMachine?.CurrentState is EnemyTurnState || StateMachine?.CurrentState is DefenseWindowState)
                    return CombatTurnState.TurnoEnemigo;
                return CombatTurnState.TurnoJugador;
            }
        }

        // FSM States
        public BattleStartState BattleStartState { get; private set; }
        public PlayerTurnState PlayerTurnState { get; private set; }
        public ActionSelectionState ActionSelectionState { get; private set; }
        public QTEExecutionState QTEExecutionState { get; private set; }
        public EnemyTurnState EnemyTurnState { get; private set; }
        public DefenseWindowState DefenseWindowState { get; private set; }
        public DamageResolutionState DamageResolutionState { get; private set; }
        public BattleEndState BattleEndState { get; private set; }

        private void Awake()
        {
            InitializeFSM();
        }

        private void Start()
        {
            if (autoStartBattle && defaultPlayerStats != null && defaultEnemyStats != null)
            {
                StartCombat(defaultPlayerStats, defaultEnemyStats, defaultPlayerSkill, defaultEnemySkill);
            }
        }

        private void Update()
        {
            StateMachine?.Update(Time.deltaTime);
        }

        private void InitializeFSM()
        {
            StateMachine = new StateMachine();
            TimingEngine = new TimingEngine();
            KeyQTEEngine = new KeyQTEEngine();

            BattleStartState = new BattleStartState(this);
            PlayerTurnState = new PlayerTurnState(this);
            ActionSelectionState = new ActionSelectionState(this);
            QTEExecutionState = new QTEExecutionState(this);
            EnemyTurnState = new EnemyTurnState(this);
            DefenseWindowState = new DefenseWindowState(this);
            DamageResolutionState = new DamageResolutionState(this);
            BattleEndState = new BattleEndState(this);
        }

        /// <summary>
        /// Starts a combat session with the provided combatant profiles and baseline skills.
        /// </summary>
        public void StartCombat(
            CharacterStatsData playerData,
            CharacterStatsData enemyData,
            SkillData playerSkill = null,
            SkillData enemySkill = null)
        {
            Player = new CombatParticipant(playerData);
            Enemy = new CombatParticipant(enemyData);

            CurrentSkill = playerSkill ?? defaultPlayerSkill;
            EnemyDefaultSkill = enemySkill ?? defaultEnemySkill;

            StateMachine.Initialize(BattleStartState);
        }

        /// <summary>
        /// Called by input / player controller when a skill is picked during ActionSelection.
        /// </summary>
        public void SelectSkill(SkillData skill)
        {
            if (StateMachine.CurrentState is ActionSelectionState actionState)
            {
                actionState.OnSkillChosen(skill);
            }
        }

        /// <summary>
        /// Consumes a health potion to restore HP to the player and passes turn to the enemy.
        /// </summary>
        public void UseHealthPotion(int amount = 35)
        {
            if (Player == null) return;
            if (StateMachine.CurrentState is ActionSelectionState)
            {
                Player.Heal(amount);
                CombatEvents.TriggerFloatingFeedback($"+{amount} SALUD", Color.green);
                StateMachine.ChangeState(EnemyTurnState);
            }
        }

        /// <summary>
        /// Consumes a mana potion to restore MP to the player and passes turn to the enemy.
        /// </summary>
        public void UseManaPotion(int amount = 25)
        {
            if (Player == null) return;
            if (StateMachine.CurrentState is ActionSelectionState)
            {
                Player.RestoreMana(amount);
                CombatEvents.TriggerFloatingFeedback($"+{amount} MANÁ", Color.cyan);
                StateMachine.ChangeState(EnemyTurnState);
            }
        }

        /// <summary>
        /// Flees the active combat encounter.
        /// </summary>
        public void FleeCombat()
        {
            if (StateMachine.CurrentState is ActionSelectionState)
            {
                CombatEvents.TriggerFloatingFeedback("¡HUISTE DEL COMBATE!", Color.yellow);
                BattleWon = false;
                StateMachine.ChangeState(BattleEndState);
            }
        }

        /// <summary>
        /// Called by input handler during an active offensive QTE when a specific key (W, A, S, D, Z, X) is pressed.
        /// </summary>
        public void TriggerKeyQTEInput(KeyCode key)
        {
            if (StateMachine.CurrentState is QTEExecutionState qteState)
            {
                qteState.OnPlayerKeyInput(key);
            }
        }

        /// <summary>
        /// Called by input handler during an active offensive QTE.
        /// </summary>
        public void TriggerQTEInput()
        {
            if (StateMachine.CurrentState is QTEExecutionState qteState)
            {
                qteState.OnPlayerQTEInput();
            }
        }

        /// <summary>
        /// Called by input handler when the player chooses Parry or Dodge during an enemy attack.
        /// </summary>
        public void TriggerDefenseInput(DefenseType defenseType)
        {
            if (StateMachine.CurrentState is DefenseWindowState defenseState)
            {
                defenseState.OnPlayerDefenseInput(defenseType);
            }
        }

        internal void ResetTurnContext()
        {
            CurrentSkill = null;
            OffensiveTimingResult = TimingResult.Miss;
            ActiveDefenseType = DefenseType.None;
            DefensiveTimingResult = TimingResult.Miss;
        }
    }
}
