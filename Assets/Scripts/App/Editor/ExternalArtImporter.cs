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
                ForceRGBA32(ti);
                ti.SaveAndReimport();
                n++;
            }
            AssetDatabase.Refresh();
            Debug.Log("MTA: imported " + n + " monster sprites as Sprite.");
        }

        // Pin monster sprites to uncompressed RGBA32 on Android so 64px pixel art stays
        // crisp with no block-compression artifacts on device (they're tiny — ~16 KB each).
        internal static void ForceRGBA32(TextureImporter ti)
        {
            var ps = ti.GetPlatformTextureSettings("Android");
            ps.overridden = true;
            ps.format = TextureImporterFormat.RGBA32;
            ps.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SetPlatformTextureSettings(ps);
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
                ExternalArtImporter.ForceRGBA32(ti);
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
            else if (p.Contains("/Resources/Ui/"))
            {
                var ti = (TextureImporter)assetImporter;   // 9-slice UI sprites
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = FilterMode.Bilinear;
                ti.mipmapEnabled = false;
                ti.spriteBorder = new Vector4(20, 20, 20, 20);
                ti.alphaIsTransparency = true;
            }
        }
    }
}
#endif
