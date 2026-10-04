using System.Collections;
using UnityEngine;
using JPTJL.Combat;
using JPTJL.Data;
using JPTJL.Timing;

namespace JPTJL.UI
{
    /// <summary>
    /// Interactive on-screen UI for testing the turn-based combat loop.
    /// Draws stats (HP, Mana, Shield), turn state indicators, ability buttons (1-4),
    /// visible QTE prompts with countdown timer, and floating feedback messages ("PERFECT!", "FALLO").
    /// Purely subscribes to CombatEvents and commands CombatController.
    /// </summary>
    public class CombatDebugUI : MonoBehaviour
    {
        [Header("Controller Reference")]
        [SerializeField] private CombatController combatController;
        [SerializeField] private SkillData[] skills;

        [Header("Legacy Display Options")]
        [Tooltip("If false, legacy IMGUI health boxes are hidden in favor of the modern Canvas HUD.")]
        [SerializeField] private bool showLegacyHealthBars = false;

        [Tooltip("If false, legacy IMGUI ability selection deck is hidden in favor of CombatActionMenu.")]
        [SerializeField] private bool showLegacyActionDeck = false;

        [Tooltip("If false, legacy IMGUI top header banner is hidden in favor of Canvas HUD.")]
        [SerializeField] private bool showLegacyHeader = false;

        // Feedback message state
        private string activeFeedbackText = "";
        private Color activeFeedbackColor = Color.white;
        private float feedbackTimer = 0f;
        private const float FeedbackDisplayDuration = 1.4f;

        // QTE Display state
        private string activeQTEKey = "";
        private float qteRemainingTime = 0f;
        private float qteProgress = 0f;
        private int qteCurrentHit = 1;
        private int qteTotalHits = 1;
        private bool isQTEPromptActive = false;

        private void OnEnable()
        {
            CombatEvents.OnKeyQTEPrompt += HandleKeyPrompt;
            CombatEvents.OnKeyQTETicked += HandleKeyTicked;
            CombatEvents.OnKeyQTEEvaluated += HandleKeyEvaluated;
            CombatEvents.OnFloatingFeedback += ShowFeedback;
            CombatEvents.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            CombatEvents.OnKeyQTEPrompt -= HandleKeyPrompt;
            CombatEvents.OnKeyQTETicked -= HandleKeyTicked;
            CombatEvents.OnKeyQTEEvaluated -= HandleKeyEvaluated;
            CombatEvents.OnFloatingFeedback -= ShowFeedback;
            CombatEvents.OnPhaseChanged -= HandlePhaseChanged;
        }

        private void Update()
        {
            if (feedbackTimer > 0f)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0f)
                {
                    activeFeedbackText = "";
                }
            }
        }

        private void HandleKeyPrompt(KeyCode key, string keyName, float duration, int hit, int total)
        {
            activeQTEKey = keyName;
            qteRemainingTime = duration;
            qteProgress = 0f;
            qteCurrentHit = hit;
            qteTotalHits = total;
            isQTEPromptActive = true;
        }

        private void HandleKeyTicked(float remaining, float progress)
        {
            qteRemainingTime = remaining;
            qteProgress = progress;
        }

        private void HandleKeyEvaluated(KeyCode key, bool success, TimingResult result, string feedback, int hit, int total)
        {
            isQTEPromptActive = false;
        }

        private void HandlePhaseChanged(CombatPhase phase)
        {
            if (phase != CombatPhase.QTEExecution)
            {
                isQTEPromptActive = false;
            }
        }

        public void ShowFeedback(string text, Color color)
        {
            activeFeedbackText = text;
            activeFeedbackColor = color;
            feedbackTimer = FeedbackDisplayDuration;
        }

        private void OnGUI()
        {
            if (combatController == null || combatController.Player == null || combatController.Enemy == null)
            {
                return;
            }

            // High-DPI GUI scaling
            GUI.skin.label.fontSize = 14;
            GUI.skin.button.fontSize = 14;

            // 1. Top Header: Combat State Badge
            if (showLegacyHeader)
            {
                DrawHeaderBanner();
            }

            // 2. Character Status Boxes (Left: Player, Right: Enemy)
            if (showLegacyHealthBars)
            {
                DrawCombatantCard(new Rect(30, 70, 320, 160), combatController.Player, true);
                DrawCombatantCard(new Rect(Screen.width - 350, 70, 320, 160), combatController.Enemy, false);
            }

            // 3. Center QTE Overlay (when active)
            if (isQTEPromptActive && combatController.CurrentTurnState == CombatTurnState.EjecutandoQTE)
            {
                DrawQTEPromptOverlay();
            }

            // 4. Floating / Alert Feedback Text
            if (!string.IsNullOrEmpty(activeFeedbackText) && feedbackTimer > 0f)
            {
                DrawFloatingFeedback();
            }

            // 5. Bottom Action Deck: Ability Selection (TurnoJugador)
            if (showLegacyActionDeck)
            {
                if (combatController.CurrentTurnState == CombatTurnState.TurnoJugador)
                {
                    DrawAbilityActionDeck();
                }
                else if (combatController.CurrentTurnState == CombatTurnState.FinCombate)
                {
                    DrawBattleEndPrompt();
                }
            }
        }

        private void DrawHeaderBanner()
        {
            string stateText = "";
            Color stateColor = Color.white;

            switch (combatController.CurrentTurnState)
            {
                case CombatTurnState.TurnoJugador:
                    stateText = "⚔️ TURNO DEL JUGADOR - Elige una habilidad [1 - 4]";
                    stateColor = new Color(0.2f, 0.8f, 1f);
                    break;
                case CombatTurnState.EjecutandoQTE:
                    stateText = "⚡ EJECUTANDO QTE - ¡Presiona la tecla requerida!";
                    stateColor = new Color(1f, 0.85f, 0.1f);
                    break;
                case CombatTurnState.TurnoEnemigo:
                    stateText = "🛡️ TURNO ENEMIGO - Defiéndete con [Z] Parry o [X] Dodge";
                    stateColor = new Color(1f, 0.4f, 0.2f);
                    break;
                case CombatTurnState.FinCombate:
                    stateText = combatController.BattleWon ? "🏆 ¡VICTORIA EN COMBATE!" : "💀 ¡HAS SIDO DERROTADO!";
                    stateColor = combatController.BattleWon ? Color.green : Color.red;
                    break;
            }

            Rect bannerRect = new Rect(Screen.width / 2f - 260, 15, 520, 42);
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.Box(bannerRect, GUIContent.none);

            GUI.color = stateColor;
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(bannerRect, stateText, headerStyle);
            GUI.color = Color.white;
        }

        private void DrawCombatantCard(Rect rect, CombatParticipant participant, bool isPlayer)
        {
            GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);
            GUI.Box(rect, GUIContent.none);

            GUI.color = isPlayer ? new Color(0.3f, 0.8f, 1f) : new Color(1f, 0.4f, 0.4f);
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect(rect.x + 15, rect.y + 10, rect.width - 30, 24), participant.Name, titleStyle);

            GUI.color = Color.white;
            float contentY = rect.y + 40;

            // HP Bar
            float hpPct = (float)participant.CurrentHealth / participant.MaxHealth;
            DrawBar(new Rect(rect.x + 15, contentY, rect.width - 30, 22),
                    $"HP: {participant.CurrentHealth} / {participant.MaxHealth}",
                    hpPct, new Color(0.2f, 0.85f, 0.3f));

            contentY += 30;

            // Mana Bar (Player only)
            if (isPlayer)
            {
                float mpPct = participant.MaxMana > 0 ? (float)participant.CurrentMana / participant.MaxMana : 0f;
                DrawBar(new Rect(rect.x + 15, contentY, rect.width - 30, 22),
                        $"MP: {participant.CurrentMana} / {participant.MaxMana}",
                        mpPct, new Color(0.25f, 0.55f, 1f));
                contentY += 30;
            }

            // Shield / Absorción Bar
            if (participant.CurrentShield > 0)
            {
                DrawBar(new Rect(rect.x + 15, contentY, rect.width - 30, 22),
                        $"ESCUDO: {participant.CurrentShield} (Absorción)",
                        1f, new Color(0.2f, 0.95f, 0.95f));
            }
            else
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
                GUI.Label(new Rect(rect.x + 15, contentY, rect.width - 30, 22), "Escudo: Inactivo (0)");
                GUI.color = Color.white;
            }
        }

        private void DrawBar(Rect rect, string label, float fillPct, Color fillColor)
        {
            GUI.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            GUI.Box(rect, GUIContent.none);

            Rect fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fillPct), rect.height);
            GUI.color = fillColor;
            GUI.Box(fillRect, GUIContent.none);

            GUI.color = Color.white;
            GUIStyle barLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            GUI.Label(rect, label, barLabelStyle);
        }

        private void DrawQTEPromptOverlay()
        {
            Rect boxRect = new Rect(Screen.width / 2f - 180, Screen.height / 2f - 140, 360, 220);
            GUI.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);
            GUI.Box(boxRect, GUIContent.none);

            // Frame accent
            GUI.color = new Color(1f, 0.85f, 0.2f);
            GUIStyle promptTitle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect(boxRect.x, boxRect.y + 12, boxRect.width, 26), $"⚡ ¡GOLPE {qteCurrentHit} DE {qteTotalHits}!", promptTitle);

            // Large Key display
            GUIStyle keyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 48,
                fontStyle = FontStyle.Bold
            };
            GUI.color = Color.white;
            GUI.Label(new Rect(boxRect.x, boxRect.y + 44, boxRect.width, 64), $"[ {activeQTEKey} ]", keyStyle);

            // Instruction subtitle
            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            };
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            GUI.Label(new Rect(boxRect.x, boxRect.y + 115, boxRect.width, 20), "Presiona la tecla exacta antes que expire el tiempo", subStyle);

            // Timer bar
            Rect timerRect = new Rect(boxRect.x + 30, boxRect.y + 145, boxRect.width - 60, 24);
            float timePct = 1f - qteProgress;
            Color timerColor = timePct > 0.4f ? new Color(0.3f, 0.9f, 0.3f) : new Color(1f, 0.25f, 0.25f);
            DrawBar(timerRect, $"{qteRemainingTime:F2}s", timePct, timerColor);

            // Hint
            GUI.Label(new Rect(boxRect.x, boxRect.y + 180, boxRect.width, 22), "Éxito = +20% Daño (PERFECT!) | Fallo = Daño Base", subStyle);
        }

        private void DrawFloatingFeedback()
        {
            Rect fbRect = new Rect(Screen.width / 2f - 200, Screen.height / 2f - 190, 400, 45);
            GUI.color = activeFeedbackColor;
            GUIStyle fbStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(fbRect, activeFeedbackText, fbStyle);
            GUI.color = Color.white;
        }

        private void DrawAbilityActionDeck()
        {
            if (skills == null || skills.Length == 0) return;

            float deckWidth = 760;
            float deckHeight = 110;
            Rect deckRect = new Rect(Screen.width / 2f - deckWidth / 2f, Screen.height - deckHeight - 20, deckWidth, deckHeight);

            GUI.color = new Color(0.06f, 0.06f, 0.1f, 0.95f);
            GUI.Box(deckRect, GUIContent.none);

            float buttonWidth = (deckWidth - 50) / 4f;
            float buttonHeight = 85;

            for (int i = 0; i < skills.Length && i < 4; i++)
            {
                SkillData skill = skills[i];
                if (skill == null) continue;

                Rect btnRect = new Rect(deckRect.x + 10 + i * (buttonWidth + 10), deckRect.y + 12, buttonWidth, buttonHeight);
                bool canAfford = combatController.Player.HasMana(skill.ManaCost);

                string costText = skill.ManaCost > 0 ? $"{skill.ManaCost} MP" : "0 MP";
                string qteText = skill.RequiresQTE ? $"QTE: {skill.QteKeyCount}T" : "Sin QTE";
                string typeDetail = skill.Type == SkillType.Shield ? $"+{skill.ShieldAmount} Escudo" : $"{skill.BasePower} Daño";

                string label = $"<b>[{i + 1}] {skill.SkillName}</b>\n{typeDetail} | {costText}\n<size=11>{qteText}</size>";

                GUI.color = canAfford ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.8f);

                if (GUI.Button(btnRect, label))
                {
                    combatController.SelectSkill(skill);
                }
            }
            GUI.color = Color.white;
        }

        private void DrawBattleEndPrompt()
        {
            Rect endRect = new Rect(Screen.width / 2f - 140, Screen.height - 90, 280, 50);
            GUI.color = new Color(0.2f, 0.8f, 0.4f, 1f);
            if (GUI.Button(endRect, "🔄 Reiniciar Combate"))
            {
                combatController.StartCombat(
                    combatController.Player.StatsData,
                    combatController.Enemy.StatsData,
                    skills != null && skills.Length > 0 ? skills[0] : null,
                    combatController.EnemyDefaultSkill
                );
            }
            GUI.color = Color.white;
        }
    }
}
