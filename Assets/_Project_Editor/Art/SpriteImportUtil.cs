using UnityEditor;

namespace Anomaly.Editor.Art
{
    // _Project 안의 UI 이미지를 Sprite로 임포트되게 맞춤(외부 에셋 폴더에는 쓰지 말 것)
    public static class SpriteImportUtil
    {
        public static void EnsureSprite(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer) return;
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
