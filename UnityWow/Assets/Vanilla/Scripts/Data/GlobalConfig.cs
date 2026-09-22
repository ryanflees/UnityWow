// Copyright (c) 2026 CatRabbit. All rights reserved.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CR
{
	[CreateAssetMenu(menuName = "CatRabbit/GlobalConfig")]
	public class GlobalConfig : ScriptableObject
	{
		public List<ConfigCollectionBase> m_ConfigCollectionBaseList = new List<ConfigCollectionBase>();

		public T GetConfigCollection<T>() where T : ConfigCollectionBase
		{
			foreach (var config in m_ConfigCollectionBaseList)
			{
				if (config is T targetConfig)
				{
					return (T)targetConfig;
				}
			}
        
			Debug.LogWarning($"ConfigCollection of type {typeof(T).Name} not found in GlobalConfig");
			return null;
		}
	}
}

