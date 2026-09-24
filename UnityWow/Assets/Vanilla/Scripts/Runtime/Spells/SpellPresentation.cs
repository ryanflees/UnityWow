// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	[DisallowMultipleComponent]
	public class SpellPresentation : MonoBehaviour
	{
		public Character m_Character;
		[Min(0f)] public float m_TransitionDuration = 0.12f;
		public bool m_UseUpperBodyLayer = true;

		private RuntimeAnimatorController m_ClipController;
		private AnimationClip m_DirectedReleaseClip;
		private AnimationClip m_OmniReleaseClip;

		public bool IsPlaying => m_Character != null && m_Character.IsPlayingSpellAnimation;

		public bool Play(SpellDefinition spell, bool isReleasePhase, Quaternion facingRotation)
		{
			if (!isActiveAndEnabled || spell == null || spell.m_Presentation == null || m_Character == null) return false;
			SpellAnimationType animationType = spell.m_Presentation.m_AnimationType;
			if (animationType == SpellAnimationType.None) return true;
			Animator animator = m_Character.m_Animator;
			string stateName = Character.GetSpellStateName(animationType, isReleasePhase);
			int layer = m_Character.SpellLayerIndex;
			if (animator == null || !animator.isActiveAndEnabled || layer < 0 ||
				m_Character.UpperBodyLayerIndex < 0 || string.IsNullOrEmpty(stateName) ||
				!animator.HasState(layer, Animator.StringToHash("SpellLayer." + stateName))) return false;

			m_Character.PlaySpellAnimation(animationType, isReleasePhase, m_TransitionDuration, m_UseUpperBodyLayer);
			SetFacing(facingRotation);
			if (m_Character.m_LookAtIk != null)
			{
				if (isReleasePhase)
				{
					CacheReleaseClips(animator.runtimeAnimatorController);
					bool directed = animationType == SpellAnimationType.CastDirected ||
						animationType == SpellAnimationType.ChannelDirected;
					AnimationClip clip = directed ? m_DirectedReleaseClip : m_OmniReleaseClip;
					if (!m_Character.m_LookAtIk.PlayUpperBodyPose(clip, Animator.StringToHash(stateName), layer, facingRotation))
						Debug.LogWarning("Upper-body stabilization requires a valid Humanoid release clip and chest bone.", this);
				}
				else m_Character.m_LookAtIk.StopUpperBodyPose();
			}
			return true;
		}

		public void SetFacing(Quaternion facingRotation)
		{
			if (m_Character != null && m_Character.m_LookAtIk != null)
				m_Character.m_LookAtIk.SetUpperBodyReferenceRotation(facingRotation);
		}

		public void SetUpperBodyLayerEnabled(bool enabled)
		{
			m_UseUpperBodyLayer = enabled;
			if (m_Character != null) m_Character.SetUpperBodySpellLayerEnabled(enabled);
		}

		public void Stop()
		{
			if (m_Character != null) m_Character.StopSpellAnimation(m_TransitionDuration);
		}

		private void OnDisable()
		{
			StopImmediately();
		}

		public void StopImmediately()
		{
			if (m_Character != null) m_Character.StopSpellAnimation(0f);
		}

		private void CacheReleaseClips(RuntimeAnimatorController controller)
		{
			if (m_ClipController == controller) return;
			m_ClipController = controller;
			m_DirectedReleaseClip = null;
			m_OmniReleaseClip = null;
			if (controller == null) return;
			foreach (AnimationClip clip in controller.animationClips)
			{
				if (clip.name == "SpellCastDirected") m_DirectedReleaseClip = clip;
				if (clip.name == "SpellCastOmni") m_OmniReleaseClip = clip;
			}
		}
	}
}
