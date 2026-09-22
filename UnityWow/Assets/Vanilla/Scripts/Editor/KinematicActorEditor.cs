// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace CR
{
	[CustomEditor(typeof(KinematicActor))]
	public class KinematicActorEditor : UnityEditor.Editor
	{
		private KinematicActor m_Script;
		private SerializedProperty m_Motor;
		private SerializedProperty m_GravityStrength;
		private SerializedProperty m_JumpSpeed;
		private SerializedProperty m_AirMoveGroundContactProbeDistance;
		private SerializedProperty m_AirMoveGroundContactDrag;
		private SerializedProperty m_AirMoveGroundContactStopSpeed;

		private void OnEnable()
		{
			m_Script = (KinematicActor)target;
			m_Motor = serializedObject.FindProperty("m_Motor");
			m_GravityStrength = serializedObject.FindProperty("m_GravityStrength");
			m_JumpSpeed = serializedObject.FindProperty("m_JumpSpeed");
			m_AirMoveGroundContactProbeDistance = serializedObject.FindProperty("m_AirMoveGroundContactProbeDistance");
			m_AirMoveGroundContactDrag = serializedObject.FindProperty("m_AirMoveGroundContactDrag");
			m_AirMoveGroundContactStopSpeed = serializedObject.FindProperty("m_AirMoveGroundContactStopSpeed");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			DrawScriptField();
			EditorGUILayout.Space(6f);
			DrawSection("References", m_Motor);
			DrawSection("Gravity", m_GravityStrength);
			DrawSection("Jump", m_JumpSpeed);
			DrawSection("Air Move Ground Contact Damping", m_AirMoveGroundContactProbeDistance, m_AirMoveGroundContactDrag, m_AirMoveGroundContactStopSpeed);

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

