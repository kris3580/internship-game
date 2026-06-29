#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BallPopColorPaletteAssetCreator
{
    private const string AssetPath = "Assets/Settings/BallPopColorPalette.asset";

    static BallPopColorPaletteAssetCreator()
    {
        EditorApplication.delayCall += EnsureAsset;
    }

    private static void EnsureAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<BallPopColorPalette>(AssetPath) != null)
            return;

        Directory.CreateDirectory("Assets/Settings");
        BallPopColorPalette palette = ScriptableObject.CreateInstance<BallPopColorPalette>();
        AssetDatabase.CreateAsset(palette, AssetPath);
        AssetDatabase.SaveAssets();
    }
}
#endif
