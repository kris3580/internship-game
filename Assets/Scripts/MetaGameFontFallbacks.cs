using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class MetaGameFontFallbacks
{
    private static readonly string[] CandidateFamilies =
    {
        "Nirmala UI",
        "Arial Unicode MS",
        "Segoe UI Symbol",
        "Noto Sans CJK SC",
        "Noto Sans CJK TC",
        "Noto Sans CJK JP",
        "Noto Sans CJK KR",
        "Noto Sans Devanagari",
        "Noto Sans Arabic",
        "Noto Sans Thai",
        "Noto Sans Bengali",
        "Noto Serif Bengali",
        "Microsoft YaHei UI",
        "Microsoft YaHei",
        "Microsoft JhengHei UI",
        "Microsoft JhengHei",
        "SimSun",
        "Malgun Gothic",
        "Yu Gothic",
        "Meiryo",
        "Mangal",
        "Kokila",
        "Aparajita",
        "Utsaah",
        "Vrinda",
        "Leelawadee UI",
        "Tahoma",
        "PingFang SC",
        "Hiragino Sans",
        "Apple SD Gothic Neo",
        "Geeza Pro",
        "Bangla Sangam MN",
        "Kohinoor Bangla"
    };

    private static readonly Dictionary<string, string[]> LanguageFamilies = new()
    {
        ["zh"] = new[] { "Microsoft YaHei UI", "Microsoft YaHei", "Noto Sans CJK SC", "SimSun" },
        ["hi"] = new[] { "Nirmala UI", "Mangal", "Noto Sans Devanagari" },
        ["ar"] = new[] { "Nirmala UI", "Segoe UI", "Noto Sans Arabic", "Tahoma" },
        ["ru"] = new[] { "Segoe UI", "Arial", "Noto Sans" },
        ["ja"] = new[] { "Yu Gothic", "Meiryo", "Noto Sans CJK JP" },
        ["th"] = new[] { "Leelawadee UI", "Nirmala UI", "Tahoma", "Noto Sans Thai" },
        ["uk"] = new[] { "Segoe UI", "Arial", "Noto Sans" },
        ["bn"] = new[] { "Nirmala UI", "Vrinda", "Noto Sans Bengali", "Noto Serif Bengali" },
        ["ko"] = new[] { "Malgun Gothic", "Noto Sans CJK KR" }
    };

    private static readonly Dictionary<string, string[]> LanguageFontFiles = new()
    {
        ["zh"] = new[] { "msyh.ttc", "simsun.ttc" },
        ["hi"] = new[] { "Nirmala.ttf", "mangal.ttf" },
        ["ar"] = new[] { "DUBAI-REGULAR.TTF", "tahoma.ttf", "segoeui.ttf", "Nirmala.ttf" },
        ["ru"] = new[] { "segoeui.ttf", "arial.ttf" },
        ["ja"] = new[] { "YuGothR.ttc", "meiryo.ttc", "msgothic.ttc" },
        ["th"] = new[] { "leelawui.ttf", "Nirmala.ttf", "tahoma.ttf" },
        ["uk"] = new[] { "segoeui.ttf", "arial.ttf" },
        ["bn"] = new[] { "Nirmala.ttf", "vrinda.ttf" },
        ["ko"] = new[] { "malgun.ttf" }
    };

    private static readonly string[] CandidateStyles =
    {
        "Regular",
        "Normal",
        "Book"
    };

    private static readonly List<TMP_FontAsset> RuntimeFallbacks = new();
    private static readonly List<Font> RuntimeUnityFonts = new();
    private static readonly Dictionary<string, TMP_FontAsset> LanguageFonts = new();
    private static bool installed;

    public static void EnsureInstalled()
    {
        List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets;

        if (fallbacks == null)
        {
            fallbacks = new List<TMP_FontAsset>();
            TMP_Settings.fallbackFontAssets = fallbacks;
        }

        RemoveInvalidFallbacks(fallbacks);

        installed = true;
    }

    private static TMP_FontAsset CreateFallbackFontAsset(string family)
    {
        foreach (string style in CandidateStyles)
        {
            try
            {
                TMP_FontAsset fallback = TMP_FontAsset.CreateFontAsset(family, style);

                if (fallback != null)
                    return fallback;
            }
            catch (Exception)
            {
                // Some OS fonts expose different style names or cannot be loaded by TMP.
            }
        }

        try
        {
            Font osFont = Font.CreateDynamicFontFromOSFont(family, 90);

            if (osFont == null)
                return null;

            osFont.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
            RuntimeUnityFonts.Add(osFont);

            return TMP_FontAsset.CreateFontAsset(
                osFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void RemoveInvalidFallbacks(List<TMP_FontAsset> fallbacks)
    {
        RuntimeFallbacks.RemoveAll(fallback => !IsRuntimeFallbackUsable(fallback));
        bool removedRuntimeFallback = false;

        for (int i = fallbacks.Count - 1; i >= 0; i--)
        {
            TMP_FontAsset fallback = fallbacks[i];

            if (fallback == null)
            {
                fallbacks.RemoveAt(i);
                continue;
            }

            if (IsRuntimeFallback(fallback))
            {
                removedRuntimeFallback = true;
                fallbacks.RemoveAt(i);
            }
        }

        if (installed && removedRuntimeFallback)
            installed = false;
    }

    private static bool IsRuntimeFallback(TMP_FontAsset font)
    {
        try
        {
            return font != null && font.name.StartsWith("Runtime Fallback - ", StringComparison.Ordinal);
        }
        catch (MissingReferenceException)
        {
            return true;
        }
    }

    private static bool IsFontAssetAlive(TMP_FontAsset font)
    {
        if (font == null)
            return false;

        try
        {
            _ = font.GetInstanceID();
            return true;
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }

    private static bool IsRuntimeFallbackUsable(TMP_FontAsset font)
    {
        if (font == null)
            return false;

        try
        {
            return font.material != null
                && font.sourceFontFile != null
                && font.atlasTextures != null
                && font.atlasTextures.Length > 0
                && font.atlasTextures[0] != null;
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

    public static TMP_FontAsset GetFontForText(TMP_FontAsset preferredFont, string value)
    {
        EnsureInstalled();

        if (string.IsNullOrEmpty(value))
            return preferredFont;

        if (CanRenderDirectly(preferredFont, value))
            return preferredFont;

        foreach (TMP_FontAsset fallback in RuntimeFallbacks)
        {
            if (CanRenderDirectly(fallback, value))
                return fallback;
        }

        return null;
    }

    public static TMP_FontAsset GetFontForLanguage(string language, TMP_FontAsset preferredFont, string value)
    {
        EnsureInstalled();

        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(language) || language == "en")
            return preferredFont;

        if (!LanguageFamilies.TryGetValue(language, out string[] families))
            return CanRenderDirectly(preferredFont, value) ? preferredFont : null;

        if (LanguageFonts.TryGetValue(language, out TMP_FontAsset cachedFont) && IsRuntimeFallbackUsable(cachedFont))
            return cachedFont;

        if (LanguageFontFiles.TryGetValue(language, out string[] fontFiles))
        {
            foreach (string fileName in fontFiles)
            {
                TMP_FontAsset font = CreateFallbackFontAssetFromFile(fileName);

                if (!IsRuntimeFallbackUsable(font))
                    continue;

                font.name = "Language Font - " + language + " - " + Path.GetFileNameWithoutExtension(fileName);
                font.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
                RuntimeFallbacks.Add(font);
                LanguageFonts[language] = font;
                return font;
            }
        }

        foreach (string family in families)
        {
            TMP_FontAsset font = CreateFallbackFontAsset(family);

            if (!IsRuntimeFallbackUsable(font))
                continue;

            font.name = "Language Font - " + language + " - " + family;
            font.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
            RuntimeFallbacks.Add(font);
            LanguageFonts[language] = font;
            return font;
        }

        return null;
    }

    private static TMP_FontAsset CreateFallbackFontAssetFromFile(string fileName)
    {
        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fileName);

        if (!File.Exists(fontPath))
            return null;

        try
        {
            Font font = new(fontPath)
            {
                hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset
            };
            RuntimeUnityFonts.Add(font);

            return TMP_FontAsset.CreateFontAsset(
                font,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool CanRenderDirectly(TMP_FontAsset font, string value)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        if (!IsFontAssetAlive(font))
            return false;

        foreach (char character in value)
        {
            if (char.IsControl(character) || char.IsWhiteSpace(character))
                continue;

            if (!CanRenderOwnCharacter(font, character))
                return false;
        }

        return true;
    }

    private static bool CanRenderCharacter(TMP_FontAsset font, char character, HashSet<int> visited)
    {
        if (!IsFontAssetAlive(font))
            return false;

        int instanceId;

        try
        {
            instanceId = font.GetInstanceID();
        }
        catch (MissingReferenceException)
        {
            return false;
        }

        if (!visited.Add(instanceId))
            return false;

        if (CanRenderOwnCharacter(font, character))
            return true;

        List<TMP_FontAsset> localFallbacks = font.fallbackFontAssetTable;

        if (localFallbacks != null)
        {
            foreach (TMP_FontAsset fallback in localFallbacks)
            {
                if (IsFontAssetAlive(fallback) && CanRenderCharacter(fallback, character, visited))
                    return true;
            }
        }

        List<TMP_FontAsset> globalFallbacks = TMP_Settings.fallbackFontAssets;

        if (globalFallbacks != null)
        {
            foreach (TMP_FontAsset fallback in globalFallbacks)
            {
                if (IsFontAssetAlive(fallback) && CanRenderCharacter(fallback, character, visited))
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
