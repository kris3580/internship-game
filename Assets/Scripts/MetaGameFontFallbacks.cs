using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class MetaGameFontFallbacks
{
    private const int FontSamplingPointSize = 90;
    private const int FontAtlasPadding = 9;
    private const int FontAtlasSize = 2048;
    private const int MaxFontCollectionFaces = 12;

    private static readonly string[] CandidateFamilies =
    {
        "sans-serif",
        "Droid Sans Fallback",
        "Droid Sans",
        "Roboto",
        "Noto Sans",
        "Noto Sans SC",
        "Noto Sans TC",
        "Noto Sans JP",
        "Noto Sans KR",
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
        ["zh"] = new[] { "Noto Sans CJK SC", "Noto Sans SC", "Droid Sans Fallback", "sans-serif", "Microsoft YaHei UI", "Microsoft YaHei", "SimSun" },
        ["hi"] = new[] { "Noto Sans Devanagari", "Nirmala UI", "Mangal", "sans-serif" },
        ["ar"] = new[] { "Noto Naskh Arabic", "Noto Sans Arabic", "Nirmala UI", "Segoe UI", "Tahoma", "sans-serif" },
        ["ru"] = new[] { "Roboto", "Noto Sans", "Segoe UI", "Arial", "sans-serif" },
        ["ja"] = new[] { "Noto Sans CJK JP", "Noto Sans JP", "Droid Sans Fallback", "Yu Gothic", "Meiryo", "sans-serif" },
        ["th"] = new[] { "Noto Sans Thai", "Leelawadee UI", "Nirmala UI", "Tahoma", "sans-serif" },
        ["uk"] = new[] { "Roboto", "Noto Sans", "Segoe UI", "Arial", "sans-serif" },
        ["bn"] = new[] { "Noto Sans Bengali", "Noto Serif Bengali", "Nirmala UI", "Vrinda", "sans-serif" },
        ["ko"] = new[] { "Noto Sans CJK KR", "Noto Sans KR", "Droid Sans Fallback", "Malgun Gothic", "sans-serif" },
        ["vi"] = new[] { "Roboto", "Noto Sans", "Segoe UI", "Arial", "sans-serif" },
        ["pl"] = new[] { "Roboto", "Noto Sans", "Segoe UI", "Arial", "sans-serif" },
        ["tr"] = new[] { "Roboto", "Noto Sans", "Segoe UI", "Arial", "sans-serif" },
        ["ro"] = new[] { "Roboto", "Noto Sans", "Segoe UI", "Arial", "sans-serif" },
        ["hu"] = new[] { "Roboto", "Noto Sans", "Segoe UI", "Arial", "sans-serif" }
    };

    private static readonly Dictionary<string, string[]> LanguageFontFiles = new()
    {
        ["zh"] = new[] { "NotoSansCJK-Regular.ttc", "NotoSansSC-Regular.otf", "NotoSansHans-Regular.otf", "DroidSansFallback.ttf", "msyh.ttc", "simsun.ttc" },
        ["hi"] = new[] { "NotoSansDevanagari-Regular.ttf", "NotoSansDevanagariUI-Regular.ttf", "NotoSerifDevanagari-Regular.ttf", "DroidSansDevanagari-Regular.ttf", "Nirmala.ttf", "mangal.ttf" },
        ["ar"] = new[] { "NotoNaskhArabic-Regular.ttf", "NotoSansArabic-Regular.ttf", "DroidNaskh-Regular-SystemUI.ttf", "DroidSansArabic.ttf", "DUBAI-REGULAR.TTF", "tahoma.ttf", "segoeui.ttf", "Nirmala.ttf" },
        ["ru"] = new[] { "Roboto-Regular.ttf", "NotoSans-Regular.ttf", "DroidSans.ttf", "segoeui.ttf", "arial.ttf" },
        ["ja"] = new[] { "NotoSansCJK-Regular.ttc", "NotoSansJP-Regular.otf", "NotoSansJpan-Regular.otf", "DroidSansJapanese.ttf", "DroidSansFallback.ttf", "YuGothR.ttc", "meiryo.ttc", "msgothic.ttc" },
        ["vi"] = new[] { "Roboto-Regular.ttf", "NotoSans-Regular.ttf", "DroidSans.ttf", "segoeui.ttf", "arial.ttf" },
        ["pl"] = new[] { "Roboto-Regular.ttf", "NotoSans-Regular.ttf", "DroidSans.ttf", "segoeui.ttf", "arial.ttf" },
        ["th"] = new[] { "NotoSansThai-Regular.ttf", "NotoSansThaiUI-Regular.ttf", "DroidSansThai.ttf", "leelawui.ttf", "Nirmala.ttf", "tahoma.ttf" },
        ["uk"] = new[] { "Roboto-Regular.ttf", "NotoSans-Regular.ttf", "DroidSans.ttf", "segoeui.ttf", "arial.ttf" },
        ["tr"] = new[] { "Roboto-Regular.ttf", "NotoSans-Regular.ttf", "DroidSans.ttf", "segoeui.ttf", "arial.ttf" },
        ["bn"] = new[] { "NotoSansBengali-Regular.ttf", "NotoSansBengaliUI-Regular.ttf", "NotoSerifBengali-Regular.ttf", "DroidSansBengali.ttf", "Nirmala.ttf", "vrinda.ttf" },
        ["ko"] = new[] { "NotoSansCJK-Regular.ttc", "NotoSansKR-Regular.otf", "NotoSansKore-Regular.otf", "DroidSansFallback.ttf", "malgun.ttf" },
        ["ro"] = new[] { "Roboto-Regular.ttf", "NotoSans-Regular.ttf", "DroidSans.ttf", "segoeui.ttf", "arial.ttf" },
        ["hu"] = new[] { "Roboto-Regular.ttf", "NotoSans-Regular.ttf", "DroidSans.ttf", "segoeui.ttf", "arial.ttf" }
    };

    private static readonly string[] FontSearchFolders =
    {
        Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
        "/system/fonts",
        "/system/font",
        "/product/fonts",
        "/system_ext/fonts",
        "/vendor/fonts"
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
                TMP_FontAsset fallback = TMP_FontAsset.CreateFontAsset(family, style, FontSamplingPointSize);

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
            Font osFont = Font.CreateDynamicFontFromOSFont(family, FontSamplingPointSize);

            if (osFont == null)
                return null;

            osFont.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
            RuntimeUnityFonts.Add(osFont);

            return TMP_FontAsset.CreateFontAsset(
                osFont,
                FontSamplingPointSize,
                FontAtlasPadding,
                GlyphRenderMode.SDFAA,
                FontAtlasSize,
                FontAtlasSize,
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

        if (CanRenderDirectly(preferredFont, value))
            return preferredFont;

        if (!LanguageFamilies.TryGetValue(language, out string[] families))
            return null;

        if (LanguageFonts.TryGetValue(language, out TMP_FontAsset cachedFont)
            && IsRuntimeFallbackUsable(cachedFont)
            && CanRenderDirectly(cachedFont, value))
        {
            return cachedFont;
        }

        if (LanguageFontFiles.TryGetValue(language, out string[] fontFiles))
        {
            foreach (string fileName in fontFiles)
            {
                TMP_FontAsset font = CreateFallbackFontAssetFromFile(fileName, value);

                if (!TryPrepareFontForText(font, value))
                    continue;

                font.name = "Language Font - " + language + " - " + Path.GetFileNameWithoutExtension(fileName);
                return RegisterLanguageFont(language, font);
            }
        }

        foreach (string family in families)
        {
            TMP_FontAsset font = CreateFallbackFontAsset(family);

            if (!TryPrepareFontForText(font, value))
                continue;

            font.name = "Language Font - " + language + " - " + family;
            return RegisterLanguageFont(language, font);
        }

        return null;
    }

    private static TMP_FontAsset RegisterLanguageFont(string language, TMP_FontAsset font)
    {
        font.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        RuntimeFallbacks.Add(font);
        AddGlobalFallback(font);
        LanguageFonts[language] = font;
        return font;
    }

    private static void AddGlobalFallback(TMP_FontAsset font)
    {
        if (font == null)
            return;

        List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets;

        if (fallbacks == null)
        {
            fallbacks = new List<TMP_FontAsset>();
            TMP_Settings.fallbackFontAssets = fallbacks;
        }

        if (!fallbacks.Contains(font))
            fallbacks.Add(font);
    }

    private static TMP_FontAsset CreateFallbackFontAssetFromFile(string fileName, string value)
    {
        string fontPath = ResolveFontPath(fileName);

        if (!File.Exists(fontPath))
            return null;

        foreach (int faceIndex in GetFaceIndicesToTry(fontPath))
        {
            TMP_FontAsset font = CreateFallbackFontAssetFromPath(fontPath, faceIndex);

            if (TryPrepareFontForText(font, value))
                return font;
        }

        return null;
    }

    private static TMP_FontAsset CreateFallbackFontAssetFromPath(string fontPath, int faceIndex)
    {
        try
        {
            return TMP_FontAsset.CreateFontAsset(
                fontPath,
                faceIndex,
                FontSamplingPointSize,
                FontAtlasPadding,
                GlyphRenderMode.SDFAA,
                FontAtlasSize,
                FontAtlasSize);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static IEnumerable<int> GetFaceIndicesToTry(string fontPath)
    {
        yield return 0;

        string extension = Path.GetExtension(fontPath);

        if (!string.Equals(extension, ".ttc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension, ".otc", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        for (int i = 1; i < MaxFontCollectionFaces; i++)
            yield return i;
    }

    private static string ResolveFontPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        if (Path.IsPathRooted(fileName) && File.Exists(fileName))
            return fileName;

        foreach (string folder in FontSearchFolders)
        {
            if (string.IsNullOrWhiteSpace(folder))
                continue;

            string path = Path.Combine(folder, fileName);

            if (File.Exists(path))
                return path;

            string matchedPath = FindFontFileIgnoringCase(folder, fileName);

            if (!string.IsNullOrEmpty(matchedPath))
                return matchedPath;
        }

        return null;
    }

    private static string FindFontFileIgnoringCase(string folder, string fileName)
    {
        try
        {
            if (!Directory.Exists(folder))
                return null;

            foreach (string path in Directory.GetFiles(folder))
            {
                if (string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase))
                    return path;
            }
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }

    private static bool TryPrepareFontForText(TMP_FontAsset font, string value)
    {
        if (!IsRuntimeFallbackUsable(font))
            return false;

        string characters = GetUniqueRenderableCharacters(value);

        if (string.IsNullOrEmpty(characters))
            return true;

        if (font.atlasPopulationMode == AtlasPopulationMode.Dynamic
            || font.atlasPopulationMode == AtlasPopulationMode.DynamicOS)
        {
            try
            {
                font.TryAddCharacters(characters, out _);
            }
            catch (MissingReferenceException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        return CanRenderDirectly(font, value);
    }

    private static string GetUniqueRenderableCharacters(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        StringBuilder builder = new();
        HashSet<char> added = new();

        foreach (char character in value)
        {
            if (char.IsControl(character) || char.IsWhiteSpace(character))
                continue;

            if (added.Add(character))
                builder.Append(character);
        }

        return builder.ToString();
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
            if (font.HasCharacter(character, false, true))
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
