using UnityEditor;
using UnityEngine;

namespace CR
{
    [CustomEditor(typeof(Unit))]
    public sealed class UnitEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying) return;
            UnitRuntimeState state = ((Unit)target).State;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime State", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("Health", state.CurrentHealth);
                EditorGUILayout.FloatField("Maximum Health", state.MaxHealth);
                EditorGUILayout.FloatField("Mana", state.CurrentMana);
                EditorGUILayout.FloatField("Maximum Mana", state.MaxMana);
                EditorGUILayout.EnumPopup("Life State", state.LifeState);
                EditorGUILayout.ObjectField("Selected Target", state.SelectedTarget, typeof(Unit), true);
                EditorGUILayout.Toggle("In Combat", state.IsInCombat);
            }
        }

        public override bool RequiresConstantRepaint() => Application.isPlaying;
    }
}

