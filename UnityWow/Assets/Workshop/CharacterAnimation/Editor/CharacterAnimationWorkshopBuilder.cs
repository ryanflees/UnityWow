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

        private static string[] GetAnimationPaths()
        {
            return Directory.GetFiles(m_ArtPath + "/Animations", "*.fbx")
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path).ToArray();
        }

        [MenuItem("CR/Workshop/Build Character Animation Scene")]
        public static void Build()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var modelImporter = (ModelImporter)AssetImporter.GetAtPath(m_ModelPath);
            if (modelImporter == null) throw new InvalidOperationException("Export and copy the HumanFemale FBX assets first.");
            modelImporter.animationType = ModelImporterAnimationType.Human;
            modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            modelImporter.importAnimation = false;
            modelImporter.optimizeGameObjects = false;
            modelImporter.preserveHierarchy = true;
            var materials = AssetDatabase.FindAssets("t:Material", new[] { m_ArtPath + "/Materials" })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).ToArray();
            foreach (var material in materials)
                modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material.name), material);
            modelImporter.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(m_ModelPath).OfType<Avatar>().Single();
            var paths = GetAnimationPaths();
            if (paths.Length != 142) throw new InvalidOperationException($"Expected 142 unique animation FBX files, found {paths.Length}.");
            foreach (string path in paths)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
                importer.importAnimation = true;
                importer.motionNodeName = "<Root Transform>";
                importer.optimizeGameObjects = false;
                importer.preserveHierarchy = true;
                importer.animationCompression = ModelImporterAnimationCompression.Off;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
                string clipName = Path.GetFileNameWithoutExtension(path).Replace("WowGirl@", "");
                foreach (var clip in clips)
                {
                    clip.name = clipName;
                    clip.loopTime = IsLoop(clipName);
                    clip.loopPose = false;
                    clip.keepOriginalOrientation = true;
                    clip.keepOriginalPositionY = true;
                    clip.keepOriginalPositionXZ = true;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
            var animations = paths.SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(clip => !clip.name.StartsWith("__preview__"))).OrderBy(clip => clip.name).ToArray();
            if (animations.Length != 142) throw new InvalidOperationException($"Expected 142 imported clips, found {animations.Length}.");
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
            animations.Single(clip => clip.name == "Stand").SampleAnimation(character, 0);
            EditorSceneManager.SaveScene(scene, m_TestPath + "/Scenes/CharacterAnimationWorkshop.unity");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = character;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(0, 0.9f, 0), new Vector3(2, 2.2f, 2)), false);
            Debug.Log($"Character Animation Workshop ready: {animations.Length} clips.");
        }

        private static bool IsLoop(string clipName)
        {
            return clipName.StartsWith("Stand") && !clipName.Contains("Wound") || clipName.StartsWith("Ready") || clipName.StartsWith("Hold") || clipName.StartsWith("ChannelCast") || clipName.StartsWith("Swim") || clipName.EndsWith("Loop") || clipName is "Walk" or "Run" or "Walkbackwards" or "Sprint" or "Jump" or "Fall" or "Sleep" or "Stun" or "StealthWalk" or "StealthStand";
        }

        [MenuItem("CR/Workshop/Validate Character Animations")]
        public static string Validate()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(m_ModelPath);
            var character = UnityEngine.Object.Instantiate(model);
            character.hideFlags = HideFlags.HideAndDontSave;
            int checkedClips = 0;
            int checkedSamples = 0;
            try
            {
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
                if (checkedClips != 142) throw new InvalidOperationException("The complete source set was not imported.");
                foreach (var renderer in character.GetComponentsInChildren<Renderer>())
                    if (renderer.sharedMaterials.Any(material => material == null || material.shader == null || material.shader.name.Contains("InternalError"))) throw new InvalidOperationException("Missing character material or shader.");
                string report = $"Validated {checkedClips} clips, {checkedSamples} sampled poses, all animation transform paths and character materials.";
                Directory.CreateDirectory(m_TestPath + "/Reports");
                File.WriteAllText(m_TestPath + "/Reports/Validation.txt", report + "\n");
                Debug.Log(report);
                return report;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(character);
            }
        }
    }
}
