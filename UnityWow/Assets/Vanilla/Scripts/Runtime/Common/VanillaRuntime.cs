// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	public class VanillaRuntime : RuntimeMonoSingleton<VanillaRuntime>
	{
		public GlobalConfig m_GlobalConfig;
		
		protected override void OnAwake()
		{
			base.OnAwake();
			RuntimeInputManager.CreateModule(true);
		}

		private void Update()
		{
			float dt = Time.deltaTime;

			if (RuntimeInputManager.Instance != null)
			{
				RuntimeInputManager.Instance.OnUpdate(dt);
			}
		}

		private void FixedUpdate()
		{
			float dt = Time.fixedDeltaTime;

			if (RuntimeInputManager.Instance != null)
			{
				RuntimeInputManager.Instance.OnFixedUpdate(dt);
			}
		}

		private void LateUpdate()
		{
			float dt = Time.deltaTime;

			if (RuntimeInputManager.Instance != null)
			{
				RuntimeInputManager.Instance.OnLateUpdate(dt);
			}
		}
		
		public static T GetConfigCollection<T>() where T : ConfigCollectionBase
		{
			if (Instance && Instance.m_GlobalConfig != null)
			{
				return Instance.m_GlobalConfig.GetConfigCollection<T>();
			}
			return null;
		}
	}
}

