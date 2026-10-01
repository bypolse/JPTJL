#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using JPTJL.Combat;
using JPTJL.Data;
using JPTJL.Timing;

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

            // 1. Player Stats
            CharacterStatsData playerStats = ScriptableObject.CreateInstance<CharacterStatsData>();
            SetSerializedField(playerStats, "characterName", "Hero");
            SetSerializedField(playerStats, "isPlayer", true);
            SetSerializedField(playerStats, "maxHealth", 120);
            SetSerializedField(playerStats, "baseAttack", 20);
            SetSerializedField(playerStats, "baseDefense", 8);
            SetSerializedField(playerStats, "speed", 12f);
            SetSerializedField(playerStats, "criticalChance", 0.15f);
            SetSerializedField(playerStats, "criticalMultiplier", 1.5f);
            CreateOrUpdateAsset(playerStats, $"{FolderPath}/HeroStats.asset");

            // 2. Enemy Stats
            CharacterStatsData enemyStats = ScriptableObject.CreateInstance<CharacterStatsData>();
            SetSerializedField(enemyStats, "characterName", "Goblin Rogue");
            SetSerializedField(enemyStats, "isPlayer", false);
            SetSerializedField(enemyStats, "maxHealth", 85);
            SetSerializedField(enemyStats, "baseAttack", 16);
            SetSerializedField(enemyStats, "baseDefense", 4);
            SetSerializedField(enemyStats, "speed", 10f);
            SetSerializedField(enemyStats, "criticalChance", 0.05f);
            SetSerializedField(enemyStats, "criticalMultiplier", 1.3f);
            CreateOrUpdateAsset(enemyStats, $"{FolderPath}/GoblinStats.asset");

            // 3. Quick Slash Skill (Fast QTE)
            SkillData slashSkill = ScriptableObject.CreateInstance<SkillData>();
            SetSerializedField(slashSkill, "skillId", "skill_quick_slash");
            SetSerializedField(slashSkill, "skillName", "Quick Slash");
            SetSerializedField(slashSkill, "description", "A swift sword swipe with a tight timing window.");
            SetSerializedField(slashSkill, "basePower", 22);
            SetSerializedField(slashSkill, "attackScaling", 1.1f);
            SetSerializedField(slashSkill, "requiresQTE", true);
            SetSerializedField(slashSkill, "timingConfig", new TimingWindowConfig(0.85f, 0.50f, 0.05f, 0.15f, false));
            SetSerializedField(slashSkill, "missMultiplier", 0.4f);
            SetSerializedField(slashSkill, "goodMultiplier", 1.15f);
            SetSerializedField(slashSkill, "perfectMultiplier", 1.6f);
            CreateOrUpdateAsset(slashSkill, $"{FolderPath}/Skill_QuickSlash.asset");

            // 4. Heavy Smash Skill (Moderate QTE, high damage)
            SkillData heavySmash = ScriptableObject.CreateInstance<SkillData>();
            SetSerializedField(heavySmash, "skillId", "skill_heavy_smash");
            SetSerializedField(heavySmash, "skillName", "Heavy Smash");
            SetSerializedField(heavySmash, "description", "A devastating windup hammer blow.");
            SetSerializedField(heavySmash, "basePower", 40);
            SetSerializedField(heavySmash, "attackScaling", 1.4f);
            SetSerializedField(heavySmash, "requiresQTE", true);
            SetSerializedField(heavySmash, "timingConfig", new TimingWindowConfig(1.3f, 0.85f, 0.07f, 0.20f, false));
            SetSerializedField(heavySmash, "missMultiplier", 0.3f);
            SetSerializedField(heavySmash, "goodMultiplier", 1.2f);
            SetSerializedField(heavySmash, "perfectMultiplier", 1.8f);
            CreateOrUpdateAsset(heavySmash, $"{FolderPath}/Skill_HeavySmash.asset");

            // 5. Enemy Claw Bite
            SkillData enemyBite = ScriptableObject.CreateInstance<SkillData>();
            SetSerializedField(enemyBite, "skillId", "skill_enemy_claw");
            SetSerializedField(enemyBite, "skillName", "Ravage");
            SetSerializedField(enemyBite, "description", "An aggressive pounce from the enemy.");
            SetSerializedField(enemyBite, "basePower", 18);
            SetSerializedField(enemyBite, "attackScaling", 1.0f);
            SetSerializedField(enemyBite, "requiresQTE", false);
            CreateOrUpdateAsset(enemyBite, $"{FolderPath}/Skill_EnemyRavage.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[JPTJL Combat]</color> Sample combat data assets generated successfully in Assets/JPTJL/Data!");
        }

        [MenuItem("Tools/JPTJL/2. Setup Combat Rig in Active Scene", false, 11)]
        public static void SetupCombatScene()
        {
            GenerateSampleData();

            CharacterStatsData heroStats = AssetDatabase.LoadAssetAtPath<CharacterStatsData>($"{FolderPath}/HeroStats.asset");
            CharacterStatsData goblinStats = AssetDatabase.LoadAssetAtPath<CharacterStatsData>($"{FolderPath}/GoblinStats.asset");
            SkillData quickSlash = AssetDatabase.LoadAssetAtPath<SkillData>($"{FolderPath}/Skill_QuickSlash.asset");
            SkillData heavySmash = AssetDatabase.LoadAssetAtPath<SkillData>($"{FolderPath}/Skill_HeavySmash.asset");
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

            // Wire controller serialized fields
            SerializedObject ctrlSo = new SerializedObject(controller);
            ctrlSo.FindProperty("defaultPlayerStats").objectReferenceValue = heroStats;
            ctrlSo.FindProperty("defaultEnemyStats").objectReferenceValue = goblinStats;
            ctrlSo.FindProperty("defaultPlayerSkill").objectReferenceValue = quickSlash;
            ctrlSo.FindProperty("defaultEnemySkill").objectReferenceValue = enemyRavage;
            ctrlSo.FindProperty("autoStartBattle").boolValue = true;
            ctrlSo.ApplyModifiedProperties();

            // Wire adapter serialized fields
            SerializedObject adaptSo = new SerializedObject(adapter);
            adaptSo.FindProperty("combatController").objectReferenceValue = controller;
            SerializedProperty skillsProp = adaptSo.FindProperty("playerSkills");
            skillsProp.arraySize = 2;
            skillsProp.GetArrayElementAtIndex(0).objectReferenceValue = quickSlash;
            skillsProp.GetArrayElementAtIndex(1).objectReferenceValue = heavySmash;
            adaptSo.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Selection.activeGameObject = combatGo;
            Debug.Log("<color=green>[JPTJL Combat]</color> CombatSystem rig setup completed! Press PLAY to test combat.");
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
                    case SerializedPropertyType.Float:
                        prop.floatValue = (float)value;
                        break;
                    case SerializedPropertyType.Generic:
                        if (value is TimingWindowConfig config)
                        {
                            prop.FindPropertyRelative("totalDuration").floatValue = config.TotalDuration;
                            prop.FindPropertyRelative("targetTime").floatValue = config.TargetTime;
                            prop.FindPropertyRelative("perfectThreshold").floatValue = config.PerfectThreshold;
                            prop.FindPropertyRelative("goodThreshold").floatValue = config.GoodThreshold;
                            prop.FindPropertyRelative("useUnscaledTime").boolValue = config.UseUnscaledTime;
                        }
                        break;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
