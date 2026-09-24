// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	public struct RuntimeInputState
	{
		public Vector2 m_Move;
		public Vector2 m_Look;
		public float m_Zoom;
		public RuntimeInputButton m_PrimaryMouse;
		public RuntimeInputButton m_SecondaryMouse;
		public RuntimeInputButton m_MoveForward;
		public RuntimeInputButton m_MoveBackward;
		public RuntimeInputButton m_MoveLeft;
		public RuntimeInputButton m_MoveRight;
		public RuntimeInputButton m_Jump;
		public RuntimeInputButton m_ActionSlot1;
		public RuntimeInputButton m_ActionSlot4;
		public RuntimeInputButton m_AutoRun;
		public RuntimeInputButton m_Interact;
		public RuntimeInputButton m_Target;
		public RuntimeInputButton m_Cancel;

		public static RuntimeInputState Empty
		{
			get
			{
				return new RuntimeInputState();
			}
		}
	}
}

