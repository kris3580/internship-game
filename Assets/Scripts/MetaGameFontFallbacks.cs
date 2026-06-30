using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class MetaGameFontFallbacks
{
    private static readonly string[] CandidateFamilies =
    {
        "Noto Sans CJK SC",
        "Noto Sans CJK TC",
        "Noto Sans CJK JP",
        "Noto Sans CJK KR",
        "Noto Sans Devanagari",
        "Noto Sans Arabic",
        "Noto Sans Thai",
        "Noto Sans Bengali",
        "Microsoft YaHei UI",
        "Microsoft YaHei",
        "Microsoft JhengHei UI",
        "Microsoft JhengHei",
        "SimSun",
        "Malgun Gothic",
        "Yu Gothic",
        "Meiryo",
        "Nirmala UI",
        "Segoe UI Symbol",
        "Arial Unicode MS",
        "PingFang SC",
        "Hiragino Sans",
        "Apple SD Gothic Neo",
        "Geeza Pro"
    };

    private static bool installed;

    public static void EnsureInstalled()
    {
        if (installed)
            return;

        installed = true;

        List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets;

        if (fallbacks == null)
        {
            fallbacks = new List<TMP_FontAsset>();
            TMP_Settings.fallbackFontAssets = fallbacks;
        }

        HashSet<string> installedFonts = GetInstalledFontNames();

        foreach (string family in CandidateFamilies)
        {
            if (installedFonts.Count > 0 && !installedFonts.Contains(family))
                continue;

            TMP_FontAsset fallback = TMP_FontAsset.CreateFontAsset(family, "Regular");

            if (fallback == null || fallbacks.Contains(fallback))
                continue;

            fallback.name = "Runtime Fallback - " + family;
            fallback.hideFlags = HideFlags.HideAndDontSave;
            fallbacks.Add(fallback);
        }
    }

    public static bool CanRender(TMP_FontAsset font, string value)
    {
        EnsureInstalled();

        if (font == null || string.IsNullOrEmpty(value))
            return true;

        foreach (char character in value)
        {
            if (char.IsControl(character) || char.IsWhiteSpace(character))
                continue;

            if (!CanRenderCharacter(font, character, new HashSet<int>()))
                return false;
        }

        return true;
    }

    private static bool CanRenderCharacter(TMP_FontAsset font, char character, HashSet<int> visited)
    {
        if (font == null)
            return false;

        int instanceId = font.GetInstanceID();

        if (!visited.Add(instanceId))
            return false;

        if (CanRenderOwnCharacter(font, character))
            return true;

        List<TMP_FontAsset> localFallbacks = font.fallbackFontAssetTable;

        if (localFallbacks != null)
        {
            foreach (TMP_FontAsset fallback in localFallbacks)
            {
                if (CanRenderCharacter(fallback, character, visited))
                    return true;
            }
        }

        List<TMP_FontAsset> globalFallbacks = TMP_Settings.fallbackFontAssets;

        if (globalFallbacks != null)
        {
            foreach (TMP_FontAsset fallback in globalFallbacks)
            {
                if (CanRenderCharacter(fallback, character, visited))
                    return true;
            }
        }

        return false;
    }

    private static bool CanRenderOwnCharacter(TMP_FontAsset font, char character)
    {
        try
        {
            if (font.HasCharacter(character, false, false))
                return true;
        }
        catch (MissingReferenceException)
        {
            return false;
        }
        catch (NullReferenceException)
        {
            return false;
        }

        if (!IsDynamicFontUsable(font))
            return false;

        try
        {
            return font.sourceFontFile != null && font.sourceFontFile.HasCharacter(character);
        }
        catch (MissingReferenceException)
        {
            return false;
        }
        catch (NullReferenceException)
        {
            return false;
        }
    }

    private static bool IsDynamicFontUsable(TMP_FontAsset font)
    {
        try
        {
            if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic
                && font.atlasPopulationMode != AtlasPopulationMode.DynamicOS)
            {
                return false;
            }

            Texture2D[] atlasTextures = font.atlasTextures;
            return atlasTextures != null && atlasTextures.Length > 0 && atlasTextures[0] != null;
        }
        catch (MissingReferenceException)
        {
            return false;
        }
        catch (NullReferenceException)
        {
            return false;
        }
    }

    private static HashSet<string> GetInstalledFontNames()
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (string fontName in Font.GetOSInstalledFontNames())
                names.Add(fontName);
        }
        catch
        {
            names.Clear();
        }

        return names;
    }
}
