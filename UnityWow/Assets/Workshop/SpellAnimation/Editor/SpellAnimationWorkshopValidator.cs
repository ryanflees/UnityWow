// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CR
{
	public static class SpellAnimationWorkshopValidator
	{
		private const string m_ControllerPath = "Assets/Art/Characters/WowGirl/Animations/WowGirlAC.controller";
		private const string m_CharacterPath = "Assets/Art/Characters/WowGirl/WowGirl.prefab";
		private const string m_ReportPath = "Assets/Workshop/SpellAnimation/Reports/Validation.txt";
		private const string m_WorkshopScenePath = "Assets/Workshop/SpellAnimation/Scenes/SpellAnimationWorkshop.unity";
		private const string m_WorkshopScriptPath = "Assets/Workshop/SpellAnimation/Scripts/SpellAnimationWorkshop.cs";

		[MenuItem("CR/Workshop/Validate Moving Instant Spells (Play Mode)")]
		public static void ValidateMovingInstantSpells()
		{
			SpellAnimationWorkshop workshop = UnityEngine.Object.FindFirstObjectByType<SpellAnimationWorkshop>();
			if (!Application.isPlaying || workshop == null)
				throw new InvalidOperationException("Run the Spell Animation Workshop in Play Mode first.");
			workshop.StartCoroutine(ValidateMovementSequence(workshop));
		}

		private static IEnumerator ValidateMovementSequence(SpellAnimationWorkshop workshop)
		{
			bool originalInPlace = workshop.m_MoveInPlace;
			bool originalUpperBody = workshop.m_UseUpperBodyLayer;
			bool originalIk = workshop.m_EnableForwardIk;
			Animator animator = workshop.m_Character.m_Animator;
			CharacterLookAtIk lookAt = workshop.m_Character.m_LookAtIk;
			Vector3 forward = workshop.m_Character.transform.forward;
			int combinations = 0;
			try
			{
				workshop.ResetPosition();
				forward = workshop.m_Character.transform.forward;
				workshop.m_MoveInPlace = false;
				workshop.m_UseUpperBodyLayer = true;
				workshop.m_EnableForwardIk = true;
				for (int y = -1; y <= 1; y++)
				{
					for (int x = -1; x <= 1; x++)
					{
						Vector2 direction = new Vector2(x, y);
						workshop.StopSpell();
						workshop.SetMovementDirection(direction);
						yield return new WaitForSeconds(0.35f);
						for (int spell = 0; spell < 2; spell++)
						{
							Vector3 position = workshop.m_Character.transform.position;
							workshop.BeginSpell(spell);
							yield return new WaitForSeconds(0.25f);
							string release = spell == 0 ? "CastSpellDirectedFinish" : "CastSpellOmniFinish";
							ValidateCurrentState(animator, 1, release);
							ValidateCurrentState(animator, 0, direction == Vector2.zero ? "Stand" :
								y < 0 ? "Movebackward" : "MoveForwardBT");
							Vector3 delta = workshop.m_Character.transform.position - position;
							if (direction != Vector2.zero)
							{
								if (animator.GetLayerWeight(2) < 0.99f || animator.GetLayerWeight(1) > 0.01f || delta.sqrMagnitude < 0.001f)
									throw new InvalidOperationException($"Movement/release failed for {direction}, spell {spell}.");
								Vector3 expected = Quaternion.LookRotation(forward) * new Vector3(x, 0f, y).normalized;
								if (Vector3.Angle(delta, expected) > 1f)
									throw new InvalidOperationException("Movement does not match the selected direction.");
							}
							else if (delta.sqrMagnitude > 0.0001f)
								throw new InvalidOperationException("Standing release moved the preview character.");
							if (!lookAt.m_Enable || lookAt.m_Head == null || lookAt.m_BodyBone == null ||
								Vector3.Angle(lookAt.m_Target.position - (workshop.m_Character.transform.position +
								Vector3.up * lookAt.m_TargetHeight), forward) > 1f)
								throw new InvalidOperationException("Forward IK is not configured or has drifted with movement.");
							workshop.StopSpell();
							yield return new WaitForSeconds(0.2f);
							ValidateCurrentState(animator, 0, direction == Vector2.zero ? "Stand" :
								y < 0 ? "Movebackward" : "MoveForwardBT");
							combinations++;
						}
					}
				}
				workshop.ResetPosition();
				workshop.BeginSpell(0);
				yield return new WaitForSeconds(0.2f);
				workshop.SetMovementDirection(Vector2.right);
				yield return new WaitForSeconds(0.25f);
				ValidateCurrentState(animator, 0, "MoveForwardBT");
				ValidateCurrentState(animator, 1, "CastSpellDirectedFinish");
				if (animator.GetLayerWeight(2) < 0.99f || animator.GetLayerWeight(1) > 0.01f) throw new InvalidOperationException("Starting to move did not reveal locomotion.");
				workshop.SetMovementDirection(Vector2.down);
				yield return new WaitForSeconds(0.2f);
				ValidateCurrentState(animator, 0, "Movebackward");
				float phaseBeforeStop = animator.GetCurrentAnimatorStateInfo(1).normalizedTime;
				workshop.SetMovementDirection(Vector2.zero);
				yield return new WaitForSeconds(0.2f);
				if (animator.GetLayerWeight(1) < 0.99f || animator.GetCurrentAnimatorStateInfo(1).normalizedTime <= phaseBeforeStop)
					throw new InvalidOperationException("Stopping did not restore the current full-body spell phase.");
				ValidateCurrentState(animator, 0, "Stand");
				ValidateCurrentState(animator, 1, "CastSpellDirectedFinish");
				workshop.SetMovementDirection(Vector2.up);
				workshop.BeginSpell(1);
				yield return new WaitForSeconds(workshop.m_ReleaseAnimationDuration + 0.25f);
				ValidateCurrentState(animator, 0, "MoveForwardBT");
				if (animator.GetLayerWeight(1) > 0.01f || animator.GetLayerWeight(2) > 0.01f) throw new InvalidOperationException("Completed release did not clear the upper layer.");
				string report = $"Passed {combinations} direction/instant combinations, translation, IK targets, cancellation, start/stop/direction changes during release, and timed completion.\n";
				Directory.CreateDirectory("Temp");
				File.WriteAllText("Temp/MovingInstantValidation.txt", report);
				Debug.Log(report);
			}
			finally
			{
				workshop.StopSpell();
				workshop.ResetPosition();
				workshop.m_MoveInPlace = originalInPlace;
				workshop.m_UseUpperBodyLayer = originalUpperBody;
				workshop.m_EnableForwardIk = originalIk;
			}
		}

		[MenuItem("CR/Workshop/Validate Spell Animations")]
		public static string Validate()
		{
			AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(m_ControllerPath);
			if (controller == null)
			{
				throw new InvalidOperationException("WowGirl AnimatorController was not found.");
			}

			ValidateController(controller);
			ValidateWorkshopScriptReference();
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_CharacterPath);
			if (prefab == null)
			{
				throw new InvalidOperationException("WowGirl prefab was not found.");
			}

			GameObject characterObject = UnityEngine.Object.Instantiate(prefab);
			characterObject.hideFlags = HideFlags.HideAndDontSave;
			try
			{
				Animator animator = characterObject.GetComponentInChildren<Animator>();
				Character character = characterObject.GetComponent<Character>() ?? characterObject.AddComponent<Character>();
				character.m_Animator = animator;
				animator.runtimeAnimatorController = controller;
				animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
				animator.Rebind();
				animator.Update(0.01f);

				ValidateFullBody(character, animator, SpellAnimationType.CastDirected, "CastSpellDirected");
				ValidateFullBody(character, animator, SpellAnimationType.CastOmnidirectional, "CastingSpellOmni");
				ValidateFullBody(character, animator, SpellAnimationType.ChannelDirected, "ChannelCastDirected");
				ValidateFullBody(character, animator, SpellAnimationType.ChannelOmnidirectional, "ChannelCastOmni");
				ValidateRelease(character, animator, SpellAnimationType.CastDirected, "CastSpellDirectedFinish");
				ValidateRelease(character, animator, SpellAnimationType.CastOmnidirectional, "CastSpellOmniFinish");
				character.StopSpellAnimation(0f);
				animator.Update(0.01f);
				if (!Mathf.Approximately(animator.GetLayerWeight(1), 0f) || !Mathf.Approximately(animator.GetLayerWeight(2), 0f))
				{
					throw new InvalidOperationException("Stopping a spell did not disable the UpperBody layer.");
				}
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(characterObject);
			}

			string report = "Validated Base / SpellLayer / synchronized UpperBody, six spell states, continuous zero-weight playback, movement weight blends, restored lower-body spell pose, repeated start/stop, overlay toggling, cancellation and automatic exit.";
			Directory.CreateDirectory(Path.GetDirectoryName(m_ReportPath));
			File.WriteAllText(m_ReportPath, report + "\n");
			AssetDatabase.ImportAsset(m_ReportPath);
			Debug.Log(report);
			return report;
		}

		private static void ValidateWorkshopScriptReference()
		{
			string scriptGuid = AssetDatabase.AssetPathToGUID(m_WorkshopScriptPath);
			if (string.IsNullOrEmpty(scriptGuid) || !File.Exists(m_WorkshopScenePath))
			{
				throw new InvalidOperationException("The Spell Animation Workshop scene or script was not found.");
			}

			string sceneData = File.ReadAllText(m_WorkshopScenePath);
			if (!sceneData.Contains($"guid: {scriptGuid}"))
			{
				throw new InvalidOperationException("The Spell Animation Workshop scene has a missing script reference.");
			}
		}

		private static void ValidateController(AnimatorController controller)
		{
			if (controller.layers.Length != 3)
			{
				throw new InvalidOperationException("WowGirl AnimatorController requires Base Layer, SpellLayer and UpperBody layers.");
			}

			AnimatorControllerLayer baseLayer = controller.layers[0];
			AnimatorControllerLayer spellLayer = controller.layers[1];
			AnimatorControllerLayer upperBodyLayer = controller.layers[2];
			if (baseLayer.name != "Base Layer" || spellLayer.name != "SpellLayer" || upperBodyLayer.name != "UpperBody")
			{
				throw new InvalidOperationException("Unexpected spell animation layer layout.");
			}

			if (upperBodyLayer.avatarMask == null)
			{
				throw new InvalidOperationException("UpperBody layer requires an AvatarMask.");
			}

			ValidateStates(spellLayer, "CastSpellDirected", "CastingSpellOmni", "ChannelCastDirected", "ChannelCastOmni", "CastSpellDirectedFinish", "CastSpellOmniFinish");
			ValidateStates(baseLayer, "Stand", "MoveForwardBT", "Movebackward");
			ValidateStates(spellLayer, "Empty");
			if (spellLayer.avatarMask != null || upperBodyLayer.syncedLayerIndex != 1 || upperBodyLayer.syncedLayerAffectsTiming)
				throw new InvalidOperationException("UpperBody must follow the unmasked SpellLayer with Timing disabled.");
			foreach (ChildAnimatorState child in spellLayer.stateMachine.states)
				if (child.state.motion != null && upperBodyLayer.GetOverrideMotion(child.state) != child.state.motion)
					throw new InvalidOperationException($"UpperBody requires an explicit clip binding for {child.state.name}, including when SpellLayer weight is zero.");
		}

		private static void ValidateStates(AnimatorControllerLayer layer, params string[] requiredStates)
		{
			string[] states = layer.stateMachine.states.Select(item => item.state.name).ToArray();
			for (int i = 0; i < requiredStates.Length; i++)
			{
				if (!states.Contains(requiredStates[i]))
				{
					throw new InvalidOperationException($"Layer {layer.name} is missing state {requiredStates[i]}.");
				}
			}
		}

		private static void ValidateFullBody(Character character, Animator animator,
			SpellAnimationType animationType, string expectedState)
		{
			character.PlaySpellAnimation(animationType, false, 0f);
			animator.Update(0.01f);
			ValidateCurrentState(animator, 1, expectedState);
			ValidateCurrentState(animator, 0, "Stand");
		}

		private static void ValidateRelease(Character character, Animator animator,
			SpellAnimationType animationType, string expectedState)
		{
			foreach (int frameRate in new[] { 30, 60 })
			{
				character.StopSpellAnimation(0f);
				character.PlayStand();
				animator.Update(0.3f);
				character.PlaySpellAnimation(animationType, true, 0f);
				Tick(character, animator, 0.01f);
				ValidateCurrentState(animator, 0, "Stand");
				ValidateSpellSynchronization(animator, expectedState);
				if (animator.GetLayerWeight(1) < 0.99f || animator.GetLayerWeight(2) < 0.99f)
					throw new InvalidOperationException("Standing spell requires both spell layers.");

				float previousPhase = animator.GetCurrentAnimatorStateInfo(1).normalizedTime;
				float previousWeight = animator.GetLayerWeight(1);
				float deltaTime = 1f / frameRate;
				int segmentFrames = frameRate / 5;
				for (int frame = 0; frame < segmentFrames * 3; frame++)
				{
					if (frame == 0) character.PlayMoveForward();
					if (frame == segmentFrames) character.PlayStand();
					if (frame == segmentFrames * 2) character.PlayMoveBackward();
					Tick(character, animator, deltaTime);
					ValidateSpellSynchronization(animator, expectedState);
					float phase = animator.GetCurrentAnimatorStateInfo(1).normalizedTime;
					float expectedAdvance = deltaTime / animator.GetCurrentAnimatorStateInfo(1).length;
					if (Mathf.Abs(phase - previousPhase - expectedAdvance) > 0.002f)
						throw new InvalidOperationException("Movement restarted, paused, or changed the spell clock.");
					float weight = animator.GetLayerWeight(1);
					if (Mathf.Abs(weight - previousWeight) > deltaTime / character.m_SpellMovementBlendDuration + 0.001f)
						throw new InvalidOperationException("Spell movement weight snapped.");
					if (frame == segmentFrames - 1 || frame == segmentFrames * 3 - 1)
						if (weight > 0.01f) throw new InvalidOperationException("Movement did not fade the full-body layer out.");
					if (frame == segmentFrames * 2 - 1)
					{
						if (weight < 0.99f) throw new InvalidOperationException("Stopping did not restore the full-body layer.");
						ValidateStoppedLowerBodyPose(animator, expectedState, phase);
					}
					previousPhase = phase;
					previousWeight = weight;
				}
				character.StopSpellAnimation(0.12f);
				for (int frame = 0; frame < frameRate / 4; frame++) Tick(character, animator, deltaTime);
				ValidateCurrentState(animator, 0, "Movebackward");
				if (animator.GetLayerWeight(1) != 0f || animator.GetLayerWeight(2) != 0f)
					throw new InvalidOperationException("Cancellation retained spell layers.");

				character.PlaySpellAnimation(animationType, true, 0.12f);
				for (int frame = 0; frame < frameRate / 3; frame++) Tick(character, animator, deltaTime);
				ValidateSpellSynchronization(animator, expectedState);
				character.PlayStand();
				character.SetUpperBodySpellLayerEnabled(false);
				for (int frame = 0; frame < frameRate / 5; frame++) Tick(character, animator, deltaTime);
				if (animator.GetLayerWeight(1) < 0.99f || animator.GetLayerWeight(2) != 0f)
					throw new InvalidOperationException("Spell-only comparison is not independent.");
				float phaseBeforeToggle = animator.GetCurrentAnimatorStateInfo(1).normalizedTime;
				character.SetUpperBodySpellLayerEnabled(true);
				Tick(character, animator, deltaTime);
				if (animator.GetCurrentAnimatorStateInfo(1).normalizedTime <= phaseBeforeToggle)
					throw new InvalidOperationException("Overlay toggle restarted the spell.");
				character.PlayMoveForward();
				for (int frame = 0; frame < frameRate * 2; frame++) Tick(character, animator, deltaTime);
				if (character.IsPlayingSpellAnimation || animator.GetLayerWeight(1) != 0f || animator.GetLayerWeight(2) != 0f)
					throw new InvalidOperationException("Zero-weight source did not complete the spell automatically.");
			}
		}

		private static void Tick(Character character, Animator animator, float deltaTime)
		{
			character.EvaluateSpellLayers(deltaTime);
			animator.Update(deltaTime);
		}

		private static void ValidateSpellSynchronization(Animator animator, string state)
		{
			ValidateCurrentState(animator, 1, state);
			ValidateCurrentState(animator, 2, state);
			if (Mathf.Abs(animator.GetCurrentAnimatorStateInfo(1).normalizedTime -
				animator.GetCurrentAnimatorStateInfo(2).normalizedTime) > 0.001f)
				throw new InvalidOperationException("SpellLayer and UpperBody phases differ.");
		}

		private static void ValidateStoppedLowerBodyPose(Animator animator, string state, float phase)
		{
			GameObject referenceObject = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(m_CharacterPath));
			referenceObject.hideFlags = HideFlags.HideAndDontSave;
			try
			{
				Animator reference = referenceObject.GetComponentInChildren<Animator>();
				reference.Rebind();
				reference.SetLayerWeight(1, 1f);
				reference.SetLayerWeight(2, 0f);
				reference.Play(state, 1, phase);
				reference.Update(0f);
				foreach (HumanBodyBones bone in new[] { HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg,
					HumanBodyBones.RightUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
					HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
				{
					Transform actual = animator.GetBoneTransform(bone);
					Transform expected = reference.GetBoneTransform(bone);
					if (Quaternion.Angle(actual.localRotation, expected.localRotation) > 0.1f ||
						Vector3.Distance(actual.localPosition, expected.localPosition) > 0.0001f)
						throw new InvalidOperationException($"Stopping did not restore the current spell pose on {bone}.");
				}
			}
			finally { UnityEngine.Object.DestroyImmediate(referenceObject); }
		}

		private static void ValidateCurrentState(Animator animator, int layer, string expectedState)
		{
			int expectedHash = Animator.StringToHash(expectedState);
			if (animator.GetCurrentAnimatorStateInfo(layer).shortNameHash != expectedHash)
			{
				throw new InvalidOperationException($"Layer {layer} did not enter state {expectedState}.");
			}
		}
	}
}

