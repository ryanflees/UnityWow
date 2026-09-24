// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace CR
{
	[CustomEditor(typeof(CharacterLookAtIk))]
	public class CharacterLookAtIkEditor : Editor
	{
		private static readonly string[] m_BlendProperties =
		{
			"m_EnableUpperBodyStabilization", "m_UpperBodyWeight", "m_UpperBodyFacingLockWeight",
			"m_UpperBodyBlendInDuration", "m_UpperBodyBlendOutDuration", "m_UpperBodyAnchorBone"
		};
		private static readonly string[] m_TorsoProperties =
		{
			"m_SpineCorrectionWeight", "m_ChestCorrectionWeight", "m_UpperChestCorrectionWeight",
			"m_MaxUpperBodyJointTwist", "m_MaxUpperBodyJointRotationSpeed"
		};
		private static readonly string[] m_OrientationProperties =
		{
			"m_PreserveUpperBodyHeadAndArmOrientation", "m_HeadOrientationWeight",
			"m_LeftArmOrientationWeight", "m_RightArmOrientationWeight", "m_MaxArmCorrectionAngle",
			"m_MaxHeadCorrectionAngle", "m_NeckCorrectionShare", "m_UpperBodyLookAtSuppression"
		};
		private static readonly string[] m_LookAtProperties =
		{
			"m_Enable", "m_EnableBody", "m_BodyWeight", "m_Weight", "m_HeadWeight", "m_MaxAngle",
			"m_TargetDistance", "m_TargetHeight", "m_UseTargetTransform"
		};
		private static readonly string[] m_BindingProperties =
		{
			"m_Animator", "m_BaseTransform", "m_Head", "m_Target", "m_BodyBone"
		};
		private bool m_ShowBlend = true;
		private bool m_ShowTorso = true;
		private bool m_ShowOrientation = true;
		private bool m_ShowLookAt;
		private bool m_ShowBindings;
		private CharacterLookAtIk m_SaveTarget;

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUILayout.HelpBox("Lower Facing Lock and arm weights for a looser moving cast. Torso shares redistribute correction; use Upper Body Weight to reduce its total strength.", MessageType.Info);
			DrawGroup("Pose and Blending", ref m_ShowBlend, m_BlendProperties);
			DrawGroup("Torso Distribution and Limits", ref m_ShowTorso, m_TorsoProperties);
			DrawGroup("Head and Arms", ref m_ShowOrientation, m_OrientationProperties);
			DrawGroup("Look At", ref m_ShowLookAt, m_LookAtProperties);
			DrawGroup("Runtime Bindings", ref m_ShowBindings, m_BindingProperties);
			serializedObject.ApplyModifiedProperties();

			CharacterLookAtIk component = (CharacterLookAtIk)target;
			if (Application.isPlaying && !EditorUtility.IsPersistent(component))
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Live Pose", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Weight / Torso Offset", $"{component.UpperBodyPoseWeight:0.00} / {component.UpperBodyRotationError:0.0} deg");
				if (component.IsUpperBodyTwistLimited)
					EditorGUILayout.HelpBox("A torso joint reached its twist limit. Lower facing lock or arm preservation before increasing the limit.", MessageType.Info);
				DrawSaveTuning(component);
				Repaint();
			}
		}

		private void DrawGroup(string title, ref bool expanded, string[] properties)
		{
			expanded = EditorGUILayout.Foldout(expanded, title, true, EditorStyles.foldoutHeader);
			if (!expanded) return;
			EditorGUI.indentLevel++;
			float previousLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = Mathf.Clamp(EditorGUIUtility.currentViewWidth * 0.5f, 150f, 235f);
			foreach (string property in properties)
			{
				SerializedProperty value = serializedObject.FindProperty(property);
				EditorGUILayout.PropertyField(value, new GUIContent(GetLabel(value), value.tooltip));
			}
			EditorGUIUtility.labelWidth = previousLabelWidth;
			EditorGUI.indentLevel--;
			EditorGUILayout.Space();
		}

		private static string GetLabel(SerializedProperty property)
		{
			switch (property.name)
			{
				case "m_EnableUpperBodyStabilization": return "Enable Stabilization";
				case "m_UpperBodyWeight": return "Stabilization Weight";
				case "m_UpperBodyFacingLockWeight": return "Facing Lock";
				case "m_UpperBodyBlendInDuration": return "Blend In (s)";
				case "m_UpperBodyBlendOutDuration": return "Blend Out (s)";
				case "m_UpperBodyAnchorBone": return "Anchor Bone";
				case "m_SpineCorrectionWeight": return "Spine Share";
				case "m_ChestCorrectionWeight": return "Chest Share";
				case "m_UpperChestCorrectionWeight": return "Upper Chest Share";
				case "m_MaxUpperBodyJointTwist": return "Joint Twist Limit (deg)";
				case "m_MaxUpperBodyJointRotationSpeed": return "Joint Turn Speed (deg/s)";
				case "m_PreserveUpperBodyHeadAndArmOrientation": return "Preserve Head / Arms";
				case "m_HeadOrientationWeight": return "Head Preservation";
				case "m_LeftArmOrientationWeight": return "Left Arm Preservation";
				case "m_RightArmOrientationWeight": return "Right Arm Preservation";
				case "m_MaxArmCorrectionAngle": return "Arm Correction Limit (deg)";
				case "m_MaxHeadCorrectionAngle": return "Head Correction Limit (deg)";
				case "m_NeckCorrectionShare": return "Neck Share";
				case "m_UpperBodyLookAtSuppression": return "LookAt Suppression";
				default: return property.displayName;
			}
		}

		private void DrawSaveTuning(CharacterLookAtIk component)
		{
			CharacterLookAtIk source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(component);
			if (source == null) source = component.m_EditorTuningSource;
			if (source == null)
			{
				m_SaveTarget = (CharacterLookAtIk)EditorGUILayout.ObjectField("Save To Prefab", m_SaveTarget, typeof(CharacterLookAtIk), false);
				source = m_SaveTarget;
			}
			bool canSave = source != null && PrefabUtility.IsPartOfPrefabAsset(source) && !PrefabUtility.IsPartOfImmutablePrefab(source);
			EditorGUILayout.HelpBox("Play Mode edits are temporary. Save Tuning to Prefab copies only pose/LookAt settings and leaves runtime bone and target references untouched.", MessageType.None);
			using (new EditorGUI.DisabledScope(!canSave))
				if (GUILayout.Button("Save Tuning to Prefab")) SaveTuning(component, source);
		}

		public static void SaveTuning(CharacterLookAtIk component, CharacterLookAtIk destination)
		{
			if (component == null || destination == null || !PrefabUtility.IsPartOfPrefabAsset(destination) ||
				PrefabUtility.IsPartOfImmutablePrefab(destination))
				throw new System.ArgumentException("Choose an editable prefab component as the tuning destination.");
			Undo.RecordObject(destination, "Save character pose tuning");
			SerializedObject from = new SerializedObject(component);
			SerializedObject to = new SerializedObject(destination);
			foreach (string[] group in new[] { m_BlendProperties, m_TorsoProperties, m_OrientationProperties, m_LookAtProperties })
				foreach (string property in group) to.CopyFromSerializedProperty(from.FindProperty(property));
			to.ApplyModifiedProperties();
			EditorUtility.SetDirty(destination);
			PrefabUtility.SavePrefabAsset(destination.transform.root.gameObject);
		}
	}
}
