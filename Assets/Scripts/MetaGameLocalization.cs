using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class MetaGameLocalization
{
    private static readonly string[] LanguageCodes =
    {
        "en", "zh", "hi", "es", "ar", "fr", "pt", "ru", "ja", "vi", "pl", "uk",
        "fil", "th", "de", "it", "tr", "bn", "ko", "ro", "hu"
    };

    private static readonly Dictionary<string, string> Labels = new()
    {
        ["en"] = "English",
        ["zh"] = "\u4e2d\u6587",
        ["hi"] = "\u0939\u093f\u0928\u094d\u0926\u0940",
        ["es"] = "Espa\u00f1ol",
        ["ar"] = "\u0627\u0644\u0639\u0631\u0628\u064a\u0629",
        ["fr"] = "Fran\u00e7ais",
        ["pt"] = "Portugu\u00eas",
        ["ru"] = "\u0420\u0443\u0441\u0441\u043a\u0438\u0439",
        ["ja"] = "\u65e5\u672c\u8a9e",
        ["vi"] = "Ti\u1ebfng Vi\u1ec7t",
        ["pl"] = "Polski",
        ["uk"] = "\u0423\u043a\u0440\u0430\u0457\u043d\u0441\u044c\u043a\u0430",
        ["fil"] = "Filipino",
        ["th"] = "\u0e44\u0e17\u0e22",
        ["de"] = "Deutsch",
        ["it"] = "Italiano",
        ["tr"] = "T\u00fcrk\u00e7e",
        ["bn"] = "\u09ac\u09be\u0982\u09b2\u09be",
        ["ko"] = "\ud55c\uad6d\uc5b4",
        ["ro"] = "Rom\u00e2n\u0103",
        ["hu"] = "Magyar"
    };

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = BuildTranslations();
    private static readonly Dictionary<TMP_Text, string> OriginalTexts = new();

    public static string CurrentLanguage
    {
        get
        {
            string saved = MetaGameSave.SaveSystem.GetString(MetaGameSaveKeys.Language, string.Empty);

            if (!string.IsNullOrWhiteSpace(saved))
                return saved;

            string detected = DetectSystemLanguage();
            MetaGameSave.SaveSystem.SetString(MetaGameSaveKeys.Language, detected);
            MetaGameSave.SaveSystem.Save();
            return detected;
        }
    }

    public static string CurrentLanguageLabel
    {
        get
        {
            MetaGameFontFallbacks.EnsureInstalled();

            string label = Labels.TryGetValue(CurrentLanguage, out string value)
                ? value
                : "English";

            return MetaGameFontFallbacks.CanRender(TMP_Settings.defaultFontAsset, label)
                ? label
                : "English";
        }
    }

    public static void CycleLanguage()
    {
        int index = Array.IndexOf(LanguageCodes, CurrentLanguage);
        index = index < 0 ? 0 : (index + 1) % LanguageCodes.Length;
        MetaGameSave.SaveSystem.SetString(MetaGameSaveKeys.Language, LanguageCodes[index]);
        MetaGameSave.SaveSystem.Save();
        Apply();
    }

    public static string Translate(string english)
    {
        if (string.IsNullOrWhiteSpace(english))
            return english;

        string original = StripRichText(english);
        string language = CurrentLanguage;

        return Translations.TryGetValue(language, out Dictionary<string, string> dictionary)
            && dictionary.TryGetValue(original, out string translated)
                ? translated
                : english;
    }

    public static string TranslateForFont(TMP_Text text, string english)
    {
        string translated = Translate(english);

        if (text == null || MetaGameFontFallbacks.CanRender(text.font, translated))
            return translated;

        return english;
    }

    public static string TranslateFormatForFont(TMP_Text text, string englishFormat, params object[] args)
    {
        string format = TranslateForFont(text, englishFormat);
        return string.Format(format, args);
    }

    public static void Apply()
    {
        MetaGameFontFallbacks.EnsureInstalled();

        string language = CurrentLanguage;
        Translations.TryGetValue(language, out Dictionary<string, string> dictionary);

        foreach (TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text == null || IsDynamicText(text))
                continue;

            if (!OriginalTexts.ContainsKey(text))
                OriginalTexts[text] = StripRichText(text.text);

            string original = OriginalTexts[text];
            string next = dictionary != null ? TranslateOriginal(original, dictionary) : original;

            text.text = MetaGameFontFallbacks.CanRender(text.font, next) ? next : original;
        }
    }

    private static string TranslateOriginal(string original, Dictionary<string, string> dictionary)
    {
        return dictionary != null && dictionary.TryGetValue(original, out string translated)
            ? translated
            : original;
    }

    private static Dictionary<string, Dictionary<string, string>> BuildTranslations()
    {
        Dictionary<string, Dictionary<string, string>> translations = new();

        Add(translations, "zh", "\u8bbe\u7f6e", "\u5546\u5e97", "\u6392\u884c\u699c", "\u65b0\u624b\u793c\u5305", "\u5f00\u59cb", "\u91cd\u65b0\u5f00\u59cb", "\u7ee7\u7eed", "\u97f3\u4e50", "\u58f0\u97f3", "\u8bed\u8a00", "\u5206\u6570", "\u6d88\u9664", "\u751f\u547d", "\u8d2d\u4e70", "\u5df2\u9009\u62e9", "\u9053\u5177", "\u7403\u684c", "\u7403", "\u7403\u6746");
        Add(translations, "hi", "\u0938\u0947\u091f\u093f\u0902\u0917\u094d\u0938", "\u0926\u0941\u0915\u093e\u0928", "\u0932\u0940\u0921\u0930\u092c\u094b\u0930\u094d\u0921", "\u0938\u094d\u091f\u093e\u0930\u094d\u091f\u0930 \u092a\u0948\u0915", "\u0916\u0947\u0932\u0947\u0902", "\u092b\u093f\u0930 \u0936\u0941\u0930\u0942", "\u091c\u093e\u0930\u0940", "\u0938\u0902\u0917\u0940\u0924", "\u0927\u094d\u0935\u0928\u093f", "\u092d\u093e\u0937\u093e", "\u0938\u094d\u0915\u094b\u0930", "\u0935\u093f\u0928\u093e\u0936", "\u091c\u0940\u0935\u0928", "\u0916\u0930\u0940\u0926\u0947\u0902", "\u091a\u092f\u0928\u093f\u0924", "\u092a\u093e\u0935\u0930\u0905\u092a", "\u092c\u094b\u0930\u094d\u0921", "\u0917\u0947\u0902\u0926\u0947\u0902", "\u092a\u0942\u0932 \u0938\u094d\u091f\u093f\u0915");
        Add(translations, "es", "Ajustes", "Tienda", "Clasificaci\u00f3n", "Paquete inicial", "Jugar", "Reiniciar", "Continuar", "M\u00fasica", "Sonido", "Idioma", "Puntuaci\u00f3n", "Destrucciones", "Vidas", "Comprar", "Seleccionado", "Potenciadores", "Mesas", "Bolas", "Tacos");
        Add(translations, "ar", "\u0627\u0644\u0625\u0639\u062f\u0627\u062f\u0627\u062a", "\u0627\u0644\u0645\u062a\u062c\u0631", "\u0644\u0648\u062d\u0629 \u0627\u0644\u0635\u062f\u0627\u0631\u0629", "\u062d\u0632\u0645\u0629 \u0627\u0644\u0628\u062f\u0621", "\u0627\u0644\u0639\u0628", "\u0625\u0639\u0627\u062f\u0629", "\u0645\u062a\u0627\u0628\u0639\u0629", "\u0645\u0648\u0633\u064a\u0642\u0649", "\u0635\u0648\u062a", "\u0627\u0644\u0644\u063a\u0629", "\u0627\u0644\u0646\u0642\u0627\u0637", "\u062a\u062f\u0645\u064a\u0631\u0627\u062a", "\u062d\u064a\u0627\u0629", "\u0634\u0631\u0627\u0621", "\u0645\u062d\u062f\u062f", "\u0642\u0648\u0649", "\u0637\u0627\u0648\u0644\u0627\u062a", "\u0643\u0631\u0627\u062a", "\u0639\u0635\u064a");
        Add(translations, "fr", "Param\u00e8tres", "Boutique", "Classement", "Pack de d\u00e9part", "Jouer", "Recommencer", "Continuer", "Musique", "Son", "Langue", "Score", "Destructions", "Vies", "Acheter", "S\u00e9lectionn\u00e9", "Bonus", "Plateaux", "Boules", "Queues");
        Add(translations, "pt", "Configura\u00e7\u00f5es", "Loja", "Classifica\u00e7\u00e3o", "Pacote inicial", "Jogar", "Reiniciar", "Continuar", "M\u00fasica", "Som", "Idioma", "Pontua\u00e7\u00e3o", "Destrui\u00e7\u00f5es", "Vidas", "Comprar", "Selecionado", "Poderes", "Mesas", "Bolas", "Tacos");
        Add(translations, "ru", "\u041d\u0430\u0441\u0442\u0440\u043e\u0439\u043a\u0438", "\u041c\u0430\u0433\u0430\u0437\u0438\u043d", "\u0420\u0435\u0439\u0442\u0438\u043d\u0433", "\u0421\u0442\u0430\u0440\u0442\u043e\u0432\u044b\u0439 \u043d\u0430\u0431\u043e\u0440", "\u0418\u0433\u0440\u0430\u0442\u044c", "\u0417\u0430\u043d\u043e\u0432\u043e", "\u041f\u0440\u043e\u0434\u043e\u043b\u0436\u0438\u0442\u044c", "\u041c\u0443\u0437\u044b\u043a\u0430", "\u0417\u0432\u0443\u043a", "\u042f\u0437\u044b\u043a", "\u0421\u0447\u0435\u0442", "\u0420\u0430\u0437\u0440\u0443\u0448\u0435\u043d\u0438\u044f", "\u0416\u0438\u0437\u043d\u0438", "\u041a\u0443\u043f\u0438\u0442\u044c", "\u0412\u044b\u0431\u0440\u0430\u043d\u043e", "\u0411\u043e\u043d\u0443\u0441\u044b", "\u0421\u0442\u043e\u043b\u044b", "\u0428\u0430\u0440\u044b", "\u041a\u0438\u0438");
        Add(translations, "ja", "\u8a2d\u5b9a", "\u30b7\u30e7\u30c3\u30d7", "\u30e9\u30f3\u30ad\u30f3\u30b0", "\u30b9\u30bf\u30fc\u30bf\u30fc\u30d1\u30c3\u30af", "\u30d7\u30ec\u30a4", "\u518d\u958b", "\u7d9a\u3051\u308b", "\u97f3\u697d", "\u30b5\u30a6\u30f3\u30c9", "\u8a00\u8a9e", "\u30b9\u30b3\u30a2", "\u7834\u58ca", "\u30e9\u30a4\u30d5", "\u8cfc\u5165", "\u9078\u629e\u4e2d", "\u30d1\u30ef\u30fc\u30a2\u30c3\u30d7", "\u30dc\u30fc\u30c9", "\u30dc\u30fc\u30eb", "\u30ad\u30e5\u30fc");
        Add(translations, "vi", "C\u00e0i \u0111\u1eb7t", "C\u1eeda h\u00e0ng", "B\u1ea3ng x\u1ebfp h\u1ea1ng", "G\u00f3i kh\u1edfi \u0111\u1ea7u", "Ch\u01a1i", "Ch\u01a1i l\u1ea1i", "Ti\u1ebfp t\u1ee5c", "\u00c2m nh\u1ea1c", "\u00c2m thanh", "Ng\u00f4n ng\u1eef", "\u0110i\u1ec3m", "Ph\u00e1 h\u1ee7y", "M\u1ea1ng", "Mua", "\u0110\u00e3 ch\u1ecdn", "S\u1ee9c m\u1ea1nh", "B\u00e0n", "Bi", "G\u1eady");
        Add(translations, "pl", "Ustawienia", "Sklep", "Ranking", "Pakiet startowy", "Graj", "Restart", "Kontynuuj", "Muzyka", "D\u017awi\u0119k", "J\u0119zyk", "Wynik", "Zniszczenia", "\u017bycia", "Kup", "Wybrano", "Bonusy", "Sto\u0142y", "Bile", "Kije");
        Add(translations, "uk", "\u041d\u0430\u043b\u0430\u0448\u0442\u0443\u0432\u0430\u043d\u043d\u044f", "\u041c\u0430\u0433\u0430\u0437\u0438\u043d", "\u0420\u0435\u0439\u0442\u0438\u043d\u0433", "\u0421\u0442\u0430\u0440\u0442\u043e\u0432\u0438\u0439 \u043f\u0430\u043a\u0435\u0442", "\u0413\u0440\u0430\u0442\u0438", "\u0417\u0430\u043d\u043e\u0432\u043e", "\u041f\u0440\u043e\u0434\u043e\u0432\u0436\u0438\u0442\u0438", "\u041c\u0443\u0437\u0438\u043a\u0430", "\u0417\u0432\u0443\u043a", "\u041c\u043e\u0432\u0430", "\u0420\u0430\u0445\u0443\u043d\u043e\u043a", "\u0420\u0443\u0439\u043d\u0443\u0432\u0430\u043d\u043d\u044f", "\u0416\u0438\u0442\u0442\u044f", "\u041a\u0443\u043f\u0438\u0442\u0438", "\u0412\u0438\u0431\u0440\u0430\u043d\u043e", "\u0411\u043e\u043d\u0443\u0441\u0438", "\u0421\u0442\u043e\u043b\u0438", "\u041a\u0443\u043b\u0456", "\u041a\u0438\u0457");
        Add(translations, "fil", "Settings", "Tindahan", "Leaderboard", "Starter Pack", "Laro", "Ulitin", "Ituloy", "Musika", "Tunog", "Wika", "Score", "Mga Sira", "Buhay", "Bilhin", "Napili", "Powerups", "Boards", "Bola", "Pool Sticks");
        Add(translations, "th", "\u0e15\u0e31\u0e49\u0e07\u0e04\u0e48\u0e32", "\u0e23\u0e49\u0e32\u0e19\u0e04\u0e49\u0e32", "\u0e01\u0e23\u0e30\u0e14\u0e32\u0e19\u0e2d\u0e31\u0e19\u0e14\u0e31\u0e1a", "\u0e41\u0e1e\u0e47\u0e01\u0e40\u0e23\u0e34\u0e48\u0e21\u0e15\u0e49\u0e19", "\u0e40\u0e25\u0e48\u0e19", "\u0e40\u0e23\u0e34\u0e48\u0e21\u0e43\u0e2b\u0e21\u0e48", "\u0e15\u0e48\u0e2d", "\u0e40\u0e1e\u0e25\u0e07", "\u0e40\u0e2a\u0e35\u0e22\u0e07", "\u0e20\u0e32\u0e29\u0e32", "\u0e04\u0e30\u0e41\u0e19\u0e19", "\u0e01\u0e32\u0e23\u0e17\u0e33\u0e25\u0e32\u0e22", "\u0e0a\u0e35\u0e27\u0e34\u0e15", "\u0e0b\u0e37\u0e49\u0e2d", "\u0e40\u0e25\u0e37\u0e2d\u0e01\u0e41\u0e25\u0e49\u0e27", "\u0e1e\u0e32\u0e27\u0e40\u0e27\u0e2d\u0e23\u0e4c\u0e2d\u0e31\u0e1e", "\u0e01\u0e23\u0e30\u0e14\u0e32\u0e19", "\u0e25\u0e39\u0e01", "\u0e44\u0e21\u0e49\u0e04\u0e34\u0e27");
        Add(translations, "de", "Einstellungen", "Shop", "Bestenliste", "Starterpaket", "Spielen", "Neustart", "Fortsetzen", "Musik", "Ton", "Sprache", "Punktzahl", "Zerst\u00f6rungen", "Leben", "Kaufen", "Ausgew\u00e4hlt", "Powerups", "Tische", "B\u00e4lle", "Queues");
        Add(translations, "it", "Impostazioni", "Negozio", "Classifica", "Pacchetto iniziale", "Gioca", "Riavvia", "Continua", "Musica", "Audio", "Lingua", "Punteggio", "Distruzioni", "Vite", "Compra", "Selezionato", "Potenziamenti", "Tavoli", "Palle", "Stecche");
        Add(translations, "tr", "Ayarlar", "Ma\u011faza", "Liderlik", "Ba\u015flang\u0131\u00e7 Paketi", "Oyna", "Yeniden", "Devam", "M\u00fczik", "Ses", "Dil", "Skor", "Y\u0131k\u0131mlar", "Can", "Sat\u0131n Al", "Se\u00e7ildi", "G\u00fc\u00e7ler", "Masalar", "Toplar", "Istakalar");
        Add(translations, "bn", "\u09b8\u09c7\u099f\u09bf\u0982\u09b8", "\u09a6\u09cb\u0995\u09be\u09a8", "\u09b2\u09bf\u09a1\u09be\u09b0\u09ac\u09cb\u09b0\u09cd\u09a1", "\u09b8\u09cd\u099f\u09be\u09b0\u09cd\u099f\u09be\u09b0 \u09aa\u09cd\u09af\u09be\u0995", "\u0996\u09c7\u09b2\u09c1\u09a8", "\u09aa\u09c1\u09a8\u09b0\u09be\u09df", "\u099a\u09be\u09b2\u09bf\u09df\u09c7 \u09af\u09be\u09a8", "\u09b8\u0999\u09cd\u0997\u09c0\u09a4", "\u09b6\u09ac\u09cd\u09a6", "\u09ad\u09be\u09b7\u09be", "\u09b8\u09cd\u0995\u09cb\u09b0", "\u09a7\u09cd\u09ac\u0982\u09b8", "\u099c\u09c0\u09ac\u09a8", "\u0995\u09bf\u09a8\u09c1\u09a8", "\u09a8\u09bf\u09b0\u09cd\u09ac\u09be\u099a\u09bf\u09a4", "\u09aa\u09be\u0993\u09df\u09be\u09b0\u0986\u09aa", "\u09ac\u09cb\u09b0\u09cd\u09a1", "\u09ac\u09b2", "\u0995\u09bf\u0989");
        Add(translations, "ko", "\uc124\uc815", "\uc0c1\uc810", "\ub9ac\ub354\ubcf4\ub4dc", "\uc2a4\ud130\ud130 \ud329", "\ud50c\ub808\uc774", "\ub2e4\uc2dc \uc2dc\uc791", "\uacc4\uc18d", "\uc74c\uc545", "\uc18c\ub9ac", "\uc5b8\uc5b4", "\uc810\uc218", "\ud30c\uad34", "\ubaa9\uc228", "\uad6c\ub9e4", "\uc120\ud0dd\ub428", "\ud30c\uc6cc\uc5c5", "\ubcf4\ub4dc", "\uacf5", "\ud050");
        Add(translations, "ro", "Setari", "Magazin", "Clasament", "Pachet Start", "Joaca", "Restart", "Continua", "Muzica", "Sunet", "Limba", "Scor", "Distrugeri", "Vieti", "Cumpara", "Selectat", "Puteri", "Mese", "Bile", "Tacuri");
        Add(translations, "hu", "Be\u00e1ll\u00edt\u00e1sok", "Bolt", "Ranglista", "Kezd\u0151 csomag", "J\u00e1t\u00e9k", "\u00dajraind\u00edt\u00e1s", "Folytat\u00e1s", "Zene", "Hang", "Nyelv", "Pontsz\u00e1m", "Rombol\u00e1sok", "\u00c9letek", "V\u00e1s\u00e1rl\u00e1s", "Kiv\u00e1lasztva", "Er\u0151s\u00edt\u0151k", "Asztalok", "Goly\u00f3k", "D\u00e1k\u00f3k");

        return translations;
    }

    private static void Add(Dictionary<string, Dictionary<string, string>> translations, string code, string settings, string shop, string leaderboards, string starterPack, string play, string restart, string resume, string music, string sound, string language, string score, string destructions, string lives, string buy, string selected, string powerups, string boards, string balls, string poolSticks)
    {
        Dictionary<string, string> values = new()
        {
            ["Settings"] = settings,
            ["Shop"] = shop,
            ["Leaderboards"] = leaderboards,
            ["Starter Pack"] = starterPack,
            ["Starter Pack!"] = starterPack,
            ["Play"] = play,
            ["Restart"] = restart,
            ["Resume"] = resume,
            ["Music"] = music,
            ["Sound"] = sound,
            ["Language"] = language,
            ["Score"] = score,
            ["Destructions"] = destructions,
            ["Lives"] = lives,
            ["Buy"] = buy,
            ["Selected"] = selected,
            ["Powerups"] = powerups,
            ["Boards"] = boards,
            ["Balls"] = balls,
            ["Pool Sticks"] = poolSticks
        };

        AddExtra(values, code, leaderboards, buy, starterPack);
        translations[code] = values;
    }

    private static void AddExtra(Dictionary<string, string> values, string code, string leaderboards, string buy, string starterPack)
    {
        switch (code)
        {
            case "zh":
                AddExtra(values, "\u6392\u884c\u699c", "\u8d2d\u4e70\u65b0\u624b\u793c\u5305\u4ee5\u83b7\u5f97\u4ee5\u4e0b\u5185\u5bb9:", "\u8fde\u51fb", "\u65b0\u7403\u52a0\u5165\u7403\u684c", "\u6682\u505c", "\u5df2\u6682\u505c", "\u6e38\u620f\u7ed3\u675f");
                return;
            case "hi":
                AddExtra(values, "\u0932\u0940\u0921\u0930\u092c\u094b\u0930\u094d\u0921", "\u0938\u094d\u091f\u093e\u0930\u094d\u091f\u0930 \u092a\u0948\u0915 \u0916\u0930\u0940\u0926\u0947\u0902 \u0914\u0930 \u092f\u0947 \u092a\u093e\u090f\u0902:", "\u0915\u0949\u092e\u094d\u092c\u094b", "\u0928\u0908 \u0917\u0947\u0902\u0926 \u092c\u094b\u0930\u094d\u0921 \u092e\u0947\u0902 \u0936\u093e\u092e\u093f\u0932 \u0939\u0941\u0908", "\u0930\u094b\u0915\u0947\u0902", "\u0930\u0941\u0915\u093e \u0939\u0941\u0906", "\u0916\u0947\u0932 \u0916\u0924\u094d\u092e");
                return;
            case "es":
                AddExtra(values, "Clasificaci\u00f3n", "Compra el paquete inicial para obtener lo siguiente:", "Combo", "Nueva bola se une al tablero", "Pausa", "En pausa", "Fin del juego");
                return;
            case "ar":
                AddExtra(values, "\u0644\u0648\u062d\u0629 \u0627\u0644\u0635\u062f\u0627\u0631\u0629", "\u0627\u0634\u062a\u0631 \u062d\u0632\u0645\u0629 \u0627\u0644\u0628\u062f\u0621 \u0644\u062a\u062d\u0635\u0644 \u0639\u0644\u0649 \u0627\u0644\u062a\u0627\u0644\u064a:", "\u0643\u0648\u0645\u0628\u0648", "\u0643\u0631\u0629 \u062c\u062f\u064a\u062f\u0629 \u062a\u0646\u0636\u0645 \u0625\u0644\u0649 \u0627\u0644\u0644\u0648\u062d\u0629", "\u0625\u064a\u0642\u0627\u0641 \u0645\u0624\u0642\u062a", "\u0645\u062a\u0648\u0642\u0641 \u0645\u0624\u0642\u062a\u0627", "\u0627\u0646\u062a\u0647\u062a \u0627\u0644\u0644\u0639\u0628\u0629");
                return;
            case "fr":
                AddExtra(values, "Classement", "Achetez le pack de d\u00e9part pour obtenir:", "Combo", "Une nouvelle boule rejoint le plateau", "Pause", "En pause", "Partie termin\u00e9e");
                return;
            case "pt":
                AddExtra(values, "Classifica\u00e7\u00e3o", "Compre o pacote inicial para receber:", "Combo", "Nova bola entra no tabuleiro", "Pausa", "Pausado", "Fim de jogo");
                return;
            case "ru":
                AddExtra(values, "\u0420\u0435\u0439\u0442\u0438\u043d\u0433", "\u041a\u0443\u043f\u0438\u0442\u0435 \u0441\u0442\u0430\u0440\u0442\u043e\u0432\u044b\u0439 \u043d\u0430\u0431\u043e\u0440, \u0447\u0442\u043e\u0431\u044b \u043f\u043e\u043b\u0443\u0447\u0438\u0442\u044c:", "\u041a\u043e\u043c\u0431\u043e", "\u041d\u043e\u0432\u044b\u0439 \u0448\u0430\u0440 \u043f\u043e\u044f\u0432\u043b\u044f\u0435\u0442\u0441\u044f \u043d\u0430 \u043f\u043e\u043b\u0435", "\u041f\u0430\u0443\u0437\u0430", "\u041f\u0430\u0443\u0437\u0430", "\u0418\u0433\u0440\u0430 \u043e\u043a\u043e\u043d\u0447\u0435\u043d\u0430");
                return;
            case "ja":
                AddExtra(values, "\u30e9\u30f3\u30ad\u30f3\u30b0", "\u30b9\u30bf\u30fc\u30bf\u30fc\u30d1\u30c3\u30af\u3092\u8cfc\u5165\u3057\u3066\u4ee5\u4e0b\u3092\u5165\u624b:", "\u30b3\u30f3\u30dc", "\u65b0\u3057\u3044\u30dc\u30fc\u30eb\u304c\u30dc\u30fc\u30c9\u306b\u53c2\u52a0", "\u4e00\u6642\u505c\u6b62", "\u4e00\u6642\u505c\u6b62", "\u30b2\u30fc\u30e0\u30aa\u30fc\u30d0\u30fc");
                return;
            case "vi":
                AddExtra(values, "B\u1ea3ng x\u1ebfp h\u1ea1ng", "Mua g\u00f3i kh\u1edfi \u0111\u1ea7u \u0111\u1ec3 nh\u1eadn:", "Combo", "B\u00f3ng m\u1edbi v\u00e0o b\u00e0n", "T\u1ea1m d\u1eebng", "T\u1ea1m d\u1eebng", "Tr\u00f2 ch\u01a1i k\u1ebft th\u00fac");
                return;
            case "pl":
                AddExtra(values, "Ranking", "Kup pakiet startowy, aby otrzyma\u0107:", "Kombinacja", "Nowa bila do\u0142\u0105cza do sto\u0142u", "Pauza", "Pauza", "Koniec gry");
                return;
            case "uk":
                AddExtra(values, "\u0420\u0435\u0439\u0442\u0438\u043d\u0433", "\u041a\u0443\u043f\u0456\u0442\u044c \u0441\u0442\u0430\u0440\u0442\u043e\u0432\u0438\u0439 \u043f\u0430\u043a\u0435\u0442, \u0449\u043e\u0431 \u043e\u0442\u0440\u0438\u043c\u0430\u0442\u0438:", "\u041a\u043e\u043c\u0431\u043e", "\u041d\u043e\u0432\u0430 \u043a\u0443\u043b\u044f \u0437'\u044f\u0432\u043b\u044f\u0454\u0442\u044c\u0441\u044f \u043d\u0430 \u043f\u043e\u043b\u0456", "\u041f\u0430\u0443\u0437\u0430", "\u041f\u0430\u0443\u0437\u0430", "\u0413\u0440\u0443 \u0437\u0430\u0432\u0435\u0440\u0448\u0435\u043d\u043e");
                return;
            case "fil":
                AddExtra(values, "Leaderboard", "Bilhin ang Starter Pack para makuha ang mga ito:", "Combo", "May bagong bola sa board", "Pause", "Naka-pause", "Game Over");
                return;
            case "th":
                AddExtra(values, "\u0e01\u0e23\u0e30\u0e14\u0e32\u0e19\u0e2d\u0e31\u0e19\u0e14\u0e31\u0e1a", "\u0e0b\u0e37\u0e49\u0e2d\u0e41\u0e1e\u0e47\u0e01\u0e40\u0e23\u0e34\u0e48\u0e21\u0e15\u0e49\u0e19\u0e40\u0e1e\u0e37\u0e48\u0e2d\u0e23\u0e31\u0e1a:", "\u0e04\u0e2d\u0e21\u0e42\u0e1a", "\u0e25\u0e39\u0e01\u0e43\u0e2b\u0e21\u0e48\u0e40\u0e02\u0e49\u0e32\u0e01\u0e23\u0e30\u0e14\u0e32\u0e19", "\u0e2b\u0e22\u0e38\u0e14\u0e0a\u0e31\u0e48\u0e27\u0e04\u0e23\u0e32\u0e27", "\u0e2b\u0e22\u0e38\u0e14\u0e0a\u0e31\u0e48\u0e27\u0e04\u0e23\u0e32\u0e27", "\u0e08\u0e1a\u0e40\u0e01\u0e21");
                return;
            case "de":
                AddExtra(values, "Bestenliste", "Kaufe das Starterpaket, um Folgendes zu erhalten:", "Combo", "Neue Kugel kommt aufs Brett", "Pause", "Pausiert", "Spiel vorbei");
                return;
            case "it":
                AddExtra(values, "Classifica", "Acquista il pacchetto iniziale per ottenere:", "Combo", "Nuova palla entra nel tavolo", "Pausa", "In pausa", "Fine partita");
                return;
            case "tr":
                AddExtra(values, "Liderlik", "Ba\u015flang\u0131\u00e7 paketini al ve \u015funlar\u0131 kazan:", "Kombo", "Yeni top tahtaya kat\u0131ld\u0131", "Duraklat", "Duraklat\u0131ld\u0131", "Oyun bitti");
                return;
            case "bn":
                AddExtra(values, "\u09b2\u09bf\u09a1\u09be\u09b0\u09ac\u09cb\u09b0\u09cd\u09a1", "\u09b8\u09cd\u099f\u09be\u09b0\u09cd\u099f\u09be\u09b0 \u09aa\u09cd\u09af\u09be\u0995 \u0995\u09bf\u09a8\u09c7 \u098f\u0997\u09c1\u09b2\u09cb \u09aa\u09be\u09a8:", "\u0995\u09ae\u09cd\u09ac\u09cb", "\u09a8\u09a4\u09c1\u09a8 \u09ac\u09b2 \u09ac\u09cb\u09b0\u09cd\u09a1\u09c7 \u09af\u09cb\u0997 \u09a6\u09bf\u09b2", "\u09ac\u09bf\u09b0\u09a4\u09bf", "\u09ac\u09bf\u09b0\u09a4\u09bf", "\u0996\u09c7\u09b2\u09be \u09b6\u09c7\u09b7");
                return;
            case "ko":
                AddExtra(values, "\ub9ac\ub354\ubcf4\ub4dc", "\uc2a4\ud130\ud130 \ud329\uc744 \uad6c\ub9e4\ud558\uc5ec \ub2e4\uc74c\uc744 \ubc1b\uc73c\uc138\uc694:", "\ucf64\ubcf4", "\uc0c8 \uacf5\uc774 \ubcf4\ub4dc\uc5d0 \ud569\ub958", "\uc77c\uc2dc\uc815\uc9c0", "\uc77c\uc2dc\uc815\uc9c0", "\uac8c\uc784 \uc624\ubc84");
                return;
            case "ro":
                AddExtra(values, "Clasament", "Cump\u0103r\u0103 pachetul de start pentru a primi:", "Combo", "O bil\u0103 nou\u0103 intr\u0103 pe tabl\u0103", "Pauz\u0103", "Pauz\u0103", "Sf\u00e2r\u0219it joc");
                return;
            case "hu":
                AddExtra(values, "Ranglista", "Vedd meg a kezd\u0151 csomagot ez\u00e9rt:", "Komb\u00f3", "\u00daj goly\u00f3 \u00e9rkezik a t\u00e1bl\u00e1ra", "Sz\u00fcnet", "Sz\u00fcnetelve", "J\u00e1t\u00e9k v\u00e9ge");
                return;
            default:
                AddExtra(values, leaderboards, buy + " " + starterPack, "Combo", "New Ball Joins The Board", "Pause", "Paused", "Game Over");
                return;
        }
    }

    private static void AddExtra(Dictionary<string, string> values, string leaderboard, string starterPackDescription, string combo, string newBallJoinsBoard, string pause, string paused, string gameOver)
    {
        values["Leaderboard"] = leaderboard;
        values["Buy the Starter Pack to get the following:"] = starterPackDescription;
        values["Buy the Starter Pack to get the following"] = starterPackDescription;
        values["Combo"] = combo;
        values["Combo x2"] = combo + " x2";
        values["Combo x{0}"] = combo + " x{0}";
        values["New Ball Joins The Board"] = newBallJoinsBoard;
        values["A New Ball Joins The Board"] = newBallJoinsBoard;
        values["Pause"] = pause;
        values["Paused"] = paused;
        values["Game Over"] = gameOver;
    }

    private static string StripRichText(string value)
    {
        return value
            .Replace("<color=green>", string.Empty)
            .Replace("</color>", string.Empty)
            .Replace("!", string.Empty)
            .Trim();
    }

    private static bool IsDynamicText(TMP_Text text)
    {
        string name = text.name;
        string path = GetPath(text.transform);

        return name == "Lives"
            || name == "TimerText"
            || name == "Score&DestructionsText"
            || name == "MoneyCountText"
            || name == "CurrentPowerupCount"
            || name == "CountText"
            || name == "ComboText"
            || name == "PriceText"
            || name == "NextBallText"
            || (name == "Score" && path.Contains("LeaderBoardElementYou", StringComparison.OrdinalIgnoreCase))
            || name.Contains("Count", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        string path = transform.name;

        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }

    private static string DetectSystemLanguage()
    {
        return Application.systemLanguage switch
        {
            SystemLanguage.Chinese or SystemLanguage.ChineseSimplified or SystemLanguage.ChineseTraditional => "zh",
            SystemLanguage.Hindi => "hi",
            SystemLanguage.Spanish => "es",
            SystemLanguage.Arabic => "ar",
            SystemLanguage.French => "fr",
            SystemLanguage.Portuguese => "pt",
            SystemLanguage.Russian => "ru",
            SystemLanguage.Japanese => "ja",
            SystemLanguage.Vietnamese => "vi",
            SystemLanguage.Polish => "pl",
            SystemLanguage.Ukrainian => "uk",
            SystemLanguage.Thai => "th",
            SystemLanguage.German => "de",
            SystemLanguage.Italian => "it",
            SystemLanguage.Turkish => "tr",
            SystemLanguage.Korean => "ko",
            SystemLanguage.Romanian => "ro",
            SystemLanguage.Hungarian => "hu",
            _ => "en"
        };
    }
}
