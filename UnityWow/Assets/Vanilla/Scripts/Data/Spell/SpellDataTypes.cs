// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using UnityEngine;

namespace CR
{
	[Serializable]
	public class SpellCastData
	{
		public SpellCastType m_Type;
		[Min(0f)] public float m_CastTime;
		[Min(0f)] public float m_ChannelDuration;
		[Min(0f)] public float m_ChannelInterval;
		public bool m_CanMoveWhileCasting;
		public bool m_RequireFacingTarget = true;
		public bool m_IsInterruptible = true;
	}

	[Serializable]
	public class SpellTargetData
	{
		public SpellTargetType m_Type;
		public SpellTargetRelation m_AllowedRelations;
		[Min(0f)] public float m_MinRange;
		[Min(0f)] public float m_MaxRange;
		[Min(0f)] public float m_Radius;
		[Range(0f, 360f)] public float m_Angle;
		[Min(0)] public int m_MaxTargetCount = 1;
		public bool m_RequireLineOfSight = true;
		public bool m_IncludeCaster;
	}

	[Serializable]
	public class SpellCostData
	{
		public SpellResourceType m_ResourceType;
		[Min(0f)] public float m_Amount;
		public SpellCostTiming m_Timing;
	}

	[Serializable]
	public class SpellCooldownData
	{
		[Min(0f)] public float m_Cooldown;
		[Tooltip("Starts the actor's global cooldown when the cast succeeds.")]
		public bool m_TriggersGlobalCooldown = true;
		[Tooltip("Blocks this spell during an existing global cooldown. Disable both switches for an off-GCD skill.")]
		public bool m_IsAffectedByGlobalCooldown = true;
		[Min(0f), Tooltip("Base duration in seconds before actor haste is applied.")]
		public float m_GlobalCooldown = 1.5f;
		public int m_SharedCooldownGroup;
		[Min(1)] public int m_MaxCharges = 1;
		[Min(0f)] public float m_ChargeRecoveryTime;
	}

	[Serializable]
	public class SpellValueData
	{
		public float m_BaseValue;
		public SpellScalingAttribute m_ScalingAttribute;
		public float m_ScalingCoefficient;
	}

	[Serializable]
	public class SpellPresentationData
	{
		public SpellAnimationType m_AnimationType;
		public GameObject m_CastEffectPrefab;
		public GameObject m_ProjectilePrefab;
		public GameObject m_ImpactEffectPrefab;
		public AudioClip m_CastAudio;
		public AudioClip m_ImpactAudio;
		[Min(0f)] public float m_ProjectileSpeed;
	}

	public enum SpellCastType
	{
		Instant,
		CastTime,
		Channeled,
		Passive
	}

	public enum SpellTargetType
	{
		None,
		Self,
		Unit,
		Ground,
		Direction
	}

	[Flags]
	public enum SpellTargetRelation
	{
		None = 0,
		Self = 1 << 0,
		Friendly = 1 << 1,
		Hostile = 1 << 2,
		Neutral = 1 << 3,
		Dead = 1 << 4
	}

	[Flags]
	public enum SpellSchool
	{
		None = 0,
		Physical = 1 << 0,
		Holy = 1 << 1,
		Fire = 1 << 2,
		Nature = 1 << 3,
		Frost = 1 << 4,
		Shadow = 1 << 5,
		Arcane = 1 << 6
	}

	public enum SpellResourceType
	{
		None,
		Health,
		Mana,
		Rage,
		Energy
	}

	public enum SpellCostTiming
	{
		CastStart,
		CastComplete,
		ChannelTick
	}

	public enum SpellEffectTrigger
	{
		CastComplete,
		ChannelTick,
		ProjectileImpact
	}

	public enum SpellScalingAttribute
	{
		None,
		AttackPower,
		SpellPower,
		MaximumHealth
	}

	public enum SpellAnimationType
	{
		None,
		CastDirected,
		CastOmnidirectional,
		ChannelDirected,
		ChannelOmnidirectional
	}
}
