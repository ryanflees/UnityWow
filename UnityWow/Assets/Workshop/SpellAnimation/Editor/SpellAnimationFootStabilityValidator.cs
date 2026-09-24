using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CR
{
    public static class SpellAnimationFootStabilityValidator
    {
        private const string m_CharacterPath = "Assets/Art/Characters/WowGirl/WowGirl.prefab";
        private const string m_ReportPath = "Assets/Workshop/SpellAnimation/Reports/FootStability.json";
        private const float m_MaximumDrift = 0.0015f;
        private static readonly HumanBodyBones[] m_Bones =
        {
            HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot,
            HumanBodyBones.LeftToes, HumanBodyBones.RightToes
        };
        private static readonly string[] m_States =
        {
            "CastSpellDirected", "CastingSpellOmni", "CastSpellDirectedFinish",
            "CastSpellOmniFinish", "ChannelCastDirected", "ChannelCastOmni"
        };

        [Serializable]
        private class FootStabilityReport
        {
            public float maximum_drift_m;
            public List<FootStabilitySample> clips = new List<FootStabilitySample>();
            public List<FootStabilitySample> transitions = new List<FootStabilitySample>();
        }

        [Serializable]
        private class FootStabilitySample
        {
            public string state;
            public int samples;
            public Vector3[] foot_and_toe_spans_m;
            public float maximum_drift_m;
        }

        [MenuItem("CR/Workshop/Validate Spell Foot Stability")]
        public static string Validate()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Validate spell feet outside Play mode.");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_CharacterPath);
            if (prefab == null) throw new InvalidOperationException("WowGirl prefab was not found.");
            Scene scene = EditorSceneManager.NewPreviewScene();
            FootStabilityReport report = new FootStabilityReport();
            try
            {
                GameObject characterObject = UnityEngine.Object.Instantiate(prefab);
                SceneManager.MoveGameObjectToScene(characterObject, scene);
                Animator animator = characterObject.GetComponentInChildren<Animator>();
                AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
                if (controller == null) throw new InvalidOperationException("WowGirl requires an AnimatorController.");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.SetLayerWeight(1, 1f);
                animator.SetLayerWeight(2, 1f);
                foreach (string stateName in m_States)
                {
                    AnimatorState state = Array.Find(controller.layers[1].stateMachine.states,
                        child => child.state.name == stateName).state;
                    if (state == null || !state.iKOnFeet || !(state.motion is AnimationClip))
                        throw new InvalidOperationException($"{stateName} requires a clip with Foot IK enabled.");
                    FootStabilitySample sample = Measure(animator, stateName, 121, index =>
                    {
                        animator.Play("SpellLayer." + stateName, 1, index / 120f);
                        animator.Update(0f);
                    });
                    report.clips.Add(sample);
                    report.maximum_drift_m = Mathf.Max(report.maximum_drift_m, sample.maximum_drift_m);
                }

                string[] readyStates = { "CastSpellDirected", "CastingSpellOmni" };
                string[] releaseStates = { "CastSpellDirectedFinish", "CastSpellOmniFinish" };
                for (int pairIndex = 0; pairIndex < readyStates.Length; pairIndex++)
                {
                    animator.Play("SpellLayer." + readyStates[pairIndex], 1, 0.5f);
                    animator.Update(0f);
                    animator.CrossFadeInFixedTime("SpellLayer." + releaseStates[pairIndex], 0.12f, 1, 0f);
                    FootStabilitySample sample = Measure(animator,
                        readyStates[pairIndex] + " -> " + releaseStates[pairIndex], 16,
                        index => animator.Update(index == 0 ? 0f : 0.01f));
                    report.transitions.Add(sample);
                    report.maximum_drift_m = Mathf.Max(report.maximum_drift_m, sample.maximum_drift_m);
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(m_ReportPath));
            File.WriteAllText(m_ReportPath, JsonUtility.ToJson(report, true) + "\n");
            AssetDatabase.ImportAsset(m_ReportPath);
            if (report.maximum_drift_m > m_MaximumDrift)
                throw new InvalidOperationException($"Spell foot drift {report.maximum_drift_m * 1000f:F3} mm exceeds {m_MaximumDrift * 1000f:F1} mm. See {m_ReportPath}.");
            string summary = $"Validated six spell clips and both cast-to-release transitions. Maximum foot/toe drift: {report.maximum_drift_m * 1000f:F3} mm.";
            Debug.Log(summary);
            return summary;
        }

        private static FootStabilitySample Measure(Animator animator, string state, int count, Action<int> evaluate)
        {
            Bounds[] bounds = new Bounds[m_Bones.Length];
            for (int sampleIndex = 0; sampleIndex < count; sampleIndex++)
            {
                evaluate(sampleIndex);
                for (int boneIndex = 0; boneIndex < m_Bones.Length; boneIndex++)
                {
                    Transform bone = animator.GetBoneTransform(m_Bones[boneIndex]);
                    if (bone == null) throw new InvalidOperationException($"Missing humanoid bone: {m_Bones[boneIndex]}");
                    Vector3 position = animator.transform.InverseTransformPoint(bone.position);
                    if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z))
                        throw new InvalidOperationException($"Non-finite foot position in {state}.");
                    if (sampleIndex == 0) bounds[boneIndex] = new Bounds(position, Vector3.zero);
                    else bounds[boneIndex].Encapsulate(position);
                }
            }

            FootStabilitySample sample = new FootStabilitySample
            {
                state = state,
                samples = count,
                foot_and_toe_spans_m = new Vector3[m_Bones.Length]
            };
            for (int boneIndex = 0; boneIndex < bounds.Length; boneIndex++)
            {
                Vector3 span = bounds[boneIndex].size;
                sample.foot_and_toe_spans_m[boneIndex] = span;
                sample.maximum_drift_m = Mathf.Max(sample.maximum_drift_m, span.x, span.y, span.z);
            }
            return sample;
        }
    }
}

