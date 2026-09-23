// Copyright (c) 2026 CatRabbit. All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace CR
{
	[CreateAssetMenu(menuName = "CatRabbit/Config/Spell Definition")]
	public class SpellDefinition : ScriptableObject
	{
		public int m_Id;
		public int m_FamilyId;
		[Min(1)] public int m_Rank = 1;
		public string m_DisplayName;
		[TextArea] public string m_Description;
		public Sprite m_Icon;
		public SpellSchool m_School;
		public SpellCastData m_Cast = new SpellCastData();
		public SpellTargetData m_Target = new SpellTargetData();
		public List<SpellCostData> m_CostList = new List<SpellCostData>();
		public SpellCooldownData m_Cooldown = new SpellCooldownData();
		[SerializeReference] public List<SpellEffect> m_EffectList = new List<SpellEffect>();
		public SpellPresentationData m_Presentation = new SpellPresentationData();
	}
}


