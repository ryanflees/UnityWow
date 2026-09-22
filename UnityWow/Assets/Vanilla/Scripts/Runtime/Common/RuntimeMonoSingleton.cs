// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	public abstract class RuntimeMonoSingleton<T> : MonoBehaviour where T : RuntimeMonoSingleton<T>
	{
		public static T Instance { get; private set; }

		public static T GetInstanceByForce()
		{
			if (Instance == null)
			{
				Instance = Object.FindFirstObjectByType<T>();
			}

			return Instance;
		}

		public static T CreateModule(bool persistent = false)
		{
			if (Instance != null)
			{
				return Instance;
			}

			GameObject moduleObject = new GameObject(typeof(T).Name);
			T module = moduleObject.AddComponent<T>();

			if (persistent)
			{
				Object.DontDestroyOnLoad(moduleObject);
			}

			return module;
		}

		protected void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = (T)this;

			if (transform.parent == null)
			{
				DontDestroyOnLoad(gameObject);
			}

			OnAwake();
		}

		protected void Start()
		{
			OnStart();
		}

		protected void OnDestroy()
		{
			OnDestroyOverride();

			if (Instance == this)
			{
				Instance = null;
			}
		}

		protected void OnApplicationQuit()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		protected virtual void OnAwake()
		{
		}

		protected virtual void OnStart()
		{
		}

		protected virtual void OnDestroyOverride()
		{
		}

		public virtual void OnUpdate(float dt)
		{
		}

		public virtual void OnFixedUpdate(float dt)
		{
		}

		public virtual void OnLateUpdate(float dt)
		{
		}
	}
}

