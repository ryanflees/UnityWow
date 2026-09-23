// Copyright (c) 2026 CatRabbit. All rights reserved.

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CR
{
	public static class SpellAnimationRigUtility
	{
		private const string m_CharacterModelPath = "Assets/Art/Characters/WowGirl/Model/WowGirl.fbx";
		private const string m_AnimationRootPath = "Assets/Art/Characters/WowGirl/Animations";

		[MenuItem("CR/Workshop/Fix Spell Animation Rig")]
		public static void Fix()
		{
			Avatar characterAvatar = AssetDatabase.LoadAllAssetsAtPath(m_CharacterModelPath)
				.OfType<Avatar>().FirstOrDefault();
			if (characterAvatar == null || !characterAvatar.isValid || !characterAvatar.isHuman)
			{
				throw new InvalidOperationException("The WowGirl character model requires a valid Humanoid Avatar.");
			}

			string[] animationPaths = AssetDatabase.FindAssets("t:Model", new[] { m_AnimationRootPath })
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
				.OrderBy(path => path)
				.ToArray();
			if (animationPaths.Length != 142)
			{
				throw new InvalidOperationException($"Expected 142 unique animation FBX files, found {animationPaths.Length}.");
			}

			for (int i = 0; i < animationPaths.Length; i++)
			{
				string animationPath = animationPaths[i];
				ModelImporter importer = AssetImporter.GetAtPath(animationPath) as ModelImporter;
				if (importer == null)
				{
					throw new InvalidOperationException($"Spell animation importer was not found: {animationPath}");
				}

				importer.animationType = ModelImporterAnimationType.Human;
				importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
				importer.sourceAvatar = characterAvatar;
				importer.importAnimation = true;
				importer.motionNodeName = "<Root Transform>";
				importer.SaveAndReimport();
			}

			AssetDatabase.SaveAssets();
			Debug.Log($"Reimported {animationPaths.Length} animations as Humanoid assets using the WowGirl prefab Avatar.");
		}
	}
}
