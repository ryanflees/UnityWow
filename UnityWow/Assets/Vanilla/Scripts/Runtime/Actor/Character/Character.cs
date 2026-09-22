// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	public class Character : MonoBehaviour
	{
		public Animator m_Animator;
		public CharacterLookAtIk m_LookAtIk;

		private CharacterAnimationState m_CurrentState = CharacterAnimationState.None;

		private void Awake()
		{
			InitializeLookAtIk();
		}

		public void PlayStand()
		{
			if (!CanPlay(CharacterAnimationState.Stand))
			{
				return;
			}

			SetWalkValue(0f);
			if (SyncStateIfAnimatorAlreadyPlaying(CharacterAnimationState.Stand, L_0__STAND_ID))
			{
				return;
			}

			PlayStand(0.15f);
			m_CurrentState = CharacterAnimationState.Stand;
		}

		public void PlayMoveForward()
		{
			if (!CanPlay(CharacterAnimationState.MoveForward))
			{
				return;
			}

			SetWalkValue(0f);
			if (SyncStateIfAnimatorAlreadyPlaying(CharacterAnimationState.MoveForward, L_0__MOVE_FORWARD_B_T_ID))
			{
				return;
			}

			PlayMoveForwardBT(0.15f);
			m_CurrentState = CharacterAnimationState.MoveForward;
		}

		public void PlayMoveBackward()
		{
			if (!CanPlay(CharacterAnimationState.MoveBackward))
			{
				return;
			}

			SetWalkValue(0f);
			PlayMovebackward(0.15f);
			m_CurrentState = CharacterAnimationState.MoveBackward;
		}

		public void PlayTurn(bool isRight, float fixedTransitionDuration)
		{
			if (m_Animator == null)
			{
				return;
			}

			SetTurnIsRightValue(isRight ? 1f : 0f);
			PlayTurnBT(fixedTransitionDuration);
			m_CurrentState = CharacterAnimationState.Turn;
		}

		public void PlayJumpStartAnimation(float fixedTransitionDuration)
		{
			if (!CanPlay(CharacterAnimationState.JumpStart))
			{
				return;
			}

			SetWalkValue(0f);
			PlayJumpStart(fixedTransitionDuration);
			m_CurrentState = CharacterAnimationState.JumpStart;
		}

		public void PlayJumpEndAnimation(float fixedTransitionDuration)
		{
			if (!CanPlay(CharacterAnimationState.JumpEnd))
			{
				return;
			}

			SetWalkValue(0f);
			PlayJumpEnd(fixedTransitionDuration);
			m_CurrentState = CharacterAnimationState.JumpEnd;
		}

		public void PlayJumpLandRunAnimation(float fixedTransitionDuration)
		{
			if (!CanPlay(CharacterAnimationState.JumpLandRun))
			{
				return;
			}

			SetWalkValue(0f);
			PlayJumpLandRun(fixedTransitionDuration);
			m_CurrentState = CharacterAnimationState.JumpLandRun;
		}

		public void PlayJumpLoopAnimation(float fixedTransitionDuration)
		{
			if (!CanPlay(CharacterAnimationState.JumpLoop))
			{
				return;
			}

			SetWalkValue(0f);
			PlayJumpLoop(fixedTransitionDuration);
			m_CurrentState = CharacterAnimationState.JumpLoop;
		}

		public bool IsPlayingGroundLocomotionAnimation()
		{
			return IsAnimatorInAnyState(L_0__STAND_ID, L_0__MOVE_FORWARD_B_T_ID, L_0__MOVEBACKWARD_ID);
		}

		public bool IsPlayingJumpLandingAnimation()
		{
			return IsAnimatorInAnyState(L_0__JUMP_END_ID, L_0__JUMP_LAND_RUN_ID);
		}

		public bool IsPlayingJumpEndAnimation()
		{
			return IsAnimatorInState(L_0__JUMP_END_ID);
		}

		public bool IsPlayingJumpLandRunAnimation()
		{
			return IsAnimatorInState(L_0__JUMP_LAND_RUN_ID);
		}
		
		public bool IsPlayingAirborneAnimation() => IsAnimatorInAnyState(L_0__JUMP_START_ID,
			L_0__JUMP_LOOP_ID);

		public void SetLookAtDirection(Vector3 direction, Vector3 up)
		{
			InitializeLookAtIk();
			if (m_LookAtIk != null)
			{
				m_LookAtIk.SetLookAtDirection(direction, up);
			}
		}

		public void ClearLookAt()
		{
			if (m_LookAtIk != null)
			{
				m_LookAtIk.ClearLookAt();
			}
		}

		#region -Generated Code DO NOT MODIFY-
		// Parameters
		private const string PARAM_ISWALK = "IsWalk"; // Float
		private readonly int PARAM_ISWALK_ID = Animator.StringToHash(PARAM_ISWALK);
		private const string PARAM_TURNISRIGHT = "TurnIsRight"; // Float
		private readonly int PARAM_TURNISRIGHT_ID = Animator.StringToHash(PARAM_TURNISRIGHT);

		// States & Layer IDs
		// Layer 0 - Base Layer
		private const string L_0__STAND = "Stand";
		private readonly int L_0__STAND_ID = Animator.StringToHash(L_0__STAND);
		private const string L_0__MOVE_FORWARD_B_T = "MoveForwardBT";
		private readonly int L_0__MOVE_FORWARD_B_T_ID = Animator.StringToHash(L_0__MOVE_FORWARD_B_T);
		private const string L_0__MOVEBACKWARD = "Movebackward";
		private readonly int L_0__MOVEBACKWARD_ID = Animator.StringToHash(L_0__MOVEBACKWARD);
		private const string L_0__TURN_B_T = "TurnBT";
		private readonly int L_0__TURN_B_T_ID = Animator.StringToHash(L_0__TURN_B_T);
		private const string L_0__JUMP_START = "JumpStart";
		private readonly int L_0__JUMP_START_ID = Animator.StringToHash(L_0__JUMP_START);
		private const string L_0__JUMP_LAND_RUN = "JumpLandRun";
		private readonly int L_0__JUMP_LAND_RUN_ID = Animator.StringToHash(L_0__JUMP_LAND_RUN);
		private const string L_0__JUMP_LOOP = "JumpLoop";
		private readonly int L_0__JUMP_LOOP_ID = Animator.StringToHash(L_0__JUMP_LOOP);
		private const string L_0__JUMP_END = "JumpEnd";
		private readonly int L_0__JUMP_END_ID = Animator.StringToHash(L_0__JUMP_END);


		// Play Animations Layer 0
		public void PlayStand(float fixedTransitionDuration)
		{
			m_Animator.CrossFadeInFixedTime(L_0__STAND_ID, fixedTransitionDuration, 0);
		}
		public void PlayMoveForwardBT(float fixedTransitionDuration)
		{
			m_Animator.CrossFadeInFixedTime(L_0__MOVE_FORWARD_B_T_ID, fixedTransitionDuration, 0);
		}
		public void PlayMovebackward(float fixedTransitionDuration)
		{
			m_Animator.CrossFadeInFixedTime(L_0__MOVEBACKWARD_ID, fixedTransitionDuration, 0);
		}
		public void PlayTurnBT(float fixedTransitionDuration)
		{
			m_Animator.CrossFadeInFixedTime(L_0__TURN_B_T_ID, fixedTransitionDuration, 0);
		}
		public void PlayJumpStart(float fixedTransitionDuration)
		{
			m_Animator.CrossFadeInFixedTime(L_0__JUMP_START_ID, fixedTransitionDuration, 0);
		}
		public void PlayJumpLandRun(float fixedTransitionDuration)
		{
			m_Animator.CrossFadeInFixedTime(L_0__JUMP_LAND_RUN_ID, fixedTransitionDuration, 0);
		}
		public void PlayJumpLoop(float fixedTransitionDuration)
		{
			m_Animator.CrossFadeInFixedTime(L_0__JUMP_LOOP_ID, fixedTransitionDuration, 0);
		}
		public void PlayJumpEnd(float fixedTransitionDuration)
		{
			m_Animator.CrossFadeInFixedTime(L_0__JUMP_END_ID, fixedTransitionDuration, 0);
		}
		#endregion




		private bool CanPlay(CharacterAnimationState state)
		{
			return m_Animator != null && m_CurrentState != state;
		}

		private void SetWalkValue(float value)
		{
			if (m_Animator == null)
			{
				return;
			}

			m_Animator.SetFloat(PARAM_ISWALK_ID, value);
		}

		private void SetTurnIsRightValue(float value)
		{
			m_Animator.SetFloat(PARAM_TURNISRIGHT_ID, value);
		}

		private bool IsJumpLandingState(int stateHash)
		{
			return stateHash == L_0__JUMP_END_ID || stateHash == L_0__JUMP_LAND_RUN_ID;
		}

		private bool SyncStateIfAnimatorAlreadyPlaying(CharacterAnimationState state, int stateHash)
		{
			if (IsAnimatorInState(stateHash))
			{
				m_CurrentState = state;
				return true;
			}

			return false;
		}

		private bool IsAnimatorInState(int stateHash)
		{
			return IsAnimatorInAnyState(stateHash);
		}

		private bool IsAnimatorInAnyState(params int[] stateHashes)
		{
			if (m_Animator == null)
			{
				return false;
			}

			if (!m_Animator.IsInTransition(0))
			{
				AnimatorStateInfo currentState = m_Animator.GetCurrentAnimatorStateInfo(0);
				return ContainsStateHash(stateHashes, currentState.shortNameHash);
			}

			AnimatorStateInfo nextState = m_Animator.GetNextAnimatorStateInfo(0);
			return ContainsStateHash(stateHashes, nextState.shortNameHash);
		}

		private bool ContainsStateHash(int[] stateHashes, int stateHash)
		{
			for (int i = 0; i < stateHashes.Length; i++)
			{
				if (stateHashes[i] == stateHash)
				{
					return true;
				}
			}

			return false;
		}

		private void InitializeLookAtIk()
		{
			if (m_LookAtIk == null)
			{
				m_LookAtIk = GetComponent<CharacterLookAtIk>();
			}

			if (m_LookAtIk == null)
			{
				m_LookAtIk = GetComponentInParent<CharacterLookAtIk>();
			}

			if (m_LookAtIk == null)
			{
				m_LookAtIk = gameObject.AddComponent<CharacterLookAtIk>();
			}

			m_LookAtIk.Initialize(m_Animator, transform.parent);
			m_LookAtIk.EnsureTarget();
		}

		private enum CharacterAnimationState
		{
			None,
			Stand,
			MoveForward,
			MoveBackward,
			Turn,
			JumpStart,
			JumpLoop,
			JumpEnd,
			JumpLandRun
		}
	}
}

