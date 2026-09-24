// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CR
{
	public static class CharacterIkValidator
	{
		private static readonly HumanBodyBones[] m_LowerBones =
		{
			HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
			HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
			HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot
		};
		private static readonly HumanBodyBones[] m_SpineBones =
		{
			HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest
		};
		private static readonly HumanBodyBones[] m_ActionBones =
		{
			HumanBodyBones.Head, HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm
		};

		[MenuItem("CR/Workshop/Validate Character IK (Play Mode)")]
		public static void Validate()
		{
			if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode before validating character IK.");
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/WowGirl/WowGirl.prefab");
			GameObject moving = UnityEngine.Object.Instantiate(prefab);
			GameObject standing = UnityEngine.Object.Instantiate(prefab);
			moving.hideFlags = standing.hideFlags = HideFlags.HideAndDontSave;
			try
			{
				Animator animator = PrepareAnimator(moving);
				Animator reference = PrepareAnimator(standing);
				CharacterLookAtIk ik = moving.GetComponent<CharacterLookAtIk>() ?? moving.AddComponent<CharacterLookAtIk>();
				ik.Initialize(animator, moving.transform);
				ik.m_Head = animator.GetBoneTransform(HumanBodyBones.Head);
				ik.m_BodyBone = animator.GetBoneTransform(HumanBodyBones.Chest);
				ik.m_EnableBody = true;
				ik.Calibrate();
				Quaternion facingFrame = Quaternion.Euler(0f, 37f, 0f);
				standing.transform.rotation = facingFrame;
				int samples = 0;
				float maximumError = 0f;
				float maximumLowerBodyDisplacement = 0f;
				float maximumTwist = 0f;
				float maximumActionError = 0f;
				int limitedSamples = 0;
				for (int spell = 0; spell < 2; spell++)
				{
					string state = spell == 0 ? "CastSpellDirectedFinish" : "CastSpellOmniFinish";
					string clipName = spell == 0 ? "SpellCastDirected" : "SpellCastOmni";
					AnimationClip clip = animator.runtimeAnimatorController.animationClips.First(item => item.name == clipName);
					for (int y = -1; y <= 1; y++)
					{
						for (int x = -1; x <= 1; x++)
						{
							ik.StopUpperBodyPose();
							ik.Evaluate(1f);
							Vector3 direction = new Vector3(x, 0f, y);
							Quaternion facing = direction == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(y < 0 ? -direction : direction);
							moving.transform.rotation = facingFrame * facing;
							ik.SetLookAtDirection(facingFrame * Vector3.forward, Vector3.up);
							for (int frame = 15; frame <= 42; frame += 9)
							{
								float phase = frame / 60f / clip.length;
								animator.Play(direction == Vector3.zero ? state : y < 0 ? "Movebackward" : "MoveForwardBT", 0, phase);
								animator.Play(state, 1, phase);
								animator.SetLayerWeight(1, direction == Vector3.zero ? 0f : 1f);
								animator.Update(0f);
								reference.Play(state, 0, phase);
								reference.Update(0f);
								Transform anchor = animator.GetBoneTransform(HumanBodyBones.UpperChest);
								Quaternion[] actionRotations = m_ActionBones.Select(bone => reference.GetBoneTransform(bone).rotation).ToArray();
								Vector3[] localPositions = m_SpineBones.Select(bone => animator.GetBoneTransform(bone).localPosition).ToArray();
								Vector3[] positions = m_LowerBones.Select(bone => animator.GetBoneTransform(bone).position).ToArray();
								Quaternion[] rotations = m_LowerBones.Select(bone => animator.GetBoneTransform(bone).rotation).ToArray();
								if (!ik.PlayUpperBodyPose(clip, Animator.StringToHash(state), 1, facingFrame))
									throw new InvalidOperationException("Failed to initialize the reference pose.");
								ik.Evaluate(1f);
								float error = Quaternion.Angle(anchor.rotation, reference.GetBoneTransform(HumanBodyBones.UpperChest).rotation);
								maximumError = Mathf.Max(maximumError, error);
								if (ik.IsUpperBodyTwistLimited) limitedSamples++;
								if (!ik.IsUpperBodyTwistLimited && error > 0.1f)
									throw new InvalidOperationException($"Reference pose mismatch: {clipName}, ({x}, {y}), frame {frame}, {error} degrees.");
								maximumTwist = Mathf.Max(maximumTwist, ValidateJointTwist(animator, ik.m_MaxUpperBodyJointTwist));
								for (int bone = 0; bone < m_ActionBones.Length; bone++)
								{
									float actionError = Quaternion.Angle(actionRotations[bone], animator.GetBoneTransform(m_ActionBones[bone]).rotation);
									maximumActionError = Mathf.Max(maximumActionError, actionError);
									if (actionError > 0.1f) throw new InvalidOperationException($"Torso limits changed {m_ActionBones[bone]} orientation by {actionError} degrees.");
								}
								for (int bone = 0; bone < m_SpineBones.Length; bone++)
									if (Vector3.Distance(localPositions[bone], animator.GetBoneTransform(m_SpineBones[bone]).localPosition) > 0.00001f)
										throw new InvalidOperationException("Spine distribution changed a local bone offset.");
								for (int bone = 0; bone < m_LowerBones.Length; bone++)
								{
									Transform lower = animator.GetBoneTransform(m_LowerBones[bone]);
									float displacement = Vector3.Distance(positions[bone], lower.position);
									maximumLowerBodyDisplacement = Mathf.Max(maximumLowerBodyDisplacement, displacement);
									if (displacement > 0.00001f || Quaternion.Angle(rotations[bone], lower.rotation) > 0.05f)
										throw new InvalidOperationException("Stabilization changed the lower body.");
								}
								samples++;
							}
						}
					}
				}
				ValidateWeightTransitions(animator, ik);
				ik.enabled = true;
				float maximumStep = ValidateSideTransitions(animator, ik);
				string headReport = ValidateHeadCycles(animator, ik);
				if (limitedSamples == 0) throw new InvalidOperationException("The extreme-twist regression cases did not exercise joint limits.");
				string report = $"Passed {samples} pose samples across two spells and nine directions with active LookAt. Joint twist maximum: {maximumTwist:F3} degrees; limited samples: {limitedSamples}; maximum retained torso offset: {maximumError:F3} degrees; head/arm orientation error: {maximumActionError:F3} degrees; lower-body displacement: {maximumLowerBodyDisplacement:F6} m. Four continuous side-cast sequences passed; maximum joint step: {maximumStep:F3} degrees at 60 Hz. Toggle, blend-in, stop, and disable checks passed.\n{headReport}\n";
				Directory.CreateDirectory("Temp");
				File.WriteAllText("Temp/CharacterIkValidation.txt", report);
				Debug.Log(report);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(moving);
				UnityEngine.Object.DestroyImmediate(standing);
			}
		}

		private static Animator PrepareAnimator(GameObject character)
		{
			Animator animator = character.GetComponentInChildren<Animator>();
			animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
			animator.Rebind();
			animator.Update(0f);
			animator.SetLayerWeight(1, 0f);
			return animator;
		}

		private static float ValidateJointTwist(Animator animator, float limit)
		{
			float maximum = 0f;
			foreach (HumanBodyBones bone in m_SpineBones)
			{
				Transform target = animator.GetBoneTransform(bone);
				Quaternion rest = animator.avatar.humanDescription.skeleton.First(item => item.name == target.name).rotation;
				HumanBodyBones childBone = bone == HumanBodyBones.Spine ? HumanBodyBones.Chest :
					bone == HumanBodyBones.Chest ? HumanBodyBones.UpperChest : HumanBodyBones.Neck;
				Vector3 axis = target.InverseTransformPoint(animator.GetBoneTransform(childBone).position).normalized;
				Quaternion relative = Quaternion.Inverse(rest) * target.localRotation;
				float projection = Vector3.Dot(new Vector3(relative.x, relative.y, relative.z), axis);
				float twist = Mathf.Abs(Mathf.DeltaAngle(0f, 2f * Mathf.Atan2(projection, relative.w) * Mathf.Rad2Deg));
				maximum = Mathf.Max(maximum, twist);
				if (twist > limit + 0.05f) throw new InvalidOperationException($"{bone} exceeds twist limit: {twist} degrees.");
			}
			return maximum;
		}

		private static float ValidateSideTransitions(Animator animator, CharacterLookAtIk ik)
		{
			float maximumStep = 0f;
			ik.m_UpperBodyBlendInDuration = 0f;
			for (int spell = 0; spell < 2; spell++)
			{
				string state = spell == 0 ? "CastSpellDirectedFinish" : "CastSpellOmniFinish";
				AnimationClip clip = animator.runtimeAnimatorController.animationClips.First(item => item.name ==
					(spell == 0 ? "SpellCastDirected" : "SpellCastOmni"));
				for (int direction = -1; direction <= 1; direction += 2)
				{
					ik.StopUpperBodyPose();
					ik.Evaluate(1f);
					animator.transform.rotation = Quaternion.Euler(0f, direction * 90f, 0f);
					animator.Play("MoveForwardBT", 0, 0f);
					animator.Play(state, 1, 0f);
					animator.SetLayerWeight(1, 1f);
					animator.Update(0f);
					ik.PlayUpperBodyPose(clip, Animator.StringToHash(state), 1, Quaternion.identity);
					Quaternion[] previous = null;
					for (int frame = 0; frame < 42; frame++)
					{
						animator.Update(1f / 60f);
						ik.Evaluate(1f / 60f);
						ValidateJointTwist(animator, ik.m_MaxUpperBodyJointTwist);
						Quaternion[] current = m_SpineBones.Select(bone => animator.GetBoneTransform(bone).localRotation).ToArray();
						if (previous != null)
							for (int bone = 0; bone < current.Length; bone++)
								maximumStep = Mathf.Max(maximumStep, Quaternion.Angle(previous[bone], current[bone]));
						previous = current;
					}
				}
			}
			if (maximumStep > ik.m_MaxUpperBodyJointRotationSpeed / 60f + 0.1f)
				throw new InvalidOperationException($"Side-cast joint rotation jumped: {maximumStep} degrees in one frame.");
			return maximumStep;
		}

		private static void ValidateWeightTransitions(Animator animator, CharacterLookAtIk ik)
		{
			bool lookAtEnabled = ik.m_Enable;
			ik.m_Enable = false;
			Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
			ik.m_EnableUpperBodyStabilization = false;
			animator.Update(0f);
			Quaternion unmodified = chest.rotation;
			ik.Evaluate(1f);
			if (ik.UpperBodyPoseWeight != 0f || Quaternion.Angle(chest.rotation, unmodified) > 0.05f)
				throw new InvalidOperationException("Disabled stabilization still modifies the pose.");
			ik.m_EnableUpperBodyStabilization = true;
			ik.Evaluate(0.03f);
			if (ik.UpperBodyPoseWeight <= 0f || ik.UpperBodyPoseWeight >= 1f)
				throw new InvalidOperationException("Stabilization did not blend in.");
			ik.StopUpperBodyPose();
			ik.Evaluate(0.01f);
			if (ik.UpperBodyPoseWeight <= 0f) throw new InvalidOperationException("Stopping snapped stabilization off.");
			ik.Evaluate(1f);
			if (ik.UpperBodyPoseWeight != 0f) throw new InvalidOperationException("Stopping retained stabilization weight.");
			ik.enabled = false;
			if (ik.UpperBodyPoseWeight != 0f) throw new InvalidOperationException("Disabling did not release stabilization.");
			ik.m_Enable = lookAtEnabled;
		}

		private static string ValidateHeadCycles(Animator animator, CharacterLookAtIk ik)
		{
			ik.m_UpperBodyBlendInDuration = 0.12f;
			ik.m_UpperBodyBlendOutDuration = 0.15f;
			ik.m_MaxAngle = 90f;
			ik.m_BodyWeight = 0.35f;
			Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
			int sequences = 0;
			float maximumIkHeadingSpan = 0f;
			float maximumHeadStep = 0f;
			foreach (int frameRate in new[] { 30, 60 })
			{
				float deltaTime = 1f / frameRate;
				int cycleFrames = Mathf.RoundToInt(1.5f * frameRate);
				for (int spell = 0; spell < 2; spell++)
				{
					string state = spell == 0 ? "CastSpellDirectedFinish" : "CastSpellOmniFinish";
					AnimationClip clip = animator.runtimeAnimatorController.animationClips.First(item => item.name ==
						(spell == 0 ? "SpellCastDirected" : "SpellCastOmni"));
					for (int direction = -1; direction <= 1; direction += 2)
					{
						for (int ikMode = 0; ikMode < 2; ikMode++)
						{
							ik.StopUpperBodyPose();
							ik.Evaluate(1f);
							animator.Rebind();
							animator.transform.rotation = Quaternion.Euler(0f, direction * 90f, 0f);
							animator.Play("MoveForwardBT", 0, 0f);
							animator.Play("Empty", 1, 0f);
							animator.SetLayerWeight(1, 0f);
							animator.Update(0.25f);
							ik.m_Enable = ikMode == 1;
							ik.SetLookAtDirection(Vector3.forward, Vector3.up);
							float previousYaw = 0f;
							float unwrappedYaw = 0f;
							float minimumYaw = float.MaxValue;
							float maximumYaw = float.MinValue;
							Quaternion previousRotation = Quaternion.identity;
							for (int frame = 0; frame < cycleFrames * 3; frame++)
							{
								int cycleFrame = frame % cycleFrames;
								if (cycleFrame == 0)
								{
									animator.SetLayerWeight(1, 1f);
									animator.CrossFadeInFixedTime(state, 0.12f, 1, 0f);
									ik.PlayUpperBodyPose(clip, Animator.StringToHash(state), 1, Quaternion.identity);
								}
								if (cycleFrame == Mathf.RoundToInt(1.1f * frameRate))
								{
									ik.StopUpperBodyPose();
									animator.CrossFadeInFixedTime("Empty", 0.12f, 1, 0f);
									animator.SetLayerWeight(1, 0f);
								}
								animator.Update(deltaTime);
								ik.Evaluate(deltaTime);
								float yaw = Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(head.forward, Vector3.up), Vector3.up);
								unwrappedYaw = frame == 0 ? yaw : unwrappedYaw + Mathf.DeltaAngle(previousYaw, yaw);
								minimumYaw = Mathf.Min(minimumYaw, unwrappedYaw);
								maximumYaw = Mathf.Max(maximumYaw, unwrappedYaw);
								if (frame > 0)
								{
									float step = Quaternion.Angle(previousRotation, head.rotation);
									maximumHeadStep = Mathf.Max(maximumHeadStep, step);
									float allowedStep = (ikMode == 1 ? 720f : 1800f) * deltaTime + 3f;
									if (step > allowedStep)
										throw new InvalidOperationException($"Head jump: {state}, side {direction}, IK {ikMode}, {frameRate} Hz, frame {frame}: {step} degrees.");
								}
								previousYaw = yaw;
								previousRotation = head.rotation;
							}
							float span = maximumYaw - minimumYaw;
							if (span > (ikMode == 1 ? 60f : 150f))
								throw new InvalidOperationException($"Head winding: {state}, side {direction}, IK {ikMode}, {frameRate} Hz: {span} degrees across three casts.");
							if (ikMode == 1) maximumIkHeadingSpan = Mathf.Max(maximumIkHeadingSpan, span);
							sequences++;
						}
					}
				}
			}
			return $"Passed {sequences} full head sequences (three casts each, both sides/spells, IK on/off, 30/60 Hz), including entry, exit, and repeat. Maximum IK heading span: {maximumIkHeadingSpan:F3} degrees; maximum head step across all modes: {maximumHeadStep:F3} degrees.";
		}
	}
}

