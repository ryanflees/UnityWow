// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	public class PlayerFacingProcessor : IPlayerProcessor
	{
		private const float DirectionThreshold = 0.0001f;
		private const float LookThreshold = 0.0001f;
		private const float InputThreshold = 0.5f;
		private const float FaceAngleThreshold = 0.1f;

		private PlayerController m_Controller;

		public PlayerFacingProcessor(PlayerController controller)
		{
			m_Controller = controller;
		}

		public void OnUpdate(PlayerBlackboard blackboard, float dt)
		{
			if (blackboard.m_StandingTurnCooldown > 0f)
			{
				blackboard.m_StandingTurnCooldown = Mathf.Max(0f, blackboard.m_StandingTurnCooldown - dt);
			}

			blackboard.m_IsFaceControlledByCamera = blackboard.m_IsSecondaryMouseRotating;
			if (blackboard.m_WasSecondaryMouseReleased && ShouldUseStandingTurn(blackboard))
			{
				FinishStandingTurn(blackboard);
			}

			if (!blackboard.m_IsSecondaryMouseRotating)
			{
				return;
			}

			Vector3 gravityUp = GetGravityUp(blackboard);
			Vector3 fd = GetPlanarDirection(blackboard.m_ReferenceFaceDirection, gravityUp);
			Vector3 visualFaceDirection = GetVisualFaceDirection(blackboard, fd, gravityUp);
			float preservedAirFaceAngle = Vector3.SignedAngle(fd, GetCurrentVisualFaceDirection(blackboard, fd, gravityUp), gravityUp);
			if (blackboard.m_WasSecondaryMousePressed)
			{
				fd = GetCameraForward(gravityUp);
			}

			if (fd.sqrMagnitude <= DirectionThreshold)
			{
				fd = GetPlanarDirection(blackboard.m_CharacterFaceDirection, gravityUp);
			}

			if (fd.sqrMagnitude <= DirectionThreshold)
			{
				fd = GetFallbackForward(gravityUp);
			}

			if (Mathf.Abs(blackboard.m_LookInput.x) > LookThreshold)
			{
				float yaw = blackboard.m_LookInput.x * m_Controller.m_FaceDirectionHorizontalSensitivity;
				fd = Quaternion.AngleAxis(yaw, gravityUp) * fd;
				fd = GetPlanarDirection(fd, gravityUp);
			}

			blackboard.m_ReferenceFaceDirection = fd;
			if (blackboard.m_IsGrounded)
			{
				blackboard.m_CharacterFaceDirection = fd;
			}
			else
			{
				// In the air, character face direction stays as its visual direction
				// to remain stable in world space even if the reference (camera) turns.
				blackboard.m_CharacterFaceDirection = visualFaceDirection;
			}
			blackboard.m_IsReturningToReferenceFace = false;

			if (!blackboard.m_IsGrounded)
			{
				blackboard.m_CurrentFaceAngle = preservedAirFaceAngle;
				blackboard.m_TargetFaceAngle = preservedAirFaceAngle;
				blackboard.m_ShouldSnapFaceAngle = true;
				return;
			}

			if (ShouldUseStandingTurn(blackboard))
			{
				UpdateStandingTurn(blackboard, visualFaceDirection, fd, gravityUp);
				return;
			}

			if (blackboard.m_WasSecondaryMousePressed)
			{
				blackboard.m_ShouldSnapFaceAngle = true;
			}

			blackboard.m_TargetFaceAngle = 0f;
		}

		public void OnFixedUpdate(PlayerBlackboard blackboard, float dt)
		{
		}

		public void OnLateUpdate(PlayerBlackboard blackboard, float dt)
		{
		}

		private Vector3 GetGravityUp(PlayerBlackboard blackboard)
		{
			if (blackboard.m_GravityUp.sqrMagnitude > DirectionThreshold)
			{
				return blackboard.m_GravityUp.normalized;
			}

			return Vector3.up;
		}

		private Vector3 GetPlanarDirection(Vector3 direction, Vector3 gravityUp)
		{
			Vector3 planarDirection = Vector3.ProjectOnPlane(direction, gravityUp);
			if (planarDirection.sqrMagnitude <= DirectionThreshold)
			{
				return Vector3.zero;
			}

			return planarDirection.normalized;
		}

		private Vector3 GetCameraForward(Vector3 gravityUp)
		{
			if (m_Controller.m_TPCameraController == null)
			{
				return Vector3.zero;
			}

			return GetPlanarDirection(m_Controller.m_TPCameraController.transform.forward, gravityUp);
		}

		private Vector3 GetFallbackForward(Vector3 gravityUp)
		{
			Vector3 axis = Mathf.Abs(Vector3.Dot(gravityUp.normalized, Vector3.forward)) > 0.95f ? Vector3.right : Vector3.forward;
			return Vector3.ProjectOnPlane(axis, gravityUp).normalized;
		}

		private Vector3 GetVisualFaceDirection(PlayerBlackboard blackboard, Vector3 fd, Vector3 gravityUp)
		{
			if (fd.sqrMagnitude <= DirectionThreshold)
			{
				fd = GetFallbackForward(gravityUp);
			}

			return GetPlanarDirection(Quaternion.AngleAxis(blackboard.m_CurrentFaceAngle, gravityUp) * fd, gravityUp);
		}

		private Vector3 GetCurrentVisualFaceDirection(PlayerBlackboard blackboard, Vector3 fd, Vector3 gravityUp)
		{
			if (m_Controller.m_CharacterRoot != null)
			{
				Vector3 visualFaceDirection = GetPlanarDirection(m_Controller.m_CharacterRoot.forward, gravityUp);
				if (visualFaceDirection.sqrMagnitude > DirectionThreshold)
				{
					return visualFaceDirection;
				}
			}

			return GetVisualFaceDirection(blackboard, fd, gravityUp);
		}

		private bool ShouldUseStandingTurn(PlayerBlackboard blackboard)
		{
			if (!blackboard.m_IsGrounded)
			{
				return false;
			}

			if (!m_Controller.m_EnableStandingSecondaryMouseTurn)
			{
				return false;
			}

			if (GetDiscreteInput(blackboard.m_MoveInput).sqrMagnitude > DirectionThreshold)
			{
				return false;
			}

			return blackboard.m_MoveVelocity.sqrMagnitude <= DirectionThreshold;
		}

		private Vector2 GetDiscreteInput(Vector2 input)
		{
			float x = Mathf.Abs(input.x) >= InputThreshold ? Mathf.Sign(input.x) : 0f;
			float y = Mathf.Abs(input.y) >= InputThreshold ? Mathf.Sign(input.y) : 0f;
			return new Vector2(x, y);
		}

		private void UpdateStandingTurn(PlayerBlackboard blackboard, Vector3 visualFaceDirection, Vector3 fd, Vector3 gravityUp)
		{
			float currentAngle = Vector3.SignedAngle(fd, visualFaceDirection, gravityUp);
			float threshold = Mathf.Max(0f, m_Controller.m_StandingTurnThresholdAngle);
			if (Mathf.Abs(currentAngle) <= threshold)
			{
				blackboard.m_CurrentFaceAngle = currentAngle;
				blackboard.m_TargetFaceAngle = currentAngle;
				return;
			}

			float clampedAngle = Mathf.Sign(currentAngle) * threshold;
			blackboard.m_CurrentFaceAngle = clampedAngle;
			blackboard.m_TargetFaceAngle = clampedAngle;
			float animationThreshold = threshold + Mathf.Max(0f, m_Controller.m_StandingTurnAnimationExtraAngle);
			if (Mathf.Abs(currentAngle) >= animationThreshold && blackboard.m_StandingTurnCooldown <= 0f)
			{
				RequestTurn(blackboard, currentAngle);
			}
		}

		private void FinishStandingTurn(PlayerBlackboard blackboard)
		{
			if (Mathf.Abs(blackboard.m_CurrentFaceAngle) <= FaceAngleThreshold)
			{
				return;
			}

			float currentAngle = blackboard.m_CurrentFaceAngle;
			blackboard.m_TargetFaceAngle = 0f;
			RequestTurn(blackboard, currentAngle);
		}

		private void RequestTurn(PlayerBlackboard blackboard, float currentAngle)
		{
			blackboard.m_RequestTurn = true;
			blackboard.m_TurnIsRight = currentAngle < 0f;
			blackboard.m_TurnAnimationTimer = Mathf.Max(0f, m_Controller.m_TurnAnimationLockDuration);
			blackboard.m_StandingTurnCooldown = Mathf.Max(0f, m_Controller.m_StandingTurnInterval);
		}
	}
}

