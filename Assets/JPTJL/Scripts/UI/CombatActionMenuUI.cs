using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JPTJL.Combat;
using JPTJL.Data;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace JPTJL.UI
{
    /// <summary>
    /// Controls the interactive Combat Action Menu at the bottom of the screen.
    /// Handles switching between:
    /// 1. Main Action Deck (Sword / Skills, Potion / Items, Arrow / Flee)
    /// 2. Skills Menu (with skill cards and Back button)
    /// 3. Potions Menu (Health/Mana potions and Back button)
    /// </summary>
    public class CombatActionMenuUI : MonoBehaviour
    {
        [Header("Controller Reference")]
        [SerializeField] private CombatController combatController;

        [Header("Main Action Bar")]
        [Tooltip("Root container for the 3 main combat buttons.")]
        [SerializeField] private GameObject mainActionBar;
        [SerializeField] private Button swordSkillsButton;
        [SerializeField] private Button potionButton;
        [SerializeField] private Button fleeButton;

        [Header("Skills Sub-Menu")]
        [Tooltip("Root panel for the Skills list.")]
        [SerializeField] private GameObject skillsPanel;
        [Tooltip("Container where skill buttons are placed.")]
        [SerializeField] private RectTransform skillsContainer;
        [Tooltip("Back button to return to the main action bar.")]
        [SerializeField] private Button skillsBackButton;

        [Header("Potions Sub-Menu")]
        [Tooltip("Root panel for the Potions menu.")]
        [SerializeField] private GameObject potionsPanel;
        [SerializeField] private Button healthPotionButton;
        [SerializeField] private Button manaPotionButton;
        [Tooltip("Back button to return to the main action bar.")]
        [SerializeField] private Button potionsBackButton;

        [Header("Skill Button Template / Prefab (Optional)")]
        [Tooltip("Optional button prefab or pre-instantiated buttons.")]
        [SerializeField] private GameObject skillButtonTemplate;

        [Header("Audio or Visual Feedback")]
        [SerializeField] private int healthPotionAmount = 35;
        [SerializeField] private int manaPotionAmount = 25;

        private readonly List<GameObject> spawnedSkillButtons = new List<GameObject>();

        private void Awake()
        {
            if (combatController == null)
            {
                combatController = FindFirstObjectByType<CombatController>();
            }

            EnsureEventSystemAndRaycaster();
            SetupButtonListeners();
        }

        private void EnsureEventSystemAndRaycaster()
        {
            // 1. Ensure Canvas has GraphicRaycaster so UI clicks register
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            // 2. Ensure EventSystem exists and is active with valid UI Input Module
            var es = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
                var uiMod = esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                uiMod.AssignDefaultActions();
#else
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }
            else
            {
#if ENABLE_INPUT_SYSTEM
                var uiMod = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (uiMod != null)
                {
                    if (uiMod.point == null || uiMod.leftClick == null || uiMod.actionsAsset == null)
                    {
                        uiMod.AssignDefaultActions();
                    }
                }
#endif
            }
        }

        public void SetSerializedReferences(
            GameObject mainBar,
            Button swordBtn,
            Button potBtn,
            Button flBtn,
            GameObject skPanel,
            RectTransform skContainer,
            Button skBackBtn,
            GameObject potPanel,
            Button hpBtn,
            Button mpBtn,
            Button potBackBtn)
        {
            mainActionBar = mainBar;
            swordSkillsButton = swordBtn;
            potionButton = potBtn;
            fleeButton = flBtn;

            skillsPanel = skPanel;
            skillsContainer = skContainer;
            skillsBackButton = skBackBtn;

            potionsPanel = potPanel;
            healthPotionButton = hpBtn;
            manaPotionButton = mpBtn;
            potionsBackButton = potBackBtn;

            SetupButtonListeners();
        }

        private void OnEnable()
        {
            CombatEvents.OnPhaseChanged += HandlePhaseChanged;
            CombatEvents.OnBattleFinished += HandleBattleFinished;
            CombatEvents.OnManaChanged += HandleManaChanged;
        }

        private void OnDisable()
        {
            CombatEvents.OnPhaseChanged -= HandlePhaseChanged;
            CombatEvents.OnBattleFinished -= HandleBattleFinished;
            CombatEvents.OnManaChanged -= HandleManaChanged;
        }

        private void Start()
        {
            EnsureEventSystemAndRaycaster();
            SetupButtonListeners();

            // Initial state: show main deck by default if player turn or preview mode
            if (combatController == null || combatController.CurrentTurnState == CombatTurnState.TurnoJugador)
            {
                ShowMainDeck();
            }
            else
            {
                CloseAllMenus();
            }
        }

        private void Update()
        {
            // Allow keyboard shortcuts as responsive fallbacks:
            if (mainActionBar != null && mainActionBar.activeSelf)
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.digit1Key.wasPressedThisFrame) OpenSkillsMenu();
                    else if (kb.digit2Key.wasPressedThisFrame) OpenPotionsMenu();
                    else if (kb.digit3Key.wasPressedThisFrame) OnFleeClicked();
                }
#else
                if (Input.GetKeyDown(KeyCode.Alpha1)) OpenSkillsMenu();
                else if (Input.GetKeyDown(KeyCode.Alpha2)) OpenPotionsMenu();
                else if (Input.GetKeyDown(KeyCode.Alpha3)) OnFleeClicked();
#endif
            }
            else if (skillsPanel != null && skillsPanel.activeSelf)
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame))
                {
                    ShowMainDeck();
                }
#else
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
                {
                    ShowMainDeck();
                }
#endif
            }
            else if (potionsPanel != null && potionsPanel.activeSelf)
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame) ShowMainDeck();
                    else if (kb.digit1Key.wasPressedThisFrame) OnHealthPotionClicked();
                    else if (kb.digit2Key.wasPressedThisFrame) OnManaPotionClicked();
                }
#else
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)) ShowMainDeck();
                else if (Input.GetKeyDown(KeyCode.Alpha1)) OnHealthPotionClicked();
                else if (Input.GetKeyDown(KeyCode.Alpha2)) OnManaPotionClicked();
#endif
            }
        }

        private void SetupButtonListeners()
        {
            if (swordSkillsButton != null)
            {
                swordSkillsButton.onClick.RemoveAllListeners();
                swordSkillsButton.onClick.AddListener(OpenSkillsMenu);
            }

            if (potionButton != null)
            {
                potionButton.onClick.RemoveAllListeners();
                potionButton.onClick.AddListener(OpenPotionsMenu);
            }

            if (fleeButton != null)
            {
                fleeButton.onClick.RemoveAllListeners();
                fleeButton.onClick.AddListener(OnFleeClicked);
            }

            if (skillsBackButton != null)
            {
                skillsBackButton.onClick.RemoveAllListeners();
                skillsBackButton.onClick.AddListener(ShowMainDeck);
            }

            if (potionsBackButton != null)
            {
                potionsBackButton.onClick.RemoveAllListeners();
                potionsBackButton.onClick.AddListener(ShowMainDeck);
            }

            if (healthPotionButton != null)
            {
                healthPotionButton.onClick.RemoveAllListeners();
                healthPotionButton.onClick.AddListener(OnHealthPotionClicked);
            }

            if (manaPotionButton != null)
            {
                manaPotionButton.onClick.RemoveAllListeners();
                manaPotionButton.onClick.AddListener(OnManaPotionClicked);
            }
        }

        // ─────────────────────────────────────────
        //  Menu Navigation
        // ─────────────────────────────────────────

        /// <summary>
        /// Displays the main 3 buttons (Sword, Potion, Flee) and closes submenus.
        /// </summary>
        public void ShowMainDeck()
        {
            if (mainActionBar != null) mainActionBar.SetActive(true);
            if (skillsPanel != null) skillsPanel.SetActive(false);
            if (potionsPanel != null) potionsPanel.SetActive(false);
        }

        /// <summary>
        /// Opens the Skills menu and populates player skills.
        /// </summary>
        public void OpenSkillsMenu()
        {
            if (mainActionBar != null) mainActionBar.SetActive(false);
            if (potionsPanel != null) potionsPanel.SetActive(false);
            if (skillsPanel != null)
            {
                skillsPanel.SetActive(true);
                PopulateSkills();
            }
        }

        /// <summary>
        /// Opens the Potions menu.
        /// </summary>
        public void OpenPotionsMenu()
        {
            if (mainActionBar != null) mainActionBar.SetActive(false);
            if (skillsPanel != null) skillsPanel.SetActive(false);
            if (potionsPanel != null) potionsPanel.SetActive(true);
        }

        /// <summary>
        /// Hides all action menus during QTE or enemy turn.
        /// </summary>
        public void CloseAllMenus()
        {
            if (mainActionBar != null) mainActionBar.SetActive(false);
            if (skillsPanel != null) skillsPanel.SetActive(false);
            if (potionsPanel != null) potionsPanel.SetActive(false);
        }

        // ─────────────────────────────────────────
        //  Skills Population
        // ─────────────────────────────────────────

        private void PopulateSkills()
        {
            if (skillsContainer == null) return;

            SkillData[] skills = null;
            if (combatController != null)
            {
                skills = combatController.PlayerSkills;
                if ((skills == null || skills.Length == 0) && combatController.CurrentSkill != null)
                {
                    skills = new SkillData[] { combatController.CurrentSkill };
                }
            }

            if (skills == null || skills.Length == 0)
            {
                skills = Resources.LoadAll<SkillData>("Skills");
            }

            if (skills == null || skills.Length == 0) return;

            // Clear previous dynamic buttons
            foreach (var btnObj in spawnedSkillButtons)
            {
                if (btnObj != null) Destroy(btnObj);
            }
            spawnedSkillButtons.Clear();

            int currentMana = combatController.Player != null ? combatController.Player.CurrentMana : 999;

            for (int i = 0; i < skills.Length; i++)
            {
                SkillData skill = skills[i];
                if (skill == null) continue;

                GameObject buttonObj;
                if (skillButtonTemplate != null)
                {
                    buttonObj = Instantiate(skillButtonTemplate, skillsContainer);
                    buttonObj.SetActive(true);
                }
                else
                {
                    buttonObj = CreateDefaultSkillButton(skill, i);
                    buttonObj.transform.SetParent(skillsContainer, false);
                }

                Button btn = buttonObj.GetComponent<Button>();
                bool hasEnoughMana = currentMana >= skill.ManaCost;

                if (btn != null)
                {
                    btn.interactable = hasEnoughMana;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnSkillSelected(skill));
                }

                spawnedSkillButtons.Add(buttonObj);
            }
        }

        private GameObject CreateDefaultSkillButton(SkillData skill, int index)
        {
            GameObject obj = new GameObject($"SkillBtn_{skill.SkillName}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180, 75);

            LayoutElement le = obj.GetComponent<LayoutElement>();
            le.preferredWidth = 180f;
            le.preferredHeight = 75f;

            Image img = obj.GetComponent<Image>();
            img.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
            img.raycastTarget = true;

            Button btn = obj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.24f, 0.34f, 0.52f, 1f);
            cb.pressedColor = new Color(0.06f, 0.09f, 0.15f, 1f);
            btn.colors = cb;

            // Add Text
            GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(obj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(8, 4);
            textRect.offsetMax = new Vector2(-8, -4);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.richText = true;
            tmp.raycastTarget = false;

            string manaText = skill.ManaCost > 0 ? $"<color=#38BDF8>({skill.ManaCost} MP)</color>" : "<color=#4ADE80>(0 MP)</color>";
            tmp.text = $"<b>{skill.SkillName}</b>\n{manaText}\n<size=11><color=#94A3B8>{skill.Description}</color></size>";

            return obj;
        }

        // ─────────────────────────────────────────
        //  Action Callbacks
        // ─────────────────────────────────────────

        private void OnSkillSelected(SkillData skill)
        {
            CloseAllMenus();
            if (combatController != null)
            {
                combatController.SelectSkill(skill);
            }
            else
            {
                Debug.Log($"<color=yellow>[CombatUI Preview]</color> Habilidad seleccionada: {skill.SkillName}");
            }
        }

        private void OnHealthPotionClicked()
        {
            CloseAllMenus();
            if (combatController != null)
            {
                combatController.UseHealthPotion(healthPotionAmount);
            }
            else
            {
                Debug.Log($"<color=green>[CombatUI Preview]</color> Poción de Salud usada (+{healthPotionAmount} HP)");
            }
        }

        private void OnManaPotionClicked()
        {
            CloseAllMenus();
            if (combatController != null)
            {
                combatController.UseManaPotion(manaPotionAmount);
            }
            else
            {
                Debug.Log($"<color=cyan>[CombatUI Preview]</color> Poción de Maná usada (+{manaPotionAmount} MP)");
            }
        }

        private void OnFleeClicked()
        {
            CloseAllMenus();
            if (combatController != null)
            {
                combatController.FleeCombat();
            }
            else
            {
                Debug.Log("<color=orange>[CombatUI Preview]</color> Huir del combate activado");
            }
        }

        // ─────────────────────────────────────────
        //  Event Handlers
        // ─────────────────────────────────────────

        private void HandlePhaseChanged(CombatPhase phase)
        {
            if (phase == CombatPhase.ActionSelection)
            {
                ShowMainDeck();
            }
            else
            {
                CloseAllMenus();
            }
        }

        private void HandleBattleFinished(bool won)
        {
            CloseAllMenus();
        }

        private void HandleManaChanged(CombatParticipant participant, int currentMp, int maxMp)
        {
            if (participant != null && participant.IsPlayer && skillsPanel != null && skillsPanel.activeSelf)
            {
                PopulateSkills();
            }
        }
    }
}
