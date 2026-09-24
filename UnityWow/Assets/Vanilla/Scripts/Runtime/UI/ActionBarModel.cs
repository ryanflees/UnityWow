// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using UnityEngine;

namespace CR
{
	[Serializable]
	public class ActionBarModel
	{
		[SerializeField] private int[] m_SpellIds = new int[12];

		public int GetSpellId(int slotIndex)
		{
			return m_SpellIds != null && slotIndex >= 0 && slotIndex < m_SpellIds.Length ? m_SpellIds[slotIndex] : 0;
		}

		public void BindSpell(int slotIndex, int spellId)
		{
			if (m_SpellIds == null || slotIndex < 0 || slotIndex >= m_SpellIds.Length)
				throw new ArgumentOutOfRangeException(nameof(slotIndex));
			if (spellId < 0) throw new ArgumentOutOfRangeException(nameof(spellId));
			m_SpellIds[slotIndex] = spellId;
		}
	}
}

