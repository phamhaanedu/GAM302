#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace ProjectBase.Editor
{
    public static class MechMaterialFixer
    {
        private const string MaterialPath = "Assets/Import/RSG_UnderWater_Pack/ISO/HDRP/Materials/Mech/M_ISO_Mech.mat";
        private const string BaseTexPath = "Assets/Import/RSG_UnderWater_Pack/ISO/HDRP/Textures/Mech/T_Mech_LOD2_B.png";
        private const string NormalTexPath = "Assets/Import/RSG_UnderWater_Pack/ISO/HDRP/Textures/Mech/T_Mech_LOD2_N.png";
        private const string MaskTexPath = "Assets/Import/RSG_UnderWater_Pack/ISO/HDRP/Textures/Mech/T_Mech_LOD2_M.png";
        private const string EmissionTexPath = "Assets/Import/RSG_UnderWater_Pack/ISO/HDRP/Textures/Mech/T_Mech_LOD2_E.png";

        [MenuItem("Tools/Convert Mech to URP")]
        public static void ConvertMechMaterialToURP()
        {
            Debug.Log("[MechMaterialFixer] Starting URP conversion for ISO Mech...");

            // 1. Fix Texture Importers
            FixTextureImporter(MaskTexPath, isNormal: false, isLinear: true);
            FixTextureImporter(NormalTexPath, isNormal: true, isLinear: true);

            // 2. Load textures
            Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseTexPath);
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTexPath);
            Texture2D maskTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MaskTexPath);
            Texture2D emissionTex = AssetDatabase.LoadAssetAtPath<Texture2D>(EmissionTexPath);

            // 3. Find URP Lit Shader
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogError("[MechMaterialFixer] Universal Render Pipeline/Lit shader not found!");
                return;
            }

            // 4. Load or create Material
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                Debug.LogError($"[MechMaterialFixer] Material not found at {MaterialPath}!");
                return;
            }

            Undo.RecordObject(mat, "Convert Mech Material to URP");

            mat.shader = urpLit;

            // Set Base Map (Albedo)
            if (baseTex != null)
            {
                mat.SetTexture("_BaseMap", baseTex);
                mat.SetTexture("_MainTex", baseTex); // fallback
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);

            // Set Normal Map
            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.SetFloat("_BumpScale", 1.0f);
                mat.EnableKeyword("_NORMALMAP");
            }

            // Set Metallic & Smoothness (Mask Map: R = Metallic, A = Smoothness)
            if (maskTex != null)
            {
                mat.SetTexture("_MetallicGlossMap", maskTex);
                mat.SetFloat("_Metallic", 1.0f);
                mat.SetFloat("_Smoothness", 1.0f);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");

                // Set Occlusion (G = Occlusion)
                mat.SetTexture("_OcclusionMap", maskTex);
                mat.SetFloat("_OcclusionStrength", 1.0f);
            }

            // Set Emission
            if (emissionTex != null)
            {
                mat.SetTexture("_EmissionMap", emissionTex);
                Color emissionColor = new Color(1.5f, 1.5f, 1.5f, 1.0f);
                mat.SetColor("_EmissionColor", emissionColor);
                mat.EnableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            // 5. Update scene instances
            var allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int updatedCount = 0;
            foreach (var r in allRenderers)
            {
                if (r.gameObject.name.Contains("Mech") || (r.transform.root != null && r.transform.root.name.Contains("Mech")))
                {
                    Undo.RecordObject(r, "Assign Mech Material");
                    r.sharedMaterial = mat;
                    EditorUtility.SetDirty(r);
                    updatedCount++;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[MechMaterialFixer] Successfully converted {MaterialPath} to URP Lit and updated {updatedCount} scene renderers!");
        }

        private static void FixTextureImporter(string path, bool isNormal, bool isLinear)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool changed = false;
            if (isNormal && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                changed = true;
            }

            if (isLinear && importer.sRGBTexture)
            {
                importer.sRGBTexture = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log($"[MechMaterialFixer] Updated TextureImporter settings for: {path}");
            }
        }
    }
}
#endif
