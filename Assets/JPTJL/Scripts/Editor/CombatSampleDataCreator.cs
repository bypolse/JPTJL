#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using JPTJL.Combat;
using JPTJL.Data;
using JPTJL.Timing;
using JPTJL.UI;

namespace JPTJL.Editor
{
    public static class CombatSampleDataCreator
    {
        private const string FolderPath = "Assets/JPTJL/Data";

        [MenuItem("Tools/JPTJL/1. Generate Sample Combat Data", false, 10)]
        public static void GenerateSampleData()
        {
            if (!AssetDatabase.IsValidFolder("Assets/JPTJL"))
            {
                AssetDatabase.CreateFolder("Assets", "JPTJL");
            }
            if (!AssetDatabase.IsValidFolder(FolderPath))
            {
                AssetDatabase.CreateFolder("Assets/JPTJL", "Data");
            }

            // 1. Player Stats (Hero)
            CharacterStatsData playerStats = ScriptableObject.CreateInstance<CharacterStatsData>();
            SetSerializedField(playerStats, "characterName", "Hero");
            SetSerializedField(playerStats, "isPlayer", true);
            SetSerializedField(playerStats, "maxHealth", 100);
            SetSerializedField(playerStats, "maxMana", 50);
            SetSerializedField(playerStats, "baseAttack", 0);
            SetSerializedField(playerStats, "baseDefense", 5);
            SetSerializedField(playerStats, "speed", 12f);
            SetSerializedField(playerStats, "criticalChance", 0.05f);
            SetSerializedField(playerStats, "criticalMultiplier", 1.5f);
            CreateOrUpdateAsset(playerStats, $"{FolderPath}/HeroStats.asset");

            // 2. Enemy Stats (Goblin Rogue)
            CharacterStatsData enemyStats = ScriptableObject.CreateInstance<CharacterStatsData>();
            SetSerializedField(enemyStats, "characterName", "Goblin Rogue");
            SetSerializedField(enemyStats, "isPlayer", false);
            SetSerializedField(enemyStats, "maxHealth", 85);
            SetSerializedField(enemyStats, "maxMana", 0);
            SetSerializedField(enemyStats, "baseAttack", 12);
            SetSerializedField(enemyStats, "baseDefense", 4);
            SetSerializedField(enemyStats, "speed", 10f);
            SetSerializedField(enemyStats, "criticalChance", 0.05f);
            SetSerializedField(enemyStats, "criticalMultiplier", 1.3f);
            CreateOrUpdateAsset(enemyStats, $"{FolderPath}/GoblinStats.asset");

            // 3. Habilidad 1: "Golpe Básico" -> Daño base: 10 | Coste de Maná: 5 MP | QTE: 1 tecla
            SkillData skillGolpeBasico = ScriptableObject.CreateInstance<SkillData>();
            SetSerializedField(skillGolpeBasico, "skillId", "skill_golpe_basico");
            SetSerializedField(skillGolpeBasico, "skillName", "Golpe Básico");
            SetSerializedField(skillGolpeBasico, "description", "Ataque rápido y confiable. Daño: 10 | Maná: 5 MP | QTE: 1 tecla.");
            SetSerializedField(skillGolpeBasico, "skillType", 0); // SkillType.Offensive
            SetSerializedField(skillGolpeBasico, "hitCount", 1);
            SetSerializedField(skillGolpeBasico, "basePower", 10);
            SetSerializedField(skillGolpeBasico, "attackScaling", 0.0f);
            SetSerializedField(skillGolpeBasico, "manaCost", 5);
            SetSerializedField(skillGolpeBasico, "requiresQTE", true);
            SetSerializedField(skillGolpeBasico, "qteKeyCount", 1);
            SetSerializedField(skillGolpeBasico, "qteDuration", 1.2f);
            SetSerializedField(skillGolpeBasico, "missMultiplier", 1.0f);
            SetSerializedField(skillGolpeBasico, "goodMultiplier", 1.0f);
            SetSerializedField(skillGolpeBasico, "perfectMultiplier", 1.20f);
            CreateOrUpdateAsset(skillGolpeBasico, $"{FolderPath}/Skill_GolpeBasico.asset");

            // 4. Habilidad 2: "Doble Estocada" -> 2 golpes de 6 de daño base cada uno | Coste de Maná: 8 MP | QTE: 1 tecla independiente por cada golpe
            SkillData skillDobleEstocada = ScriptableObject.CreateInstance<SkillData>();
            SetSerializedField(skillDobleEstocada, "skillId", "skill_doble_estocada");
            SetSerializedField(skillDobleEstocada, "skillName", "Doble Estocada");
            SetSerializedField(skillDobleEstocada, "description", "Dos cortes consecutivos. Daño: 2x6 (12) | Maná: 8 MP | QTE: 2 teclas.");
            SetSerializedField(skillDobleEstocada, "skillType", 0); // SkillType.Offensive
            SetSerializedField(skillDobleEstocada, "hitCount", 2);
            SetSerializedField(skillDobleEstocada, "basePower", 6);
            SetSerializedField(skillDobleEstocada, "attackScaling", 0.0f);
            SetSerializedField(skillDobleEstocada, "manaCost", 8);
            SetSerializedField(skillDobleEstocada, "requiresQTE", true);
            SetSerializedField(skillDobleEstocada, "qteKeyCount", 2);
            SetSerializedField(skillDobleEstocada, "qteDuration", 1.2f);
            SetSerializedField(skillDobleEstocada, "missMultiplier", 1.0f);
            SetSerializedField(skillDobleEstocada, "goodMultiplier", 1.0f);
            SetSerializedField(skillDobleEstocada, "perfectMultiplier", 1.20f);
            CreateOrUpdateAsset(skillDobleEstocada, $"{FolderPath}/Skill_DobleEstocada.asset");

            // 5. Habilidad 3: "Barrera Escudo" -> Otorga 10 de Escudo/Absorción | Coste de Maná: 0 MP
            SkillData skillBarreraEscudo = ScriptableObject.CreateInstance<SkillData>();
            SetSerializedField(skillBarreraEscudo, "skillId", "skill_barrera_escudo");
            SetSerializedField(skillBarreraEscudo, "skillName", "Barrera Escudo");
            SetSerializedField(skillBarreraEscudo, "description", "Crea una barrera protectora que absorbe 10 de daño. Coste: 0 MP.");
            SetSerializedField(skillBarreraEscudo, "skillType", 1); // SkillType.Shield
            SetSerializedField(skillBarreraEscudo, "hitCount", 1);
            SetSerializedField(skillBarreraEscudo, "shieldAmount", 10);
            SetSerializedField(skillBarreraEscudo, "basePower", 0);
            SetSerializedField(skillBarreraEscudo, "attackScaling", 0.0f);
            SetSerializedField(skillBarreraEscudo, "manaCost", 0);
            SetSerializedField(skillBarreraEscudo, "requiresQTE", false);
            CreateOrUpdateAsset(skillBarreraEscudo, $"{FolderPath}/Skill_BarreraEscudo.asset");

            // 6. Habilidad 4: "Ataque Pesado" -> Daño base: 23 | Coste de Maná: 14 MP | QTE: 1 tecla
            SkillData skillAtaquePesado = ScriptableObject.CreateInstance<SkillData>();
            SetSerializedField(skillAtaquePesado, "skillId", "skill_ataque_pesado");
            SetSerializedField(skillAtaquePesado, "skillName", "Ataque Pesado");
            SetSerializedField(skillAtaquePesado, "description", "Golpe devastador de alto impacto. Daño: 23 | Maná: 14 MP | QTE: 1 tecla.");
            SetSerializedField(skillAtaquePesado, "skillType", 0); // SkillType.Offensive
            SetSerializedField(skillAtaquePesado, "hitCount", 1);
            SetSerializedField(skillAtaquePesado, "basePower", 23);
            SetSerializedField(skillAtaquePesado, "attackScaling", 0.0f);
            SetSerializedField(skillAtaquePesado, "manaCost", 14);
            SetSerializedField(skillAtaquePesado, "requiresQTE", true);
            SetSerializedField(skillAtaquePesado, "qteKeyCount", 1);
            SetSerializedField(skillAtaquePesado, "qteDuration", 1.2f);
            SetSerializedField(skillAtaquePesado, "missMultiplier", 1.0f);
            SetSerializedField(skillAtaquePesado, "goodMultiplier", 1.0f);
            SetSerializedField(skillAtaquePesado, "perfectMultiplier", 1.20f);
            CreateOrUpdateAsset(skillAtaquePesado, $"{FolderPath}/Skill_AtaquePesado.asset");

            // 7. Habilidad Enemiga: "Zarpazo Goblin"
            SkillData enemyBite = ScriptableObject.CreateInstance<SkillData>();
            SetSerializedField(enemyBite, "skillId", "skill_enemy_claw");
            SetSerializedField(enemyBite, "skillName", "Zarpazo Goblin");
            SetSerializedField(enemyBite, "description", "Ataque rápido del enemigo.");
            SetSerializedField(enemyBite, "basePower", 15);
            SetSerializedField(enemyBite, "attackScaling", 0.0f);
            SetSerializedField(enemyBite, "requiresQTE", false);
            CreateOrUpdateAsset(enemyBite, $"{FolderPath}/Skill_EnemyRavage.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[JPTJL Combat]</color> ¡Datos de combate y 4 habilidades generadas correctamente!");
        }

        [MenuItem("Tools/JPTJL/2. Setup Combat Rig in Active Scene", false, 11)]
        public static void SetupCombatScene()
        {
            GenerateSampleData();

            CharacterStatsData heroStats = AssetDatabase.LoadAssetAtPath<CharacterStatsData>($"{FolderPath}/HeroStats.asset");
            CharacterStatsData goblinStats = AssetDatabase.LoadAssetAtPath<CharacterStatsData>($"{FolderPath}/GoblinStats.asset");

            SkillData golpeBasico = AssetDatabase.LoadAssetAtPath<SkillData>($"{FolderPath}/Skill_GolpeBasico.asset");
            SkillData dobleEstocada = AssetDatabase.LoadAssetAtPath<SkillData>($"{FolderPath}/Skill_DobleEstocada.asset");
            SkillData barreraEscudo = AssetDatabase.LoadAssetAtPath<SkillData>($"{FolderPath}/Skill_BarreraEscudo.asset");
            SkillData ataquePesado = AssetDatabase.LoadAssetAtPath<SkillData>($"{FolderPath}/Skill_AtaquePesado.asset");
            SkillData enemyRavage = AssetDatabase.LoadAssetAtPath<SkillData>($"{FolderPath}/Skill_EnemyRavage.asset");

            GameObject combatGo = GameObject.Find("CombatSystem");
            if (combatGo == null)
            {
                combatGo = new GameObject("CombatSystem");
                Undo.RegisterCreatedObjectUndo(combatGo, "Create CombatSystem GameObject");
            }

            CombatController controller = combatGo.GetComponent<CombatController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<CombatController>(combatGo);
            }

            CombatInputAdapter adapter = combatGo.GetComponent<CombatInputAdapter>();
            if (adapter == null)
            {
                adapter = Undo.AddComponent<CombatInputAdapter>(combatGo);
            }

            CombatDebugUI debugUI = combatGo.GetComponent<CombatDebugUI>();
            if (debugUI == null)
            {
                debugUI = Undo.AddComponent<CombatDebugUI>(combatGo);
            }

            // Wire controller serialized fields
            SerializedObject ctrlSo = new SerializedObject(controller);
            ctrlSo.FindProperty("defaultPlayerStats").objectReferenceValue = heroStats;
            ctrlSo.FindProperty("defaultEnemyStats").objectReferenceValue = goblinStats;
            ctrlSo.FindProperty("defaultPlayerSkill").objectReferenceValue = golpeBasico;
            ctrlSo.FindProperty("defaultEnemySkill").objectReferenceValue = enemyRavage;
            ctrlSo.FindProperty("autoStartBattle").boolValue = true;
            ctrlSo.ApplyModifiedProperties();

            // Wire adapter serialized fields (4 player skills)
            SerializedObject adaptSo = new SerializedObject(adapter);
            adaptSo.FindProperty("combatController").objectReferenceValue = controller;
            SerializedProperty skillsProp = adaptSo.FindProperty("playerSkills");
            skillsProp.arraySize = 4;
            skillsProp.GetArrayElementAtIndex(0).objectReferenceValue = golpeBasico;
            skillsProp.GetArrayElementAtIndex(1).objectReferenceValue = dobleEstocada;
            skillsProp.GetArrayElementAtIndex(2).objectReferenceValue = barreraEscudo;
            skillsProp.GetArrayElementAtIndex(3).objectReferenceValue = ataquePesado;
            adaptSo.ApplyModifiedProperties();

            // Wire debug UI serialized fields
            SerializedObject uiSo = new SerializedObject(debugUI);
            uiSo.FindProperty("combatController").objectReferenceValue = controller;
            SerializedProperty uiSkillsProp = uiSo.FindProperty("skills");
            uiSkillsProp.arraySize = 4;
            uiSkillsProp.GetArrayElementAtIndex(0).objectReferenceValue = golpeBasico;
            uiSkillsProp.GetArrayElementAtIndex(1).objectReferenceValue = dobleEstocada;
            uiSkillsProp.GetArrayElementAtIndex(2).objectReferenceValue = barreraEscudo;
            uiSkillsProp.GetArrayElementAtIndex(3).objectReferenceValue = ataquePesado;
            uiSo.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Selection.activeGameObject = combatGo;
            Debug.Log("<color=green>[JPTJL Combat]</color> ¡CombatSystem configurado con las 4 habilidades y UI interactiva en la escena!");
        }

        private static void CreateOrUpdateAsset(Object asset, string path)
        {
            Object existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
            }
            else
            {
                AssetDatabase.CreateAsset(asset, path);
            }
        }

        private static void SetSerializedField(Object target, string fieldName, object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.String:
                        prop.stringValue = (string)value;
                        break;
                    case SerializedPropertyType.Boolean:
                        prop.boolValue = (bool)value;
                        break;
                    case SerializedPropertyType.Integer:
                        prop.intValue = (int)value;
                        break;
                    case SerializedPropertyType.Enum:
                        prop.enumValueIndex = (int)value;
                        break;
                    case SerializedPropertyType.Float:
                        prop.floatValue = (float)value;
                        break;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
