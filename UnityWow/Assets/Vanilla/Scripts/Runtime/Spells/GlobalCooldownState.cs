// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;

namespace CR
{
	public sealed class GlobalCooldownState
	{
		private double m_EndsAt;
		private float m_Duration;
		public float Duration => m_Duration;

		public float GetRemaining(double now)
		{
			return (float)Math.Max(0d, m_EndsAt - now);
		}

		public void Start(double now, float duration)
		{
			if (double.IsNaN(now) || double.IsInfinity(now)) throw new ArgumentOutOfRangeException(nameof(now));
			if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f)
				throw new ArgumentOutOfRangeException(nameof(duration));
			if (duration == 0f || now + duration <= m_EndsAt) return;
			m_Duration = duration;
			m_EndsAt = now + duration;
		}
	}
}

