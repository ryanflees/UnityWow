// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace CR
{
	[CustomEditor(typeof(PlayerController))]
	public class PlayerControllerEditor : UnityEditor.Editor
	{
		private PlayerController m_Script;
		private SerializedProperty m_CameraTarget;
		private SerializedProperty m_TPCameraController;
		private SerializedProperty m_PlayerMotor;
		private SerializedProperty m_CharacterRoot;
		private SerializedProperty m_Character;
		private SerializedProperty m_CharacterFaceSharpness;
		private SerializedProperty m_ReturnToReferenceFaceSharpness;
		private SerializedProperty m_SecondaryMouseFaceSharpness;
		private SerializedProperty m_FaceDirectionHorizontalSensitivity;
		private SerializedProperty m_SnapFaceToReferenceWithSecondaryMouse;
		private SerializedProperty m_EnableStandingSecondaryMouseTurn;
		private SerializedProperty m_StandingTurnThresholdAngle;
		private SerializedProperty m_StandingTurnAnimationExtraAngle;
		private SerializedProperty m_StandingTurnInterval;
		private SerializedProperty m_StandingTurnAngularSpeed;
		private SerializedProperty m_TurnAnimationLockDuration;
		private SerializedProperty m_TurnTransitionDuration;
		private SerializedProperty m_MoveSpeed;
		private SerializedProperty m_BackMoveSpeedMultiplier;
		private SerializedProperty m_StandingJumpAirMoveSpeed;
		private SerializedProperty m_JumpStartAnimationLockDuration;
		private SerializedProperty m_MinAirborneDurationForLandingAnimation;
		private SerializedProperty m_AirborneLocomotionToJumpLoopDelay;
		private SerializedProperty m_JumpTransitionDuration;

		private void OnEnable()
		{
			m_Script = (PlayerController)target;
			m_CameraTarget = serializedObject.FindProperty("m_CameraTarget");
			m_TPCameraController = serializedObject.FindProperty("m_TPCameraController");
			m_PlayerMotor = serializedObject.FindProperty("m_PlayerMotor");
			m_CharacterRoot = serializedObject.FindProperty("m_CharacterRoot");
			m_Character = serializedObject.FindProperty("m_Character");
			m_CharacterFaceSharpness = serializedObject.FindProperty("m_CharacterFaceSharpness");
			m_ReturnToReferenceFaceSharpness = serializedObject.FindProperty("m_ReturnToReferenceFaceSharpness");
			m_SecondaryMouseFaceSharpness = serializedObject.FindProperty("m_SecondaryMouseFaceSharpness");
			m_FaceDirectionHorizontalSensitivity = serializedObject.FindProperty("m_FaceDirectionHorizontalSensitivity");
			m_SnapFaceToReferenceWithSecondaryMouse = serializedObject.FindProperty("m_SnapFaceToReferenceWithSecondaryMouse");
			m_EnableStandingSecondaryMouseTurn = serializedObject.FindProperty("m_EnableStandingSecondaryMouseTurn");
			m_StandingTurnThresholdAngle = serializedObject.FindProperty("m_StandingTurnThresholdAngle");
			m_StandingTurnAnimationExtraAngle = serializedObject.FindProperty("m_StandingTurnAnimationExtraAngle");
			m_StandingTurnInterval = serializedObject.FindProperty("m_StandingTurnInterval");
			m_StandingTurnAngularSpeed = serializedObject.FindProperty("m_StandingTurnAngularSpeed");
			m_TurnAnimationLockDuration = serializedObject.FindProperty("m_TurnAnimationLockDuration");
			m_TurnTransitionDuration = serializedObject.FindProperty("m_TurnTransitionDuration");
			m_MoveSpeed = serializedObject.FindProperty("m_MoveSpeed");
			m_BackMoveSpeedMultiplier = serializedObject.FindProperty("m_BackMoveSpeedMultiplier");
			m_StandingJumpAirMoveSpeed = serializedObject.FindProperty("m_StandingJumpAirMoveSpeed");
			m_JumpStartAnimationLockDuration = serializedObject.FindProperty("m_JumpStartAnimationLockDuration");
			m_MinAirborneDurationForLandingAnimation = serializedObject.FindProperty("m_MinAirborneDurationForLandingAnimation");
			m_AirborneLocomotionToJumpLoopDelay = serializedObject.FindProperty("m_AirborneLocomotionToJumpLoopDelay");
			m_JumpTransitionDuration = serializedObject.FindProperty("m_JumpTransitionDuration");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			DrawScriptField();
			EditorGUILayout.Space(6f);
			DrawSection("References", m_CameraTarget, m_TPCameraController, m_PlayerMotor, m_CharacterRoot, m_Character);
			DrawSection("Facing", m_CharacterFaceSharpness, m_ReturnToReferenceFaceSharpness, m_SecondaryMouseFaceSharpness, m_FaceDirectionHorizontalSensitivity, m_SnapFaceToReferenceWithSecondaryMouse);
			DrawSection("Standing Turn", m_EnableStandingSecondaryMouseTurn, m_StandingTurnThresholdAngle, m_StandingTurnAnimationExtraAngle, m_StandingTurnInterval, m_StandingTurnAngularSpeed, m_TurnAnimationLockDuration, m_TurnTransitionDuration);
			DrawSection("Movement", m_MoveSpeed, m_BackMoveSpeedMultiplier);
			DrawSection("Jump And Air", m_StandingJumpAirMoveSpeed, m_JumpStartAnimationLockDuration, m_MinAirborneDurationForLandingAnimation, m_AirborneLocomotionToJumpLoopDelay, m_JumpTransitionDuration);

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawSection(string title, params SerializedProperty[] properties)
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
			for (int i = 0; i < properties.Length; i++)
			{
				if (properties[i] != null)
				{
					EditorGUILayout.PropertyField(properties[i]);
				}
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space(3f);
		}

		private void DrawScriptField()
		{
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(m_Script), typeof(MonoScript), false);
			EditorGUI.EndDisabledGroup();
		}
	}
}

