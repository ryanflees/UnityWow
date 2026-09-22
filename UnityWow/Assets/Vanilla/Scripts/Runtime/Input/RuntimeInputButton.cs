// Copyright (c) 2026 CatRabbit. All rights reserved.

namespace CR
{
	public struct RuntimeInputButton
	{
		public bool m_IsPressed;
		public bool m_WasPressed;
		public bool m_WasReleased;

		public RuntimeInputButton(bool isPressed, bool wasPressed, bool wasReleased)
		{
			m_IsPressed = isPressed;
			m_WasPressed = wasPressed;
			m_WasReleased = wasReleased;
		}
	}
}

