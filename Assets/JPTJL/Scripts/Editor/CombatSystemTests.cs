using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using JPTJL.Combat;
using JPTJL.Data;
using JPTJL.Timing;

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
    }
}
