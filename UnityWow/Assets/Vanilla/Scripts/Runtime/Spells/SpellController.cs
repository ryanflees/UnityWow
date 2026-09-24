// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using UnityEngine;

namespace CR
{
	[DisallowMultipleComponent]
	public class SpellController : MonoBehaviour
	{
		public SpellConfigCollection m_SpellCollection;
		public SpellPresentation m_Presentation;
		public ActorAttributes m_Attributes;
		public event Action<SpellDefinition> m_SpellReleased;
		private readonly GlobalCooldownState m_GlobalCooldown = new GlobalCooldownState();

		public float GlobalCooldownRemaining => m_GlobalCooldown.GetRemaining(Time.timeAsDouble);
		public float GlobalCooldownDuration => m_GlobalCooldown.Duration;

		public SpellCastResult TryCast(SpellCastRequest request)
		{
			if (!isActiveAndEnabled) return SpellCastResult.Disabled;
			if (m_SpellCollection == null || !m_SpellCollection.TryGetSpell(request.m_SpellId, out SpellDefinition spell))
				return SpellCastResult.UnknownSpell;
			foreach (SpellValidationIssue issue in SpellDefinitionValidator.Validate(spell))
				if (issue.m_Severity == SpellValidationSeverity.Error) return SpellCastResult.InvalidDefinition;
			// The first runtime slice deliberately accepts only free, targetless animation spells.
			if (spell.m_Cast.m_Type != SpellCastType.Instant || spell.m_Target.m_Type != SpellTargetType.None ||
				spell.m_CostList.Count != 0 || spell.m_EffectList.Count != 0 ||
				spell.m_Cooldown.m_Cooldown != 0f ||
				spell.m_Cooldown.m_SharedCooldownGroup != 0 || spell.m_Cooldown.m_MaxCharges != 1 ||
				spell.m_Cooldown.m_ChargeRecoveryTime != 0f || spell.m_Cast.m_RequireFacingTarget ||
				spell.m_Presentation.m_CastEffectPrefab != null || spell.m_Presentation.m_ImpactEffectPrefab != null ||
				spell.m_Presentation.m_ProjectilePrefab != null || spell.m_Presentation.m_CastAudio != null ||
				spell.m_Presentation.m_ImpactAudio != null) return SpellCastResult.UnsupportedSpell;
			if (IsGlobalCooldownBlocking(spell)) return SpellCastResult.GlobalCooldown;
			if (m_Presentation == null || !m_Presentation.Play(spell, true, request.m_FacingRotation))
				return SpellCastResult.PresentationUnavailable;
			if (spell.m_Cooldown.m_TriggersGlobalCooldown)
			{
				float duration = m_Attributes != null ? m_Attributes.GetGlobalCooldownDuration(spell.m_Cooldown.m_GlobalCooldown) : spell.m_Cooldown.m_GlobalCooldown;
				m_GlobalCooldown.Start(Time.timeAsDouble, duration);
			}
			m_SpellReleased?.Invoke(spell);
			return SpellCastResult.Success;
		}

		public bool IsGlobalCooldownBlocking(SpellDefinition spell)
		{
			return spell != null && spell.m_Cooldown != null && spell.m_Cooldown.m_IsAffectedByGlobalCooldown && GlobalCooldownRemaining > 0f;
		}

		private void Awake()
		{
			if (m_Attributes == null) m_Attributes = GetComponent<ActorAttributes>();
		}

		public void SetFacing(Quaternion facingRotation)
		{
			if (m_Presentation != null) m_Presentation.SetFacing(facingRotation);
		}

		public void CancelPresentation()
		{
			if (m_Presentation != null) m_Presentation.StopImmediately();
		}

		private void OnDisable()
		{
			CancelPresentation();
		}
	}
}
