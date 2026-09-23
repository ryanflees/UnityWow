// Copyright (c) 2026 CatRabbit. All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace CR
{
	[CreateAssetMenu(menuName = "CatRabbit/Config/Spell Config Collection")]
	public class SpellConfigCollection : ConfigCollectionBase
	{
		public List<SpellDefinition> m_SpellList = new List<SpellDefinition>();

		private Dictionary<int, SpellDefinition> m_SpellById;

		public bool TryGetSpell(int spellId, out SpellDefinition spell)
		{
			EnsureLookup();
			return m_SpellById.TryGetValue(spellId, out spell);
		}

		public SpellDefinition GetSpell(int spellId)
		{
			TryGetSpell(spellId, out SpellDefinition spell);
			return spell;
		}

		private void OnEnable()
		{
			RebuildLookup();
		}

		private void OnValidate()
		{
			RebuildLookup();
		}

		private void EnsureLookup()
		{
			if (m_SpellById == null)
			{
				RebuildLookup();
			}
		}

		private void RebuildLookup()
		{
			m_SpellById = new Dictionary<int, SpellDefinition>();
			for (int i = 0; i < m_SpellList.Count; i++)
			{
				SpellDefinition spell = m_SpellList[i];
				if (spell == null || spell.m_Id <= 0)
				{
					continue;
				}

				if (!m_SpellById.TryAdd(spell.m_Id, spell))
				{
					Debug.LogWarning($"Duplicate spell id {spell.m_Id} in {name}", this);
				}
			}
		}
	}
}


