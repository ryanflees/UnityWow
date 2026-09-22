// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	public class PlayerMovementProcessor : IPlayerProcessor
	{
		private const float InputThreshold = 0.5f;
		private const float DirectionThreshold = 0.0001f;

		private PlayerController m_Controller;

		public PlayerMovementProcessor(PlayerController controller)
		{
			m_Controller = controller;
		}

		public void OnUpdate(PlayerBlackboard blackboard, float dt)
		{
			if (!blackboard.m_IsGrounded && TryUpdateAirMove(blackboard))
			{
				return;
			}

			Vector2 discreteInput = GetDiscreteInput(blackboard.m_MoveInput);
			if (discreteInput.sqrMagnitude <= DirectionThreshold)
			{
				if (!blackboard.m_IsGrounded)
				{
					ResetMovementOnly(blackboard);
					return;
				}

				ResetMovementAndFaceReference(blackboard);
				return;
			}

			Vector3 gravityUp = GetGravityUp(blackboard);
			Vector3 fd = GetPlanarDirection(blackboard.m_ReferenceFaceDirection, gravityUp);
			Vector3 right = Vector3.Cross(gravityUp, fd).normalized;
			Vector3 md = fd * discreteInput.y + right * discreteInput.x;
			md = GetPlanarDirection(md, gravityUp);

			if (md.sqrMagnitude <= DirectionThreshold)
			{
				if (!blackboard.m_IsGrounded)
				{
					ResetMovementOnly(blackboard);
					return;
				}

				ResetMovementAndFaceReference(blackboard);
				return;
			}

			bool isBackMove = discreteInput.y < 0f;
			float speed = m_Controller.m_MoveSpeed;
			if (isBackMove)
			{
				speed *= Mathf.Clamp01(m_Controller.m_BackMoveSpeedMultiplier);
			}

			Vector3 targetFaceDirection = isBackMove ? -md : md;
			targetFaceDirection = GetPlanarDirection(targetFaceDirection, gravityUp);
			float targetFaceAngle = Vector3.SignedAngle(fd, targetFaceDirection, gravityUp);
			targetFaceAngle = Mathf.Clamp(targetFaceAngle, -90f, 90f);

			blackboard.m_MoveDirection = md;
			blackboard.m_MoveVelocity = md * speed;
			blackboard.m_IsBackMove = isBackMove;
			if (!blackboard.m_IsGrounded)
			{
				return;
			}

			blackboard.m_IsReturningToReferenceFace = false;

			blackboard.m_CharacterFaceDirection = targetFaceDirection;
			blackboard.m_TargetFaceAngle = targetFaceAngle;
		}

		public void OnFixedUpdate(PlayerBlackboard blackboard, float dt)
		{
		}

		public void OnLateUpdate(PlayerBlackboard blackboard, float dt)
		{
		}

		private Vector2 GetDiscreteInput(Vector2 input)
		{
			float x = Mathf.Abs(input.x) >= InputThreshold ? Mathf.Sign(input.x) : 0f;
			float y = Mathf.Abs(input.y) >= InputThreshold ? Mathf.Sign(input.y) : 0f;
			return new Vector2(x, y);
		}

		private bool TryUpdateAirMove(PlayerBlackboard blackboard)
		{
			if (blackboard.m_HasAirMoveIntent)
			{
				ApplyAirMoveIntent(blackboard);
				return true;
			}

			if (!blackboard.m_CanStartStandingJumpAirMove)
			{
				return false;
			}

			if (!TryGetSinglePressedAirMoveInput(blackboard, out Vector2 input))
			{
				return false;
			}

			Vector3 gravityUp = GetGravityUp(blackboard);
			Vector3 fd = GetPlanarDirection(blackboard.m_ReferenceFaceDirection, gravityUp);
			if (fd.sqrMagnitude <= DirectionThreshold)
			{
				return false;
			}

			Vector3 right = Vector3.Cross(gravityUp, fd).normalized;
			Vector3 direction = GetPlanarDirection(fd * input.y + right * input.x, gravityUp);
			if (direction.sqrMagnitude <= DirectionThreshold)
			{
				return false;
			}

			blackboard.m_CanStartStandingJumpAirMove = false;
			blackboard.m_HasAirMoveIntent = true;
			blackboard.m_AirMoveIntentDirection = direction;
			blackboard.m_AirMoveIntentSpeed = m_Controller.m_StandingJumpAirMoveSpeed;
			blackboard.m_AirMoveIntentControlsFacing = true;
			blackboard.m_AirMoveIntentKeepFacingForward = input.y != 0f;
			ApplyAirMoveIntent(blackboard);
			return true;
		}

		private bool TryGetSinglePressedAirMoveInput(PlayerBlackboard blackboard, out Vector2 input)
		{
			input = Vector2.zero;

			int pressedCount = 0;
			if (blackboard.m_WasMoveForwardPressed)
			{
				input = Vector2.up;
				pressedCount++;
			}

			if (blackboard.m_WasMoveBackwardPressed)
			{
				input = Vector2.down;
				pressedCount++;
			}

			if (blackboard.m_WasMoveLeftPressed)
			{
				input = Vector2.left;
				pressedCount++;
			}

			if (blackboard.m_WasMoveRightPressed)
			{
				input = Vector2.right;
				pressedCount++;
			}

			return pressedCount == 1;
		}

		private void ApplyAirMoveIntent(PlayerBlackboard blackboard)
		{
			Vector3 direction = blackboard.m_AirMoveIntentDirection;
			blackboard.m_MoveDirection = direction;
			blackboard.m_MoveVelocity = direction * blackboard.m_AirMoveIntentSpeed;
			blackboard.m_IsBackMove = false;

			if (!blackboard.m_AirMoveIntentControlsFacing)
			{
				return;
			}

			Vector3 gravityUp = GetGravityUp(blackboard);
			Vector3 fd = GetPlanarDirection(blackboard.m_ReferenceFaceDirection, gravityUp);
			if (fd.sqrMagnitude <= DirectionThreshold)
			{
				return;
			}

			Vector3 targetFaceDirection = blackboard.m_AirMoveIntentKeepFacingForward ? fd : direction;
			float targetFaceAngle = Vector3.SignedAngle(fd, targetFaceDirection, gravityUp);
			blackboard.m_CharacterFaceDirection = targetFaceDirection;
			blackboard.m_TargetFaceAngle = targetFaceAngle;
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

		private void ResetMovementAndFaceReference(PlayerBlackboard blackboard)
		{
			Vector3 gravityUp = GetGravityUp(blackboard);
			Vector3 fd = GetPlanarDirection(blackboard.m_ReferenceFaceDirection, gravityUp);

			ResetMovementOnly(blackboard);

			if (blackboard.m_IsSecondaryMouseRotating && m_Controller.m_EnableStandingSecondaryMouseTurn)
			{
				if (fd.sqrMagnitude > DirectionThreshold)
				{
					blackboard.m_CharacterFaceDirection = fd;
				}

				return;
			}

			blackboard.m_TargetFaceAngle = 0f;
			blackboard.m_IsReturningToReferenceFace = Mathf.Abs(blackboard.m_CurrentFaceAngle) > 0.1f;

			if (fd.sqrMagnitude > DirectionThreshold)
			{
				blackboard.m_CharacterFaceDirection = fd;
			}
		}

		private void ResetMovementOnly(PlayerBlackboard blackboard)
		{
			blackboard.m_MoveDirection = Vector3.zero;
			blackboard.m_MoveVelocity = Vector3.zero;
			blackboard.m_IsBackMove = false;
		}
	}
}

