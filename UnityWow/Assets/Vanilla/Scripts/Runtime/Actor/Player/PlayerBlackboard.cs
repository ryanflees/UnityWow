// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
	public class PlayerBlackboard
	{
		public bool m_InputEnabled = true;
		public Vector2 m_MoveInput;
		public Vector2 m_LookInput;
		public float m_ZoomInput;
		public bool m_IsCameraRotating;
		public bool m_IsPrimaryMouseRotating;
		public bool m_IsSecondaryMouseRotating;
		public bool m_WasSecondaryMousePressed;
		public bool m_WasSecondaryMouseReleased;
		public bool m_WasMoveForwardPressed;
		public bool m_WasMoveBackwardPressed;
		public bool m_WasMoveLeftPressed;
		public bool m_WasMoveRightPressed;
		public bool m_WasJumpPressed;
		public bool m_WasActionSlot1Pressed;
		public bool m_WasActionSlot4Pressed;
		public Vector3 m_GravityUp => Vector3.up;
		public Quaternion m_GravityRotation => Quaternion.identity;
		public Vector3 m_ReferenceFaceDirection = Vector3.forward;
		public Vector3 m_CharacterFaceDirection = Vector3.forward;
		public Vector3 m_MoveDirection;
		public Vector3 m_MoveVelocity;
		public bool m_IsBackMove;
		public bool m_IsFaceControlledByCamera;
		public bool m_IsReturningToReferenceFace;
		public bool m_ShouldSnapFaceAngle;
		public float m_TargetFaceAngle;
		public float m_CurrentFaceAngle;
		public bool m_RequestTurn;
		public bool m_TurnIsRight;
		public float m_TurnAnimationTimer;
		public float m_StandingTurnCooldown;
		public bool m_IsGrounded = true;
		public bool m_WasJumpStarted;
		public bool m_WasGroundedThisFrame;
		public bool m_WasStandingJumpRequested;
		public float m_AirborneDuration;
		public float m_LandedAirborneDuration;
		public int m_JumpCount;
		public bool m_CanStartStandingJumpAirMove;
		public bool m_HasAirMoveIntent;
		public Vector3 m_AirMoveIntentDirection;
		public float m_AirMoveIntentSpeed;
		public bool m_AirMoveIntentControlsFacing;
		public bool m_AirMoveIntentKeepFacingForward;
		public float m_JumpStartAnimationTimer;
	}
}

