// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	public class RuntimeInputManager : RuntimeMonoSingleton<RuntimeInputManager>
	{
		public RuntimeInputProvider m_InputProvider;
		public bool m_AutoFindInputProvider = true;
		private RuntimeInputState m_CurrentInput;

		public static RuntimeInputState CurrentInput
		{
			get
			{
				if (Instance != null)
				{
					return Instance.m_CurrentInput;
				}

				return RuntimeInputState.Empty;
			}
		}

		public void SetInputProvider(RuntimeInputProvider inputProvider)
		{
			if (m_InputProvider == inputProvider)
			{
				return;
			}

			if (m_InputProvider != null)
			{
				m_InputProvider.OnDetached(this);
			}

			m_InputProvider = inputProvider;

			if (m_InputProvider != null)
			{
				m_InputProvider.OnAttached(this);
			}
		}

		public override void OnUpdate(float dt)
		{
			base.OnUpdate(dt);
			ResolveInputProvider();

			if (m_InputProvider != null)
			{
				m_CurrentInput = m_InputProvider.ReadInput(dt);
			}
			else
			{
				m_CurrentInput = RuntimeInputState.Empty;
			}
		}

		protected override void OnDestroyOverride()
		{
			base.OnDestroyOverride();
			SetInputProvider(null);
		}

		private void ResolveInputProvider()
		{
			if (m_InputProvider != null || !m_AutoFindInputProvider)
			{
				return;
			}

			RuntimeInputProvider[] inputProviders = FindObjectsByType<RuntimeInputProvider>(FindObjectsSortMode.InstanceID);

			for (int i = 0; i < inputProviders.Length; i++)
			{
				RuntimeInputProvider inputProvider = inputProviders[i];

				if (inputProvider != null && inputProvider.IsInputEnabled)
				{
					SetInputProvider(inputProvider);
					return;
				}
			}
		}
	}
}

