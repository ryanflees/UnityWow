// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
    [DisallowMultipleComponent]
    public abstract class PlayerMotor : MonoBehaviour
    {
        [Min(0f)] public float m_GravityStrength = 25f;
        public float m_JumpSpeed = 9f;
        public float m_AirMoveGroundContactProbeDistance = 0.12f;
        public float m_AirMoveGroundContactDrag = 24f;
        public float m_AirMoveGroundContactStopSpeed = 0.05f;

        protected PlayerBlackboard m_Blackboard;
        protected bool m_WasOnGround;
        private bool m_JumpPending;
        private float m_AirborneDuration;

        public abstract Vector3 Position { get; }
        public virtual Vector3 VisualPosition => transform.position;
        public abstract Vector3 Velocity { get; }
        public abstract bool IsOnGround();
        public abstract void SetPositionAndRotation(Vector3 position, Quaternion rotation, bool bypassInterpolation);
        protected abstract void InitializeMotor();
        protected abstract void ForceUnground();
        protected abstract bool HasStableGroundContactBelow();
        protected virtual Vector3 ConstrainAirMoveVelocity(Vector3 planarVelocity, Vector3 verticalVelocity, Vector3 up, float deltaTime) => planarVelocity;

        public static Quaternion UprightRotation(Quaternion rotation)
        {
            Vector3 forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up);
            return Quaternion.LookRotation(forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward, Vector3.up);
        }

		public virtual void Initialize(PlayerBlackboard blackboard)
		{
			m_Blackboard = blackboard;
			InitializeMotor();
			m_WasOnGround = IsOnGround();
			m_AirborneDuration = 0f;
			m_Blackboard.m_IsGrounded = m_WasOnGround;
			m_Blackboard.m_AirborneDuration = 0f;
			ResetAirMove();
		}

		public void RequestJump()
		{
			m_JumpPending = true;
		}

		private Vector3 ProjectVelocityOnGround(Vector3 velocity, Vector3 groundNormal, Vector3 gravityUp)
		{
			if (velocity.sqrMagnitude <= 0.0001f)
			{
				return Vector3.zero;
			}

			Vector3 inputRight = Vector3.Cross(velocity, gravityUp);
			Vector3 groundVelocity = Vector3.Cross(groundNormal, inputRight);
			if (groundVelocity.sqrMagnitude <= 0.0001f)
			{
				return Vector3.zero;
			}

			return groundVelocity.normalized * velocity.magnitude;
		}

		private void UpdateJumpVelocity(ref Vector3 currentVelocity, Vector3 gravityUp)
		{
			if (!m_JumpPending || !IsOnGround())
			{
				return;
			}

			ForceUnground();
			currentVelocity += gravityUp * m_JumpSpeed - Vector3.Project(currentVelocity, gravityUp);
			m_JumpPending = false;
			if (m_Blackboard != null)
			{
				m_Blackboard.m_WasJumpStarted = true;
				m_Blackboard.m_JumpCount++;
				m_Blackboard.m_IsGrounded = false;
				m_Blackboard.m_AirborneDuration = 0f;
				if (m_Blackboard.m_WasStandingJumpRequested)
				{
					m_Blackboard.m_CanStartStandingJumpAirMove = true;
					ClearAirMoveIntent();
				}
				else
				{
					TryStartAirMoveFromVelocity(m_Blackboard.m_MoveVelocity, currentVelocity, gravityUp, false, m_Blackboard.m_IsBackMove);
				}

				m_Blackboard.m_WasStandingJumpRequested = false;
			}
		}

		private Vector3 GetAirbornePlanarVelocity(Vector3 currentVelocity, Vector3 gravityUp)
		{
			if (m_Blackboard == null || !m_Blackboard.m_HasAirMoveIntent)
			{
				return Vector3.ProjectOnPlane(currentVelocity, gravityUp);
			}

			Vector3 direction = Vector3.ProjectOnPlane(m_Blackboard.m_AirMoveIntentDirection, gravityUp);
			if (direction.sqrMagnitude <= 0.0001f)
			{
				return Vector3.ProjectOnPlane(currentVelocity, gravityUp);
			}

			return direction.normalized * Mathf.Max(0f, m_Blackboard.m_AirMoveIntentSpeed);
		}

		protected void UpdateGroundEvents(float deltaTime)
		{
			if (m_Blackboard == null)
			{
				m_WasOnGround = IsOnGround();
				m_AirborneDuration = 0f;
				return;
			}

			bool isOnGround = IsOnGround();
			if (!isOnGround)
			{
				m_AirborneDuration += Mathf.Max(0f, deltaTime);
				if (m_WasOnGround && !m_Blackboard.m_CanStartStandingJumpAirMove && !m_Blackboard.m_HasAirMoveIntent)
				{
					Vector3 currentVelocity = Velocity;
					TryStartAirMoveFromVelocity(m_Blackboard.m_MoveVelocity, currentVelocity, Vector3.up, false, m_Blackboard.m_IsBackMove);
				}

				ApplyAirMoveGroundContactDrag(deltaTime);
			}

			if (isOnGround && !m_WasOnGround)
			{
				m_Blackboard.m_WasGroundedThisFrame = true;
				m_Blackboard.m_LandedAirborneDuration = m_AirborneDuration;
				m_AirborneDuration = 0f;
				ResetAirMove();
			}
			else if (isOnGround)
			{
				m_AirborneDuration = 0f;
				ResetAirMove();
			}

			m_Blackboard.m_IsGrounded = isOnGround;
			m_Blackboard.m_AirborneDuration = m_AirborneDuration;
			m_WasOnGround = isOnGround;
		}

		private bool TryStartAirMoveFromVelocity(Vector3 intendedVelocity, Vector3 currentVelocity, Vector3 gravityUp, bool controlsFacing, bool keepFacingForward)
		{
			if (m_Blackboard == null)
			{
				return false;
			}

			Vector3 intendedPlanarVelocity = Vector3.ProjectOnPlane(intendedVelocity, gravityUp);
			Vector3 currentPlanarVelocity = Vector3.ProjectOnPlane(currentVelocity, gravityUp);
			Vector3 directionSource = intendedPlanarVelocity.sqrMagnitude > 0.0001f ? intendedPlanarVelocity : currentPlanarVelocity;
			if (directionSource.sqrMagnitude <= 0.0001f)
			{
				return false;
			}

			float speed = Mathf.Max(intendedPlanarVelocity.magnitude, currentPlanarVelocity.magnitude);
			if (speed <= 0.0001f)
			{
				return false;
			}

			m_Blackboard.m_CanStartStandingJumpAirMove = false;
			m_Blackboard.m_HasAirMoveIntent = true;
			m_Blackboard.m_AirMoveIntentDirection = directionSource.normalized;
			m_Blackboard.m_AirMoveIntentSpeed = speed;
			m_Blackboard.m_AirMoveIntentControlsFacing = controlsFacing;
			m_Blackboard.m_AirMoveIntentKeepFacingForward = keepFacingForward;
			return true;
		}

		private void ApplyAirMoveGroundContactDrag(float deltaTime)
		{
			if (m_Blackboard == null || !m_Blackboard.m_HasAirMoveIntent || m_Blackboard.m_AirMoveIntentSpeed <= 0f)
			{
				return;
			}

			if (!HasStableGroundContactBelow())
			{
				return;
			}

			float drag = Mathf.Max(0f, m_AirMoveGroundContactDrag);
			m_Blackboard.m_AirMoveIntentSpeed *= Mathf.Exp(-drag * Mathf.Max(0f, deltaTime));
			if (m_Blackboard.m_AirMoveIntentSpeed <= Mathf.Max(0f, m_AirMoveGroundContactStopSpeed))
			{
				m_Blackboard.m_AirMoveIntentSpeed = 0f;
			}
		}

		protected void ResetAirMove()
		{
			if (m_Blackboard == null)
			{
				return;
			}

			m_Blackboard.m_CanStartStandingJumpAirMove = false;
			m_Blackboard.m_WasStandingJumpRequested = false;
			ClearAirMoveIntent();
		}

		private void ClearAirMoveIntent()
		{
			m_Blackboard.m_HasAirMoveIntent = false;
			m_Blackboard.m_AirMoveIntentDirection = Vector3.zero;
			m_Blackboard.m_AirMoveIntentSpeed = 0f;
			m_Blackboard.m_AirMoveIntentControlsFacing = false;
			m_Blackboard.m_AirMoveIntentKeepFacingForward = false;
		}

		protected void CalculateVelocity(ref Vector3 currentVelocity, bool grounded, Vector3 groundNormal, float deltaTime)
		{
			if (m_Blackboard == null)
			{
				return;
			}

			Vector3 gravityUp = Vector3.up;
			Vector3 targetVelocity = Vector3.ProjectOnPlane(m_Blackboard.m_MoveVelocity, gravityUp);

			if (grounded)
			{
				currentVelocity = ProjectVelocityOnGround(targetVelocity, groundNormal, gravityUp);
				UpdateJumpVelocity(ref currentVelocity, gravityUp);
				return;
			}

			m_JumpPending = false;
			Vector3 planarVelocity = GetAirbornePlanarVelocity(currentVelocity, gravityUp);
			Vector3 verticalVelocity = Vector3.Project(currentVelocity, gravityUp);
			verticalVelocity += -gravityUp * Mathf.Max(0f, m_GravityStrength) * deltaTime;
			planarVelocity = ConstrainAirMoveVelocity(planarVelocity, verticalVelocity, gravityUp, deltaTime);
			currentVelocity = planarVelocity + verticalVelocity;
		}
	}
}

