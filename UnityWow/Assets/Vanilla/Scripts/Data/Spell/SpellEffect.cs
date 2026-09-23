// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;

namespace CR
{
	[Serializable]
	public abstract class SpellEffect
	{
		public int m_Index;
		public SpellEffectTrigger m_Trigger;
		public float m_Delay;
		public bool m_UseSpellTarget = true;
		public SpellTargetData m_Target = new SpellTargetData();
	}

	[Serializable]
	public class DamageSpellEffect : SpellEffect
	{
		public SpellSchool m_School;
		public SpellValueData m_Value = new SpellValueData();
		public bool m_CanCritical = true;
	}

	[Serializable]
	public class HealSpellEffect : SpellEffect
	{
		public SpellValueData m_Value = new SpellValueData();
		public bool m_CanCritical = true;
	}

	[Serializable]
	public class ApplyAuraSpellEffect : SpellEffect
	{
		public int m_AuraId;
		public float m_Duration;
		public int m_StackCount = 1;
	}

	[Serializable]
	public class ModifyResourceSpellEffect : SpellEffect
	{
		public SpellResourceType m_ResourceType;
		public SpellValueData m_Value = new SpellValueData();
	}

	[Serializable]
	public class TriggerSpellEffect : SpellEffect
	{
		public int m_TriggeredSpellId;
	}
}


