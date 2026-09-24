// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CR
{
	public static class SpellFoundationValidator
	{
		[MenuItem("CR/Workshop/Validate Spell Foundation")]
		public static string Validate()
		{
			if (EditorApplication.isPlaying) throw new InvalidOperationException("Run foundation checks outside Play mode.");
			ValidateConfiguration();
			ValidatePresentation();
			string report = "Passed spell validation failures, preview/content distinction, duplicate lookup rejection and rebuild, None/invalid animation no-op, shared presentation, and animation completion at 0.5x speed.";
			Directory.CreateDirectory("Temp");
			File.WriteAllText("Temp/SpellFoundationValidation.txt", report + "\n");
			Debug.Log(report);
			return report;
		}

		private static void ValidateConfiguration()
		{
			SpellDefinition spell = ScriptableObject.CreateInstance<SpellDefinition>();
			SpellDefinition duplicate = ScriptableObject.CreateInstance<SpellDefinition>();
			SpellConfigCollection collection = ScriptableObject.CreateInstance<SpellConfigCollection>();
			try
			{
				spell.m_Id = 1;
				spell.m_DisplayName = "Foundation Test";
				Require(!SpellDefinitionValidator.Validate(spell).Any(issue => issue.m_Severity == SpellValidationSeverity.Error), "An effect-free animation preview must be structurally valid.");
				Require(SpellDefinitionValidator.Validate(spell, true).Count(issue => issue.m_Severity == SpellValidationSeverity.Error) == 2, "Combat content must require effects and an icon.");
				spell.m_Cast.m_Type = SpellCastType.Channeled;
				Require(HasError(spell, "m_Cast.m_ChannelInterval"), "Zero channel intervals must be rejected.");
				spell.m_Cast.m_ChannelDuration = 3f;
				spell.m_Cast.m_ChannelInterval = 1f;
				spell.m_Presentation.m_AnimationType = SpellAnimationType.CastDirected;
				Require(HasError(spell, "m_Presentation.m_AnimationType"), "Channels must not enter cast preparation loops.");
				spell.m_Presentation.m_AnimationType = SpellAnimationType.ChannelDirected;
				Require(!SpellDefinitionValidator.Validate(spell).Any(issue => issue.m_Severity == SpellValidationSeverity.Error), "A valid channel configuration must pass.");
				spell.m_Cast.m_ChannelInterval = float.NaN;
				Require(HasError(spell, "m_Cast.m_ChannelInterval"), "Nonfinite timing must be rejected.");
				spell.m_Cast.m_ChannelInterval = 1f;
				spell.m_Target.m_MinRange = 20f;
				spell.m_Target.m_MaxRange = 10f;
				Require(HasError(spell, "m_Target"), "Reversed target ranges must be rejected.");
				spell.m_Target.m_MinRange = 0f;
				spell.m_EffectList.Add(new DamageSpellEffect { m_Trigger = SpellEffectTrigger.ProjectileImpact });
				Require(HasError(spell, "m_EffectList[0].m_Trigger"), "Impact effects without projectiles must be rejected.");
				spell.m_EffectList.Clear();
				spell.m_EffectList.Add(new DamageSpellEffect());
				spell.m_EffectList.Add(new HealSpellEffect());
				Require(HasError(spell, "m_EffectList[1].m_Index"), "Duplicate effect indices must be rejected.");
				spell.m_CostList = null;
				spell.m_Target = null;
				spell.m_Presentation = null;
				Require(HasError(spell, "m_CostList") && HasError(spell, "m_Target") && HasError(spell, "m_Presentation"), "Missing serialized blocks must produce diagnostics instead of exceptions.");

				duplicate.m_Id = 1;
				collection.m_SpellList.Add(spell);
				collection.m_SpellList.Add(duplicate);
				collection.m_SpellList.Add(spell);
				collection.RebuildLookup();
				Require(!collection.TryGetSpell(1, out _), "Duplicate ids must not silently select a spell.");
				collection.m_SpellList.RemoveRange(1, 2);
				collection.RebuildLookup();
				Require(collection.GetSpell(1) == spell, "Explicit lookup rebuild must reflect collection changes.");
				collection.m_SpellList = null;
				collection.RebuildLookup();
				Require(!collection.TryGetSpell(1, out _), "A cleared collection must not retain old definitions.");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(collection);
				UnityEngine.Object.DestroyImmediate(duplicate);
				UnityEngine.Object.DestroyImmediate(spell);
			}
		}

		private static void ValidatePresentation()
		{
			Scene scene = EditorSceneManager.NewPreviewScene();
			SpellDefinition spell = ScriptableObject.CreateInstance<SpellDefinition>();
			try
			{
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/WowGirl/WowGirl.prefab");
				GameObject characterObject = UnityEngine.Object.Instantiate(prefab);
				SceneManager.MoveGameObjectToScene(characterObject, scene);
				Character character = characterObject.GetComponent<Character>() ?? characterObject.AddComponent<Character>();
				Animator animator = characterObject.GetComponentInChildren<Animator>();
				character.m_Animator = animator;
				character.SetLookAtDirection(Vector3.forward, Vector3.up);
				animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
				animator.Rebind();
				animator.Update(0f);
				character.StopSpellAnimation(0f);
				character.PlaySpellAnimation(SpellAnimationType.None, true, 0f);
				character.PlaySpellAnimation((SpellAnimationType)999, true, 0f);
				Require(!character.IsPlayingSpellAnimation && animator.GetLayerWeight(character.SpellLayerIndex) == 0f,
					"None and unknown animation types must not start an omni release.");
				SpellPresentation presentation = characterObject.AddComponent<SpellPresentation>();
				presentation.m_Character = character;
				presentation.m_TransitionDuration = 0f;
				spell.m_Presentation.m_AnimationType = SpellAnimationType.CastDirected;
				Require(presentation.Play(spell, true, Quaternion.Euler(0f, 45f, 0f)), "The shared presentation adapter must play a release.");
				animator.speed = 0.5f;
				for (int i = 0; i < 72; i++) Step(character, animator);
				Require(presentation.IsPlaying, "A slower release must continue beyond the old 1.1-second workshop timer.");
				for (int i = 0; i < 360 && presentation.IsPlaying; i++) Step(character, animator);
				Require(!presentation.IsPlaying && animator.GetLayerWeight(character.UpperBodyLayerIndex) == 0f,
					"Animator completion must clear spell playback and both layers.");
				Require(presentation.Play(spell, true, Quaternion.identity), "A completed release must be restartable.");
				presentation.Stop();
				Require(!presentation.IsPlaying && animator.GetLayerWeight(character.UpperBodyLayerIndex) == 0f,
					"Stopping presentation with zero blend duration must clear action layers immediately.");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(spell);
				EditorSceneManager.ClosePreviewScene(scene);
			}
		}

		private static void Step(Character character, Animator animator)
		{
			character.EvaluateSpellLayers(1f / 60f);
			animator.Update(1f / 60f);
		}

		private static bool HasError(SpellDefinition spell, string path)
		{
			return SpellDefinitionValidator.Validate(spell).Any(issue => issue.m_Severity == SpellValidationSeverity.Error && issue.m_Path == path);
		}

		private static void Require(bool condition, string message)
		{
			if (!condition) throw new InvalidOperationException(message);
		}
	}
}
