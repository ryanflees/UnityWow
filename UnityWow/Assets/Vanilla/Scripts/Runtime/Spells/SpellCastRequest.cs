// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	public readonly struct SpellCastRequest
	{
		public readonly int m_SpellId;
		public readonly Quaternion m_FacingRotation;

		public SpellCastRequest(int spellId, Quaternion facingRotation)
		{
			m_SpellId = spellId;
			m_FacingRotation = facingRotation;
		}
	}

	public enum SpellCastResult
	{
		Success,
		Disabled,
		UnknownSpell,
		InvalidDefinition,
		UnsupportedSpell,
		PresentationUnavailable,
		GlobalCooldown
	}
}
