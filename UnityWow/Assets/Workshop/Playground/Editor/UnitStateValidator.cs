using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CR
{
    public static class UnitStateValidator
    {
        [MenuItem("CR/Workshop/Validate Unit State (Play Mode)")]
        public static string Validate()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run unit checks in Play Mode.");
            GameObject firstObject = new GameObject("Unit State Validation A");
            GameObject secondObject = new GameObject("Unit State Validation B");
            GameObject playerObject = new GameObject("Unit State Validation Player");
            try
            {
                Unit first = firstObject.AddComponent<Unit>();
                Unit second = secondObject.AddComponent<Unit>();
                Require(first.State.CurrentHealth == 100f && first.State.CurrentMana == 100f && first.State.IsAlive,
                    "Units must initialize with full resources and an explicit alive state.");
                int changes = 0;
                first.m_StateChanged += (previous, next) => changes++;
                UnitRuntimeState snapshot = first.State;
                first.SetHealth(60f);
                Require(snapshot.CurrentHealth == 100f && second.State.CurrentHealth == 100f,
                    "Snapshots and other units must not change with their owner.");
                Require(!first.SetHealth(60f) && changes == 1, "No-op writes must not notify.");
                first.SetMaxHealth(200f);
                Require(first.State.CurrentHealth == 60f, "Increasing capacity must not heal.");
                first.SetMaxHealth(40f);
                Require(first.State.CurrentHealth == 40f, "Reducing capacity must clamp current health.");
                first.SetHealth(float.MaxValue);
                first.SetMana(30f);
                first.SetMaxMana(0f);
                Require(first.State.CurrentHealth == 40f && first.State.CurrentMana == 0f && first.State.ManaNormalized == 0f,
                    "Resources must clamp and zero mana capacity must normalize safely.");
                ExpectInvalid(() => first.SetHealth(float.NaN));
                ExpectInvalid(() => first.SetHealth(-1f));
                ExpectInvalid(() => first.SetMaxHealth(0f));
                ExpectInvalid(() => first.SetMana(float.PositiveInfinity));
                ExpectInvalid(() => first.SetMaxMana(-1f));
                Require(first.State.CurrentHealth == 40f && first.State.CurrentMana == 0f,
                    "Invalid requests must leave state intact.");
                first.SetTarget(second);
                Require(!first.State.IsInCombat, "Selection must not enter combat.");
                first.SetCombatState(true);
                first.ClearTarget();
                Require(first.State.IsInCombat, "Clearing selection must not leave combat.");
                first.SetTarget(second);
                bool observedDeath = false;
                Action<UnitRuntimeState, UnitRuntimeState> observeDeath = (previous, next) =>
                {
                    if (!next.IsAlive)
                    {
                        Require(next.CurrentHealth == 0f && !next.IsInCombat, "Death must publish a consistent snapshot.");
                        observedDeath = true;
                    }
                };
                first.m_StateChanged += observeDeath;
                first.SetHealth(0f);
                Require(observedDeath && !first.SetHealth(20f) && !first.SetCombatState(true),
                    "Ordinary health writes and combat toggles must not revive a dead unit.");
                ExpectInvalid(() => first.Revive(0f));
                first.m_StateChanged -= observeDeath;
                Require(first.Revive(20f) && first.State.CurrentHealth == 20f && first.State.IsAlive,
                    "Revival must explicitly restore life and health together.");
                second.SetHealth(0f);
                Require(first.State.SelectedTarget == second, "A dead target must remain selected.");
                second.enabled = false;
                Require(ReferenceEquals(first.State.SelectedTarget, null) && !first.SetTarget(second),
                    "Disabled targets must be cleared and rejected.");
                second.enabled = true;
                first.SetTarget(second);
                secondObject.SetActive(false);
                Require(ReferenceEquals(first.State.SelectedTarget, null), "Inactive targets must be cleared.");
                secondObject.SetActive(true);
                first.SetTarget(second);
                UnityEngine.Object.DestroyImmediate(second);
                Require(ReferenceEquals(first.State.SelectedTarget, null), "Destroyed components must clear selection.");
                first.SetTarget(first);
                first.SetCombatState(true);
                first.enabled = false;
                first.enabled = true;
                Require(first.State.CurrentHealth == 20f && !first.State.IsInCombat && first.State.SelectedTarget == null,
                    "Disable/enable must preserve resources and release relationships, including self-selection.");
                List<float> notifications = new List<float>();
                Action<UnitRuntimeState, UnitRuntimeState> nestedChange = (previous, next) =>
                {
                    if (next.CurrentHealth == 10f) first.SetHealth(5f);
                };
                Action<UnitRuntimeState, UnitRuntimeState> record = (previous, next) => notifications.Add(next.CurrentHealth);
                first.m_StateChanged += nestedChange;
                first.m_StateChanged += record;
                first.SetHealth(10f);
                first.m_StateChanged -= nestedChange;
                first.m_StateChanged -= record;
                Require(notifications.Count == 2 && notifications[0] == 10f && notifications[1] == 5f,
                    "Nested writes must deliver snapshots in commit order.");
                first.SetMaxMana(50f);
                first.ResetForSpawn();
                Require(first.State.CurrentHealth == 40f && first.State.CurrentMana == 50f && first.State.IsAlive,
                    "Spawn reset must use current capacities.");
                playerObject.SetActive(false);
                PlayerController player = playerObject.AddComponent<PlayerController>();
                Require(player.Unit != null && player.Unit.gameObject == playerObject,
                    "Player controllers must own a Unit on the same object.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(firstObject);
                UnityEngine.Object.DestroyImmediate(secondObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
            string report = "Passed unit initialization, isolation, snapshots, bounds, invalid inputs, life transitions, selection, combat independence, target cleanup, disable/enable, nested notifications, spawn reset and player integration.";
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/UnitStateValidation.txt", report + "\n");
            Debug.Log(report);
            return report;
        }

        private static void ExpectInvalid(Action action)
        {
            try { action(); }
            catch (ArgumentOutOfRangeException) { return; }
            throw new InvalidOperationException("An invalid state value was accepted.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}

