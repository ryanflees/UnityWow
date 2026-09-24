// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using System.Collections.Generic;

namespace CR
{
	public enum SpellValidationSeverity
	{
		Warning,
		Error
	}

	public sealed class SpellValidationIssue
	{
		public readonly SpellValidationSeverity m_Severity;
		public readonly string m_Path;
		public readonly string m_Message;

		public SpellValidationIssue(SpellValidationSeverity severity, string path, string message)
		{
			m_Severity = severity;
			m_Path = path;
			m_Message = message;
		}
	}

	public static class SpellDefinitionValidator
	{
		public static List<SpellValidationIssue> Validate(SpellDefinition spell, bool requireCombatContent = false)
		{
			List<SpellValidationIssue> issues = new List<SpellValidationIssue>();
			if (spell == null)
			{
				AddError(issues, "spell", "Spell definition is missing.");
				return issues;
			}
			if (spell.m_Id <= 0) AddError(issues, "m_Id", "Use a positive, unique spell id.");
			if (spell.m_Rank < 1) AddError(issues, "m_Rank", "Rank must be at least one.");
			if (string.IsNullOrWhiteSpace(spell.m_DisplayName)) AddError(issues, "m_DisplayName", "Display name is required.");
			if (spell.m_Icon == null) AddContentIssue(issues, requireCombatContent, "m_Icon", "No Sprite is assigned.");
			ValidateCast(spell, issues);
			ValidateTarget(spell.m_Target, "m_Target", issues);
			ValidateCooldown(spell.m_Cooldown, issues);
			ValidateCosts(spell, issues);
			ValidateEffects(spell, issues, requireCombatContent);
			ValidatePresentation(spell, issues);
			return issues;
		}

		private static void ValidateCast(SpellDefinition spell, List<SpellValidationIssue> issues)
		{
			SpellCastData cast = spell.m_Cast;
			if (cast == null)
			{
				AddError(issues, "m_Cast", "Cast settings are missing.");
				return;
			}
			ValidateEnum(cast.m_Type, "m_Cast.m_Type", issues);
			ValidateNumber(cast.m_CastTime, "m_Cast.m_CastTime", issues, cast.m_Type == SpellCastType.CastTime);
			ValidateNumber(cast.m_ChannelDuration, "m_Cast.m_ChannelDuration", issues, cast.m_Type == SpellCastType.Channeled);
			ValidateNumber(cast.m_ChannelInterval, "m_Cast.m_ChannelInterval", issues, cast.m_Type == SpellCastType.Channeled);
			if (cast.m_Type == SpellCastType.Channeled && cast.m_ChannelInterval > cast.m_ChannelDuration)
				AddError(issues, "m_Cast.m_ChannelInterval", "The interval exceeds the duration; no channel tick can occur.");
			if (cast.m_Type != SpellCastType.CastTime && cast.m_CastTime != 0f)
				AddError(issues, "m_Cast.m_CastTime", "Only CastTime spells use a cast time.");
			if (cast.m_Type != SpellCastType.Channeled && (cast.m_ChannelDuration != 0f || cast.m_ChannelInterval != 0f))
				AddError(issues, "m_Cast", "Only Channeled spells use channel timing.");
		}

		private static void ValidateTarget(SpellTargetData target, string path, List<SpellValidationIssue> issues)
		{
			if (target == null)
			{
				AddError(issues, path, "Target settings are missing.");
				return;
			}
			ValidateEnum(target.m_Type, path + ".m_Type", issues);
			ValidateNumber(target.m_MinRange, path + ".m_MinRange", issues);
			ValidateNumber(target.m_MaxRange, path + ".m_MaxRange", issues);
			ValidateNumber(target.m_Radius, path + ".m_Radius", issues);
			ValidateNumber(target.m_Angle, path + ".m_Angle", issues);
			if (target.m_MinRange > target.m_MaxRange) AddError(issues, path, "Minimum range exceeds maximum range.");
			if (target.m_Angle > 360f) AddError(issues, path + ".m_Angle", "Angle must be at most 360 degrees.");
			if (target.m_MaxTargetCount < 1) AddError(issues, path + ".m_MaxTargetCount", "Use a positive target limit.");
			SpellTargetRelation livingRelations = SpellTargetRelation.Self | SpellTargetRelation.Friendly |
				SpellTargetRelation.Hostile | SpellTargetRelation.Neutral;
			if ((target.m_AllowedRelations & ~(livingRelations | SpellTargetRelation.Dead)) != 0)
				AddError(issues, path + ".m_AllowedRelations", "Unknown target relation flags.");
			if (target.m_Type != SpellTargetType.None && target.m_Type != SpellTargetType.Self &&
				(target.m_AllowedRelations & livingRelations) == 0)
				AddError(issues, path + ".m_AllowedRelations", "Choose a relation; Dead is a life-state modifier, not a faction.");
		}

		private static void ValidateCooldown(SpellCooldownData cooldown, List<SpellValidationIssue> issues)
		{
			if (cooldown == null)
			{
				AddError(issues, "m_Cooldown", "Cooldown settings are missing.");
				return;
			}
			ValidateNumber(cooldown.m_Cooldown, "m_Cooldown.m_Cooldown", issues);
			ValidateNumber(cooldown.m_GlobalCooldown, "m_Cooldown.m_GlobalCooldown", issues);
			ValidateNumber(cooldown.m_ChargeRecoveryTime, "m_Cooldown.m_ChargeRecoveryTime", issues, cooldown.m_MaxCharges > 1);
			if (cooldown.m_MaxCharges < 1) AddError(issues, "m_Cooldown.m_MaxCharges", "Maximum charges must be at least one.");
			if (cooldown.m_SharedCooldownGroup < 0) AddError(issues, "m_Cooldown.m_SharedCooldownGroup", "Use zero for no shared group or a positive group id.");
		}

		private static void ValidateCosts(SpellDefinition spell, List<SpellValidationIssue> issues)
		{
			if (spell.m_CostList == null)
			{
				AddError(issues, "m_CostList", "Cost list is missing; use an empty list for free spells.");
				return;
			}
			for (int i = 0; i < spell.m_CostList.Count; i++)
			{
				SpellCostData cost = spell.m_CostList[i];
				string path = $"m_CostList[{i}]";
				if (cost == null) { AddError(issues, path, "Cost is missing."); continue; }
				ValidateEnum(cost.m_ResourceType, path + ".m_ResourceType", issues);
				ValidateEnum(cost.m_Timing, path + ".m_Timing", issues);
				ValidateNumber(cost.m_Amount, path + ".m_Amount", issues);
				if (cost.m_Amount > 0f && cost.m_ResourceType == SpellResourceType.None)
					AddError(issues, path, "A positive cost requires a resource type.");
				if (cost.m_Timing == SpellCostTiming.ChannelTick && spell.m_Cast != null && spell.m_Cast.m_Type != SpellCastType.Channeled)
					AddError(issues, path + ".m_Timing", "ChannelTick costs require a channeled spell.");
			}
		}

		private static void ValidateEffects(SpellDefinition spell, List<SpellValidationIssue> issues, bool requireCombatContent)
		{
			if (spell.m_EffectList == null)
			{
				AddError(issues, "m_EffectList", "Effect list is missing.");
				return;
			}
			if (spell.m_EffectList.Count == 0)
				AddContentIssue(issues, requireCombatContent, "m_EffectList", "No gameplay effects are configured; this is an animation preview.");
			HashSet<int> indices = new HashSet<int>();
			for (int i = 0; i < spell.m_EffectList.Count; i++)
			{
				SpellEffect effect = spell.m_EffectList[i];
				string path = $"m_EffectList[{i}]";
				if (effect == null) { AddError(issues, path, "Effect is missing or its serialized type cannot be loaded."); continue; }
				if (effect.m_Index < 0 || !indices.Add(effect.m_Index)) AddError(issues, path + ".m_Index", "Effect indices must be nonnegative and unique within a spell.");
				ValidateEnum(effect.m_Trigger, path + ".m_Trigger", issues);
				ValidateNumber(effect.m_Delay, path + ".m_Delay", issues);
				if (!effect.m_UseSpellTarget) ValidateTarget(effect.m_Target, path + ".m_Target", issues);
				if (effect.m_Trigger == SpellEffectTrigger.ChannelTick && spell.m_Cast != null && spell.m_Cast.m_Type != SpellCastType.Channeled)
					AddError(issues, path + ".m_Trigger", "ChannelTick effects require a channeled spell.");
				if (effect.m_Trigger == SpellEffectTrigger.ProjectileImpact && (spell.m_Presentation == null ||
					spell.m_Presentation.m_ProjectilePrefab == null || spell.m_Presentation.m_ProjectileSpeed <= 0f))
					AddError(issues, path + ".m_Trigger", "ProjectileImpact requires a projectile prefab and positive speed.");
				switch (effect)
				{
					case DamageSpellEffect damage: ValidateValue(damage.m_Value, path + ".m_Value", issues, false); break;
					case HealSpellEffect heal: ValidateValue(heal.m_Value, path + ".m_Value", issues, false); break;
					case ModifyResourceSpellEffect resource:
						ValidateEnum(resource.m_ResourceType, path + ".m_ResourceType", issues);
						if (resource.m_ResourceType == SpellResourceType.None) AddError(issues, path, "Resource effects require a resource type.");
						ValidateValue(resource.m_Value, path + ".m_Value", issues, true);
						break;
					case ApplyAuraSpellEffect aura:
						if (aura.m_AuraId <= 0 || aura.m_StackCount < 1) AddError(issues, path, "Auras require a positive id and stack count.");
						ValidateNumber(aura.m_Duration, path + ".m_Duration", issues);
						break;
					case TriggerSpellEffect trigger:
						if (trigger.m_TriggeredSpellId <= 0) AddError(issues, path, "Triggered spell id must be positive.");
						if (trigger.m_TriggeredSpellId == spell.m_Id) AddError(issues, path, "A spell cannot directly trigger itself.");
						break;
				}
			}
		}

		private static void ValidatePresentation(SpellDefinition spell, List<SpellValidationIssue> issues)
		{
			SpellPresentationData presentation = spell.m_Presentation;
			if (presentation == null) { AddError(issues, "m_Presentation", "Presentation settings are missing."); return; }
			ValidateEnum(presentation.m_AnimationType, "m_Presentation.m_AnimationType", issues);
			ValidateNumber(presentation.m_ProjectileSpeed, "m_Presentation.m_ProjectileSpeed", issues, presentation.m_ProjectilePrefab != null);
			if (spell.m_Cast == null || presentation.m_AnimationType == SpellAnimationType.None) return;
			bool channelAnimation = presentation.m_AnimationType == SpellAnimationType.ChannelDirected ||
				presentation.m_AnimationType == SpellAnimationType.ChannelOmnidirectional;
			if (channelAnimation != (spell.m_Cast.m_Type == SpellCastType.Channeled))
				AddError(issues, "m_Presentation.m_AnimationType", "Channel animations require Channeled; active cast/release spells require a cast animation.");
			if (spell.m_Cast.m_Type == SpellCastType.Passive)
				AddError(issues, "m_Presentation.m_AnimationType", "Passive spells do not request an active cast animation.");
			if (spell.m_Cast.m_CanMoveWhileCasting && (spell.m_Cast.m_Type == SpellCastType.CastTime || spell.m_Cast.m_Type == SpellCastType.Channeled))
				issues.Add(new SpellValidationIssue(SpellValidationSeverity.Warning, "m_Cast.m_CanMoveWhileCasting",
					"Moving cast/channel overlays are not implemented; current movement blending covers release animations only."));
		}

		private static void ValidateValue(SpellValueData value, string path, List<SpellValidationIssue> issues, bool allowNegative)
		{
			if (value == null) { AddError(issues, path, "Value settings are missing."); return; }
			ValidateEnum(value.m_ScalingAttribute, path + ".m_ScalingAttribute", issues);
			if (allowNegative)
			{
				if (!IsFinite(value.m_BaseValue)) AddError(issues, path + ".m_BaseValue", "Value must be finite.");
			}
			else ValidateNumber(value.m_BaseValue, path + ".m_BaseValue", issues);
			if (!IsFinite(value.m_ScalingCoefficient)) AddError(issues, path + ".m_ScalingCoefficient", "Coefficient must be finite.");
		}

		private static void ValidateEnum<T>(T value, string path, List<SpellValidationIssue> issues) where T : Enum
		{
			if (!Enum.IsDefined(typeof(T), value)) AddError(issues, path, "Unknown enum value.");
		}

		private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

		private static void ValidateNumber(float value, string path, List<SpellValidationIssue> issues, bool positive = false)
		{
			if (!IsFinite(value) || value < 0f || (positive && value == 0f))
				AddError(issues, path, positive ? "Use a finite positive number." : "Use a finite nonnegative number.");
		}

		private static void AddContentIssue(List<SpellValidationIssue> issues, bool required, string path, string message)
		{
			issues.Add(new SpellValidationIssue(required ? SpellValidationSeverity.Error : SpellValidationSeverity.Warning, path, message));
		}

		private static void AddError(List<SpellValidationIssue> issues, string path, string message)
		{
			issues.Add(new SpellValidationIssue(SpellValidationSeverity.Error, path, message));
		}
	}
}

