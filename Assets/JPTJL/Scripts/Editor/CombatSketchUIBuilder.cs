#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JPTJL.Combat;
using JPTJL.Data;
using JPTJL.UI;
using Project.Combat.UI;

namespace JPTJL.Editor
{
    public static class CombatSketchUIBuilder
    {
        private const string TexturesPath = "Assets/_Project/Textures/UI";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool("JPTJL_SketchUI_AutoBuilt_v3", false))
                {
                    if (!EditorApplication.isPlaying)
                    {
                        var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                        if (string.IsNullOrEmpty(activeScene.path) || !activeScene.path.Contains("SampleScene"))
                        {
                            if (System.IO.File.Exists("Assets/Scenes/SampleScene.unity"))
                            {
                                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
                            }
                        }
                    }

                    BuildCombatUI();
                    EditorPrefs.SetBool("JPTJL_SketchUI_AutoBuilt_v3", true);
                }
            };
        }

        [MenuItem("Tools/JPTJL/3. Build Combat UI from Sketch", false, 12)]
        public static void BuildCombatUI()
        {
            // 1. Ensure Sprite Assets are imported properly
            Sprite swordSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesPath}/icon_sword.png");
            Sprite potionSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesPath}/icon_potion.png");
            Sprite fleeSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesPath}/icon_flee.png");
            Sprite heroSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesPath}/icon_hero.png");
            Sprite enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesPath}/icon_enemy.png");
            Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesPath}/icon_back.png");
            Sprite platformSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesPath}/combat_platform.png");
            Sprite potionHpSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesPath}/icon_potion_hp.png");
            Sprite potionMpSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesPath}/icon_potion_mp.png");

            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            // 2. Find or Create Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject canvasGo;
            if (canvas == null)
            {
                canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
            }
            else
            {
                canvasGo = canvas.gameObject;
            }

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }

            // Ensure EventSystem exists
            UnityEngine.EventSystems.EventSystem es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject esGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                var uiMod = esGo.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                uiMod.AssignDefaultActions();
                Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
            }
            else
            {
                var uiMod = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (uiMod != null && (uiMod.point == null || uiMod.leftClick == null || uiMod.actionsAsset == null))
                {
                    uiMod.AssignDefaultActions();
                }
            }

            // 3. Build Ground Platforms (Middle Area: "Donde estará Parado el Personaje / Enemigo")
            Transform platformsRoot = canvasGo.transform.Find("Platforms");
            if (platformsRoot == null)
            {
                GameObject pGo = new GameObject("Platforms", typeof(RectTransform));
                pGo.transform.SetParent(canvasGo.transform, false);
                platformsRoot = pGo.transform;
            }
            // Put platforms behind UI
            platformsRoot.SetAsFirstSibling();

            CreateOrUpdatePlatform(platformsRoot, "HeroPlatform", platformSprite, new Vector2(-480, -90), new Vector2(400, 220));
            CreateOrUpdatePlatform(platformsRoot, "EnemyPlatform", platformSprite, new Vector2(480, -90), new Vector2(400, 220));

            // 4. Build Hero_UI (Top-Left: Portrait, Character Name, Salud, Maná)
            CombatUnitUI heroUnitUI = BuildHeroUI(canvasGo.transform, heroSprite, fontAsset);

            // 5. Build Enemy_UI (Top-Right: Character Name, Salud, Portrait)
            CombatUnitUI enemyUnitUI = BuildEnemyUI(canvasGo.transform, enemySprite, fontAsset);

            // 6. Build Combat Action Menu (Bottom: Sword -> Skills, Potion -> Potions, Flee -> Flee)
            CombatActionMenuUI actionMenuUI = BuildActionMenu(canvasGo.transform, swordSprite, potionSprite, fleeSprite, backSprite, potionHpSprite, potionMpSprite, fontAsset);

            // 7. Build Parry Visuals (Red Dot + Enemy Attack Lunge + Shield Flash)
            Sprite redDotSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesPath}/icon_parry_red_dot.png");
            BuildParryVisuals(canvasGo.transform, redDotSprite, fontAsset);

            // 8. Wire to CombatController & CombatHUDController
            CombatController controller = Object.FindFirstObjectByType<CombatController>();
            CombatHUDController hudController = Object.FindFirstObjectByType<CombatHUDController>();

            if (controller != null)
            {
                // Ensure player skills are assigned to controller
                SerializedObject ctrlSo = new SerializedObject(controller);
                SerializedProperty skillsProp = ctrlSo.FindProperty("playerSkills");
                if (skillsProp != null && skillsProp.arraySize == 0)
                {
                    SkillData s1 = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/JPTJL/Data/Skill_GolpeBasico.asset");
                    SkillData s2 = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/JPTJL/Data/Skill_DobleEstocada.asset");
                    SkillData s3 = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/JPTJL/Data/Skill_AtaquePesado.asset");
                    SkillData s4 = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/JPTJL/Data/Skill_BarreraEscudo.asset");

                    skillsProp.arraySize = 4;
                    skillsProp.GetArrayElementAtIndex(0).objectReferenceValue = s1;
                    skillsProp.GetArrayElementAtIndex(1).objectReferenceValue = s2;
                    skillsProp.GetArrayElementAtIndex(2).objectReferenceValue = s3;
                    skillsProp.GetArrayElementAtIndex(3).objectReferenceValue = s4;
                    ctrlSo.ApplyModifiedProperties();
                }
            }

            if (hudController != null)
            {
                SerializedObject hudSo = new SerializedObject(hudController);
                hudSo.FindProperty("heroUI").objectReferenceValue = heroUnitUI;
                hudSo.FindProperty("enemyUI").objectReferenceValue = enemyUnitUI;
                hudSo.FindProperty("actionMenuUI").objectReferenceValue = actionMenuUI;
                hudSo.FindProperty("heroPortrait").objectReferenceValue = heroSprite;
                hudSo.FindProperty("enemyPortrait").objectReferenceValue = enemySprite;
                hudSo.ApplyModifiedProperties();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            try
            {
                string prefabPath = "Assets/_Project/Prefabs/UI/Canvas.prefab";
                PrefabUtility.SaveAsPrefabAsset(canvasGo, prefabPath);
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[JPTJL UI] Could not save Canvas prefab: " + ex.Message);
            }

            var currScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(currScene.path))
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(currScene);
            }

            Debug.Log("<color=green>[JPTJL UI]</color> ¡Interfaz del combate construida con éxito siguiendo el boceto!");
        }

        private static void CreateOrUpdatePlatform(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
        {
            Transform t = parent.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = t.gameObject;
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = new Color(1f, 1f, 1f, 0.85f);
            img.raycastTarget = false;
        }

        private static CombatUnitUI BuildHeroUI(Transform canvas, Sprite heroPortrait, TMP_FontAsset font)
        {
            Transform existing = canvas.Find("Hero_UI");
            GameObject heroGo;
            if (existing != null)
            {
                heroGo = existing.gameObject;
            }
            else
            {
                heroGo = new GameObject("Hero_UI", typeof(RectTransform));
                heroGo.transform.SetParent(canvas, false);
            }

            RectTransform rootRect = heroGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(40f, -40f);
            rootRect.sizeDelta = new Vector2(420f, 110f);

            // 1. Portrait Frame & Icon (Left)
            GameObject portraitGo = GetOrCreateChild(heroGo, "Portrait", typeof(Image));
            RectTransform pRect = portraitGo.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0f, 0.5f);
            pRect.anchorMax = new Vector2(0f, 0.5f);
            pRect.pivot = new Vector2(0f, 0.5f);
            pRect.anchoredPosition = new Vector2(0f, 0f);
            pRect.sizeDelta = new Vector2(85f, 85f);
            Image portraitImg = portraitGo.GetComponent<Image>();
            portraitImg.sprite = heroPortrait;
            portraitImg.preserveAspect = true;

            // 2. Name Text
            GameObject nameGo = GetOrCreateChild(heroGo, "NameText", typeof(TextMeshProUGUI));
            RectTransform nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(0f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.anchoredPosition = new Vector2(100f, -5f);
            nameRect.sizeDelta = new Vector2(300f, 26f);
            TextMeshProUGUI nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
            if (font != null) nameTmp.font = font;
            nameTmp.fontSize = 20;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.text = "(Personaje) Héroe";
            nameTmp.color = Color.white;

            // 3. Health Bar Slider
            GameObject hpSliderGo = GetOrCreateSlider(heroGo, "HealthBar", new Vector2(100f, -36f), new Vector2(300f, 24f), new Color(0.13f, 0.77f, 0.37f), font, "Salud 100/100", out TextMeshProUGUI hpNumeric);

            // 4. Mana Bar Slider
            GameObject manaSliderGo = GetOrCreateSlider(heroGo, "ManaBar", new Vector2(100f, -66f), new Vector2(300f, 22f), new Color(0.06f, 0.65f, 0.91f), font, "Maná 50/50", out TextMeshProUGUI manaNumeric);

            // Wire CombatUnitUI
            CombatUnitUI unitUI = heroGo.GetComponent<CombatUnitUI>();
            if (unitUI == null) unitUI = heroGo.AddComponent<CombatUnitUI>();

            SerializedObject so = new SerializedObject(unitUI);
            so.FindProperty("nameText").objectReferenceValue = nameTmp;
            so.FindProperty("portraitImage").objectReferenceValue = portraitImg;
            so.FindProperty("hpSlider").objectReferenceValue = hpSliderGo.GetComponent<Slider>();
            so.FindProperty("hpNumericText").objectReferenceValue = hpNumeric;
            so.FindProperty("manaSlider").objectReferenceValue = manaSliderGo.GetComponent<Slider>();
            so.FindProperty("manaNumericText").objectReferenceValue = manaNumeric;
            so.ApplyModifiedProperties();

            return unitUI;
        }

        private static CombatUnitUI BuildEnemyUI(Transform canvas, Sprite enemyPortrait, TMP_FontAsset font)
        {
            Transform existing = canvas.Find("Enemy_UI");
            GameObject enemyGo;
            if (existing != null)
            {
                enemyGo = existing.gameObject;
            }
            else
            {
                enemyGo = new GameObject("Enemy_UI", typeof(RectTransform));
                enemyGo.transform.SetParent(canvas, false);
            }

            RectTransform rootRect = enemyGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.anchoredPosition = new Vector2(-40f, -40f);
            rootRect.sizeDelta = new Vector2(420f, 110f);

            // 1. Portrait Frame & Icon (Right)
            GameObject portraitGo = GetOrCreateChild(enemyGo, "Portrait", typeof(Image));
            RectTransform pRect = portraitGo.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(1f, 0.5f);
            pRect.anchorMax = new Vector2(1f, 0.5f);
            pRect.pivot = new Vector2(1f, 0.5f);
            pRect.anchoredPosition = new Vector2(0f, 0f);
            pRect.sizeDelta = new Vector2(85f, 85f);
            Image portraitImg = portraitGo.GetComponent<Image>();
            portraitImg.sprite = enemyPortrait;
            portraitImg.preserveAspect = true;

            // 2. Name Text (Aligned to right)
            GameObject nameGo = GetOrCreateChild(enemyGo, "NameText", typeof(TextMeshProUGUI));
            RectTransform nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(1f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(1f, 1f);
            nameRect.anchoredPosition = new Vector2(-100f, -5f);
            nameRect.sizeDelta = new Vector2(300f, 26f);
            TextMeshProUGUI nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
            if (font != null) nameTmp.font = font;
            nameTmp.fontSize = 20;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.text = "Goblin (Enemigo)";
            nameTmp.alignment = TextAlignmentOptions.TopRight;
            nameTmp.color = Color.white;

            // 3. Health Bar Slider
            GameObject hpSliderGo = GetOrCreateSlider(enemyGo, "HealthBar", new Vector2(-100f, -36f), new Vector2(300f, 24f), new Color(0.94f, 0.27f, 0.27f), font, "Salud 85/85", out TextMeshProUGUI hpNumeric, isRightAligned: true);

            // Wire CombatUnitUI
            CombatUnitUI unitUI = enemyGo.GetComponent<CombatUnitUI>();
            if (unitUI == null) unitUI = enemyGo.AddComponent<CombatUnitUI>();

            SerializedObject so = new SerializedObject(unitUI);
            so.FindProperty("nameText").objectReferenceValue = nameTmp;
            so.FindProperty("portraitImage").objectReferenceValue = portraitImg;
            so.FindProperty("hpSlider").objectReferenceValue = hpSliderGo.GetComponent<Slider>();
            so.FindProperty("hpNumericText").objectReferenceValue = hpNumeric;
            so.FindProperty("manaSlider").objectReferenceValue = null;
            so.FindProperty("manaNumericText").objectReferenceValue = null;
            so.ApplyModifiedProperties();

            return unitUI;
        }

        private static CombatActionMenuUI BuildActionMenu(
            Transform canvas,
            Sprite swordSprite,
            Sprite potionSprite,
            Sprite fleeSprite,
            Sprite backSprite,
            Sprite potionHpSprite,
            Sprite potionMpSprite,
            TMP_FontAsset font)
        {
            Transform existing = canvas.Find("CombatActionMenu");
            GameObject menuGo;
            if (existing != null)
            {
                menuGo = existing.gameObject;
            }
            else
            {
                menuGo = new GameObject("CombatActionMenu", typeof(RectTransform));
                menuGo.transform.SetParent(canvas, false);
            }

            RectTransform rootRect = menuGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 30f);
            rootRect.sizeDelta = new Vector2(850f, 160f);

            // ─────────────────────────────────────────
            // 1. Main Action Bar (Sword, Potion, Flee)
            // ─────────────────────────────────────────
            GameObject mainBarGo = GetOrCreateChild(menuGo, "MainActionBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            RectTransform mbRect = mainBarGo.GetComponent<RectTransform>();
            mbRect.anchorMin = Vector2.zero;
            mbRect.anchorMax = Vector2.one;
            mbRect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup hlg = mainBarGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 40f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            Button swordBtn = CreateActionCardButton(mainBarGo.transform, "Btn_Habilidades", swordSprite, "Habilidades", new Color(0.99f, 0.88f, 0.28f), font);
            Button potionBtn = CreateActionCardButton(mainBarGo.transform, "Btn_Pociones", potionSprite, "Pociones", new Color(0.96f, 0.45f, 0.71f), font);
            Button fleeBtn = CreateActionCardButton(mainBarGo.transform, "Btn_Huir", fleeSprite, "Huir", new Color(0.96f, 0.62f, 0.04f), font);

            // ─────────────────────────────────────────
            // 2. Skills Sub-Menu Panel
            // ─────────────────────────────────────────
            GameObject skillsPanelGo = GetOrCreateChild(menuGo, "SkillsPanel", typeof(RectTransform), typeof(Image));
            RectTransform spRect = skillsPanelGo.GetComponent<RectTransform>();
            spRect.anchorMin = Vector2.zero;
            spRect.anchorMax = Vector2.one;
            spRect.sizeDelta = Vector2.zero;
            Image spImg = skillsPanelGo.GetComponent<Image>();
            spImg.color = new Color(0.06f, 0.09f, 0.16f, 0.95f);

            // Title
            GameObject skillsTitleGo = GetOrCreateChild(skillsPanelGo, "Title", typeof(TextMeshProUGUI));
            RectTransform stRect = skillsTitleGo.GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0.5f, 1f);
            stRect.anchorMax = new Vector2(0.5f, 1f);
            stRect.pivot = new Vector2(0.5f, 1f);
            stRect.anchoredPosition = new Vector2(0f, -8f);
            stRect.sizeDelta = new Vector2(400f, 26f);
            TextMeshProUGUI stTmp = skillsTitleGo.GetComponent<TextMeshProUGUI>();
            if (font != null) stTmp.font = font;
            stTmp.fontSize = 18;
            stTmp.fontStyle = FontStyles.Bold;
            stTmp.alignment = TextAlignmentOptions.Center;
            stTmp.text = "⚔️ HABILIDADES";
            stTmp.color = new Color(0.99f, 0.88f, 0.28f);

            // Container
            GameObject scGo = GetOrCreateChild(skillsPanelGo, "SkillsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            RectTransform scRect = scGo.GetComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0.5f, 0.5f);
            scRect.anchorMax = new Vector2(0.5f, 0.5f);
            scRect.pivot = new Vector2(0.5f, 0.5f);
            scRect.anchoredPosition = new Vector2(-40f, -12f);
            scRect.sizeDelta = new Vector2(620f, 85f);
            HorizontalLayoutGroup scHlg = scGo.GetComponent<HorizontalLayoutGroup>();
            scHlg.spacing = 15f;
            scHlg.childAlignment = TextAnchor.MiddleCenter;
            scHlg.childControlWidth = false;
            scHlg.childControlHeight = false;

            // Back button
            Button skillsBackBtn = CreateBackButton(skillsPanelGo.transform, "Btn_BackSkills", backSprite, font, new Vector2(340f, -12f));

            // ─────────────────────────────────────────
            // 3. Potions Sub-Menu Panel
            // ─────────────────────────────────────────
            GameObject potionsPanelGo = GetOrCreateChild(menuGo, "PotionsPanel", typeof(RectTransform), typeof(Image));
            RectTransform ppRect = potionsPanelGo.GetComponent<RectTransform>();
            ppRect.anchorMin = Vector2.zero;
            ppRect.anchorMax = Vector2.one;
            ppRect.sizeDelta = Vector2.zero;
            Image ppImg = potionsPanelGo.GetComponent<Image>();
            ppImg.color = new Color(0.06f, 0.09f, 0.16f, 0.95f);

            // Title
            GameObject potionsTitleGo = GetOrCreateChild(potionsPanelGo, "Title", typeof(TextMeshProUGUI));
            RectTransform ptRect = potionsTitleGo.GetComponent<RectTransform>();
            ptRect.anchorMin = new Vector2(0.5f, 1f);
            ptRect.anchorMax = new Vector2(0.5f, 1f);
            ptRect.pivot = new Vector2(0.5f, 1f);
            ptRect.anchoredPosition = new Vector2(0f, -8f);
            ptRect.sizeDelta = new Vector2(400f, 26f);
            TextMeshProUGUI ptTmp = potionsTitleGo.GetComponent<TextMeshProUGUI>();
            if (font != null) ptTmp.font = font;
            ptTmp.fontSize = 18;
            ptTmp.fontStyle = FontStyles.Bold;
            ptTmp.alignment = TextAlignmentOptions.Center;
            ptTmp.text = "🧪 POCIONES (SALUD / MANÁ)";
            ptTmp.color = new Color(0.96f, 0.45f, 0.71f);

            // Container with 2 potion buttons
            GameObject pcGo = GetOrCreateChild(potionsPanelGo, "PotionsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            RectTransform pcRect = pcGo.GetComponent<RectTransform>();
            pcRect.anchorMin = new Vector2(0.5f, 0.5f);
            pcRect.anchorMax = new Vector2(0.5f, 0.5f);
            pcRect.pivot = new Vector2(0.5f, 0.5f);
            pcRect.anchoredPosition = new Vector2(-40f, -12f);
            pcRect.sizeDelta = new Vector2(550f, 85f);
            HorizontalLayoutGroup pcHlg = pcGo.GetComponent<HorizontalLayoutGroup>();
            pcHlg.spacing = 20f;
            pcHlg.childAlignment = TextAnchor.MiddleCenter;
            pcHlg.childControlWidth = false;
            pcHlg.childControlHeight = false;

            Button hpPotionBtn = CreatePotionButton(pcGo.transform, "Btn_HpPotion", potionHpSprite != null ? potionHpSprite : potionSprite, "Poción de Salud", "+35 HP", new Color(0.22f, 0.82f, 0.44f), font);
            Button mpPotionBtn = CreatePotionButton(pcGo.transform, "Btn_MpPotion", potionMpSprite != null ? potionMpSprite : potionSprite, "Poción de Maná", "+25 MP", new Color(0.14f, 0.71f, 0.99f), font);

            // Back button
            Button potionsBackBtn = CreateBackButton(potionsPanelGo.transform, "Btn_BackPotions", backSprite, font, new Vector2(300f, -12f));

            // Initially hide submenus
            skillsPanelGo.SetActive(false);
            potionsPanelGo.SetActive(false);
            mainBarGo.SetActive(true);

            // Wire CombatActionMenuUI
            CombatActionMenuUI menuScript = menuGo.GetComponent<CombatActionMenuUI>();
            if (menuScript == null) menuScript = menuGo.AddComponent<CombatActionMenuUI>();

            SerializedObject mSo = new SerializedObject(menuScript);
            mSo.FindProperty("mainActionBar").objectReferenceValue = mainBarGo;
            mSo.FindProperty("swordSkillsButton").objectReferenceValue = swordBtn;
            mSo.FindProperty("potionButton").objectReferenceValue = potionBtn;
            mSo.FindProperty("fleeButton").objectReferenceValue = fleeBtn;

            mSo.FindProperty("skillsPanel").objectReferenceValue = skillsPanelGo;
            mSo.FindProperty("skillsContainer").objectReferenceValue = scRect;
            mSo.FindProperty("skillsBackButton").objectReferenceValue = skillsBackBtn;

            mSo.FindProperty("potionsPanel").objectReferenceValue = potionsPanelGo;
            mSo.FindProperty("healthPotionButton").objectReferenceValue = hpPotionBtn;
            mSo.FindProperty("manaPotionButton").objectReferenceValue = mpPotionBtn;
            mSo.FindProperty("potionsBackButton").objectReferenceValue = potionsBackBtn;

            mSo.ApplyModifiedProperties();

            return menuScript;
        }

        private static void BuildParryVisuals(Transform canvas, Sprite redDotSprite, TMP_FontAsset font)
        {
            Transform existing = canvas.Find("ParryVisuals");
            GameObject parryGo;
            if (existing != null)
            {
                parryGo = existing.gameObject;
            }
            else
            {
                parryGo = new GameObject("ParryVisuals", typeof(RectTransform));
                parryGo.transform.SetParent(canvas, false);
            }

            RectTransform rootRect = parryGo.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;

            // 1. Red Dot Container ("Punto Rojo")
            GameObject dotRootGo = GetOrCreateChild(parryGo, "ParryRedDot", typeof(RectTransform));
            RectTransform dotRect = dotRootGo.GetComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.anchoredPosition = new Vector2(-160f, -80f);
            dotRect.sizeDelta = new Vector2(95f, 95f);

            // Red Dot Image
            GameObject dotImgGo = GetOrCreateChild(dotRootGo, "DotImage", typeof(Image), typeof(Button));
            RectTransform imgRect = dotImgGo.GetComponent<RectTransform>();
            imgRect.anchorMin = Vector2.zero;
            imgRect.anchorMax = Vector2.one;
            imgRect.sizeDelta = Vector2.zero;
            Image dotImg = dotImgGo.GetComponent<Image>();
            dotImg.sprite = redDotSprite;
            dotImg.color = new Color(1f, 0.1f, 0.15f, 1f);
            dotImg.preserveAspect = true;
            Button dotBtn = dotImgGo.GetComponent<Button>();

            // Text Prompt
            GameObject textGo = GetOrCreateChild(dotRootGo, "PromptText", typeof(TextMeshProUGUI));
            RectTransform tr = textGo.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.5f, 1f);
            tr.anchorMax = new Vector2(0.5f, 1f);
            tr.pivot = new Vector2(0.5f, 0f);
            tr.anchoredPosition = new Vector2(0f, 6f);
            tr.sizeDelta = new Vector2(220f, 50f);
            TextMeshProUGUI promptTmp = textGo.GetComponent<TextMeshProUGUI>();
            if (font != null) promptTmp.font = font;
            promptTmp.fontSize = 18;
            promptTmp.fontStyle = FontStyles.Bold;
            promptTmp.alignment = TextAlignmentOptions.Center;
            promptTmp.text = "<b><color=#FF2233>● ¡PARRY!</color></b>\n<size=14><color=#FFFF88>[Z / Clic / Espacio]</color></size>";
            promptTmp.raycastTarget = false;

            dotRootGo.SetActive(false);

            // 2. Shield Flash
            GameObject shieldGo = GetOrCreateChild(parryGo, "ShieldFlash", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            RectTransform sRect = shieldGo.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.5f, 0.5f);
            sRect.anchorMax = new Vector2(0.5f, 0.5f);
            sRect.pivot = new Vector2(0.5f, 0.5f);
            sRect.anchoredPosition = new Vector2(-160f, -80f);
            sRect.sizeDelta = new Vector2(150f, 150f);
            Image sImg = shieldGo.GetComponent<Image>();
            sImg.sprite = redDotSprite;
            sImg.color = new Color(1f, 0.9f, 0.3f, 0.85f);
            shieldGo.SetActive(false);

            // 3. Controller component
            CombatParryVisualController parryCtrl = parryGo.GetComponent<CombatParryVisualController>();
            if (parryCtrl == null) parryCtrl = parryGo.AddComponent<CombatParryVisualController>();

            Transform platforms = canvas.Find("Platforms");
            RectTransform heroP = platforms != null ? platforms.Find("HeroPlatform") as RectTransform : null;
            RectTransform enemyP = platforms != null ? platforms.Find("EnemyPlatform") as RectTransform : null;
            CombatController combatCtrl = Object.FindFirstObjectByType<CombatController>();

            parryCtrl.SetupComponents(
                combatCtrl,
                enemyP,
                heroP,
                dotRootGo,
                dotImg,
                promptTmp,
                dotBtn,
                shieldGo
            );
        }

        private static Button CreateActionCardButton(Transform parent, string name, Sprite icon, string label, Color textColor, TMP_FontAsset font)
        {
            GameObject btnGo = GetOrCreateChild(parent.gameObject, name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform rect = btnGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(190f, 140f);

            LayoutElement le = btnGo.GetComponent<LayoutElement>();
            le.preferredWidth = 190f;
            le.preferredHeight = 140f;

            Image bg = btnGo.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.12f, 0.20f, 0.95f);

            Button btn = btnGo.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.20f, 0.30f, 0.48f, 1f);
            cb.pressedColor = new Color(0.04f, 0.07f, 0.12f, 1f);
            btn.colors = cb;

            // Icon
            GameObject iconGo = GetOrCreateChild(btnGo, "Icon", typeof(Image));
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 14f);
            iconRect.sizeDelta = new Vector2(80f, 80f);
            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Label
            GameObject labelGo = GetOrCreateChild(btnGo, "Label", typeof(TextMeshProUGUI));
            RectTransform lblRect = labelGo.GetComponent<RectTransform>();
            lblRect.anchorMin = new Vector2(0f, 0f);
            lblRect.anchorMax = new Vector2(1f, 0f);
            lblRect.pivot = new Vector2(0.5f, 0f);
            lblRect.anchoredPosition = new Vector2(0f, 10f);
            lblRect.sizeDelta = new Vector2(0f, 26f);
            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = 17;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = label;
            tmp.color = textColor;
            tmp.raycastTarget = false;

            return btn;
        }

        private static Button CreateBackButton(Transform parent, string name, Sprite backIcon, TMP_FontAsset font, Vector2 pos)
        {
            GameObject btnGo = GetOrCreateChild(parent.gameObject, name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = btnGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(120f, 55f);

            Image bg = btnGo.GetComponent<Image>();
            bg.color = new Color(0.24f, 0.12f, 0.14f, 0.95f);

            Button btn = btnGo.GetComponent<Button>();

            // Icon
            GameObject iconGo = GetOrCreateChild(btnGo, "Icon", typeof(Image));
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(12f, 0f);
            iconRect.sizeDelta = new Vector2(30f, 30f);
            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = backIcon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Label
            GameObject labelGo = GetOrCreateChild(btnGo, "Label", typeof(TextMeshProUGUI));
            RectTransform lblRect = labelGo.GetComponent<RectTransform>();
            lblRect.anchorMin = new Vector2(0f, 0f);
            lblRect.anchorMax = new Vector2(1f, 1f);
            lblRect.offsetMin = new Vector2(45f, 0f);
            lblRect.offsetMax = new Vector2(-5f, 0f);
            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = 15;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.text = "Volver";
            tmp.color = new Color(1f, 0.75f, 0.75f);
            tmp.raycastTarget = false;

            return btn;
        }

        private static Button CreatePotionButton(Transform parent, string name, Sprite sprite, string title, string effect, Color effectColor, TMP_FontAsset font)
        {
            GameObject btnGo = GetOrCreateChild(parent.gameObject, name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = btnGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(230f, 65f);

            Image bg = btnGo.GetComponent<Image>();
            bg.color = new Color(0.11f, 0.16f, 0.25f, 0.95f);

            Button btn = btnGo.GetComponent<Button>();

            // Icon
            GameObject iconGo = GetOrCreateChild(btnGo, "Icon", typeof(Image));
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(10f, 0f);
            iconRect.sizeDelta = new Vector2(45f, 45f);
            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = sprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Label
            GameObject labelGo = GetOrCreateChild(btnGo, "Label", typeof(TextMeshProUGUI));
            RectTransform lblRect = labelGo.GetComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.offsetMin = new Vector2(65f, 5f);
            lblRect.offsetMax = new Vector2(-10f, -5f);
            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = 15;
            tmp.richText = true;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            string hexColor = ColorUtility.ToHtmlStringRGB(effectColor);
            tmp.text = $"<b>{title}</b>\n<size=12><color=#{hexColor}>{effect}</color></size>";
            tmp.raycastTarget = false;

            return btn;
        }

        private static GameObject GetOrCreateSlider(GameObject parent, string name, Vector2 pos, Vector2 size, Color fillColor, TMP_FontAsset font, string defaultText, out TextMeshProUGUI numericText, bool isRightAligned = false)
        {
            GameObject sliderGo = GetOrCreateChild(parent, name, typeof(RectTransform), typeof(Slider));
            RectTransform rect = sliderGo.GetComponent<RectTransform>();
            rect.anchorMin = isRightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rect.anchorMax = isRightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rect.pivot = isRightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Slider slider = sliderGo.GetComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            // Background
            GameObject bgGo = GetOrCreateChild(sliderGo, "Background", typeof(Image));
            RectTransform bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.1f, 0.12f, 0.18f, 0.9f);

            // Fill Area
            GameObject fillAreaGo = GetOrCreateChild(sliderGo, "Fill Area", typeof(RectTransform));
            RectTransform faRect = fillAreaGo.GetComponent<RectTransform>();
            faRect.anchorMin = Vector2.zero;
            faRect.anchorMax = Vector2.one;
            faRect.sizeDelta = Vector2.zero;

            // Fill
            GameObject fillGo = GetOrCreateChild(fillAreaGo, "Fill", typeof(Image));
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            Image fillImg = fillGo.GetComponent<Image>();
            fillImg.color = fillColor;

            slider.fillRect = fillRect;
            slider.targetGraphic = fillImg;
            slider.value = 1f;

            // Numeric Label overlay
            GameObject textGo = GetOrCreateChild(sliderGo, "NumericText", typeof(TextMeshProUGUI));
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            numericText = textGo.GetComponent<TextMeshProUGUI>();
            if (font != null) numericText.font = font;
            numericText.fontSize = 13;
            numericText.fontStyle = FontStyles.Bold;
            numericText.alignment = TextAlignmentOptions.Center;
            numericText.text = defaultText;
            numericText.color = Color.white;
            numericText.raycastTarget = false;

            return sliderGo;
        }

        private static GameObject GetOrCreateChild(GameObject parent, string name, params System.Type[] types)
        {
            Transform t = parent.transform.Find(name);
            if (t != null)
            {
                return t.gameObject;
            }

            GameObject go = new GameObject(name, types);
            go.transform.SetParent(parent.transform, false);
            return go;
        }
    }
}
#endif
