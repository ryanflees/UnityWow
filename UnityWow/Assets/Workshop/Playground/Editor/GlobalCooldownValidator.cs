// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CR
{
	public static class GlobalCooldownValidator
	{
		[MenuItem("CR/Workshop/Validate Global Cooldown")]
		public static string Validate()
		{
			if (EditorApplication.isPlaying) throw new InvalidOperationException("Run GCD unit checks outside Play Mode.");
			GlobalCooldownState clock = new GlobalCooldownState();
			clock.Start(10d, 1.5f);
			Require(clock.GetRemaining(11.4999d) > 0f && clock.GetRemaining(11.5d) == 0f, "GCD must unlock at its exact end time.");
			Require(clock.GetRemaining(100d) == 0f, "Long frames must not leave a negative or stuck timer.");
			clock.Start(100d, 1.5f);
			clock.Start(100.1d, 0.2f);
			clock.Start(100.2d, 0f);
			Require(Mathf.Approximately(clock.GetRemaining(100.5d), 1f) && clock.Duration == 1.5f, "A shorter or zero GCD must not shorten an active GCD.");
			clock.Start(100.5d, 2f);
			Require(clock.GetRemaining(102.5d) == 0f && clock.Duration == 2f, "An independently allowed cast may extend the GCD.");

			Scene scene = EditorSceneManager.NewPreviewScene();
			SpellConfigCollection collection = ScriptableObject.CreateInstance<SpellConfigCollection>();
			try
			{
				SpellDefinition normal = CreateSpell(1, true, true);
				SpellDefinition offGlobalCooldown = CreateSpell(2, false, false);
				SpellDefinition waitsOnly = CreateSpell(3, false, true);
				collection.m_SpellList.AddRange(new[] { normal, offGlobalCooldown, waitsOnly });
				collection.RebuildLookup();
				SpellController first = CreateController(scene, collection);
				SpellController second = CreateController(scene, collection);
				SpellController third = CreateController(scene, collection);
				Require(normal.m_Cooldown.m_GlobalCooldown == 1.5f, "New spells must default to a 1.5-second GCD.");
				Require(Mathf.Approximately(first.m_Attributes.GetGlobalCooldownDuration(1.5f), 1.5f), "Zero haste must preserve the base GCD.");
				second.m_Attributes.m_HastePercent = 50f;
				Require(Mathf.Approximately(second.m_Attributes.GetGlobalCooldownDuration(1.5f), 1f), "50% haste must reduce 1.5 seconds to 1 second.");
				SpellCastResult nestedResult = SpellCastResult.Success;
				Action<SpellDefinition> onRelease = spell => nestedResult = first.TryCast(new SpellCastRequest(1, Quaternion.identity));
				first.m_SpellReleased += onRelease;
				Require(Cast(first, 1) == SpellCastResult.Success && nestedResult == SpellCastResult.GlobalCooldown,
					"The GCD must be committed before release callbacks can submit another cast.");
				first.m_SpellReleased -= onRelease;
				Require(Cast(first, 1) == SpellCastResult.GlobalCooldown && Cast(first, 3) == SpellCastResult.GlobalCooldown, "Affected spells must share the actor's GCD.");
				float remaining = first.GlobalCooldownRemaining;
				Require(Cast(first, 2) == SpellCastResult.Success && Mathf.Approximately(first.GlobalCooldownRemaining, remaining), "Off-GCD casts must preserve the existing timer.");
				first.CancelPresentation();
				first.enabled = false;
				first.enabled = true;
				Require(Cast(first, 1) == SpellCastResult.GlobalCooldown, "Cancellation and disable/enable must not bypass GCD.");
				first.m_Attributes.m_HastePercent = 100f;
				Require(first.GlobalCooldownDuration == 1.5f, "Attribute changes must affect the next GCD, not rewrite the active one.");
				Require(Cast(second, 1) == SpellCastResult.Success && Mathf.Approximately(second.GlobalCooldownDuration, 1f), "Actor GCDs must be independent and apply haste on acceptance.");
				Require(Cast(third, 3) == SpellCastResult.Success && third.GlobalCooldownRemaining == 0f, "A non-triggering spell must not create a GCD.");
				third.m_Presentation = null;
				Require(Cast(third, 1) == SpellCastResult.PresentationUnavailable && third.GlobalCooldownRemaining == 0f, "Failed casts must not start GCD.");
			}
			finally
			{
				EditorSceneManager.ClosePreviewScene(scene);
				foreach (SpellDefinition spell in collection.m_SpellList) UnityEngine.Object.DestroyImmediate(spell);
				UnityEngine.Object.DestroyImmediate(collection);
			}
			string report = "Passed GCD boundary/long-frame timing, default 1.5 seconds, 50% haste, snapshot duration, reentrant rejection, actor isolation, trigger/affected opt-outs, failed casts and cancellation/disable behavior.";
			Directory.CreateDirectory("Temp");
			File.WriteAllText("Temp/GlobalCooldownValidation.txt", report + "\n");
			Debug.Log(report);
			return report;
		}

		private static SpellDefinition CreateSpell(int id, bool triggers, bool affected)
		{
			SpellDefinition spell = ScriptableObject.CreateInstance<SpellDefinition>();
			spell.m_Id = id;
			spell.m_DisplayName = "GCD Validation";
			spell.m_Cast.m_RequireFacingTarget = false;
			spell.m_Cooldown.m_TriggersGlobalCooldown = triggers;
			spell.m_Cooldown.m_IsAffectedByGlobalCooldown = affected;
			return spell;
		}

		private static SpellController CreateController(Scene scene, SpellConfigCollection collection)
		{
			GameObject actor = new GameObject("GCD Validation Actor");
			SceneManager.MoveGameObjectToScene(actor, scene);
			SpellPresentation presentation = actor.AddComponent<SpellPresentation>();
			presentation.m_Character = actor.AddComponent<Character>();
			SpellController controller = actor.AddComponent<SpellController>();
			controller.m_Attributes = actor.AddComponent<ActorAttributes>();
			controller.m_Presentation = presentation;
			controller.m_SpellCollection = collection;
			return controller;
		}

		private static SpellCastResult Cast(SpellController controller, int id)
		{
			return controller.TryCast(new SpellCastRequest(id, Quaternion.identity));
		}

		private static void Require(bool condition, string message)
		{
			if (!condition) throw new InvalidOperationException(message);
		}
	}
}

