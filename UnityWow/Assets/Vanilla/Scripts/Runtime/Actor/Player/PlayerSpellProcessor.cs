// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	public class PlayerSpellProcessor : IPlayerProcessor
	{
		private readonly PlayerController m_Controller;

		public PlayerSpellProcessor(PlayerController controller)
		{
			m_Controller = controller;
		}

		public void OnUpdate(PlayerBlackboard blackboard, float dt)
		{
			if (blackboard.m_WasActionSlot1Pressed) m_Controller.TryActivateActionSlot(0);
			else if (blackboard.m_WasActionSlot4Pressed) m_Controller.TryActivateActionSlot(3);
			blackboard.m_WasActionSlot1Pressed = false;
			blackboard.m_WasActionSlot4Pressed = false;
			if (m_Controller.m_SpellController != null)
				m_Controller.m_SpellController.SetFacing(Quaternion.LookRotation(blackboard.m_ReferenceFaceDirection, blackboard.m_GravityUp));
		}

		public void OnFixedUpdate(PlayerBlackboard blackboard, float dt) { }
		public void OnLateUpdate(PlayerBlackboard blackboard, float dt) { }
	}
}
