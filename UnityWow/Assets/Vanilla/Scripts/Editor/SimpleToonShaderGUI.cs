// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CR
{
    // Material setup only; the shader has no runtime component or renderer feature.
    public sealed class SimpleToonShaderGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
        {
            EditorGUI.BeginChangeCheck();
            editor.PropertiesDefaultGUI(properties);
            if (EditorGUI.EndChangeCheck())
                foreach (Object target in editor.targets) Setup((Material)target);
        }

        public override void ValidateMaterial(Material material) => Setup(material);

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            Setup(material);
        }

        public static void Setup(Material material)
        {
            bool transparent = material.GetFloat("_Surface") > 0.5f;
            bool cutout = material.GetFloat("_AlphaClip") > 0.5f;
            int queue = transparent ? (int)RenderQueue.Transparent : cutout ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;
            material.renderQueue = queue + Mathf.RoundToInt(material.GetFloat("_QueueOffset"));
            material.SetOverrideTag("RenderType", transparent ? "Transparent" : cutout ? "TransparentCutout" : "Opaque");
            material.SetFloat("_SrcBlend", (float)(transparent ? BlendMode.SrcAlpha : BlendMode.One));
            material.SetFloat("_DstBlend", (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);
            material.doubleSidedGI = material.GetFloat("_Cull") != (float)CullMode.Back;
            SetKeyword(material, "_SURFACE_TYPE_TRANSPARENT", transparent);
            SetKeyword(material, "_ALPHATEST_ON", cutout);
            SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap") != null);
            SetKeyword(material, "_EMISSION", material.GetColor("_EmissionColor").maxColorComponent > 0f);
            material.SetShaderPassEnabled("ShadowCaster", !transparent);
            material.SetShaderPassEnabled("DepthOnly", !transparent);
            material.SetShaderPassEnabled("DepthNormalsOnly", !transparent);
            material.SetShaderPassEnabled("SRPDefaultUnlit", !transparent && material.GetFloat("_OutlineWidth") > 0f);
        }

        private static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled) material.EnableKeyword(keyword);
            else material.DisableKeyword(keyword);
        }
    }
}

