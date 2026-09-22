// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	public class PlayerJumpProcessor : IPlayerProcessor
	{
		private const float InputThreshold = 0.5f;

		private PlayerController m_Controller;

		public PlayerJumpProcessor(PlayerController controller)
		{
			m_Controller = controller;
		}

		public void OnUpdate(PlayerBlackboard blackboard, float dt)
		{
			if (!blackboard.m_WasJumpPressed || m_Controller.m_PlayerMotor == null)
			{
				return;
			}

			if (m_Controller.m_PlayerMotor.IsOnGround())
			{
				blackboard.m_WasStandingJumpRequested = Mathf.Abs(blackboard.m_MoveInput.x) < InputThreshold && Mathf.Abs(blackboard.m_MoveInput.y) < InputThreshold;
				m_Controller.m_PlayerMotor.RequestJump();
			}
		}

		public void OnFixedUpdate(PlayerBlackboard blackboard, float dt)
		{
		}

		public void OnLateUpdate(PlayerBlackboard blackboard, float dt)
		{
		}
	}
}

