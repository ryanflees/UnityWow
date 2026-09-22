// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	public abstract class RuntimeInputProvider : MonoBehaviour
	{
		public bool m_IsInputEnabled = true;

		public bool IsInputEnabled
		{
			get
			{
				return m_IsInputEnabled && isActiveAndEnabled;
			}
		}

		public virtual void OnAttached(RuntimeInputManager inputManager)
		{
		}

		public virtual void OnDetached(RuntimeInputManager inputManager)
		{
		}

		public RuntimeInputState ReadInput(float dt)
		{
			if (!IsInputEnabled)
			{
				return RuntimeInputState.Empty;
			}

			return ReadInputOverride(dt);
		}

		protected abstract RuntimeInputState ReadInputOverride(float dt);
	}
}

