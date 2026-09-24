// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CR
{
	public partial class CharacterLookAtIk
	{
		[Header("Upper Body Pose")]
		public bool m_EnableUpperBodyStabilization = true;
		[Range(0f, 1f)] public float m_UpperBodyWeight = 1f;
		[Min(0f)] public float m_UpperBodyBlendInDuration = 0.12f;
		[Min(0f)] public float m_UpperBodyBlendOutDuration = 0.15f;
		public HumanBodyBones m_UpperBodyAnchorBone = HumanBodyBones.UpperChest;
		[Range(0f, 90f)] public float m_MaxUpperBodyJointTwist = 45f;
		[Min(1f)] public float m_MaxUpperBodyJointRotationSpeed = 360f;
		public bool m_PreserveUpperBodyHeadAndArmOrientation = true;

		private Transform m_Anchor;
		private GameObject m_ReferenceRoot;
		private Animator m_ReferenceAnimator;
		private Transform m_ReferenceAnchor;
		private PlayableGraph m_Graph;
		private AnimationPlayableOutput m_Output;
		private AnimationClipPlayable m_ClipPlayable;
		private AnimationClip m_Clip;
		private Quaternion m_ReferenceRotation = Quaternion.identity;
		private int m_StateHash;
		private int m_SourceLayer;
		private bool m_HasRequest;
		private float m_SampleTime;
		private float m_UpperBodyPoseWeight;
		private SpineJoint[] m_SpineJoints;
		private bool m_HasPreviousPose;
		private Quaternion m_PreviousCorrection;
		private Transform m_Neck;
		private Transform m_UpperBodyHead;
		private Transform m_LeftUpperArm;
		private Transform m_RightUpperArm;
		private Transform m_ReferenceHead;
		private Transform m_ReferenceLeftUpperArm;
		private Transform m_ReferenceRightUpperArm;

		private sealed class SpineJoint
		{
			public Transform m_Transform;
			public Quaternion m_RestRotation;
			public Vector3 m_TwistAxis;
			public Quaternion m_AnimatedRotation;
			public Quaternion m_PreviousRotation;
		}

		public float UpperBodyPoseWeight => m_UpperBodyPoseWeight;
		public Quaternion UpperBodyTargetRotation { get; private set; }
		public float UpperBodyRotationError { get; private set; }
		public float UpperBodySampleTime => m_SampleTime;
		public bool IsUpperBodyTwistLimited { get; private set; }

		public bool PlayUpperBodyPose(AnimationClip clip, int stateHash, int sourceLayer, Quaternion referenceRotation)
		{
			if (!isActiveAndEnabled || clip == null || !clip.isHumanMotion || m_Animator == null || !m_Animator.isHuman ||
				sourceLayer < 0 || sourceLayer >= m_Animator.layerCount || !EnsureReference())
			{
				StopUpperBodyPose();
				return false;
			}
			if (m_Clip != clip)
			{
				if (m_ClipPlayable.IsValid()) m_Graph.DestroyPlayable(m_ClipPlayable);
				m_ClipPlayable = AnimationClipPlayable.Create(m_Graph, clip);
				m_ClipPlayable.SetApplyFootIK(true);
				m_ClipPlayable.SetApplyPlayableIK(false);
				m_ClipPlayable.SetSpeed(0d);
				m_Output.SetSourcePlayable(m_ClipPlayable);
				m_Clip = clip;
			}
			m_StateHash = stateHash;
			m_SourceLayer = sourceLayer;
			m_ReferenceRotation = referenceRotation;
			m_SampleTime = 0f;
			m_HasRequest = true;
			return true;
		}

		public void SetUpperBodyReferenceRotation(Quaternion rotation)
		{
			m_ReferenceRotation = rotation;
		}

		public void StopUpperBodyPose()
		{
			m_HasRequest = false;
		}

		private void EvaluateUpperBodyPose(float deltaTime)
		{
			IsUpperBodyTwistLimited = false;
			float influence = 0f;
			if (m_HasRequest && m_Animator != null && m_Animator.isActiveAndEnabled &&
				m_SourceLayer < m_Animator.layerCount) influence = ReadAnimationPhase();
			float targetWeight = m_EnableUpperBodyStabilization ? influence * Mathf.Clamp01(m_UpperBodyWeight) : 0f;
			float duration = targetWeight > m_UpperBodyPoseWeight ? m_UpperBodyBlendInDuration : m_UpperBodyBlendOutDuration;
			m_UpperBodyPoseWeight = duration <= 0f ? targetWeight :
				Mathf.MoveTowards(m_UpperBodyPoseWeight, targetWeight, Mathf.Max(0f, deltaTime) / duration);
			if (m_UpperBodyPoseWeight <= 0f || m_Anchor == null || !m_Graph.IsValid() || !m_ClipPlayable.IsValid())
			{
				UpperBodyRotationError = 0f;
				m_HasPreviousPose = false;
				return;
			}

			m_ClipPlayable.SetTime(m_SampleTime);
			m_Graph.Evaluate(0f);
			Quaternion modelRotation = Quaternion.Inverse(m_ReferenceRoot.transform.rotation) * m_ReferenceAnchor.rotation;
			UpperBodyTargetRotation = m_ReferenceRotation * modelRotation;
			ApplySpinePose(deltaTime);
			if (m_PreserveUpperBodyHeadAndArmOrientation) PreserveActionOrientation();
			UpperBodyRotationError = Quaternion.Angle(m_Anchor.rotation, UpperBodyTargetRotation);
		}

		private void PreserveActionOrientation()
		{
			// Blend absolute clip poses after the constrained torso solve. A residual from the already
			// blended chest applies the outgoing animation twice and can wind the head around during fade-out.
			BlendReferenceOrientation(m_LeftUpperArm, m_ReferenceLeftUpperArm);
			BlendReferenceOrientation(m_RightUpperArm, m_ReferenceRightUpperArm);
			if (m_UpperBodyHead == null || m_ReferenceHead == null) return;
			Quaternion headRotation = Quaternion.Slerp(m_UpperBodyHead.rotation, GetReferenceWorldRotation(m_ReferenceHead), m_UpperBodyPoseWeight);
			Quaternion correction = headRotation * Quaternion.Inverse(m_UpperBodyHead.rotation);
			if (m_Neck != null)
				m_Neck.rotation = Quaternion.Slerp(Quaternion.identity, correction, 0.5f) * m_Neck.rotation;
			m_UpperBodyHead.rotation = headRotation;
		}

		private void BlendReferenceOrientation(Transform bone, Transform reference)
		{
			if (bone != null && reference != null)
				bone.rotation = Quaternion.Slerp(bone.rotation, GetReferenceWorldRotation(reference), m_UpperBodyPoseWeight);
		}

		private Quaternion GetReferenceWorldRotation(Transform reference)
		{
			return m_ReferenceRotation * Quaternion.Inverse(m_ReferenceRoot.transform.rotation) * reference.rotation;
		}

		private void ApplySpinePose(float deltaTime)
		{
			Quaternion correction = UpperBodyTargetRotation * Quaternion.Inverse(m_Anchor.rotation);
			Quaternion branchReference = m_HasPreviousPose && Quaternion.Angle(Quaternion.identity, correction) > 90f ?
				m_PreviousCorrection : Quaternion.identity;
			correction = MatchHemisphere(correction, branchReference);
			m_PreviousCorrection = correction;
			correction.ToAngleAxis(out float correctionAngle, out Vector3 correctionAxis);
			foreach (SpineJoint joint in m_SpineJoints) joint.m_AnimatedRotation = joint.m_Transform.localRotation;
			for (int i = 0; i < m_SpineJoints.Length; i++)
			{
				SpineJoint joint = m_SpineJoints[i];
				Quaternion remaining = UpperBodyTargetRotation * Quaternion.Inverse(m_Anchor.rotation);
				Quaternion expectedRemainder = Quaternion.AngleAxis(correctionAngle *
					(m_SpineJoints.Length - i) / m_SpineJoints.Length, correctionAxis);
				remaining = MatchHemisphere(remaining, expectedRemainder);
				remaining.ToAngleAxis(out float angle, out Vector3 axis);
				joint.m_Transform.rotation = Quaternion.AngleAxis(angle / (m_SpineJoints.Length - i), axis) *
					joint.m_Transform.rotation;
				Quaternion constrained = LimitTwist(joint, joint.m_Transform.localRotation);
				// A shortest-arc solution can switch sides near 180 degrees. Bound that change in local space.
				if (m_HasPreviousPose)
					constrained = LimitTwist(joint, Quaternion.RotateTowards(joint.m_PreviousRotation, constrained,
						Mathf.Max(0f, deltaTime) * m_MaxUpperBodyJointRotationSpeed));
				joint.m_Transform.localRotation = constrained;
				joint.m_PreviousRotation = constrained;
			}
			m_HasPreviousPose = true;
			foreach (SpineJoint joint in m_SpineJoints)
				joint.m_Transform.localRotation = Quaternion.Slerp(joint.m_AnimatedRotation,
					joint.m_PreviousRotation, m_UpperBodyPoseWeight);
		}

		private static Quaternion MatchHemisphere(Quaternion rotation, Quaternion reference)
		{
			// Preserve the rotation branch across 180 degrees before distributing it over the spine.
			return Quaternion.Dot(rotation, reference) < 0f ?
				new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w) : rotation;
		}

		private Quaternion LimitTwist(SpineJoint joint, Quaternion localRotation)
		{
			Quaternion delta = Quaternion.Inverse(joint.m_RestRotation) * localRotation;
			Vector3 projected = Vector3.Project(new Vector3(delta.x, delta.y, delta.z), joint.m_TwistAxis);
			float magnitude = Mathf.Sqrt(projected.sqrMagnitude + delta.w * delta.w);
			if (magnitude < 0.00001f) return localRotation;
			Quaternion twist = new Quaternion(projected.x / magnitude, projected.y / magnitude,
				projected.z / magnitude, delta.w / magnitude);
			if (twist.w < 0f) twist = new Quaternion(-twist.x, -twist.y, -twist.z, -twist.w);
			float angle = 2f * Mathf.Atan2(Vector3.Dot(new Vector3(twist.x, twist.y, twist.z), joint.m_TwistAxis),
				twist.w) * Mathf.Rad2Deg;
			float limitedAngle = Mathf.Clamp(angle, -m_MaxUpperBodyJointTwist, m_MaxUpperBodyJointTwist);
			if (Mathf.Abs(angle - limitedAngle) < 0.001f) return localRotation;
			IsUpperBodyTwistLimited = true;
			Quaternion swing = delta * Quaternion.Inverse(twist);
			return joint.m_RestRotation * swing * Quaternion.AngleAxis(limitedAngle, joint.m_TwistAxis);
		}

		private float ReadAnimationPhase()
		{
			AnimatorStateInfo currentState = m_Animator.GetCurrentAnimatorStateInfo(m_SourceLayer);
			float influence = currentState.shortNameHash == m_StateHash ? 1f : 0f;
			AnimatorStateInfo sourceState = currentState;
			if (m_Animator.IsInTransition(m_SourceLayer))
			{
				AnimatorStateInfo nextState = m_Animator.GetNextAnimatorStateInfo(m_SourceLayer);
				float progress = Mathf.Clamp01(m_Animator.GetAnimatorTransitionInfo(m_SourceLayer).normalizedTime);
				if (nextState.shortNameHash == m_StateHash)
				{
					sourceState = nextState;
					influence = progress;
				}
				else influence *= 1f - progress;
			}
			if (influence > 0f)
			{
				m_SampleTime = Mathf.Clamp01(sourceState.normalizedTime) * m_Clip.length;
			}
			return influence;
		}

		private bool EnsureReference()
		{
			Transform anchor = m_Animator.GetBoneTransform(m_UpperBodyAnchorBone);
			if (anchor == null && m_UpperBodyAnchorBone == HumanBodyBones.UpperChest)
				anchor = m_Animator.GetBoneTransform(HumanBodyBones.Chest);
			if (m_ReferenceRoot != null && m_Anchor == anchor) return true;
			ReleaseReference();
			m_Anchor = anchor;
			if (m_Anchor == null) return false;
			BuildSpineChain();
			if (m_SpineJoints.Length == 0) return false;
			m_ReferenceRoot = CopyHierarchy(m_Animator.transform, null).gameObject;
			m_ReferenceRoot.name = "UpperBodyPoseReference";
			m_ReferenceRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			m_ReferenceRoot.transform.localScale = m_Animator.transform.lossyScale;
			m_ReferenceAnimator = m_ReferenceRoot.AddComponent<Animator>();
			m_ReferenceAnimator.avatar = m_Animator.avatar;
			m_ReferenceAnimator.applyRootMotion = false;
			m_ReferenceAnimator.fireEvents = false;
			m_ReferenceAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
			m_Graph = PlayableGraph.Create("UpperBodyPoseReference");
			m_Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
			m_Output = AnimationPlayableOutput.Create(m_Graph, "ReferencePose", m_ReferenceAnimator);
			m_Graph.Play();
			return m_ReferenceAnchor != null;
		}

		private void BuildSpineChain()
		{
			var joints = new System.Collections.Generic.List<SpineJoint>();
			SkeletonBone[] restBones = m_Animator.avatar.humanDescription.skeleton;
			foreach (HumanBodyBones bone in new[] { HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest })
			{
				Transform target = m_Animator.GetBoneTransform(bone);
				if (target == null || (target != m_Anchor && !m_Anchor.IsChildOf(target))) continue;
				Quaternion restRotation = target.localRotation;
				foreach (SkeletonBone rest in restBones)
					if (rest.name == target.name) { restRotation = rest.rotation; break; }
				Transform child = m_Animator.GetBoneTransform(bone == HumanBodyBones.Spine ? HumanBodyBones.Chest :
					bone == HumanBodyBones.Chest ? HumanBodyBones.UpperChest : HumanBodyBones.Neck);
				if (child == null) child = m_Animator.GetBoneTransform(HumanBodyBones.Neck);
				Vector3 axis = child != null ? target.InverseTransformPoint(child.position).normalized : Vector3.up;
				joints.Add(new SpineJoint { m_Transform = target, m_RestRotation = restRotation, m_TwistAxis = axis });
			}
			m_SpineJoints = joints.ToArray();
			m_Neck = m_Animator.GetBoneTransform(HumanBodyBones.Neck);
			m_UpperBodyHead = m_Animator.GetBoneTransform(HumanBodyBones.Head);
			m_LeftUpperArm = m_Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
			m_RightUpperArm = m_Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
		}

		private Transform CopyHierarchy(Transform source, Transform parent)
		{
			GameObject copy = new GameObject(source.name) { hideFlags = HideFlags.HideAndDontSave };
			copy.transform.SetParent(parent, false);
			copy.transform.localPosition = source.localPosition;
			copy.transform.localRotation = source.localRotation;
			copy.transform.localScale = source.localScale;
			if (source == m_Anchor) m_ReferenceAnchor = copy.transform;
			if (source == m_UpperBodyHead) m_ReferenceHead = copy.transform;
			if (source == m_LeftUpperArm) m_ReferenceLeftUpperArm = copy.transform;
			if (source == m_RightUpperArm) m_ReferenceRightUpperArm = copy.transform;
			for (int i = 0; i < source.childCount; i++) CopyHierarchy(source.GetChild(i), copy.transform);
			return copy.transform;
		}

		private void OnDisable()
		{
			ReleaseReference();
		}

		private void OnDestroy()
		{
			ReleaseReference();
		}

		private void ReleaseReference()
		{
			if (m_Graph.IsValid()) m_Graph.Destroy();
			if (m_ReferenceRoot != null) Destroy(m_ReferenceRoot);
			m_ReferenceRoot = null;
			m_ReferenceAnimator = null;
			m_ReferenceAnchor = null;
			m_ReferenceHead = null;
			m_ReferenceLeftUpperArm = null;
			m_ReferenceRightUpperArm = null;
			m_Clip = null;
			m_HasRequest = false;
			m_UpperBodyPoseWeight = 0f;
			m_SpineJoints = null;
			m_HasPreviousPose = false;
			IsUpperBodyTwistLimited = false;
			UpperBodyRotationError = 0f;
		}
	}
}

