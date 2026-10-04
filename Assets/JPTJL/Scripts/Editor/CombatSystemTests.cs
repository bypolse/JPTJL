using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using JPTJL.Combat;
using JPTJL.Data;
using JPTJL.Timing;
using JPTJL.Damage;

namespace JPTJL.Tests
{
    public class CombatSystemTests
    {
        [Test]
        public void Test_Shield_Absorbs_Damage_Before_Health()
        {
            // Arrange
            CharacterStatsData stats = ScriptableObject.CreateInstance<CharacterStatsData>();
            var so = new UnityEditor.SerializedObject(stats);
            so.FindProperty("maxHealth").intValue = 100;
            so.FindProperty("baseDefense").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();

            CombatParticipant participant = new CombatParticipant(stats);
            Assert.AreEqual(100, participant.CurrentHealth);
            Assert.AreEqual(0, participant.CurrentShield);

            // Act 1: Grant 10 shield (Barrera Escudo)
            participant.AddShield(10);
            Assert.AreEqual(10, participant.CurrentShield);

            // Act 2: Receive 6 damage (absorbed entirely by shield)
            participant.TakeDamage(6);
            Assert.AreEqual(4, participant.CurrentShield, "Shield should absorb the 6 damage, leaving 4.");
            Assert.AreEqual(100, participant.CurrentHealth, "Health should remain intact while shield absorbs.");

            // Act 3: Receive 10 damage (4 absorbed by shield, 6 to health)
            participant.TakeDamage(10);
            Assert.AreEqual(0, participant.CurrentShield, "Shield should be depleted to 0.");
            Assert.AreEqual(94, participant.CurrentHealth, "Remaining 6 damage must reduce HP from 100 to 94.");
        }

        [Test]
        public void Test_Mana_Validation_And_Consumption()
        {
            // Arrange
            CharacterStatsData stats = ScriptableObject.CreateInstance<CharacterStatsData>();
            var so = new UnityEditor.SerializedObject(stats);
            so.FindProperty("maxMana").intValue = 10;
            so.ApplyModifiedPropertiesWithoutUndo();

            CombatParticipant participant = new CombatParticipant(stats);
            Assert.AreEqual(10, participant.CurrentMana);

            // Act & Assert 1: Insufficient mana
            bool canAffordHeavy = participant.HasMana(14); // Ataque Pesado requires 14 MP
            Assert.IsFalse(canAffordHeavy, "Should return false for 14 MP when participant only has 10 MP.");

            bool consumed = participant.ConsumeMana(14);
            Assert.IsFalse(consumed, "ConsumeMana should return false and block action.");
            Assert.AreEqual(10, participant.CurrentMana, "Mana should not be deducted if insufficient.");

            // Act & Assert 2: Sufficient mana (Golpe Básico = 5 MP)
            bool canAffordBasic = participant.HasMana(5);
            Assert.IsTrue(canAffordBasic);

            bool consumedBasic = participant.ConsumeMana(5);
            Assert.IsTrue(consumedBasic);
            Assert.AreEqual(5, participant.CurrentMana, "Mana should be reduced from 10 to 5.");
        }

        [Test]
        public void Test_QTE_Correct_Key_Applies_20_Percent_Bonus_And_Perfect_Feedback()
        {
            KeyQTEEngine engine = new KeyQTEEngine();
            List<QTEHitResult> resolvedHits = null;

            engine.StartSequence(hits: 1, duration: 1.2f, results =>
            {
                resolvedHits = results;
            });

            Assert.IsTrue(engine.IsActive);
            KeyCode expectedKey = engine.CurrentTargetKey;

            // Submit the correct expected key
            engine.SubmitKeyInput(expectedKey);

            Assert.IsFalse(engine.IsActive);
            Assert.IsNotNull(resolvedHits);
            Assert.AreEqual(1, resolvedHits.Count);

            var hit = resolvedHits[0];
            Assert.IsTrue(hit.IsSuccess);
            Assert.AreEqual(1.20f, hit.DamageMultiplier, 0.001f, "Success should apply +20% extra damage (1.20x).");
            Assert.AreEqual("PERFECT!", hit.FeedbackText);
            Assert.AreEqual(TimingResult.Perfect, hit.Result);
        }

        [Test]
        public void Test_QTE_Wrong_Key_Applies_Base_Damage_And_Failure_Feedback()
        {
            KeyQTEEngine engine = new KeyQTEEngine();
            List<QTEHitResult> resolvedHits = null;

            engine.StartSequence(hits: 1, duration: 1.2f, results =>
            {
                resolvedHits = results;
            });

            KeyCode expectedKey = engine.CurrentTargetKey;
            // Deliberately pick a wrong key
            KeyCode wrongKey = (expectedKey == KeyCode.W) ? KeyCode.S : KeyCode.W;

            engine.SubmitKeyInput(wrongKey);

            Assert.IsNotNull(resolvedHits);
            Assert.AreEqual(1, resolvedHits.Count);

            var hit = resolvedHits[0];
            Assert.IsFalse(hit.IsSuccess);
            Assert.AreEqual(1.0f, hit.DamageMultiplier, 0.001f, "Failure should apply only base damage (1.0x).");
            Assert.AreEqual("FALLO", hit.FeedbackText);
            Assert.AreEqual(TimingResult.Miss, hit.Result);
        }

        [Test]
        public void Test_MultiHit_DobleEstocada_Resolves_Both_Hits_Independently()
        {
            KeyQTEEngine engine = new KeyQTEEngine();
            List<QTEHitResult> resolvedHits = null;

            // Doble Estocada has 2 hits
            engine.StartSequence(hits: 2, duration: 1.2f, results =>
            {
                resolvedHits = results;
            });

            Assert.AreEqual(0, engine.CurrentHitIndex);
            Assert.AreEqual(2, engine.TotalHits);

            // Hit 1: submit correct key
            KeyCode hit1Key = engine.CurrentTargetKey;
            engine.SubmitKeyInput(hit1Key);

            // Engine should now be on hit 2
            Assert.IsTrue(engine.IsActive, "Engine should stay active for hit 2.");
            Assert.AreEqual(1, engine.CurrentHitIndex);

            // Hit 2: submit wrong key
            KeyCode hit2Expected = engine.CurrentTargetKey;
            KeyCode hit2Wrong = (hit2Expected == KeyCode.Z) ? KeyCode.X : KeyCode.Z;
            engine.SubmitKeyInput(hit2Wrong);

            // Now sequence should complete
            Assert.IsFalse(engine.IsActive);
            Assert.IsNotNull(resolvedHits);
            Assert.AreEqual(2, resolvedHits.Count);

            Assert.IsTrue(resolvedHits[0].IsSuccess, "Hit 1 was correct -> SUCCESS (+20%)");
            Assert.AreEqual(1.20f, resolvedHits[0].DamageMultiplier, 0.001f);

            Assert.IsFalse(resolvedHits[1].IsSuccess, "Hit 2 was wrong -> FALLO (1.0x base)");
            Assert.AreEqual(1.0f, resolvedHits[1].DamageMultiplier, 0.001f);
        }

        [Test]
        public void Test_Health_Potion_Restores_Health_Capped_At_Max()
        {
            CharacterStatsData stats = ScriptableObject.CreateInstance<CharacterStatsData>();
            var so = new UnityEditor.SerializedObject(stats);
            so.FindProperty("maxHealth").intValue = 100;
            so.ApplyModifiedPropertiesWithoutUndo();

            CombatParticipant participant = new CombatParticipant(stats);
            participant.TakeDamage(50); // HP is now 50
            Assert.AreEqual(50, participant.CurrentHealth);

            // Act 1: Heal 35 (Health Potion)
            participant.Heal(35);
            Assert.AreEqual(85, participant.CurrentHealth);

            // Act 2: Heal another 35 (should cap at max 100)
            participant.Heal(35);
            Assert.AreEqual(100, participant.CurrentHealth);
        }

        [Test]
        public void Test_Mana_Potion_Restores_Mana_Capped_At_Max()
        {
            CharacterStatsData stats = ScriptableObject.CreateInstance<CharacterStatsData>();
            var so = new UnityEditor.SerializedObject(stats);
            so.FindProperty("maxMana").intValue = 50;
            so.ApplyModifiedPropertiesWithoutUndo();

            CombatParticipant participant = new CombatParticipant(stats);
            participant.ConsumeMana(40); // MP is now 10
            Assert.AreEqual(10, participant.CurrentMana);

            // Act 1: Restore 25 (Mana Potion)
            participant.RestoreMana(25);
            Assert.AreEqual(35, participant.CurrentMana);

            // Act 2: Restore another 25 (should cap at max 50)
            participant.RestoreMana(25);
            Assert.AreEqual(50, participant.CurrentMana);
        }

        [Test]
        public void Test_Perfect_Parry_Deflects_All_Enemy_Damage_To_Zero_And_Reflects_Counter_Damage()
        {
            CharacterStatsData attackerStats = ScriptableObject.CreateInstance<CharacterStatsData>();
            var soAttacker = new UnityEditor.SerializedObject(attackerStats);
            soAttacker.FindProperty("baseAttack").intValue = 20;
            soAttacker.ApplyModifiedPropertiesWithoutUndo();

            CharacterStatsData defenderStats = ScriptableObject.CreateInstance<CharacterStatsData>();
            var soDefender = new UnityEditor.SerializedObject(defenderStats);
            soDefender.FindProperty("baseDefense").intValue = 0;
            soDefender.FindProperty("maxHealth").intValue = 100;
            soDefender.ApplyModifiedPropertiesWithoutUndo();

            CombatParticipant attacker = new CombatParticipant(attackerStats);
            CombatParticipant defender = new CombatParticipant(defenderStats);

            SkillData attackSkill = ScriptableObject.CreateInstance<SkillData>();
            var soSkill = new UnityEditor.SerializedObject(attackSkill);
            soSkill.FindProperty("baseDamage").intValue = 20;
            soSkill.ApplyModifiedPropertiesWithoutUndo();

            // Act: Calculate damage with Perfect Parry
            DamageResult result = DamageCalculator.Calculate(
                attacker: attackerStats,
                defender: defenderStats,
                skill: attackSkill,
                attackTiming: TimingResult.Good,
                defenseAction: DefenseType.Parry,
                defenseTiming: TimingResult.Perfect
            );

            // Assert: 100% damage deflected away from player (0 damage taken!)
            Assert.AreEqual(0, result.FinalDamage, "Perfect Parry must deflect 100% of the damage so the player takes 0 damage.");
            Assert.IsTrue(result.WasParried);
            Assert.Greater(result.CounterDamage, 0, "Counter damage must be reflected back against the enemy attacker.");
        }

        [Test]
        public void Test_Parry_Window_Is_200_Milliseconds()
        {
            // Total parry window: targetTime 0.50s, 0.10s perfect threshold => 0.20s = 200 milliseconds total window
            TimingWindowConfig config = new TimingWindowConfig(0.85f, 0.50f, 0.10f, 0.20f, false);
            
            float windowDuration = (config.TargetTime + config.PerfectThreshold) - (config.TargetTime - config.PerfectThreshold);
            Assert.AreEqual(0.20f, windowDuration, 0.001f, "Parry window duration must be exactly 200 milliseconds (0.20s)");

            // Hit 90ms before impact (inside 200ms window) => Perfect
            Assert.AreEqual(TimingResult.Perfect, config.Evaluate(0.41f));

            // Hit 90ms after impact (inside 200ms window) => Perfect
            Assert.AreEqual(TimingResult.Perfect, config.Evaluate(0.59f));

            // Hit right at impact => Perfect
            Assert.AreEqual(TimingResult.Perfect, config.Evaluate(0.50f));

            // Hit outside 200ms window (e.g. 150ms before impact) => Not Perfect
            Assert.AreNotEqual(TimingResult.Perfect, config.Evaluate(0.35f));
        }
    }
}
