// Copyright (c) 2026 CatRabbit. All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace CR
{
	public class PlayerController : MonoBehaviour
	{
		public GameObject m_CameraTarget;
		public TPCameraController m_TPCameraController;
		[UnityEngine.Serialization.FormerlySerializedAs("m_KinematicActor")]
		public PlayerMotor m_PlayerMotor;
		public Transform m_CharacterRoot;
		public Character m_Character;
		public float m_CharacterFaceSharpness = 12f;
		[Min(0f), Tooltip("How fast the character turns back to its forward direction after stopping. Use a smaller value for a slower turn.")]
		public float m_ReturnToReferenceFaceSharpness = 8f;
		public float m_SecondaryMouseFaceSharpness = 30f;
		public float m_FaceDirectionHorizontalSensitivity = 1f;
		public bool m_SnapFaceToReferenceWithSecondaryMouse = true;
		[UnityEngine.Serialization.FormerlySerializedAs("m_EnableStandingSecondaryMouseShuffleTurn")]
		public bool m_EnableStandingSecondaryMouseTurn = true;
		[UnityEngine.Serialization.FormerlySerializedAs("m_StandingShuffleTurnThresholdAngle")]
		public float m_StandingTurnThresholdAngle = 75f;
		[UnityEngine.Serialization.FormerlySerializedAs("m_StandingShuffleTurnAnimationExtraAngle")]
		public float m_StandingTurnAnimationExtraAngle = 0.2f;
		[UnityEngine.Serialization.FormerlySerializedAs("m_StandingShuffleTurnInterval")]
		public float m_StandingTurnInterval = 0.5f;
		[UnityEngine.Serialization.FormerlySerializedAs("m_StandingShuffleTurnAngularSpeed")]
		public float m_StandingTurnAngularSpeed = 180f;
		[UnityEngine.Serialization.FormerlySerializedAs("m_ShuffleTurnAnimationLockDuration")]
		public float m_TurnAnimationLockDuration = 0.35f;
		[UnityEngine.Serialization.FormerlySerializedAs("m_ShuffleTurnTransitionDuration")]
		public float m_TurnTransitionDuration = 0.05f;
		public float m_MoveSpeed = 6f;
		public float m_BackMoveSpeedMultiplier = 0.4f;
		public float m_StandingJumpAirMoveSpeed = 2.5f;
		public float m_JumpStartAnimationLockDuration = 0.5f;
		[Tooltip("If the player stays in the air for less than this time, skip the landing animation. Small hops onto a ledge can then go straight back to idle or movement.")]
		public float m_MinAirborneDurationForLandingAnimation = 0.5f;
		public float m_AirborneLocomotionToJumpLoopDelay = 0.5f;
		public float m_JumpTransitionDuration = 0.05f;
		private PlayerBlackboard m_Blackboard;
		private PlayerInputProcessor m_InputProcessor;
		private List<IPlayerProcessor> m_Processors = new List<IPlayerProcessor>();
		private bool m_HasInitializedSpawn;

		private void Awake()
		{
			EnsureRuntimeInitialized();
		}

		private void OnEnable()
		{
			EnsureRuntimeInitialized();
		}

		private void OnDisable()
		{
			m_InputProcessor?.ReleaseCursor(Application.isFocused);
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			// Never warp the desktop pointer after the user switches to another application.
			if (!hasFocus) m_InputProcessor?.ReleaseCursor(false);
		}

		private void Start()
		{
			if (!m_HasInitializedSpawn)
			{
				InitializePlayerMotor();
				InitializeCharacterRoot();
				InitializeCharacter();
			}

			InitializeCameraTarget(!m_HasInitializedSpawn);
		}

		private void Update()
		{
			OnUpdate(Time.deltaTime);
		}

		private void FixedUpdate()
		{
			OnFixedUpdate(Time.fixedDeltaTime);
		}

		private void LateUpdate()
		{
			OnLateUpdate(Time.deltaTime);
		}

		public void OnUpdate(float dt)
		{
			EnsureRuntimeInitialized();
			for (int i = 0; i < m_Processors.Count; i++)
			{
				m_Processors[i].OnUpdate(m_Blackboard, dt);
			}
		}

		public void OnFixedUpdate(float dt)
		{
			EnsureRuntimeInitialized();
			for (int i = 0; i < m_Processors.Count; i++)
			{
				m_Processors[i].OnFixedUpdate(m_Blackboard, dt);
			}
		}

		public void OnLateUpdate(float dt)
		{
			EnsureRuntimeInitialized();
			SyncCharacterVisual(dt, false);

			for (int i = 0; i < m_Processors.Count; i++)
			{
				m_Processors[i].OnLateUpdate(m_Blackboard, dt);
			}
		}

		public void AddProcessor(IPlayerProcessor processor)
		{
			if (!m_Processors.Contains(processor))
			{
				m_Processors.Add(processor);
			}
		}

		public void InitializeSpawn(Vector3 position, Vector3 faceDirection, Vector3 gravityUp, TPCameraController cameraController)
		{
			EnsureRuntimeInitialized();
			InitializePlayerMotor();
			InitializeCharacterRoot();
			InitializeCharacter();

			m_TPCameraController = cameraController;
			SetGravityUp(gravityUp);

			Vector3 planarFaceDirection = GetValidPlanarDirection(faceDirection, m_Blackboard.m_GravityUp);
			if (planarFaceDirection.sqrMagnitude <= 0.0001f)
			{
				planarFaceDirection = GetValidPlanarDirection(transform.forward, m_Blackboard.m_GravityUp);
			}

			if (planarFaceDirection.sqrMagnitude <= 0.0001f)
			{
				planarFaceDirection = GetFallbackForward(m_Blackboard.m_GravityUp);
			}

			m_Blackboard.m_CharacterFaceDirection = planarFaceDirection;
			m_Blackboard.m_ReferenceFaceDirection = planarFaceDirection;
			m_Blackboard.m_TargetFaceAngle = 0f;
			m_Blackboard.m_CurrentFaceAngle = 0f;
			Quaternion spawnRotation = Quaternion.LookRotation(planarFaceDirection, m_Blackboard.m_GravityUp);
			//transform.SetPositionAndRotation(position, spawnRotation);
			transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

			if (m_PlayerMotor != null)
			{
				m_PlayerMotor.SetPositionAndRotation(position, spawnRotation, true);
			}

			SyncCharacterVisual(0f, true);
			InitializeCameraTarget(false);

			if (m_TPCameraController != null)
			{
				m_TPCameraController.SetWorldUp(m_Blackboard.m_GravityUp);
				m_TPCameraController.ResetBehindTarget(planarFaceDirection, m_Blackboard.m_GravityUp);
			}

			m_HasInitializedSpawn = true;
		}

		public void SetGravityUp(Vector3 gravityUp)
        {
            EnsureRuntimeInitialized();
            Vector3 forward = Vector3.ProjectOnPlane(m_Blackboard.m_ReferenceFaceDirection, Vector3.up);
            m_Blackboard.m_ReferenceFaceDirection = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            if (m_TPCameraController != null) m_TPCameraController.SetWorldUp(Vector3.up);
        }

		private void InitializeProcessors()
		{
			m_InputProcessor = new PlayerInputProcessor(this);
			AddProcessor(m_InputProcessor);
			AddProcessor(new PlayerJumpProcessor(this));
			AddProcessor(new PlayerFacingProcessor(this));
			AddProcessor(new PlayerMovementProcessor(this));
			AddProcessor(new PlayerCharacterProcessor(this));
			AddProcessor(new PlayerCameraProcessor(this));
		}

		private void EnsureRuntimeInitialized()
		{
			bool createdBlackboard = false;
			if (m_Blackboard == null)
			{
				m_Blackboard = new PlayerBlackboard();
				createdBlackboard = true;
			}

			if (m_Processors == null)
			{
				m_Processors = new List<IPlayerProcessor>();
			}

			if (m_Processors.Count == 0)
			{
				InitializeProcessors();
			}

			if (createdBlackboard && m_PlayerMotor != null)
			{
				m_PlayerMotor.Initialize(m_Blackboard);
			}
		}

		private void InitializeCameraTarget(bool findSceneCamera = true)
		{
			if (findSceneCamera && m_TPCameraController == null && Camera.main != null)
			{
				m_TPCameraController = Camera.main.GetComponent<TPCameraController>();
			}

			if (m_TPCameraController != null && m_CameraTarget != null)
			{
				m_TPCameraController.SetTarget(m_CameraTarget.transform);
				m_TPCameraController.SetWorldUp(m_Blackboard.m_GravityUp);
			}
		}

		private void InitializePlayerMotor()
		{
			if (m_PlayerMotor == null)
			{
				m_PlayerMotor = GetComponentInChildren<PlayerMotor>();
			}

			if (m_PlayerMotor == null)
			{
				Transform motorTransform = transform.Find("Motor");
				if (motorTransform != null)
				{
					m_PlayerMotor = motorTransform.gameObject.AddComponent<KinematicActor>();
				}
			}

			if (m_PlayerMotor != null)
			{
				m_PlayerMotor.Initialize(m_Blackboard);
			}
		}

		private void InitializeCharacterRoot()
		{
			if (m_CharacterRoot != null)
			{
				return;
			}

			Transform characterTransform = transform.Find("Character");
			if (characterTransform != null)
			{
				m_CharacterRoot = characterTransform;
				return;
			}

			Transform characterRootTransform = transform.Find("CharacterRoot");
			if (characterRootTransform != null)
			{
				m_CharacterRoot = characterRootTransform;
			}
		}

		private void InitializeCharacter()
		{
			if (m_Character != null)
			{
				return;
			}

			if (m_CharacterRoot != null)
			{
				m_Character = m_CharacterRoot.GetComponentInChildren<Character>();
			}

			if (m_Character == null)
			{
				m_Character = GetComponentInChildren<Character>();
			}
		}

		private void SyncCharacterVisual(float dt, bool immediate)
		{
			if (m_CharacterRoot == null || m_PlayerMotor == null)
			{
				return;
			}

			m_CharacterRoot.position = m_PlayerMotor.VisualPosition;

			Vector3 gravityUp = m_Blackboard.m_GravityUp;
			Vector3 faceDirection = GetValidPlanarDirection(m_Blackboard.m_CharacterFaceDirection, gravityUp);
			if (faceDirection.sqrMagnitude <= 0.0001f)
			{
				return;
			}

			bool isStandingTurn = IsStandingTurnVisualActive();
			if (immediate || m_Blackboard.m_ShouldSnapFaceAngle)
			{
				m_Blackboard.m_CurrentFaceAngle = m_Blackboard.m_TargetFaceAngle;
				m_Blackboard.m_ShouldSnapFaceAngle = false;
			}
			else if (isStandingTurn)
			{
				float speed = Mathf.Max(0f, m_StandingTurnAngularSpeed);
				m_Blackboard.m_CurrentFaceAngle = Mathf.MoveTowards(m_Blackboard.m_CurrentFaceAngle, m_Blackboard.m_TargetFaceAngle, speed * dt);
			}
			else if (m_Blackboard.m_IsFaceControlledByCamera && m_SnapFaceToReferenceWithSecondaryMouse && m_Blackboard.m_MoveVelocity.sqrMagnitude <= 0.0001f)
			{
				m_Blackboard.m_CurrentFaceAngle = m_Blackboard.m_TargetFaceAngle;
			}
			else
			{
				float sharpness = m_Blackboard.m_IsFaceControlledByCamera ? m_SecondaryMouseFaceSharpness : m_CharacterFaceSharpness;
				if (m_Blackboard.m_IsGrounded && m_Blackboard.m_IsReturningToReferenceFace && !m_Blackboard.m_IsFaceControlledByCamera)
				{
					sharpness = m_ReturnToReferenceFaceSharpness;
				}
				float lerp = 1f - Mathf.Exp(-sharpness * dt);
				m_Blackboard.m_CurrentFaceAngle = Mathf.Lerp(m_Blackboard.m_CurrentFaceAngle, m_Blackboard.m_TargetFaceAngle, lerp);
			}

			Vector3 frontSideFaceDirection = Quaternion.AngleAxis(m_Blackboard.m_CurrentFaceAngle, gravityUp) * m_Blackboard.m_ReferenceFaceDirection;
			Quaternion targetRotation = Quaternion.LookRotation(frontSideFaceDirection, gravityUp);
			if (immediate)
			{
				m_CharacterRoot.rotation = targetRotation;
				return;
			}

			m_CharacterRoot.rotation = targetRotation;
		}

		private bool IsStandingTurnVisualActive()
		{
			if (!m_EnableStandingSecondaryMouseTurn)
			{
				return false;
			}

			if (m_Blackboard.m_MoveVelocity.sqrMagnitude > 0.0001f)
			{
				return false;
			}

			return m_Blackboard.m_IsSecondaryMouseRotating || m_Blackboard.m_TurnAnimationTimer > 0f || m_Blackboard.m_RequestTurn;
		}

		private Vector3 GetValidPlanarDirection(Vector3 direction, Vector3 gravityUp)
		{
			Vector3 planarDirection = Vector3.ProjectOnPlane(direction, gravityUp);
			if (planarDirection.sqrMagnitude <= 0.0001f)
			{
				return Vector3.zero;
			}

			return planarDirection.normalized;
		}

		private Vector3 GetFallbackForward(Vector3 gravityUp)
		{
			Vector3 axis = Mathf.Abs(Vector3.Dot(gravityUp.normalized, Vector3.forward)) > 0.95f ? Vector3.right : Vector3.forward;
			return Vector3.ProjectOnPlane(axis, gravityUp).normalized;
		}

		#region Getters

		public Vector3 GetPlayerPosition()
		{
			return m_PlayerMotor != null ? m_PlayerMotor.Position : transform.position;
		}

		public Quaternion GetPlayerRotation()
		{
			return Quaternion.LookRotation(m_Blackboard.m_ReferenceFaceDirection, m_Blackboard.m_GravityUp);
		}

		public PlayerState GetState()
		{
			EnsureRuntimeInitialized();
			return new PlayerState
			{
				position = GetPlayerPosition(),
				rotation = GetPlayerRotation(),
				velocity = m_PlayerMotor != null ? m_PlayerMotor.Velocity : Vector3.zero,
				characterFaceDirection = m_Blackboard.m_CharacterFaceDirection,
				moveInput = m_Blackboard.m_MoveInput,
				isGrounded = m_Blackboard.m_IsGrounded,
				isBackMove = m_Blackboard.m_IsBackMove,
				jumpCount = m_Blackboard.m_JumpCount
			};
		}

		#endregion
	}
}

