// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CR
{
	public static class SpellAnimationWorkshopBuilder
	{
		private const string m_WorkshopPath = "Assets/Workshop/SpellAnimation";
		private const string m_DataPath = m_WorkshopPath + "/Data";
		private const string m_SpellPath = m_DataPath + "/Spells";
		private const string m_ScenePath = m_WorkshopPath + "/Scenes/SpellAnimationWorkshop.unity";
		private const string m_CharacterPath = "Assets/Art/Characters/WowGirl/WowGirl.prefab";
		private const string m_CollectionPath = m_DataPath + "/SpellAnimationSpellCollection.asset";
		private const string m_GlobalConfigPath = m_DataPath + "/SpellAnimationGlobalConfig.asset";

		[MenuItem("CR/Workshop/Build Spell Animation Scene")]
		public static void Build()
		{
			CreateFolders();
			SpellConfigCollection collection = CreateSpellData();
			GlobalConfig globalConfig = CreateGlobalConfig(collection);
			PreserveDirtyScene();
			Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
			scene.name = "SpellAnimationWorkshop";

			GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_CharacterPath);
			if (characterPrefab == null)
			{
				throw new InvalidOperationException("WowGirl prefab was not found.");
			}

			GameObject characterObject = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab);
			characterObject.name = "WowGirl_SpellPreview";
			characterObject.transform.position = Vector3.zero;
			Character character = characterObject.GetComponent<Character>() ?? characterObject.AddComponent<Character>();
			character.m_Animator = characterObject.GetComponentInChildren<Animator>();
			if (character.m_Animator == null)
			{
				throw new InvalidOperationException("WowGirl prefab requires an Animator.");
			}

			CreateEnvironment();
			VanillaRuntime runtime = new GameObject("VanillaRuntime").AddComponent<VanillaRuntime>();
			runtime.m_GlobalConfig = globalConfig;
			SpellAnimationWorkshop workshop = new GameObject("SpellAnimationWorkshop").AddComponent<SpellAnimationWorkshop>();
			workshop.Configure(character, collection);

			EditorSceneManager.SaveScene(scene, m_ScenePath);
			AssetDatabase.SaveAssets();
			Selection.activeGameObject = characterObject;
			if (SceneView.lastActiveSceneView != null)
			{
				SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(0f, 1f, 0f), new Vector3(3f, 2.5f, 3f)), false);
			}

			Debug.Log("Spell Animation Workshop scene and six test spells are ready.");
		}

		public static void BuildAndValidate()
		{
			Build();
			ValidateGeneratedContent();
		}

		private static SpellConfigCollection CreateSpellData()
		{
			SpellConfigCollection collection = LoadOrCreate<SpellConfigCollection>(m_CollectionPath);
			collection.m_SpellList = new List<SpellDefinition>
			{
				CreateSpell(99001, "Instant Directed", SpellCastType.Instant, SpellAnimationType.CastDirected, 0f, 0f),
				CreateSpell(99002, "Instant Omni", SpellCastType.Instant, SpellAnimationType.CastOmnidirectional, 0f, 0f),
				CreateSpell(99003, "Cast Directed", SpellCastType.CastTime, SpellAnimationType.CastDirected, 2.5f, 0f),
				CreateSpell(99004, "Cast Omni", SpellCastType.CastTime, SpellAnimationType.CastOmnidirectional, 2.5f, 0f),
				CreateSpell(99005, "Channel Directed", SpellCastType.Channeled, SpellAnimationType.ChannelDirected, 0f, 3f),
				CreateSpell(99006, "Channel Omni", SpellCastType.Channeled, SpellAnimationType.ChannelOmnidirectional, 0f, 3f)
			};
			EditorUtility.SetDirty(collection);
			return collection;
		}

		private static SpellDefinition CreateSpell(int id, string displayName, SpellCastType castType,
			SpellAnimationType animationType, float castTime, float channelDuration)
		{
			string assetName = displayName.Replace(" ", string.Empty);
			SpellDefinition spell = LoadOrCreate<SpellDefinition>($"{m_SpellPath}/{assetName}.asset");
			spell.m_Id = id;
			spell.m_FamilyId = id;
			spell.m_Rank = 1;
			spell.m_DisplayName = displayName;
			spell.m_Description = GetDescription(castType, animationType);
			spell.m_School = SpellSchool.Arcane;
			spell.m_Cast.m_Type = castType;
			spell.m_Cast.m_CastTime = castTime;
			spell.m_Cast.m_ChannelDuration = channelDuration;
			spell.m_Cast.m_ChannelInterval = castType == SpellCastType.Channeled ? 1f : 0f;
			spell.m_Cast.m_CanMoveWhileCasting = false;
			spell.m_Cast.m_RequireFacingTarget = animationType == SpellAnimationType.CastDirected ||
				animationType == SpellAnimationType.ChannelDirected;
			spell.m_Target.m_Type = SpellTargetType.Direction;
			spell.m_Target.m_AllowedRelations = SpellTargetRelation.Hostile;
			spell.m_Target.m_MaxRange = 30f;
			spell.m_Cooldown.m_GlobalCooldown = 1.5f;
			spell.m_Presentation.m_AnimationType = animationType;
			EditorUtility.SetDirty(spell);
			return spell;
		}

		private static string GetDescription(SpellCastType castType, SpellAnimationType animationType)
		{
			string direction = animationType == SpellAnimationType.CastDirected ||
				animationType == SpellAnimationType.ChannelDirected ? "directed" : "omnidirectional";
			return castType switch
			{
				SpellCastType.Instant => $"Immediately plays the {direction} release animation.",
				SpellCastType.CastTime => $"Casts for 2.5 seconds, then plays the {direction} release animation.",
				SpellCastType.Channeled => $"Plays the {direction} channel animation for up to 3 seconds.",
				_ => string.Empty
			};
		}

		private static GlobalConfig CreateGlobalConfig(SpellConfigCollection collection)
		{
			GlobalConfig globalConfig = LoadOrCreate<GlobalConfig>(m_GlobalConfigPath);
			globalConfig.m_ConfigCollectionBaseList = new List<ConfigCollectionBase> { collection };
			EditorUtility.SetDirty(globalConfig);
			return globalConfig;
		}

		private static T LoadOrCreate<T>(string path) where T : ScriptableObject
		{
			T asset = AssetDatabase.LoadAssetAtPath<T>(path);
			if (asset != null)
			{
				return asset;
			}

			asset = ScriptableObject.CreateInstance<T>();
			AssetDatabase.CreateAsset(asset, path);
			return asset;
		}

		private static void CreateEnvironment()
		{
			GameObject cameraObject = new GameObject("WorkshopCamera", typeof(Camera), typeof(AudioListener));
			cameraObject.tag = "MainCamera";
			Camera camera = cameraObject.GetComponent<Camera>();
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = new Color(0.055f, 0.075f, 0.11f);
			camera.fieldOfView = 35f;
			camera.nearClipPlane = 0.05f;
			camera.transform.position = new Vector3(0f, 1.35f, 3.8f);
			camera.transform.LookAt(new Vector3(0f, 1f, 0f));

			Light keyLight = new GameObject("KeyLight", typeof(Light)).GetComponent<Light>();
			keyLight.type = LightType.Directional;
			keyLight.intensity = 1.7f;
			keyLight.shadows = LightShadows.Soft;
			keyLight.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
			Light fillLight = new GameObject("FillLight", typeof(Light)).GetComponent<Light>();
			fillLight.type = LightType.Directional;
			fillLight.intensity = 0.55f;
			fillLight.color = new Color(0.55f, 0.7f, 1f);
			fillLight.transform.rotation = Quaternion.Euler(25f, 150f, 0f);
			RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
			RenderSettings.ambientLight = new Color(0.5f, 0.56f, 0.68f);

			GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
			floor.name = "PreviewFloor";
			floor.transform.localScale = Vector3.one * 1.6f;
			Material floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(
				"Assets/Workshop/CharacterAnimation/Materials/PreviewFloor.mat");
			if (floorMaterial != null)
			{
				floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
			}
		}

		private static void CreateFolders()
		{
			Directory.CreateDirectory(m_DataPath);
			Directory.CreateDirectory(m_SpellPath);
			Directory.CreateDirectory(m_WorkshopPath + "/Scenes");
			AssetDatabase.Refresh();
		}

		private static void PreserveDirtyScene()
		{
			Scene currentScene = SceneManager.GetActiveScene();
			if (!currentScene.IsValid() || !currentScene.isDirty)
			{
				return;
			}

			string backupPath = AssetDatabase.GenerateUniqueAssetPath(m_WorkshopPath + "/Scenes/PreviousSceneBackup.unity");
			if (!EditorSceneManager.SaveScene(currentScene, backupPath, true))
			{
				throw new IOException("Could not preserve the previous unsaved scene.");
			}
		}

		private static void ValidateGeneratedContent()
		{
			SpellConfigCollection collection = AssetDatabase.LoadAssetAtPath<SpellConfigCollection>(m_CollectionPath);
			GlobalConfig globalConfig = AssetDatabase.LoadAssetAtPath<GlobalConfig>(m_GlobalConfigPath);
			if (collection == null || collection.m_SpellList.Count != 6)
			{
				throw new InvalidOperationException("The workshop must contain exactly six spell definitions.");
			}

			if (globalConfig == null || globalConfig.GetConfigCollection<SpellConfigCollection>() != collection)
			{
				throw new InvalidOperationException("The spell collection is not registered in GlobalConfig.");
			}

			if (collection.m_SpellList[2].m_Cast.m_CastTime != 2.5f ||
				collection.m_SpellList[3].m_Cast.m_CastTime != 2.5f ||
				collection.m_SpellList[4].m_Cast.m_ChannelDuration != 3f ||
				collection.m_SpellList[5].m_Cast.m_ChannelDuration != 3f)
			{
				throw new InvalidOperationException("Workshop cast or channel durations are invalid.");
			}

			if (!File.Exists(m_ScenePath) || UnityEngine.Object.FindFirstObjectByType<SpellAnimationWorkshop>() == null)
			{
				throw new InvalidOperationException("The spell animation workshop scene is invalid.");
			}

			Debug.Log("Validated the Spell Animation Workshop scene, GlobalConfig, collection, and six spell definitions.");
		}
	}
}

