// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;
namespace CR
{
	public class TPCameraController : MonoBehaviour
	{
		public GameObject m_Target;
		public Vector3 m_TargetOffset = Vector3.zero;
		public float m_DefaultDistance = 5f;
		public float m_MinDistance = 1.5f;
		public float m_MaxDistance = 12f;
		public float m_HorizontalSensitivity = 1f;
		public float m_VerticalSensitivity = 1f;
		[Min(0f), Tooltip("How far the camera zooms for each input unit at Default Distance. One wheel step from the built-in input gives 0.1 units.")]
		public float m_ZoomSensitivity = 8f;
		[Min(0f), Tooltip("How long the wheel zoom takes to smooth out, in seconds. Set it to 0 if you want zoom to change right away.")]
		public float m_ZoomSmoothTime = 0.12f;
		public float m_MinVerticalDegree = -45f;
		public float m_MaxVerticalDegree = 80f;
		[Header("Collision")]
		public bool m_EnableCollision = true;
		[Tooltip("Choose which layers can block the camera. It ignores triggers and the player it follows.")]
		public LayerMask m_CollisionLayers = Physics.DefaultRaycastLayers;
		[Min(0.01f), Tooltip("The smallest radius used to check for walls. It gets larger if needed to cover the camera near clip plane.")]
		public float m_CollisionRadius = 0.2f;
		[Min(0f)] public float m_CollisionPadding = 0.05f;
		[Min(0f), Tooltip("How long the camera takes to move back out after a wall is gone. When a wall blocks the view, it moves in right away.")]
		public float m_CollisionRecoverySmoothTime = 0.16f;
		[Tooltip("Colliders under this object will not block the camera. You can leave it empty to ignore the player being followed.")]
		public Transform m_CollisionIgnoreRoot;
		public Vector3 m_WorldUp => Vector3.up;
		private Transform m_TargetTransform;
		private Vector3 m_OrbitForward = Vector3.forward;
		private float m_HorizontalDegree;
		private float m_VerticalDegree = 20f;
		private float m_Distance;
		private float m_TargetDistance;
		private float m_ZoomVelocity;
		private float m_RenderedDistance;
		private float m_CollisionRecoveryVelocity;
		private bool m_IsRecoveringFromCollision;
		private Camera m_Camera;
		private Transform m_DefaultIgnoreRoot;
		private RaycastHit[] m_CollisionHits = new RaycastHit[16];
		private Collider[] m_Overlaps = new Collider[16];

		private void Awake()
		{
			m_Distance = Mathf.Clamp(m_DefaultDistance, m_MinDistance, m_MaxDistance);
			m_TargetDistance = m_RenderedDistance = m_Distance;
			m_Camera = GetComponent<Camera>();

			if (m_Target != null)
			{
				SetTarget(m_Target.transform);
			}
		}

		public void SetTarget(Transform targetTransform)
		{
			m_TargetTransform = targetTransform;
			var player = targetTransform != null ? targetTransform.GetComponentInParent<PlayerController>() : null;
			m_DefaultIgnoreRoot = player != null ? player.transform : targetTransform;

			if (targetTransform != null)
			{
				m_Target = targetTransform.gameObject;
			}
			else
			{
				m_Target = null;
			}
		}

		public void SetWorldUp(Vector3 worldUp)
		{
			Vector3 planarForward = Vector3.ProjectOnPlane(m_OrbitForward, m_WorldUp);
			if (planarForward.sqrMagnitude > 0.0001f)
			{
				m_OrbitForward = planarForward.normalized;
			}
		}

		public void ResetOrbit(Vector3 forward, Vector3 worldUp)
		{
			SetWorldUp(worldUp);

			Vector3 planarForward = Vector3.ProjectOnPlane(forward, m_WorldUp);
			if (planarForward.sqrMagnitude <= 0.0001f)
			{
				planarForward = Vector3.ProjectOnPlane(transform.forward, m_WorldUp);
			}

			if (planarForward.sqrMagnitude <= 0.0001f)
			{
				planarForward = GetFallbackForward(m_WorldUp);
			}

			m_OrbitForward = planarForward.normalized;
			m_HorizontalDegree = 0f;
			m_VerticalDegree = Mathf.Clamp(m_VerticalDegree, m_MinVerticalDegree, m_MaxVerticalDegree);
		}

		public void ResetBehindTarget(Vector3 targetForward, Vector3 worldUp)
		{
			ResetOrbit(targetForward, worldUp);
			m_Distance = m_RenderedDistance = Mathf.Clamp(m_TargetDistance, m_MinDistance, m_MaxDistance);
			m_ZoomVelocity = m_CollisionRecoveryVelocity = 0f;
			m_IsRecoveringFromCollision = false;
			ExecuteLateUpdate(0f);
		}

		public void UpdateCameraInput(Vector2 lookInput, float zoomInput, float dt)
		{
			m_HorizontalDegree += lookInput.x * m_HorizontalSensitivity;
			m_VerticalDegree = Mathf.Clamp(m_VerticalDegree - lookInput.y * m_VerticalSensitivity, m_MinVerticalDegree, m_MaxVerticalDegree);
			// Accumulate wheel notches against the destination, not the interpolated position.
			// Scale the step with distance so close-up adjustments stay precise.
			if (zoomInput != 0f)
			{
				float distanceScale = Mathf.Clamp(m_TargetDistance / Mathf.Max(0.01f, m_DefaultDistance), 0.25f, 2f);
				float zoomDelta = -zoomInput * Mathf.Max(0f, m_ZoomSensitivity) * distanceScale;
				if (zoomDelta * (m_TargetDistance - m_Distance) < 0f)
				{
					// Reversing the wheel cancels unfinished travel in the old direction.
					m_TargetDistance = m_Distance;
					m_ZoomVelocity = 0f;
				}
				m_TargetDistance = Mathf.Clamp(m_TargetDistance + zoomDelta, m_MinDistance, m_MaxDistance);
			}
		}

		public void ExecuteLateUpdate(float dt)
		{
			if (m_TargetTransform == null)
			{
				if (m_Target == null)
				{
					return;
				}

				SetTarget(m_Target.transform);
			}

			m_TargetDistance = Mathf.Clamp(m_TargetDistance, m_MinDistance, m_MaxDistance);
			m_Distance = SmoothDistance(m_Distance, m_TargetDistance, ref m_ZoomVelocity, m_ZoomSmoothTime, dt);
			Vector3 targetPosition = m_TargetTransform.position + m_TargetOffset;
			Vector3 gravityUp = m_WorldUp.sqrMagnitude > 0.0001f ? m_WorldUp.normalized : Vector3.up;
			Vector3 orbitForward = Vector3.ProjectOnPlane(m_OrbitForward, gravityUp);
			if (orbitForward.sqrMagnitude <= 0.0001f)
			{
				orbitForward = Vector3.ProjectOnPlane(m_TargetTransform.forward, gravityUp);
			}

			if (orbitForward.sqrMagnitude <= 0.0001f)
			{
				orbitForward = GetFallbackForward(gravityUp);
			}

			Quaternion planarRotation = Quaternion.LookRotation(orbitForward.normalized, gravityUp);
			Quaternion cameraRotation = Quaternion.AngleAxis(m_HorizontalDegree, gravityUp) * planarRotation;
			cameraRotation = Quaternion.AngleAxis(m_VerticalDegree, cameraRotation * Vector3.right) * cameraRotation;
			Vector3 awayFromTarget = -(cameraRotation * Vector3.forward);
			float safeDistance = m_EnableCollision ? GetCollisionDistance(targetPosition, awayFromTarget, m_Distance) : m_Distance;
			if (safeDistance < m_Distance) m_IsRecoveringFromCollision = true;
			if (m_IsRecoveringFromCollision)
			{
				if (safeDistance <= m_RenderedDistance)
				{
					// Never smooth through an obstacle; only the outward recovery is damped.
					m_RenderedDistance = safeDistance;
					m_CollisionRecoveryVelocity = 0f;
				}
				else
				{
					m_RenderedDistance = SmoothDistance(m_RenderedDistance, safeDistance,
						ref m_CollisionRecoveryVelocity, m_CollisionRecoverySmoothTime, dt);
				}
				m_IsRecoveringFromCollision = m_RenderedDistance < m_Distance - 0.001f;
			}
			if (!m_IsRecoveringFromCollision)
			{
				m_RenderedDistance = safeDistance;
				m_CollisionRecoveryVelocity = 0f;
			}
			Vector3 cameraPosition = targetPosition + awayFromTarget * m_RenderedDistance;

			transform.SetPositionAndRotation(cameraPosition, cameraRotation);
		}

		private static float SmoothDistance(float current, float target, ref float velocity, float smoothTime, float dt)
		{
			if (smoothTime <= 0f || Mathf.Abs(current - target) < 0.001f)
			{
				velocity = 0f;
				return target;
			}
			// SmoothDamp can produce NaN velocity at zero delta time.
			if (dt <= 0f) return current;
			return Mathf.SmoothDamp(current, target, ref velocity, smoothTime, Mathf.Infinity, dt);
		}

		private float GetCollisionDistance(Vector3 origin, Vector3 direction, float requestedDistance)
		{
			if (requestedDistance <= 0f || m_CollisionLayers.value == 0) return requestedDistance;
			float radius = Mathf.Max(0.01f, m_CollisionRadius);
			if (m_Camera != null)
			{
				float near = m_Camera.nearClipPlane;
				float halfHeight = m_Camera.orthographic ? m_Camera.orthographicSize : near * Mathf.Tan(m_Camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
				float halfWidth = halfHeight * m_Camera.aspect;
				radius = Mathf.Max(radius, Mathf.Sqrt(near * near + halfHeight * halfHeight + halfWidth * halfWidth));
			}

			// Sweeps do not reliably report a shape already overlapping at the origin.
			int overlaps;
			while ((overlaps = Physics.OverlapSphereNonAlloc(origin, radius, m_Overlaps, m_CollisionLayers, QueryTriggerInteraction.Ignore)) == m_Overlaps.Length)
				System.Array.Resize(ref m_Overlaps, m_Overlaps.Length * 2);
			for (int i = 0; i < overlaps; i++)
				if (!IgnoreCollider(m_Overlaps[i])) return 0f;

			float distance = requestedDistance;
			int count = Sweep(origin, radius, direction, requestedDistance);
			for (int i = 0; i < count; i++)
				if (!IgnoreCollider(m_CollisionHits[i].collider))
					distance = Mathf.Min(distance, m_CollisionHits[i].distance - m_CollisionPadding);

			// Also sweep toward the target so a single-sided mesh cannot be missed from its back.
			count = Sweep(origin + direction * requestedDistance, radius, -direction, requestedDistance);
			for (int i = 0; i < count; i++)
				if (!IgnoreCollider(m_CollisionHits[i].collider))
					distance = Mathf.Min(distance, requestedDistance - m_CollisionHits[i].distance - 2f * radius - m_CollisionPadding);
			// Collision can pull closer than the user's zoom limit without changing the requested zoom.
			return Mathf.Max(0f, distance);
		}

		private int Sweep(Vector3 origin, float radius, Vector3 direction, float distance)
		{
			int count;
			while ((count = Physics.SphereCastNonAlloc(origin, radius, direction, m_CollisionHits, distance, m_CollisionLayers, QueryTriggerInteraction.Ignore)) == m_CollisionHits.Length)
				System.Array.Resize(ref m_CollisionHits, m_CollisionHits.Length * 2);
			return count;
		}

		private bool IgnoreCollider(Collider collider)
		{
			if (collider == null) return true;
			Transform ignoredRoot = m_CollisionIgnoreRoot != null ? m_CollisionIgnoreRoot : m_DefaultIgnoreRoot;
			return collider.transform.IsChildOf(transform) || (ignoredRoot != null && collider.transform.IsChildOf(ignoredRoot));
		}

		private Vector3 GetFallbackForward(Vector3 worldUp)
		{
			Vector3 axis = Mathf.Abs(Vector3.Dot(worldUp.normalized, Vector3.forward)) > 0.95f ? Vector3.right : Vector3.forward;
			return Vector3.ProjectOnPlane(axis, worldUp).normalized;
		}
	}
}

