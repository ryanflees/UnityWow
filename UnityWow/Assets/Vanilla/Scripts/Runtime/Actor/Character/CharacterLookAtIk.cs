// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	[DefaultExecutionOrder(10000)]
	public class CharacterLookAtIk : MonoBehaviour
	{
		public bool m_Enable = true;
		public Animator m_Animator;
		public Transform m_BaseTransform;
		public Transform m_Head;
		public Transform m_Target;
		public Transform m_BodyBone;
		public bool m_EnableBody;

		[Range(0f, 1f)]
		public float m_BodyWeight = 0.2f;
		[Range(0f, 1f)]
		public float m_Weight = 1f;
		[Range(0f, 1f)]
		public float m_HeadWeight = 1f;
		public float m_MaxAngle = 85f;
		public float m_TargetDistance = 8f;
		public float m_TargetHeight = 1.45f;
		public bool m_UseTargetTransform = true;

		private Vector3 m_Up = Vector3.up;
		private bool m_HasTarget;
		private bool m_HasCalibration;
		private Quaternion m_HeadNeutralLocalRotation;
		private Quaternion m_BodyNeutralLocalRotation;

		public void Initialize(Animator animator, Transform baseTransform)
		{
			if (m_Animator == null)
			{
				m_Animator = animator;
			}

			if (m_BaseTransform == null)
			{
				m_BaseTransform = baseTransform;
			}
		}

		public void EnsureTarget()
		{
			if (m_Target != null)
			{
				return;
			}

			GameObject targetObject = new GameObject("LookAtTarget");
			targetObject.hideFlags = HideFlags.DontSaveInBuild;
			Transform baseTransform = m_BaseTransform != null ? m_BaseTransform : transform;
			targetObject.transform.SetParent(baseTransform, false);
			m_Target = targetObject.transform;
			m_Target.position = GetLookAtOrigin() + GetBaseForward() * Mathf.Max(0.01f, m_TargetDistance);
		}

		public void SetLookAtDirection(Vector3 direction, Vector3 up)
		{
			Vector3 validUp = up.sqrMagnitude > 0.0001f ? up.normalized : Vector3.up;
			Vector3 planarDirection = Vector3.ProjectOnPlane(direction, validUp);
			if (planarDirection.sqrMagnitude <= 0.0001f)
			{
				ClearLookAt();
				return;
			}

			m_Up = validUp;
			EnsureTarget();

			if (m_Target != null)
			{
				Vector3 origin = GetLookAtOrigin();
				m_Target.position = origin + planarDirection.normalized * Mathf.Max(0.01f, m_TargetDistance);
				m_HasTarget = true;
			}
		}

		public void ClearLookAt()
		{
			m_HasTarget = false;
		}

		public void Calibrate()
		{
			if (m_Head == null)
			{
				return;
			}

			m_HeadNeutralLocalRotation = m_Head.localRotation;
			if (m_BodyBone != null)
			{
				m_BodyNeutralLocalRotation = m_BodyBone.localRotation;
			}

			m_HasCalibration = true;
		}

		private void Start()
		{
			Calibrate();
		}

		private void OnValidate()
		{
			m_HasCalibration = false;
		}

		private void LateUpdate()
		{
			if (!m_Enable || m_Target == null)
			{
				return;
			}

			if (!m_UseTargetTransform && !m_HasTarget)
			{
				return;
			}

			if (!m_HasCalibration)
			{
				Calibrate();
			}

			if (!m_HasCalibration || m_Head == null)
			{
				return;
			}

			Vector3 targetDirection = Vector3.ProjectOnPlane(m_Target.position - m_Head.position, m_Up);
			if (targetDirection.sqrMagnitude <= 0.0001f)
			{
				return;
			}

			float desiredYaw = GetPlanarYaw(targetDirection.normalized);
			float fullWeight = Mathf.Clamp01(m_Weight);
			float bodyYaw = ApplyBodyLookAt(desiredYaw, fullWeight);
			ApplyHeadLookAt(desiredYaw, bodyYaw, fullWeight);
		}

		private float ApplyBodyLookAt(float desiredYaw, float fullWeight)
		{
			if (!m_EnableBody || m_BodyBone == null)
			{
				return 0f;
			}

			float bodyYaw = desiredYaw * fullWeight * Mathf.Clamp01(m_BodyWeight);
			if (Mathf.Abs(bodyYaw) <= 0.0001f)
			{
				return 0f;
			}

			m_BodyBone.rotation = Quaternion.AngleAxis(bodyYaw, m_Up) * GetBodySourceRotation();
			return bodyYaw;
		}

		private void ApplyHeadLookAt(float desiredYaw, float inheritedBodyYaw, float fullWeight)
		{
			float headYaw = desiredYaw * fullWeight * Mathf.Clamp01(m_HeadWeight) - inheritedBodyYaw;
			if (Mathf.Abs(headYaw) <= 0.0001f)
			{
				return;
			}

			m_Head.rotation = Quaternion.AngleAxis(headYaw, m_Up) * GetHeadSourceRotation();
		}

		private void OnDrawGizmosSelected()
		{
			if (m_Head == null || m_Target == null)
			{
				return;
			}

			Gizmos.color = Color.cyan;
			Gizmos.DrawLine(m_Head.position, m_Target.position);
			Gizmos.DrawWireSphere(m_Target.position, 0.1f);

			Gizmos.color = Color.blue;
			Gizmos.DrawRay(m_Head.position, GetBaseForward() * 2f);
		}

		private float GetPlanarYaw(Vector3 targetDirection)
		{
			Vector3 baseForward = GetBaseForward();
			float desiredYaw = Vector3.SignedAngle(baseForward, targetDirection, m_Up);
			return Mathf.Clamp(desiredYaw, -Mathf.Abs(m_MaxAngle), Mathf.Abs(m_MaxAngle));
		}

		private Vector3 GetLookAtOrigin()
		{
			Transform baseTransform = m_BaseTransform != null ? m_BaseTransform : transform;
			return baseTransform.position + m_Up * Mathf.Max(0f, m_TargetHeight);
		}

		private Vector3 GetBaseForward()
		{
			Transform baseTransform = m_BaseTransform != null ? m_BaseTransform : transform;
			Vector3 baseForward = Vector3.ProjectOnPlane(baseTransform.forward, m_Up);
			if (baseForward.sqrMagnitude <= 0.0001f)
			{
				return Vector3.forward;
			}

			return baseForward.normalized;
		}

		private Quaternion GetWorldRotationFromLocal(Transform targetTransform, Quaternion localRotation)
		{
			if (targetTransform == null || targetTransform.parent == null)
			{
				return localRotation;
			}

			return targetTransform.parent.rotation * localRotation;
		}

		private Quaternion GetBodySourceRotation()
		{
			if (ShouldUseAnimatedPose())
			{
				return m_BodyBone.rotation;
			}

			return GetWorldRotationFromLocal(m_BodyBone, m_BodyNeutralLocalRotation);
		}

		private Quaternion GetHeadSourceRotation()
		{
			if (ShouldUseAnimatedPose())
			{
				return m_Head.rotation;
			}

			return GetWorldRotationFromLocal(m_Head, m_HeadNeutralLocalRotation);
		}

		private bool ShouldUseAnimatedPose()
		{
			return m_Animator != null && m_Animator.isActiveAndEnabled && m_Animator.runtimeAnimatorController != null;
		}
	}
}

