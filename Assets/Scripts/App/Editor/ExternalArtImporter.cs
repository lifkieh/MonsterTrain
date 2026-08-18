#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MTA.App.EditorTools
{
    // Ensures downloaded monster PNGs under Resources/MonSprites import as crisp
    // point-filtered Sprites so Resources.Load<Sprite> works at runtime.
    // Invoke: -executeMethod MTA.App.EditorTools.ExternalArtImporter.ImportAll
    public static class ExternalArtImporter
    {
        [MenuItem("MTA/Import External Art")]
        public static void ImportAll()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/MonSprites" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = FilterMode.Point;
                ti.mipmapEnabled = false;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.spritePixelsPerUnit = 64;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
                n++;
            }
            AssetDatabase.Refresh();
            Debug.Log("MTA: imported " + n + " monster sprites as Sprite.");
        }
    }

    // Auto-applies import settings to downloaded art.
    public class ArtPostprocessor : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            var p = assetPath.Replace('\\', '/');
            if (p.Contains("/Resources/MonSprites/"))
            {
                var ti = (TextureImporter)assetImporter;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = FilterMode.Point;
                ti.mipmapEnabled = false;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.spritePixelsPerUnit = 64;
                ti.alphaIsTransparency = true;
            }
            else if (p.Contains("/Resources/Vfx/") || p.Contains("/Resources/Arena/"))
            {
                var ti = (TextureImporter)assetImporter;   // RawImage textures: no mipmap, clamp
                ti.textureType = TextureImporterType.Default;
                ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.filterMode = FilterMode.Bilinear;
                ti.alphaIsTransparency = true;
            }
        }
    }
}
#endif
