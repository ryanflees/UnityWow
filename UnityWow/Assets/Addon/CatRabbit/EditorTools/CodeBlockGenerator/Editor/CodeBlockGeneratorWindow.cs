using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using UnityEditor.Animations;
using UnityEngine.UI;

namespace CR
{
	public class CodeBlockGeneratorWindow : EditorWindow
	{
		public enum GenerateType
		{
			ObjectEditorProperties,
			ShaderHashID,
			AnimatorController,
			PostShaderPropertiesBuiltin,
			PostShaderPropertiesURP,
			PostShaderPropertiesHDRP
		}

		private GenerateType m_GenerateType;
		private string m_InputText = "";
		private string m_ClassName = "";
		//private Shader m_PostShader;

		private float m_TextAreaHeight = 250f;
		private AnimatorController m_AnimatorController;
		
		[MenuItem("CatRabbit/Scripts/CodeBlockGenerator")]
		private static void OpenPanel()
		{
			CodeBlockGeneratorWindow window = (CodeBlockGeneratorWindow)GetWindow(typeof(CodeBlockGeneratorWindow));
			window.minSize = new Vector2(600, 300);
			window.titleContent = new GUIContent("CodeBlockGenerator");
			window.Show();
		}

		private void OnGUI()
		{
			m_GenerateType = (GenerateType)EditorGUILayout.EnumPopup(new GUIContent("Generate Type"), m_GenerateType);

			EditorGUILayout.BeginVertical("box");

			int oldHelpFontSize = EditorStyles.helpBox.fontSize;
			//EditorStyles.helpBox.fontSize = 12;
			
			if (m_GenerateType == GenerateType.ObjectEditorProperties)
			{
				string helpTxt = "Enter name of a class which is derived from MonoBehavior or ScriptableObject. Result will be like:\r\n"
					+ "SerializedProperty proprety = serializedObject.FindProperty(xxx);\r\n"
					+ "EditorGUILayout.PropertyField(xxx);\r\n"
					+ "......";
				EditorGUILayout.HelpBox(helpTxt, MessageType.Info);
				m_ClassName = EditorGUILayout.TextField(new GUIContent("Class Name"), m_ClassName);
			}
			else if (m_GenerateType == GenerateType.ShaderHashID)
			{
				string helpTxt = "Input the code block of shader variables, for example:\r\n"
			                  + "uniform float4 _EmitColor;\r\n"
			                  + "sampler2D _DissolveTex;\r\n"
			                  + "......\r\n"
			                  + "result will be like:\r\n"
			                  + "private int _EmitColor = Shader.PropertyToID(_EmitColor);\r\n"
			                  + "private int _DissolveTex = Shader.PropertyToID(_DissolveTex);\r\n"
			                  + "......";
				EditorGUILayout.HelpBox(helpTxt, MessageType.Info);
				m_InputText = EditorGUILayout.TextArea(m_InputText, GUI.skin.textArea, GUILayout.MinHeight(m_TextAreaHeight));
			}
			else if (m_GenerateType == GenerateType.AnimatorController)
			{
				string helpTxt = "Generate code blocks for Animator usage\r\n"
					+ "result will be copied to clipboard";
				EditorGUILayout.HelpBox(helpTxt, MessageType.Info);
				m_AnimatorController = (AnimatorController)EditorGUILayout.ObjectField(
					new GUIContent("AnimatorController"),
					m_AnimatorController, typeof(AnimatorController), false);
			}
			else if (m_GenerateType == GenerateType.PostShaderPropertiesBuiltin)
			{
				string helpTxt = "Input the code block of shader variables, for example:\r\n"
				                 + "uniform float4 _TestColor;\r\n"
				                 + "float _TestFloat;\r\n"
				                 + "......\r\n"
				                 + "Some helpful codes for post processing will be generated and copied to clipboard";
				EditorGUILayout.HelpBox(helpTxt, MessageType.Info);
				m_InputText = EditorGUILayout.TextArea(m_InputText, GUI.skin.textArea, GUILayout.MinHeight(m_TextAreaHeight));
				//m_PostShader = (Shader)EditorGUILayout.ObjectField(new GUIContent("Post Shader"), m_PostShader, typeof(Shader), false);
				//testShader.pro
			}
			else if (m_GenerateType == GenerateType.PostShaderPropertiesURP)
			{
				string helpTxt = "Input the code block of shader variables, for example:\r\n"
			                  + "uniform float4 _TestColor;\r\n"
			                  + "float _TestFloat;\r\n"
			                  + "......\r\n"
			                  + "Some helpful codes for post processing will be generated and copied to clipboard";
				EditorGUILayout.HelpBox(helpTxt, MessageType.Info);
				m_InputText = EditorGUILayout.TextArea(m_InputText, GUI.skin.textArea, GUILayout.MinHeight(m_TextAreaHeight));
			}
			else if (m_GenerateType == GenerateType.PostShaderPropertiesHDRP)
			{
				string helpTxt = "Input the code block of shader variables, for example:\r\n"
				                 + "uniform float4 _TestColor;\r\n"
				                 + "float _TestFloat;\r\n"
				                 + "......\r\n"
				                 + "Some helpful codes for post processing will be generated and copied to clipboard";
				EditorGUILayout.HelpBox(helpTxt, MessageType.Info);
				m_InputText = EditorGUILayout.TextArea(m_InputText, GUI.skin.textArea, GUILayout.MinHeight(m_TextAreaHeight));
			}
			//m_InputText = EditorGUILayout.TextArea(m_InputText);
			//new GUIContent("Input Text"), 

			//EditorGUILayout.HelpBox(new GUIContent("Generated result will be copied to clipboard"));
			EditorGUILayout.LabelField("Generated result will be copied to clipboard");
			
			if (GUILayout.Button("Generate"))
			{
				Generate();	
			}
			EditorGUILayout.EndVertical();

			EditorStyles.helpBox.fontSize = oldHelpFontSize;
			//m_Test = serializedObject.Fin
		}

		private void Generate()
		{
			if (m_GenerateType == GenerateType.ObjectEditorProperties)
			{
				GenerateEditorProperties();
			}
			else if (m_GenerateType == GenerateType.ShaderHashID)
			{
				GenerateShaderHashID();
			}
			else if (m_GenerateType == GenerateType.AnimatorController)
			{
				GenerateAnimatorCodes(m_AnimatorController);
			}
			else if (m_GenerateType == GenerateType.PostShaderPropertiesBuiltin)
			{
				GeneratePostShaderPropertiesBuiltin();
			}
			else if (m_GenerateType == GenerateType.PostShaderPropertiesURP)
			{
				GeneratePostShaderPropertiesURP();
			}
			else if (m_GenerateType == GenerateType.PostShaderPropertiesHDRP)
			{
				GeneratePostShaderPropertiesHDRP();
			}
		}

		private void GenerateEditorProperties()
		{
			string txt = "";
			var classType = ReflectionTool.TypeByName(m_ClassName);
			if (classType != null)
			{
				List<string> fieldNames = ReflectionTool.GetFieldNames(classType);
				List<FieldInfo> fieldInfoList = new List<FieldInfo>();
				for (int i = 0; i < fieldNames.Count; i++)
				{
					FieldInfo fi = ReflectionTool.Field(classType, fieldNames[i]);
					if (fi.IsPublic)
					{
						fieldInfoList.Add(fi);
						Debug.Log(fi.Name);
					}
				}

				txt += "\t\t//SerializedProperty\r\n";
				for (int i = 0; i < fieldInfoList.Count; i++)
				{
					txt += "\t\tprivate SerializedProperty " + fieldInfoList[i].Name + ";\r\n";
				}

				txt += "\t\t////\r\n";

				txt += "\r\n";

				txt += "\t\tvoid OnEnable()\r\n";
				txt += "\t\t{\r\n";
				txt += "\t\t\tm_Target = target as " + m_ClassName + ";\r\n";
				txt += "\t\t\tGetProperties();\r\n";
				txt += "\t\t}\r\n";

				txt += "\r\n";
				
				txt += "\t\tpublic override void OnInspectorGUI()\r\n";
				txt += "\t\t{\r\n";
				txt += "\t\t\t//base.OnInspectorGUI();\r\n";
				txt += "\t\t\tSerializedProperty scriptProperty = serializedObject.FindProperty(\"m_Script\");\r\n";
				txt += "\t\t\tusing (new EditorGUI.DisabledScope(\"m_Script\" == scriptProperty.propertyPath))\r\n";
				txt += "\t\t\t{\r\n";
				txt += "\t\t\t\tEditorGUILayout.PropertyField(scriptProperty, true);\r\n";
				txt += "\t\t\t}\r\n";
				
				for (int i = 0; i < fieldInfoList.Count; i++)
				{
					txt += "\t\t\tEditorGUILayout.PropertyField(" + fieldInfoList[i].Name + ");\r\n";
				}
				txt += "\t\tserializedObject.ApplyModifiedProperties();\r\n";
				txt += "\t\t}\r\n";
				
				txt += "\r\n";

				txt += "\t\tprivate void GetProperties()\r\n";
				txt += "\t\t{\r\n";
				for (int i = 0; i < fieldInfoList.Count; i++)
				{
					txt += "\t\t\t" + fieldInfoList[i].Name + " = serializedObject.FindProperty(\"" + fieldInfoList[i].Name + "\");\r\n";
				}
				txt += "\t\t}\r\n";

				GUIUtility.systemCopyBuffer = txt;
				Debug.Log(txt);
			}
		}

		public static string RemoveParentheses(string str)
		{
			string res = "";
			bool addingChar = true;
			for (int i = 0; i < str.Length; i++)
			{
				if (str[i] == '[')
				{
					addingChar = false;
				}
				else if (str[i] == ']')
				{
					addingChar = true;
					continue;
				}

				if (addingChar)
				{
					res += str[i];
				}
			}
			return res;
		}
		
		private void GenerateShaderHashID()
		{
			string[] separatingStrings = { "\r\n", "\n"};
			string[] lines = m_InputText.Split(separatingStrings, System.StringSplitOptions.RemoveEmptyEntries);

			string prefix = "uniform";
			//

			string txt = "";
			for (int i = 0; i < lines.Length;i ++)
			{
				string line = lines[i];
				line = line.Trim();
				if (line.StartsWith(prefix))
				{
					line = line.Remove(0, prefix.Length);
				}
				Debug.Log(line);

				string[] splits = line.Split(new string[] { " ", ";" }, System.StringSplitOptions.RemoveEmptyEntries);
				if (splits.Length >= 2)
				{
					string name = splits[1];
					name = RemoveParentheses(name);
					string shaderIDLine = "\tprivate int " + name + " = Shader.PropertyToID(\"" + name + "\");\r\n";
					txt += shaderIDLine;
				}
			}

			Debug.Log(txt);
			GUIUtility.systemCopyBuffer = txt;
		}

		#region AnimatorController

		[MenuItem("Assets/-- Animator Generate CodeBlock")]
		public static void ProcessAnimatorController()
		{
			var selected = Selection.activeObject as AnimatorController;
			if (selected != null)
			{				
				GenerateAnimatorCodes(selected);
			}
		}
		
		// [MenuItem("Assets/-- Animator Generate CodeBlock")]
		// public static bool ValidateProcessAnimatorController()
		// {
		// 	// Only enable the menu item if an AnimatorController is selected
		// 	bool res = Selection.activeObject is AnimatorController;
		// 	return res;
		// }
		
		public static string[] StringSplitter(string stringToSplit)
		{
			if (!string.IsNullOrEmpty(stringToSplit))
			{
				List<string> words = new List<string>();

				string temp = string.Empty;

				foreach (char ch in stringToSplit)
				{
					if (ch >= 'a' && ch <= 'z')
						temp = temp + ch;
					else
					{
						words.Add(temp);
						temp = string.Empty + ch;
					}
				}
				words.Add(temp);
				return words.ToArray();
			}
			else
				return null;
		}
		
		public static string GetCamelSplitResult(string stringToSplit)
		{
			string res = "";
			string[] splitRes = StringSplitter(stringToSplit);
			for (int i = 0; i < splitRes.Length; i++)
			{
				res += splitRes[i].ToUpper();
				if (i == splitRes.Length - 1)
				{

				}
				else
				{
					res += "_";
				}
			}
			return res;
		}

		static void GenerateAnimatorCodes(AnimatorController ac)
		{
			GUIUtility.systemCopyBuffer = GenerateAnimatorCodeBlock(ac);
			EditorUtility.DisplayDialog("Animator Code", "Generated code copied to clipboard.", "OK");
		}

		public static string GenerateAnimatorCodeBlock(AnimatorController ac)
		{
			if (ac != null)
			{
				AnimatorControllerLayer[] layers = ac.layers;

				string result = "";
				string parametersText = "";
				string stateDefinitionsText = "";
				string playAnimationText = "";

				// 1. 处理参数 (Parameters)
				var parameters = ac.parameters;
				for (int i = 0; i < parameters.Length; i++)
				{
					string paramName = parameters[i].name;
					string cleanName = paramName.Replace(" ", "").ToUpper();

					// 直接在声明时初始化 ID
					parametersText +=
						$"\t\tprivate const string PARAM_{cleanName} = \"{paramName}\"; // {parameters[i].type}\r\n";
					parametersText +=
						$"\t\tprivate readonly int PARAM_{cleanName}_ID = Animator.StringToHash(PARAM_{cleanName});\r\n";
				}

				// 2. 处理状态 (States)
				for (int i = 0; i < layers.Length; i++)
				{
					stateDefinitionsText += $"\t\t// Layer {i} - {layers[i].name}\r\n";
					stateDefinitionsText += $"\t\tprivate readonly int LAYER_{i} = {i};\r\n";

					playAnimationText += $"\t\t// Play Animations Layer {i}\r\n";

					ChildAnimatorState[] states = layers[i].stateMachine.states;

					for (int n = 0; n < states.Length; n++)
					{
						string originalStateName = states[n].state.name;
						string simplifiedName = originalStateName.Replace(" ", "");
						simplifiedName = originalStateName.Replace("-", "");
						string camelCaseName = GetCamelSplitResult(simplifiedName);

						string stateStrVar = $"L_{i}_{camelCaseName}";
						string stateIDStr = $"L_{i}_{camelCaseName}_ID";

						// 字符串定义
						stateDefinitionsText +=
							$"\t\tprivate const string {stateStrVar} = \"{originalStateName}\";\r\n";
						// ID 声明即初始化
						stateDefinitionsText +=
							$"\t\tprivate readonly int {stateIDStr} = Animator.StringToHash({stateStrVar});\r\n";

						// 生成 Play 函数
						string methodName = (i == 0) ? $"Play{simplifiedName}" : $"Play{simplifiedName}_{i}";
						playAnimationText += $"\t\tpublic void {methodName}(float fixedTransitionDuration)\r\n";
						playAnimationText += "\t\t{\r\n";
						playAnimationText +=
							$"\t\t\tm_Animator.CrossFadeInFixedTime({stateIDStr}, fixedTransitionDuration, LAYER_{i});\r\n";
						playAnimationText += "\t\t}\r\n";
					}

					stateDefinitionsText += "\r\n";
				}

				// 3. 整合结果
				result += "\t\t#region -Generated Code DO NOT MODIFY-\r\n";
				result += "\t\t// Parameters\r\n" + parametersText + "\r\n";
				result += "\t\t// States & Layer IDs\r\n" + stateDefinitionsText + "\r\n";
				result += playAnimationText;
				result += "\t\t#endregion\r\n";

				return result;
			}
			return string.Empty;
		}

		#endregion

		#region Post Process Codes
		private class PostPropertyInfo
		{
			public enum PropertyType
			{
				None,
				Float,
				Vector2,
				Vector3,
				Vector4,
				Color,
				Texture
			}

			public PropertyType m_PropertyType = PropertyType.None;
			public string m_ShaderPropertyName = "";
		}
		
		private string[] floatNames = new[] { "float", "half", "fixed" };
		private string[] vector2Names = new[] { "float2", "half2", "fixed2" };
		private string[] vector3Names = new[] { "float3", "half3", "fixed3" };
		private string[] vector4Names = new[] { "float4", "half4", "fixed4" };

		private bool MatchName(string[] names, string name)
		{
			for (int i = 0; i < names.Length; i++)
			{
				if (names[i] == name)
				{
					return true;
				}
			}

			return false;
		}

		private PostPropertyInfo CheckSpecialTex(string line)
		{
			if (line.Contains("TEXTURE"))
			{
				string s = line;
				int start = s.IndexOf("(") + 1;
				int end = s.IndexOf(")", start);
				string result = s.Substring(start, end - start);
				if (result.Length > 0)
				{
					PostPropertyInfo info = new PostPropertyInfo();
					info.m_PropertyType = PostPropertyInfo.PropertyType.Texture;
					info.m_ShaderPropertyName = result;
					return info;
				}
			}

			return null;
		}
		
		private PostPropertyInfo CreatePropertyInfo(string line)
		{
			if (line.StartsWith("//"))
			{
				return null;
			}

			PostPropertyInfo specInfo = CheckSpecialTex(line);
			if (specInfo != null)
			{
				return specInfo;
			}
			
			string[] splits = line.Split(new string[] { " ", ";"}, System.StringSplitOptions.RemoveEmptyEntries);
			if (splits.Length >= 2)
			{
				PostPropertyInfo info = new PostPropertyInfo();
				info.m_PropertyType = PostPropertyInfo.PropertyType.None;

				string typeName = splits[0];
				string propertyName = splits[1];

				if (MatchName(floatNames, typeName))
				{
					info.m_PropertyType = PostPropertyInfo.PropertyType.Float;
				}
				else if (MatchName(vector2Names, typeName))
				{
					info.m_PropertyType = PostPropertyInfo.PropertyType.Vector2;
				} 
				else if (MatchName(vector3Names, typeName))
				{
					info.m_PropertyType = PostPropertyInfo.PropertyType.Vector3;
				} 
				else if (MatchName(vector4Names, typeName))
				{
					info.m_PropertyType = PostPropertyInfo.PropertyType.Vector4;
					if (propertyName.ToLower().Contains("color"))
					{
						info.m_PropertyType = PostPropertyInfo.PropertyType.Color;
					}
				} 
				else if (typeName == "sampler2D")
				{
					info.m_PropertyType = PostPropertyInfo.PropertyType.Texture;

				}

				if (info.m_PropertyType == PostPropertyInfo.PropertyType.None)
				{
					return null;
				}

				info.m_ShaderPropertyName = propertyName;
				
				return info;
			}

			return null;
		}

		private List<PostPropertyInfo> GeneratePostInfoList()
		{
			List<PostPropertyInfo> infoList = new List<PostPropertyInfo>();
			string[] separatingStrings = { "\r\n", "\n"};
			string[] lines = m_InputText.Split(separatingStrings, System.StringSplitOptions.RemoveEmptyEntries);
			string prefix = "uniform";
			for (int i = 0; i < lines.Length;i ++)
			{
				string line = lines[i];
				line = line.Trim();
				if (line.StartsWith(prefix))
				{
					line = line.Remove(0, prefix.Length);
				}

				PostPropertyInfo info = CreatePropertyInfo(line);
				Debug.Log(line);
				if (info != null)
				{
					infoList.Add(info);
				}
			}

			return infoList;
		}

		private void GeneratePostShaderPropertiesURP()
		{
			List<PostPropertyInfo> infoList = GeneratePostInfoList();

			string editorParamterText = "";
			string editorFindParamterText = "";
			string editorInspectorText = "";
			
			string runtimePropertyText = "";
			string runtimeShaderIDText = "";
			string runtimeRenderText = "";

			for (int i = 0; i < infoList.Count; i++)
			{
				var info = infoList[i];
				
					string runtimePropertyName = info.m_ShaderPropertyName;
				string runtimePropertyType = "";
				string runtimePropertyDefaultValue = "";
				string runtimeRenderOperation = "";
				
				string shaderIDName = info.m_ShaderPropertyName.ToUpper() + "_ID";

				if (info.m_PropertyType == PostPropertyInfo.PropertyType.Float)
				{
					runtimePropertyType = "FloatParameter";
					runtimePropertyDefaultValue = "1f";
					runtimeRenderOperation = "SetFloat";
				}
				else if (info.m_PropertyType == PostPropertyInfo.PropertyType.Color)
				{
					runtimePropertyType = "ColorParameter";
					runtimePropertyDefaultValue = "Color.white";
					runtimeRenderOperation = "SetColor";
				}
				else if (info.m_PropertyType == PostPropertyInfo.PropertyType.Vector2)
				{
					runtimePropertyType = "Vector2Parameter";
					runtimePropertyDefaultValue = "Vector2.zero";
					runtimeRenderOperation = "SetVector";
				}
				else if (info.m_PropertyType == PostPropertyInfo.PropertyType.Vector3)
				{
					runtimePropertyType = "Vector3Parameter";
					runtimePropertyDefaultValue = "Vector3.zero";
					runtimeRenderOperation = "SetVector";
				}
				else if (info.m_PropertyType == PostPropertyInfo.PropertyType.Vector4)
				{
					runtimePropertyType = "Vector4Parameter";
					runtimePropertyDefaultValue = "Vector4.zero";
					runtimeRenderOperation = "SetVector";
				}
				else if (info.m_PropertyType == PostPropertyInfo.PropertyType.Texture)
				{
					runtimePropertyType = "TextureParameter";
					runtimePropertyDefaultValue = "null";
					runtimeRenderOperation = "SetTexture";
				}
				string runtimePropertyLine = "\t\tpublic " + runtimePropertyType + " " + runtimePropertyName + " = new " + runtimePropertyType
				                             + "(" + runtimePropertyDefaultValue +", false);\r\n";
				runtimePropertyText += runtimePropertyLine;

				string shaderIDLine = "\t\tprivate int " + shaderIDName + " = Shader.PropertyToID(\"" + info.m_ShaderPropertyName + "\");\r\n";
				runtimeShaderIDText += shaderIDLine;

				string renderText = "\t\tm_Material." + runtimeRenderOperation + "(" + shaderIDName + ", m_Volume." + runtimePropertyName + ".value);\r\n";
				runtimeRenderText += renderText;
				
				editorParamterText += "\t\tSerializedDataParameter " + runtimePropertyName + ";\r\n";
				editorFindParamterText += "\t\t\t" + runtimePropertyName + " = Unpack(o.Find(x => x." + runtimePropertyName + "));\r\n";
				editorInspectorText += "\t\t\t" + "PropertyField(" + runtimePropertyName + ");\r\n";
			}
			
			Debug.Log(runtimePropertyText);
			Debug.Log(runtimeShaderIDText);
			Debug.Log(runtimeRenderText);
			
			string editorText = editorParamterText + "\r\n";
			//editorText += "\t\tpublic override void OnEnable()\r\n";
			//editorText += "\t\t{\r\n";
			editorText += "\t\t\t////for on enable\r\n";
			editorText += editorFindParamterText;
			//editorText += "\t\t}\r\n";
			editorText += "\r\n";
			editorText += "\t\tpublic override void OnInspectorGUI()\r\n";
			editorText += "\t\t{\r\n";
			editorText += editorInspectorText;
			editorText += "\t\t}\r\n";
				
			Debug.Log(editorText);

			string txt = runtimePropertyText + "\r\n\r\n" + runtimeShaderIDText + "\r\n\r\n" +runtimeRenderText + "\r\n\r\n" + editorText;
			GUIUtility.systemCopyBuffer = txt;
		}

		private void GeneratePostShaderPropertiesHDRP()
		{
			List<PostPropertyInfo> infoList = GeneratePostInfoList();

			string editorParamterText = "";
			string editorFindParamterText = "";
			string editorInspectorText = "";
			
			string runtimePropertyText = "";
			string runtimeShaderIDText = "";
			string runtimeRenderText = "";

			for (int i = 0; i < infoList.Count; i++)
			{
				var info = infoList[i];
				
				string runtimePropertyName = info.m_ShaderPropertyName;
				string runtimePropertyType = "";
				string runtimePropertyDefaultValue = "";
				string runtimeRenderOperation = "";
				
				string shaderIDName = info.m_ShaderPropertyName.ToUpper() + "_ID";

				if (info.m_PropertyType == PostPropertyInfo.PropertyType.Float)
				{
					runtimePropertyType = "FloatParameter";
					runtimePropertyDefaultValue = "1f";
					runtimeRenderOperation = "SetFloat";
				}
				else if (info.m_PropertyType == PostPropertyInfo.PropertyType.Color)
				{
					runtimePropertyType = "ColorParameter";
					runtimePropertyDefaultValue = "Color.white";
					runtimeRenderOperation = "SetColor";
				}
				else if (info.m_PropertyType == PostPropertyInfo.PropertyType.Vector2)
				{
					runtimePropertyType = "Vector2Parameter";
					runtimePropertyDefaultValue = "Vector2.zero";
					runtimeRenderOperation = "SetVector";
				}
				else if (info.m_PropertyType == PostPropertyInfo.PropertyType.Vector3)
				{
					runtimePropertyType = "Vector3Parameter";
					runtimePropertyDefaultValue = "Vector3.zero";
					runtimeRenderOperation = "SetVector";
				}
				else if (info.m_PropertyType == PostPropertyInfo.PropertyType.Vector4)
				{
					runtimePropertyType = "Vector4Parameter";
					runtimePropertyDefaultValue = "Vector4.zero";
					runtimeRenderOperation = "SetVector";
				}
				else if (info.m_PropertyType == PostPropertyInfo.PropertyType.Texture)
				{
					runtimePropertyType = "TextureParameter";
					runtimePropertyDefaultValue = "null";
					runtimeRenderOperation = "SetTexture";
				}
				string runtimePropertyLine = "\t\tpublic " + runtimePropertyType + " " + runtimePropertyName + " = new " + runtimePropertyType
				                             + "(" + runtimePropertyDefaultValue +", true);\r\n";
				runtimePropertyText += runtimePropertyLine;

				string shaderIDLine = "\t\tprivate int " + shaderIDName + " = Shader.PropertyToID(\"" + info.m_ShaderPropertyName + "\");\r\n";
				runtimeShaderIDText += shaderIDLine;

				string renderText = "\t\tm_Material." + runtimeRenderOperation + "(" + shaderIDName + ", " + runtimePropertyName + ".value);\r\n";
				runtimeRenderText += renderText;
				
				editorParamterText += "\t\tSerializedDataParameter " + runtimePropertyName + ";\r\n";
				editorFindParamterText += "\t\t\t" + runtimePropertyName + " = Unpack(o.Find(x => x." + runtimePropertyName + "));\r\n";
				editorInspectorText += "\t\t\t" + "PropertyField(" + runtimePropertyName + ");\r\n";

			}
		
			Debug.Log(runtimePropertyText);
			Debug.Log(runtimeShaderIDText);
			Debug.Log(runtimeRenderText);
			
			string editorText = editorParamterText + "\r\n";
			//editorText += "\t\tpublic override void OnEnable()\r\n";
			//editorText += "\t\t{\r\n";
			editorText += "\t\t\t////for on enable\r\n";
			editorText += editorFindParamterText;
			//editorText += "\t\t}\r\n";
			editorText += "\r\n";
			editorText += "\t\tpublic override void OnInspectorGUI()\r\n";
			editorText += "\t\t{\r\n";
			editorText += editorInspectorText;
			editorText += "\t\t}\r\n";
				
			Debug.Log(editorText);

			string txt = runtimePropertyText + "\r\n\r\n" + runtimeShaderIDText + "\r\n\r\n" +runtimeRenderText + "\r\n\r\n" + editorText;
			GUIUtility.systemCopyBuffer = txt;
		}
		
		private void GeneratePostShaderPropertiesBuiltin()
		{
			List<PostPropertyInfo> infoList = GeneratePostInfoList();

			string editorParamterText = "";
			string editorFindParamterText = "";
			string editorInspectorText = "";
			
			string runtimePropertyText = "";
			string runtimeShaderIDText = "";
			string runtimeRenderText = "";
			
			for (int i = 0; i < infoList.Count; i++)
			{
				var info = infoList[i];

				string runtimePropertyName = info.m_ShaderPropertyName;
				string runtimePropertyType = "";
				string runtimePropertyDefaultValue = "";
				string runtimeRenderOperation = "";
				if (info.m_PropertyType == PostPropertyInfo.PropertyType.Float)
				{
					runtimePropertyType = "FloatParameter";
					runtimePropertyDefaultValue = "1f";
					runtimeRenderOperation = "SetFloat";
				}
				else if (info.m_PropertyType == PostPropertyInfo.PropertyType.Color)
				{
					runtimePropertyType = "ColorParameter";
					runtimePropertyDefaultValue = "Color.white";
					runtimeRenderOperation = "SetColor";
				}
				else if (info.m_PropertyType == PostPropertyInfo.PropertyType.Vector2)
				{
					runtimePropertyType = "Vector2Parameter";
					runtimePropertyDefaultValue = "Vector2.zero";
					runtimeRenderOperation = "SetVector";
				}
				else if (info.m_PropertyType == PostPropertyInfo.PropertyType.Vector3)
				{
					runtimePropertyType = "Vector3Parameter";
					runtimePropertyDefaultValue = "Vector3.zero";
					runtimeRenderOperation = "SetVector";
				}
				else if (info.m_PropertyType == PostPropertyInfo.PropertyType.Vector4)
				{
					runtimePropertyType = "Vector4Parameter";
					runtimePropertyDefaultValue = "Vector4.zero";
					runtimeRenderOperation = "SetVector";
				}
				else if (info.m_PropertyType == PostPropertyInfo.PropertyType.Texture)
				{
					runtimePropertyType = "TextureParameter";
					runtimePropertyDefaultValue = "null";
					runtimeRenderOperation = "SetTexture";
				}
				string runtimePropertyLine = "\t\tpublic " + runtimePropertyType + " " + runtimePropertyName + " = new " + runtimePropertyType
				                             + "{ value = " + runtimePropertyDefaultValue +"};\r\n";
				runtimePropertyText += runtimePropertyLine;
				
				string shaderIDName = info.m_ShaderPropertyName.ToUpper() + "_ID";
				string shaderIDLine = "\t\tprivate int " + shaderIDName + " = Shader.PropertyToID(\"" + info.m_ShaderPropertyName + "\");\r\n";
				runtimeShaderIDText += shaderIDLine;
				
				string shaderRenderLine = "\t\t\tsheet.properties." + runtimeRenderOperation  + "(" + shaderIDName + ", settings." + runtimePropertyName + ".value);\r\n";
				runtimeRenderText += shaderRenderLine;

				editorParamterText += "\t\tSerializedParameterOverride " + info.m_ShaderPropertyName + ";\r\n";
				editorFindParamterText += "\t\t\t" + info.m_ShaderPropertyName + " = FindParameterOverride(x => x." + info.m_ShaderPropertyName + ");\r\n";
				editorInspectorText += "\t\t\tPropertyField(" + info.m_ShaderPropertyName + ");\r\n";
			}
			
			Debug.Log(runtimePropertyText);
			Debug.Log(runtimeShaderIDText);			
			Debug.Log(runtimeRenderText);

			string editorText = editorParamterText + "\r\n";
			editorText += "\t\tpublic override void OnEnable()\r\n";
			editorText += "\t\t{\r\n";
			editorText += editorFindParamterText;
			editorText += "\t\t}\r\n";
			editorText += "\r\n";
			editorText += "\t\tpublic override void OnInspectorGUI()\r\n";
			editorText += "\t\t{\r\n";
			editorText += editorInspectorText;
			editorText += "\t\t}\r\n";
			
			Debug.Log(editorText);

			string txt = runtimePropertyText + "\r\n\r\n" + runtimeShaderIDText + "\r\n\r\n" +runtimeRenderText + "\r\n\r\n" + editorText;
			GUIUtility.systemCopyBuffer = txt;
		}
		#endregion
	}
}

