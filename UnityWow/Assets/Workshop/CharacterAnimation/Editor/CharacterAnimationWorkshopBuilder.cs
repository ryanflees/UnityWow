using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CR
{
    public static class CharacterAnimationWorkshopBuilder
    {
        private const string m_ArtPath = "Assets/Art/Characters/WowGirl";
        private const string m_TestPath = "Assets/Workshop/CharacterAnimation";
        private const string m_ModelPath = m_ArtPath + "/Model/WowGirl.fbx";
        private const string m_ScenePath = m_TestPath + "/Scenes/CharacterAnimationWorkshop.unity";

        private static string[] GetAnimationPaths()
        {
            return Directory.GetFiles(m_ArtPath + "/Animations", "*.fbx")
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path).ToArray();
        }

        private static AnimationClip[] GetBrowserClips()
        {
            var clips = GetAnimationPaths().SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>().Where(clip => !clip.name.StartsWith("__preview__"))).ToArray();
            if (clips.Length == 0) throw new InvalidOperationException("No WowGirl animation clips were found.");
            return clips.Where(clip => !IsCompatibilityAlias(clip, clips)).Distinct()
                .OrderBy(clip => clip.name, StringComparer.Ordinal).ToArray();
        }

        private static bool IsCompatibilityAlias(AnimationClip clip, AnimationClip[] clips)
        {
            string originalName = clip.name switch { "TurnLeft" => "ShuffleLeft", "TurnRight" => "ShuffleRight", _ => null };
            if (originalName == null) return false;
            AnimationClip original = clips.FirstOrDefault(item => item.name == originalName);
            return original != null && File.ReadAllBytes(AssetDatabase.GetAssetPath(clip))
                .SequenceEqual(File.ReadAllBytes(AssetDatabase.GetAssetPath(original)));
        }

        [MenuItem("CR/Workshop/Refresh Character Animation Clips")]
        public static void RefreshClips()
        {
            AnimationClip[] clips = GetBrowserClips();
            WithWorkshop(workshop =>
            {
                Undo.RecordObject(workshop, "Refresh character animation clips");
                var serialized = new SerializedObject(workshop);
                var character = serialized.FindProperty("m_Character").objectReferenceValue as Animator;
                var camera = serialized.FindProperty("m_Camera").objectReferenceValue as Camera;
                workshop.Configure(character, clips, camera);
                EditorUtility.SetDirty(workshop);
                EditorSceneManager.MarkSceneDirty(workshop.gameObject.scene);
                if (!EditorSceneManager.SaveScene(workshop.gameObject.scene))
                    throw new IOException("Could not save the refreshed workshop scene.");
            });
            Debug.Log($"Character Animation Workshop refreshed: {clips.Length} clips.");
        }

        private static void WithWorkshop(Action<CharacterAnimationWorkshop> operation)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Run workshop maintenance outside Play mode.");
            Scene scene = SceneManager.GetSceneByPath(m_ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(m_ScenePath, OpenSceneMode.Additive);
            try
            {
                CharacterAnimationWorkshop workshop = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<CharacterAnimationWorkshop>(true)).Single();
                operation(workshop);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("CR/Workshop/Build Character Animation Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Build the workshop outside Play mode.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(m_ModelPath).OfType<Avatar>().SingleOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("The shared WowGirl model requires a valid Humanoid Avatar.");
            AnimationClip[] animations = GetBrowserClips();
            Directory.CreateDirectory(m_TestPath + "/Scenes");
            var previousScene = SceneManager.GetActiveScene();
            if (previousScene.isDirty)
            {
                string backup = AssetDatabase.GenerateUniqueAssetPath(m_TestPath + "/Scenes/PreviousSceneBackup.unity");
                if (!EditorSceneManager.SaveScene(previousScene, backup, true)) throw new IOException("Could not preserve the previous unsaved scene.");
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CharacterAnimationWorkshop";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(m_ModelPath);
            var character = (GameObject)PrefabUtility.InstantiatePrefab(model);
            character.name = "WowGirl";
            var animator = character.GetComponent<Animator>() ?? character.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            foreach (var renderer in character.GetComponentsInChildren<SkinnedMeshRenderer>()) renderer.updateWhenOffscreen = true;
            var backgroundCamera = new GameObject("BackgroundCamera", typeof(Camera)).GetComponent<Camera>();
            backgroundCamera.depth = -100;
            backgroundCamera.cullingMask = 0;
            backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
            backgroundCamera.backgroundColor = new Color(0.025f, 0.035f, 0.055f);
            var cameraObject = new GameObject("WorkshopCamera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.105f, 0.145f);
            camera.fieldOfView = 38;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100;
            camera.rect = new Rect(0.28f, 0.19f, 0.72f, 0.81f);
            camera.transform.position = new Vector3(0, 1.37f, 3.4f);
            camera.transform.LookAt(new Vector3(0, 0.9f, 0));
            var light = new GameObject("KeyLight", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(40, -35, 0);
            var fill = new GameObject("FillLight", typeof(Light)).GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.45f;
            fill.color = new Color(0.68f, 0.8f, 1);
            fill.transform.rotation = Quaternion.Euler(25, 150, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.65f, 0.75f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "PreviewFloor";
            floor.transform.localScale = Vector3.one * 2;
            Directory.CreateDirectory(m_TestPath + "/Materials");
            string floorPath = m_TestPath + "/Materials/PreviewFloor.mat";
            var floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(floorPath);
            if (floorMaterial == null)
            {
                floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                floorMaterial.SetColor("_BaseColor", new Color(0.16f, 0.2f, 0.25f));
                floorMaterial.SetFloat("_Smoothness", 0.15f);
                AssetDatabase.CreateAsset(floorMaterial, floorPath);
            }
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            var workshop = new GameObject("CharacterAnimationWorkshop").AddComponent<CharacterAnimationWorkshop>();
            workshop.Configure(animator, animations, camera);
            (animations.FirstOrDefault(clip => clip.name == "Stand") ?? animations[0]).SampleAnimation(character, 0);
            EditorSceneManager.SaveScene(scene, m_ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = character;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(0, 0.9f, 0), new Vector3(2, 2.2f, 2)), false);
            Debug.Log($"Character Animation Workshop ready: {animations.Length} clips.");
        }

        [MenuItem("CR/Workshop/Validate Character Animations")]
        public static string Validate()
        {
            AnimationClip[] browserClips = GetBrowserClips();
            WithWorkshop(workshop =>
            {
                var serialized = new SerializedObject(workshop);
                var entries = serialized.FindProperty("m_Clips");
                var actual = Enumerable.Range(0, entries.arraySize)
                    .Select(index => entries.GetArrayElementAtIndex(index).objectReferenceValue as AnimationClip).ToArray();
                if (!actual.SequenceEqual(browserClips))
                    throw new InvalidOperationException("The workshop clip list is stale. Run CR/Workshop/Refresh Character Animation Clips.");
                var animator = serialized.FindProperty("m_Character").objectReferenceValue as Animator;
                if (animator == null || !animator.isHuman || serialized.FindProperty("m_Camera").objectReferenceValue == null)
                    throw new InvalidOperationException("The workshop requires its Humanoid character and preview camera.");
            });
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(m_ModelPath);
            if (model == null) throw new InvalidOperationException("The shared WowGirl model was not found.");
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            int checkedClips = 0;
            int checkedSamples = 0;
            try
            {
                var character = (GameObject)PrefabUtility.InstantiatePrefab(model, previewScene);
                var paths = GetAnimationPaths();
                foreach (var path in paths)
                {
                    var clip = AssetDatabase.LoadAllAssetsAtPath(path.Replace('\\', '/')).OfType<AnimationClip>().Single(item => !item.name.StartsWith("__preview__"));
                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    if (bindings.Length == 0) throw new InvalidOperationException(clip.name + " has no animation curves.");
                    foreach (var binding in bindings)
                        if (binding.type == typeof(Transform) && binding.path.Length > 0 && character.transform.Find(binding.path) == null)
                            throw new InvalidOperationException(clip.name + " missing bone: " + binding.path);
                    for (int sample = 0; sample <= 4; sample++)
                    {
                        clip.SampleAnimation(character, clip.length * sample / 4f);
                        foreach (var bone in character.GetComponentsInChildren<Transform>())
                            if (!float.IsFinite(bone.position.x) || !float.IsFinite(bone.position.y) || !float.IsFinite(bone.position.z)) throw new InvalidOperationException(clip.name + " invalid pose.");
                        checkedSamples++;
                    }
                    checkedClips++;
                }
                foreach (var renderer in character.GetComponentsInChildren<Renderer>())
                    if (renderer.sharedMaterials.Any(material => material == null || material.shader == null || material.shader.name.Contains("InternalError"))) throw new InvalidOperationException("Missing character material or shader.");
                string report = $"Validated {checkedClips} asset clips, {browserClips.Length} unique workshop entries, {checkedSamples} sampled poses, scene references, animation transform paths and character materials.";
                Directory.CreateDirectory(m_TestPath + "/Reports");
                File.WriteAllText(m_TestPath + "/Reports/Validation.txt", report + "\n");
                Debug.Log(report);
                return report;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }
    }
}
