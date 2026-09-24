// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CR
{
	public static class CharacterPoseTuningValidator
	{
		private static readonly HumanBodyBones[] m_Bones =
		{
			HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest,
			HumanBodyBones.Neck, HumanBodyBones.Head, HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm,
			HumanBodyBones.Hips, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot
		};

		private sealed class PoseSample
		{
			public Quaternion[] m_Local;
			public Quaternion[] m_World;
		}

		[MenuItem("CR/Workshop/Validate Character Pose Tuning (Play Mode)")]
		public static void Validate()
		{
			if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode before validating pose tuning.");
			foreach (float facing in new[] { -90f, 90f })
			{
				PoseSample raw = Sample(facing, ik => ik.m_EnableUpperBodyStabilization = false);
				PoseSample full = Sample(facing, ik => { });
				PoseSample torso = Sample(facing, ik => ik.m_PreserveUpperBodyHeadAndArmOrientation = false);
				PoseSample zero = Sample(facing, ik =>
				{
					ik.m_SpineCorrectionWeight = ik.m_ChestCorrectionWeight = ik.m_UpperChestCorrectionWeight = 0f;
					ik.m_PreserveUpperBodyHeadAndArmOrientation = false;
				});
				for (int i = 0; i < m_Bones.Length; i++) Require(Angle(raw.m_Local[i], zero.m_Local[i]) < 0.06f, "Zero torso shares must preserve local animation.");
				PoseSample upper = Sample(facing, ik => ik.m_SpineCorrectionWeight = 0f);
				Require(Angle(raw.m_Local[0], upper.m_Local[0]) < 0.06f && Angle(raw.m_Local[1], upper.m_Local[1]) > 1f,
					"Torso distribution must release Spine while retaining upper-joint correction.");
				PoseSample unlocked = Sample(facing, ik => ik.m_UpperBodyFacingLockWeight = 0f);
				Require(Angle(raw.m_World[2], unlocked.m_World[2]) < Angle(raw.m_World[2], full.m_World[2]), "Reducing facing lock must let the chest turn with the model.");
				PoseSample oneArm = Sample(facing, ik => ik.m_LeftArmOrientationWeight = 0f);
				Require(Angle(torso.m_World[5], oneArm.m_World[5]) < 0.06f && Angle(full.m_World[6], oneArm.m_World[6]) < 0.06f,
					"Left and right arm weights must operate independently.");
				PoseSample limitedArms = Sample(facing, ik => ik.m_MaxArmCorrectionAngle = 10f);
				for (int i = 5; i <= 6; i++)
					Require(Angle(torso.m_World[i], limitedArms.m_World[i]) <= 10.06f, "Extra arm correction must respect its angular cap.");
				PoseSample noHead = Sample(facing, ik => ik.m_HeadOrientationWeight = 0f);
				Require(Angle(torso.m_World[4], noHead.m_World[4]) < 0.06f, "Zero head weight must preserve the post-torso head orientation.");
				PoseSample limitedHead = Sample(facing, ik => ik.m_MaxHeadCorrectionAngle = 10f);
				Require(Angle(torso.m_World[4], limitedHead.m_World[4]) <= 10.06f, "Extra head correction must respect its angular cap.");
				PoseSample headOnly = Sample(facing, ik => ik.m_NeckCorrectionShare = 0f);
				PoseSample neckOnly = Sample(facing, ik => ik.m_NeckCorrectionShare = 1f);
				Require(Angle(torso.m_Local[3], headOnly.m_Local[3]) < 0.06f && Angle(headOnly.m_World[4], neckOnly.m_World[4]) < 0.06f,
					"Neck distribution must preserve the final head orientation.");
				PoseSample lookAt = Sample(facing, ik => { ik.m_Enable = true; ik.m_UpperBodyLookAtSuppression = 0f; });
				Require(Angle(full.m_World[1], lookAt.m_World[1]) > 1f, "LookAt suppression must control the extra body turn during a spell.");
			}
			string report = "Passed pose tuning on both strafe sides: zero/redistributed torso correction, facing lock, independent arms, head/arm angular caps, neck distribution and LookAt suppression. Every sample preserved hips and feet.";
			Directory.CreateDirectory("Temp");
			File.WriteAllText("Temp/CharacterPoseTuningValidation.txt", report + "\n");
			Debug.Log(report);
		}

		private static PoseSample Sample(float facing, Action<CharacterLookAtIk> configure)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/WowGirl/WowGirl.prefab");
			GameObject root = UnityEngine.Object.Instantiate(prefab);
			root.hideFlags = HideFlags.HideAndDontSave;
			try
			{
				root.transform.rotation = Quaternion.Euler(0f, facing, 0f);
				Animator animator = root.GetComponent<Animator>();
				animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
				animator.Rebind();
				animator.Update(0f);
				animator.SetLayerWeight(1, 0f);
				animator.SetLayerWeight(2, 1f);
				animator.Play("MoveForwardBT", 0, 0.35f);
				animator.Play("CastSpellOmniFinish", 1, 0.35f);
				animator.Update(0f);
				CharacterLookAtIk ik = root.GetComponent<CharacterLookAtIk>();
				ik.Initialize(animator, root.transform);
				ik.m_Enable = false;
				ik.m_UpperBodyBlendInDuration = 0f;
				ik.Calibrate();
				ik.SetLookAtDirection(Vector3.forward, Vector3.up);
				configure(ik);
				Transform[] bones = m_Bones.Select(animator.GetBoneTransform).ToArray();
				Vector3[] lowerPositions = bones.Skip(7).Select(bone => bone.position).ToArray();
				Quaternion[] lowerRotations = bones.Skip(7).Select(bone => bone.rotation).ToArray();
				AnimationClip clip = animator.runtimeAnimatorController.animationClips.First(item => item.name == "SpellCastOmni");
				Require(ik.PlayUpperBodyPose(clip, Animator.StringToHash("CastSpellOmniFinish"), 1, Quaternion.identity), "Pose sampling must initialize successfully.");
				ik.Evaluate(1f);
				for (int i = 7; i < bones.Length; i++)
					Require(Vector3.Distance(bones[i].position, lowerPositions[i - 7]) < 0.00001f && Angle(bones[i].rotation, lowerRotations[i - 7]) < 0.06f,
						"Pose tuning must not alter hips or feet.");
				return new PoseSample { m_Local = bones.Select(bone => bone.localRotation).ToArray(), m_World = bones.Select(bone => bone.rotation).ToArray() };
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
		}

		private static float Angle(Quaternion first, Quaternion second) => Quaternion.Angle(first, second);

		private static void Require(bool condition, string message)
		{
			if (!condition) throw new InvalidOperationException(message);
		}
	}
}

