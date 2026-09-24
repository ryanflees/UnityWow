// Copyright (c) 2026 CatRabbit. All rights reserved.

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CR
{
	[CustomEditor(typeof(SpellDefinition))]
	public class SpellDefinitionEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			foreach (SpellValidationIssue issue in SpellDefinitionValidator.Validate((SpellDefinition)target))
				EditorGUILayout.HelpBox(issue.m_Path + ": " + issue.m_Message,
					issue.m_Severity == SpellValidationSeverity.Error ? MessageType.Error : MessageType.Warning);
		}

		[MenuItem("CR/Spells/Validate Spell Definitions")]
		public static void ValidateAll()
		{
			StringBuilder report = new StringBuilder("Spell definition audit\n");
			Dictionary<int, string> spellPaths = new Dictionary<int, string>();
			List<SpellDefinition> spells = new List<SpellDefinition>();
			int errors = 0;
			int warnings = 0;
			foreach (string guid in AssetDatabase.FindAssets("t:SpellDefinition", new[] { "Assets" }))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				SpellDefinition spell = AssetDatabase.LoadAssetAtPath<SpellDefinition>(path);
				spells.Add(spell);
				if (spell != null && spell.m_Id > 0 && !spellPaths.TryAdd(spell.m_Id, path))
				{
					report.AppendLine($"ERROR {path}: duplicate id {spell.m_Id}, also used by {spellPaths[spell.m_Id]}.");
					errors++;
				}
				foreach (SpellValidationIssue issue in SpellDefinitionValidator.Validate(spell))
				{
					report.AppendLine($"{issue.m_Severity} {path} / {issue.m_Path}: {issue.m_Message}");
					if (issue.m_Severity == SpellValidationSeverity.Error) errors++;
					else warnings++;
				}
			}
			foreach (SpellDefinition spell in spells)
			{
				if (spell == null || spell.m_EffectList == null) continue;
				foreach (SpellEffect effect in spell.m_EffectList)
				{
					if (effect is TriggerSpellEffect trigger && !spellPaths.ContainsKey(trigger.m_TriggeredSpellId))
					{
						report.AppendLine($"ERROR {AssetDatabase.GetAssetPath(spell)}: triggered spell {trigger.m_TriggeredSpellId} was not found.");
						errors++;
					}
				}
			}
			foreach (string guid in AssetDatabase.FindAssets("t:SpellConfigCollection", new[] { "Assets" }))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				SpellConfigCollection collection = AssetDatabase.LoadAssetAtPath<SpellConfigCollection>(path);
				HashSet<int> ids = new HashSet<int>();
				if (collection.m_SpellList == null)
				{
					report.AppendLine($"ERROR {path}: spell list is missing.");
					errors++;
					continue;
				}
				foreach (SpellDefinition spell in collection.m_SpellList)
				{
					if (spell == null || spell.m_Id <= 0 || !ids.Add(spell.m_Id))
					{
						report.AppendLine($"ERROR {path}: missing spell, invalid id, or duplicate collection entry.");
						errors++;
					}
				}
			}
			int iconCount = 0;
			int unpreparedIcons = 0;
			foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/UI/Icon" }))
			{
				TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
				if (importer == null) continue;
				iconCount++;
				if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode == SpriteImportMode.None) unpreparedIcons++;
			}
			report.AppendLine($"\n{spells.Count} definitions; {errors} errors; {warnings} warnings.");
			report.AppendLine($"{iconCount} icon textures; {unpreparedIcons} require Sprite import before assignment.");
			report.AppendLine("Configuration validation does not verify a combat executor. Gameplay controllers and effect handlers still require implementation.");
			Directory.CreateDirectory("Temp");
			File.WriteAllText("Temp/SpellDefinitionAudit.txt", report.ToString());
			Debug.Log(report.ToString());
		}

		[MenuItem("Assets/CR/Prepare Selected Spell Icons")]
		public static void PrepareSelectedIcons()
		{
			int count = 0;
			foreach (Texture2D texture in Selection.GetFiltered<Texture2D>(SelectionMode.DeepAssets))
			{
				string path = AssetDatabase.GetAssetPath(texture);
				if (!path.StartsWith("Assets/Art/UI/Icon/", System.StringComparison.Ordinal)) continue;
				TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
				if (importer == null) continue;
				Undo.RecordObject(importer, "Prepare Spell Icon");
				importer.textureType = TextureImporterType.Sprite;
				importer.spriteImportMode = SpriteImportMode.Single;
				importer.mipmapEnabled = false;
				importer.alphaIsTransparency = true;
				importer.wrapMode = TextureWrapMode.Clamp;
				importer.SaveAndReimport();
				count++;
			}
			Debug.Log($"Prepared {count} selected spell icons as Sprites.");
		}
	}
}

