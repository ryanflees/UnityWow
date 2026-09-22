// Copyright (c) 2026 CatRabbit. All rights reserved.

namespace CR
{
	public class PlayerCameraProcessor : IPlayerProcessor
	{
		private PlayerController m_Controller;

		public PlayerCameraProcessor(PlayerController controller)
		{
			m_Controller = controller;
		}

		public void OnUpdate(PlayerBlackboard blackboard, float dt)
		{
			if (m_Controller.m_TPCameraController != null)
			{
				m_Controller.m_TPCameraController.UpdateCameraInput(blackboard.m_LookInput, blackboard.m_ZoomInput, dt);
			}
		}

		public void OnFixedUpdate(PlayerBlackboard blackboard, float dt)
		{
		}

		public void OnLateUpdate(PlayerBlackboard blackboard, float dt)
		{
			if (m_Controller.m_TPCameraController != null)
			{
				m_Controller.m_TPCameraController.ExecuteLateUpdate(dt);
			}
		}
	}
}

