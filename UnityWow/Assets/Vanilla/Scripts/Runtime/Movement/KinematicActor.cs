// Copyright (c) 2026 CatRabbit. All rights reserved.

using KinematicCharacterController;
using UnityEngine;

namespace CR
{
    [RequireComponent(typeof(KinematicCharacterMotor))]
    public class KinematicActor : PlayerMotor, ICharacterController
    {
        public KinematicCharacterMotor m_Motor;
        private readonly RaycastHit[] m_AirMoveGroundProbeHits = new RaycastHit[8];
        private readonly RaycastHit[] m_GroundSupportHits = new RaycastHit[8];
        private readonly RaycastHit[] m_AirObstructionHits = new RaycastHit[8];
        private float m_RequestedVerticalSpeed;
        private bool m_HitAirEdge;
        private bool m_HitRigidbody;

        public override Vector3 Position => m_Motor != null ? m_Motor.TransientPosition : transform.position;
        public override Vector3 Velocity => m_Motor != null ? m_Motor.BaseVelocity : Vector3.zero;

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = UprightRotation(currentRotation);
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (m_Motor == null) return;
            CalculateVelocity(ref currentVelocity, m_Motor.GroundingStatus.IsStableOnGround, m_Motor.GroundingStatus.GroundNormal, deltaTime);
            m_RequestedVerticalSpeed = currentVelocity.y;
        }

        protected override void ForceUnground()
        {
            m_Motor.ForceUnground();
        }

		private void Awake()
		{
			InitializeMotor();
		}

		private void OnEnable()
		{
			InitializeMotor();
		}

		public override bool IsOnGround()
		{
			return m_Motor != null && m_Motor.GroundingStatus.IsStableOnGround && !m_Motor.MustUnground();
		}

		public override void SetPositionAndRotation(Vector3 position, Quaternion rotation, bool bypassInterpolation)
		{
			InitializeMotor();
			rotation = UprightRotation(rotation);
			if (m_Motor != null)
			{
				m_Motor.SetPositionAndRotation(position, rotation, bypassInterpolation);
				return;
			}

			transform.SetPositionAndRotation(position, rotation);
		}

		public void BeforeCharacterUpdate(float deltaTime)
		{
			m_HitAirEdge = false;
			m_HitRigidbody = false;
		}

		public void PostGroundingUpdate(float deltaTime)
		{
		}

		public void AfterCharacterUpdate(float deltaTime)
		{
			UpdateGroundEvents(deltaTime);
			if (m_Motor != null && m_Blackboard != null && !IsOnGround() && m_Blackboard.m_HasAirMoveIntent && m_HitAirEdge && !m_HitRigidbody)
			{
				Vector3 up = Vector3.up;
				float addedLift = Vector3.Dot(m_Motor.BaseVelocity, up) - m_RequestedVerticalSpeed;
				if (addedLift > 0f)
				{
					// Collision sliding may lift this tick, but must not reset the falling momentum.
					m_Motor.BaseVelocity -= up * addedLift;
				}
			}
		}

		public bool IsColliderValidForCollisions(Collider coll)
		{
			return true;
		}

		public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
		{
		}

		public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
		{
			m_HitRigidbody |= hitCollider.attachedRigidbody != null;
			if (!IsOnGround() && Vector3.Dot(hitNormal, Vector3.up) > 0.0001f)
			{
				m_HitAirEdge = true;
			}
		}

		public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
		{
			if (m_Motor == null || !m_Motor.LedgeAndDenivelationHandling || (m_WasOnGround && !m_Motor.MustUnground()))
			{
				return;
			}

			Vector3 up = atCharacterRotation * Vector3.up;
			if (hitStabilityReport.ValidStepDetected)
			{
				if (m_Motor.AllowSteppingWithoutStableGrounding)
				{
					return;
				}
				hitStabilityReport.ValidStepDetected = false;
				hitStabilityReport.IsStable = Vector3.Dot(hitNormal, up) >= Mathf.Cos(m_Motor.MaxStableSlopeAngle * Mathf.Deg2Rad);
			}
			if (!hitStabilityReport.IsStable) return;
			if (Vector3.Dot(hitNormal, up) >= 0.9999f)
			{
				return;
			}

			Vector3 bottom = atCharacterPosition + up * (m_Motor.Capsule.center.y - m_Motor.Capsule.height * 0.5f);
			Vector3 towardContact = Vector3.ProjectOnPlane(hitPoint - bottom, up);
			Vector3 supportOffset = towardContact.normalized * Mathf.Max(0f, m_Motor.MaxStableDistanceFromLedge);
			// A rounded capsule/box contact normal can resemble a walkable slope.
			// Verify support under the permitted footprint rather than only at the edge.
			if (!HasStableSupport(bottom, up, hitPoint) && !HasStableSupport(bottom + supportOffset, up, hitPoint))
			{
				hitStabilityReport.IsStable = false;
			}
		}

		private bool HasStableSupport(Vector3 position, Vector3 up, Vector3 contactPoint)
		{
			float radius = m_Motor.Capsule.radius;
			float slopeCosine = Mathf.Max(0.01f, Mathf.Cos(m_Motor.MaxStableSlopeAngle * Mathf.Deg2Rad));
			float padding = KinematicCharacterMotor.CollisionOffset * 2f;
			Vector3 origin = position + up * (radius + padding);
			float distance = radius / slopeCosine + padding * 2f;
			int hitCount = m_Motor.CharacterCollisionsRaycast(origin, -up, distance, out RaycastHit hit, m_GroundSupportHits, true);
			// A separate floor below a low box cannot support a contact on the box's lip.
			return hitCount > 0 && Vector3.Dot(hit.normal, up) >= slopeCosine
				&& Mathf.Abs(Vector3.Dot(contactPoint - hit.point, hit.normal)) <= padding;
		}

		public void OnDiscreteCollisionDetected(Collider hitCollider)
		{
		}

		protected override void InitializeMotor()
		{
			if (m_Motor == null)
			{
				m_Motor = GetComponent<KinematicCharacterMotor>();
			}

			if (m_Motor != null)
			{
				m_Motor.CharacterController = this;
			}
		}

		protected override Vector3 ConstrainAirMoveVelocity(Vector3 planarVelocity, Vector3 verticalVelocity, Vector3 up, float deltaTime)
		{
			if (m_Motor == null || !m_Blackboard.m_HasAirMoveIntent || deltaTime <= 0f)
			{
				return planarVelocity;
			}

			// Test this tick's motion before an inward air intent can become upward sliding.
			// Only constrain the planar command; keep gravity and the stored intent intact.
			for (int i = 0; i < m_AirObstructionHits.Length; i++)
			{
				Vector3 velocity = planarVelocity + verticalVelocity;
				if (velocity.sqrMagnitude <= 0.0001f) break;
				Vector3 direction = velocity.normalized;
				float distance = velocity.magnitude * deltaTime + KinematicCharacterMotor.CollisionOffset;
				int hits = m_Motor.CharacterCollisionsSweep(m_Motor.TransientPosition, m_Motor.TransientRotation,
					direction, distance, out RaycastHit hit, m_AirObstructionHits);
				if (hits == 0 || Vector3.Dot(hit.normal, up) <= 0.0001f) break;
				Vector3 hitPosition = m_Motor.TransientPosition + direction * Mathf.Max(0f, hit.distance - KinematicCharacterMotor.CollisionOffset);
				HitStabilityReport stability = new HitStabilityReport();
				m_Motor.EvaluateHitStability(hit.collider, hit.normal, hit.point, hitPosition, m_Motor.TransientRotation, velocity, ref stability);
				if (stability.IsStable) break;
				m_HitAirEdge = true;
				m_HitRigidbody |= hit.collider.attachedRigidbody != null;
				Vector3 normal = Vector3.ProjectOnPlane(hit.normal, up).normalized;
				float inwardSpeed = Vector3.Dot(planarVelocity, normal);
				if (inwardSpeed >= -0.0001f) break;
				planarVelocity -= normal * inwardSpeed;
			}
			return planarVelocity;
		}

		protected override bool HasStableGroundContactBelow()
		{
			if (m_Motor == null)
			{
				return false;
			}

			float probeDistance = Mathf.Max(0f, m_AirMoveGroundContactProbeDistance);
			if (probeDistance <= 0f)
			{
				return false;
			}

			Vector3 gravityUp = Vector3.up;
			Vector3 sweepDirection = -gravityUp;
			int hitCount = m_Motor.CharacterCollisionsSweep(
				m_Motor.TransientPosition,
				m_Motor.TransientRotation,
				sweepDirection,
				probeDistance,
				out RaycastHit closestHit,
				m_AirMoveGroundProbeHits,
				0f,
				true);

			if (hitCount <= 0 || closestHit.collider == null)
			{
				return false;
			}

			Vector3 hitPosition = m_Motor.TransientPosition + sweepDirection * closestHit.distance;
			HitStabilityReport stabilityReport = new HitStabilityReport();
			m_Motor.EvaluateHitStability(closestHit.collider, closestHit.normal, closestHit.point, hitPosition, m_Motor.TransientRotation, m_Motor.BaseVelocity, ref stabilityReport);
			return stabilityReport.IsStable;
		}
	}
}

