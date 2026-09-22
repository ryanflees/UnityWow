// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	public class PlayerCharacterProcessor : IPlayerProcessor
	{
		private const float MoveThreshold = 0.0001f;

		private PlayerController m_Controller;

		public PlayerCharacterProcessor(PlayerController controller)
		{
			m_Controller = controller;
		}

		public void OnUpdate(PlayerBlackboard blackboard, float dt)
		{
			Character character = m_Controller.m_Character;
			if (character == null)
			{
				return;
			}

			if (blackboard.m_WasJumpStarted)
			{
				blackboard.m_WasJumpStarted = false;
				blackboard.m_JumpStartAnimationTimer = Mathf.Max(0f, m_Controller.m_JumpStartAnimationLockDuration);
				character.PlayJumpStartAnimation(m_Controller.m_JumpTransitionDuration);
				return;
			}

			if (blackboard.m_WasGroundedThisFrame)
			{
				blackboard.m_WasGroundedThisFrame = false;
				if (blackboard.m_LandedAirborneDuration < m_Controller.m_MinAirborneDurationForLandingAnimation)
				{
					blackboard.m_LandedAirborneDuration = 0f;
					PlayGroundLocomotionAnimation(character, blackboard);
					return;
				}

				blackboard.m_LandedAirborneDuration = 0f;
				PlayJumpLandAnimation(character, blackboard);
				return;
			}

			if (blackboard.m_JumpStartAnimationTimer > 0f)
			{
				blackboard.m_JumpStartAnimationTimer = Mathf.Max(0f, blackboard.m_JumpStartAnimationTimer - dt);
				return;
			}

			if (!blackboard.m_IsGrounded)
			{
				PlayJumpLoopAfterAirborneLocomotionDelay(character, blackboard);
				return;
			}

			if (character.IsPlayingJumpLandingAnimation())
			{
				if (character.IsPlayingJumpLandRunAnimation())
				{
					if (blackboard.m_MoveVelocity.sqrMagnitude <= MoveThreshold)
					{
						character.PlayStand();
						return;
					}

					if (blackboard.m_IsBackMove)
					{
						character.PlayMoveBackward();
						return;
					}
				}

				if (blackboard.m_MoveVelocity.sqrMagnitude <= MoveThreshold || !character.IsPlayingJumpEndAnimation())
				{
					return;
				}
			}

			if (blackboard.m_MoveVelocity.sqrMagnitude <= MoveThreshold && blackboard.m_RequestTurn)
			{
				character.PlayTurn(blackboard.m_TurnIsRight, m_Controller.m_TurnTransitionDuration);
				blackboard.m_RequestTurn = false;
				return;
			}

			if (blackboard.m_TurnAnimationTimer > 0f)
			{
				blackboard.m_TurnAnimationTimer = Mathf.Max(0f, blackboard.m_TurnAnimationTimer - dt);
				if (blackboard.m_MoveVelocity.sqrMagnitude <= MoveThreshold)
				{
					return;
				}
			}

			blackboard.m_RequestTurn = false;
			PlayGroundLocomotionAnimation(character, blackboard);
		}

		private void PlayGroundLocomotionAnimation(Character character, PlayerBlackboard blackboard)
		{
			if (blackboard.m_MoveVelocity.sqrMagnitude <= MoveThreshold)
			{
				character.PlayStand();
				return;
			}

			if (blackboard.m_IsBackMove)
			{
				character.PlayMoveBackward();
				return;
			}

			character.PlayMoveForward();
		}

		public void OnFixedUpdate(PlayerBlackboard blackboard, float dt)
		{
		}

		public void OnLateUpdate(PlayerBlackboard blackboard, float dt)
		{
			Character character = m_Controller.m_Character;
			if (character == null)
			{
				return;
			}

			character.SetLookAtDirection(blackboard.m_ReferenceFaceDirection, blackboard.m_GravityUp);
		}

		private void PlayJumpLandAnimation(Character character, PlayerBlackboard blackboard)
		{
			if (blackboard.m_MoveVelocity.sqrMagnitude <= MoveThreshold)
			{
				character.PlayJumpEndAnimation(m_Controller.m_JumpTransitionDuration);
				return;
			}

			if (blackboard.m_IsBackMove)
			{
				character.PlayMoveBackward();
				return;
			}

			character.PlayJumpLandRunAnimation(m_Controller.m_JumpTransitionDuration);
		}

		private void PlayJumpLoopAfterAirborneLocomotionDelay(Character character, PlayerBlackboard blackboard)
		{
			if (!character.IsPlayingGroundLocomotionAnimation())
			{
				return;
			}

			if (blackboard.m_AirborneDuration < m_Controller.m_AirborneLocomotionToJumpLoopDelay)
			{
				return;
			}

			character.PlayJumpLoopAnimation(m_Controller.m_JumpTransitionDuration);
		}
	}
}

