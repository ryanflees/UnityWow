// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	public partial class Character
	{
		[Min(0f)] public float m_SpellMovementBlendDuration = 0.15f;

		private static readonly int m_EmptySpellStateHash = Animator.StringToHash("Empty");
		private bool m_HasSpellPlayback;
		private bool m_IsSpellStopping;
		private bool m_HasObservedSpellState;
		private bool m_IsSpellRelease;
		private bool m_UseUpperBodySpellLayer = true;
		private int m_SpellStateHash;

		public int SpellLayerIndex => m_Animator != null ? m_Animator.GetLayerIndex("SpellLayer") : -1;
		public int UpperBodyLayerIndex => m_Animator != null ? m_Animator.GetLayerIndex("UpperBody") : -1;
		public bool IsPlayingSpellAnimation => m_HasSpellPlayback && !m_IsSpellStopping;
		public float SpellNormalizedTime
		{
			get
			{
				int layer = SpellLayerIndex;
				if (!m_HasSpellPlayback || layer < 0) return 0f;
				if (m_Animator.IsInTransition(layer))
				{
					AnimatorStateInfo next = m_Animator.GetNextAnimatorStateInfo(layer);
					if (next.shortNameHash == m_SpellStateHash) return next.normalizedTime;
				}
				AnimatorStateInfo current = m_Animator.GetCurrentAnimatorStateInfo(layer);
				return current.shortNameHash == m_SpellStateHash ? current.normalizedTime : 0f;
			}
		}

		private void Update()
		{
			EvaluateSpellLayers(Time.deltaTime);
		}

		public void PlaySpellAnimation(SpellAnimationType animationType, bool isReleasePhase,
			float fixedTransitionDuration, bool useUpperBody = true)
		{
			int spellLayer = SpellLayerIndex;
			int upperLayer = UpperBodyLayerIndex;
			if (spellLayer < 0 || upperLayer < 0)
			{
				Debug.LogError("Spell playback requires SpellLayer and UpperBody animation layers.", this);
				return;
			}

			string stateName = GetSpellStateName(animationType, isReleasePhase);
			if (string.IsNullOrEmpty(stateName)) return;
			bool wasPlaying = m_HasSpellPlayback;
			m_HasSpellPlayback = true;
			m_IsSpellStopping = false;
			m_HasObservedSpellState = false;
			m_IsSpellRelease = isReleasePhase;
			m_UseUpperBodySpellLayer = useUpperBody;
			m_SpellStateHash = Animator.StringToHash(stateName);
			if (!IsLocomotionRequested()) PlayStand();
			if (!wasPlaying) m_Animator.SetLayerWeight(spellLayer, GetSpellLayerTargetWeight());
			m_Animator.SetLayerWeight(upperLayer, ShouldUseUpperBodySpellLayer() ? 1f : 0f);
			// UpperBody is synchronized to SpellLayer; only the source receives playback commands.
			m_Animator.CrossFadeInFixedTime("SpellLayer." + stateName,
				Mathf.Max(0f, fixedTransitionDuration), spellLayer, 0f);
		}

		public void SetUpperBodySpellLayerEnabled(bool enabled)
		{
			m_UseUpperBodySpellLayer = enabled;
		}

		public void StopSpellAnimation(float fixedTransitionDuration)
		{
			int spellLayer = SpellLayerIndex;
			if (spellLayer < 0) return;
			if (m_LookAtIk != null) m_LookAtIk.StopUpperBodyPose();
			if (fixedTransitionDuration <= 0f)
			{
				m_Animator.Play("SpellLayer.Empty", spellLayer, 0f);
				ClearSpellPlayback();
				return;
			}
			if (!m_HasSpellPlayback || m_IsSpellStopping) return;
			m_IsSpellStopping = true;
			m_Animator.CrossFadeInFixedTime("SpellLayer.Empty", fixedTransitionDuration, spellLayer, 0f);
		}

		public void EvaluateSpellLayers(float deltaTime)
		{
			if (!m_HasSpellPlayback || m_Animator == null || !m_Animator.isActiveAndEnabled) return;
			int spellLayer = SpellLayerIndex;
			int upperLayer = UpperBodyLayerIndex;
			if (spellLayer < 0 || upperLayer < 0) return;
			AnimatorStateInfo current = m_Animator.GetCurrentAnimatorStateInfo(spellLayer);
			bool transitioning = m_Animator.IsInTransition(spellLayer);
			if (current.shortNameHash == m_SpellStateHash || (transitioning &&
				m_Animator.GetNextAnimatorStateInfo(spellLayer).shortNameHash == m_SpellStateHash))
				m_HasObservedSpellState = true;
			if ((m_IsSpellStopping || m_HasObservedSpellState) && !transitioning &&
				current.shortNameHash == m_EmptySpellStateHash)
			{
				ClearSpellPlayback();
				return;
			}

			float targetWeight = GetSpellLayerTargetWeight();
			float weight = m_SpellMovementBlendDuration <= 0f ? targetWeight :
				Mathf.MoveTowards(m_Animator.GetLayerWeight(spellLayer), targetWeight,
					Mathf.Max(0f, deltaTime) / m_SpellMovementBlendDuration);
			m_Animator.SetLayerWeight(spellLayer, weight);
			m_Animator.SetLayerWeight(upperLayer, ShouldUseUpperBodySpellLayer() ? 1f : 0f);
		}

		private float GetSpellLayerTargetWeight()
		{
			return m_IsSpellRelease && IsLocomotionRequested() ? 0f : 1f;
		}

		private bool ShouldUseUpperBodySpellLayer()
		{
			return m_UseUpperBodySpellLayer || (m_IsSpellRelease && IsLocomotionRequested());
		}

		private static string GetSpellStateName(SpellAnimationType animationType, bool isReleasePhase)
		{
			if (isReleasePhase)
				return animationType == SpellAnimationType.CastDirected || animationType == SpellAnimationType.ChannelDirected
					? "CastSpellDirectedFinish" : "CastSpellOmniFinish";
			switch (animationType)
			{
				case SpellAnimationType.CastDirected: return "CastSpellDirected";
				case SpellAnimationType.CastOmnidirectional: return "CastingSpellOmni";
				case SpellAnimationType.ChannelDirected: return "ChannelCastDirected";
				case SpellAnimationType.ChannelOmnidirectional: return "ChannelCastOmni";
				default: return null;
			}
		}

		private void OnDisable()
		{
			ClearSpellPlayback();
		}

		private void ClearSpellPlayback()
		{
			m_HasSpellPlayback = false;
			m_IsSpellStopping = false;
			m_HasObservedSpellState = false;
			int spellLayer = SpellLayerIndex;
			int upperLayer = UpperBodyLayerIndex;
			if (spellLayer >= 0) m_Animator.SetLayerWeight(spellLayer, 0f);
			if (upperLayer >= 0) m_Animator.SetLayerWeight(upperLayer, 0f);
			if (m_LookAtIk != null) m_LookAtIk.StopUpperBodyPose();
		}
	}
}

