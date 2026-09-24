// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	[DisallowMultipleComponent]
	public class ActorAttributes : MonoBehaviour
	{
		[Min(0f), Tooltip("50 means 50% haste: a 1.5-second global cooldown becomes 1 second.")]
		public float m_HastePercent;

		public float GetGlobalCooldownDuration(float baseDuration)
		{
			float haste = float.IsNaN(m_HastePercent) || float.IsInfinity(m_HastePercent) ? 0f : Mathf.Max(0f, m_HastePercent);
			return Mathf.Max(0f, baseDuration) / (1f + haste / 100f);
		}
	}
}

