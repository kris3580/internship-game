using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MetaGameSceneController : MonoBehaviour
{
    [SerializeField] private Color menuColor = new(0.24f, 0.58f, 0.22f, 1f);
    [SerializeField] private Color leaderboardColor = new(0.72f, 0.16f, 0.16f, 1f);
    [SerializeField] private Color starterPackColor = new(0.18f, 0.34f, 0.78f, 1f);
    [SerializeField] private Color shopColor = new(0.9f, 0.77f, 0.2f, 1f);
    [SerializeField] private Color activeTabColor = new(0.05f, 0.28f, 0.13f, 1f);
    [SerializeField] private float backgroundLerpSpeed = 7f;
    [SerializeField] private float leaderboardTimerHours = 24f;
    [SerializeField] private float starterPackTimerHours = 12f;

    private readonly Dictionary<string, GameObject> panels = new();
    private readonly Dictionary<Button, Color> normalButtonColors = new();
    private Image backgroundImage;
    private Color targetBackgroundColor;
    private float nextSlowRefreshTime;
    private string activeShopTab = "PoolSticks";

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        Rebuild();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (backgroundImage != null)
            backgroundImage.color = Color.Lerp(backgroundImage.color, targetBackgroundColor, Time.deltaTime * backgroundLerpSpeed);
        else if (IsMenuScene())
            backgroundImage = FindBackgroundImage();

        if (Time.unscaledTime >= nextSlowRefreshTime)
        {
            nextSlowRefreshTime = Time.unscaledTime + 0.5f;
            RefreshDynamicText();
            RefreshButtons();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Rebuild();
    }

    private void Rebuild()
    {
        panels.Clear();
        normalButtonColors.Clear();

        CachePanels();
        backgroundImage = FindBackgroundImage();
        targetBackgroundColor = menuColor;
        HookButtons();
        OpenPanel(IsMenuScene() ? "MainMenu" : null);
        RefreshAll();
    }

    private void CachePanels()
    {
        AddPanel("MainMenu");
        AddPanel("SettingsMenu");
        AddPanel("Shop");
        AddPanel("Leaderboards");
        AddPanel("StarterPack");
    }

    private void AddPanel(string name)
    {
        GameObject panel = FindSceneObject(name);

        if (panel != null)
            panels[name] = panel;
    }

    private void HookButtons()
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button == null)
                continue;

            string name = button.name;

            if (button.TryGetComponent(out Image image) && !normalButtonColors.ContainsKey(button))
                normalButtonColors[button] = image.color;

            if (name == "MusicButton")
                ReplaceClick(button, ToggleMusic);
            else if (name == "SoundButton")
                ReplaceClick(button, ToggleSound);
            else if (name == "LanguageButton")
                ReplaceClick(button, CycleLanguage);
            else if (name == "GoToSettingsButton")
                ReplaceClick(button, () => OpenPanel("SettingsMenu"));
            else if (name == "GoToShopButton")
                ReplaceClick(button, () => OpenPanel("Shop"));
            else if (name == "OpenLeaderboards")
                ReplaceClick(button, () => OpenPanel("Leaderboards"));
            else if (name == "OpenStarterPack")
                ReplaceClick(button, () => OpenPanel("StarterPack"));
            else if (name == "GoToMainMenuButton")
                ReplaceClick(button, GoToMainMenu);
            else if (name.Contains("RestartGameButton", StringComparison.OrdinalIgnoreCase))
                ReplaceClick(button, RestartGame);
            else if (name.Contains("Play", StringComparison.OrdinalIgnoreCase)
                || name.Contains("StartGame", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Start", StringComparison.OrdinalIgnoreCase))
                ReplaceClick(button, PlayGame);
            else if (name == "ButtonImage")
                ReplaceClick(button, () => ShowShowcaseForButton(button));
            else if (name.StartsWith("Button", StringComparison.OrdinalIgnoreCase))
                ReplaceClick(button, () => SelectShopTab(name.Replace("Button", string.Empty)));
            else if (name.Contains("Buy", StringComparison.OrdinalIgnoreCase)
                || name.Contains("BuyText", StringComparison.OrdinalIgnoreCase)
                || HasChild(button.transform, "PriceText"))
                ReplaceClick(button, () => TryBuy(button));
            else if (HasChild(button.transform, "SelectedImage"))
                ReplaceClick(button, () => SelectSkin(button));
        }
    }

    private void ReplaceClick(Button button, UnityEngine.Events.UnityAction action)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void ToggleMusic()
    {
        AudioPreferences.MusicEnabled = !AudioPreferences.MusicEnabled;
        RefreshSettingsButtons();
    }

    private void ToggleSound()
    {
        AudioPreferences.SoundEnabled = !AudioPreferences.SoundEnabled;
        RefreshSettingsButtons();
    }

    private void CycleLanguage()
    {
        MetaGameLocalization.CycleLanguage();
        RefreshDynamicText();
    }

    private void PlayGame()
    {
        if (!MetaGameSave.TrySpendLife())
            return;

        SceneManager.LoadScene("Game");
    }

    private void RestartGame()
    {
        if (!MetaGameSave.TrySpendLife())
            return;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToMainMenu()
    {
        if (IsMenuScene())
            OpenPanel("MainMenu");
        else
            SceneManager.LoadScene("Menu");
    }

    private void OpenPanel(string panelName)
    {
        foreach (KeyValuePair<string, GameObject> pair in panels)
            pair.Value.SetActive(pair.Key == panelName || (string.IsNullOrEmpty(panelName) && pair.Value.activeSelf));

        SetShowcaseActive(panelName == "Shop");

        targetBackgroundColor = panelName switch
        {
            "Leaderboards" => leaderboardColor,
            "StarterPack" => starterPackColor,
            "Shop" => shopColor,
            _ => menuColor
        };

        RefreshAll();
        RefreshBackgroundTarget();
    }

    private void SelectShopTab(string tab)
    {
        activeShopTab = tab;
        OpenPanel("Shop");
        SetShopTabPanels(tab);

        RefreshButtons();
        RefreshTabButtons();
        RefreshBackgroundTarget();
    }

    private void TryBuy(Button button)
    {
        string path = GetPath(button.transform);
        int price = ReadPrice(button.transform);
        bool usesHard = path.Contains("Hard", StringComparison.OrdinalIgnoreCase)
            || button.name.Contains("Hard", StringComparison.OrdinalIgnoreCase)
            || activeShopTab.Contains("Gold", StringComparison.OrdinalIgnoreCase);
        bool realMoney = button.name.Contains("RealMoney", StringComparison.OrdinalIgnoreCase)
            || button.name.Contains("BuyText", StringComparison.OrdinalIgnoreCase)
            || path.Contains("RealMoney", StringComparison.OrdinalIgnoreCase)
            || path.Contains("StarterPack", StringComparison.OrdinalIgnoreCase);

        if (realMoney)
        {
            SimulatePurchasePopup();
            CompleteRealMoneyPurchase(path);
            return;
        }

        if (usesHard)
        {
            if (MetaGameSave.HardCurrency < price)
            {
                SelectShopTab("Gold");
                return;
            }

            MetaGameSave.HardCurrency -= price;
        }
        else
        {
            if (MetaGameSave.SoftCurrency < price)
                return;

            MetaGameSave.SoftCurrency -= price;
        }

        GrantShopItem(button);
        MetaGameButtonSfx.PlayBuy();
        RefreshAll();
    }

    private void CompleteRealMoneyPurchase(string path)
    {
        if (path.Contains("Starter", StringComparison.OrdinalIgnoreCase))
            MetaGameSave.BuyStarterPack();
        else
            MetaGameSave.HardCurrency += 100;

        MetaGameButtonSfx.PlayBuy();
        RefreshAll();
    }

    private void GrantShopItem(Button button)
    {
        string path = GetPath(button.transform);

        if (path.Contains("Power", StringComparison.OrdinalIgnoreCase) || path.Contains("fire", StringComparison.OrdinalIgnoreCase))
        {
            MetaGameSave.AddPowerUps(1);
            return;
        }

        string category = DetectSkinCategory(path);
        string id = DetectSkinId(path);

        if (!string.IsNullOrEmpty(category))
        {
            MetaGameSave.OwnSkin(category, id);
            MetaGameSave.SetSelectedSkin(category, id);
        }
    }

    private void SelectSkin(Button button)
    {
        string path = GetPath(button.transform);
        string category = DetectSkinCategory(path);
        string id = DetectSkinId(path);

        if (string.IsNullOrEmpty(category))
            return;

        if (MetaGameSave.IsSkinOwned(category, id))
            MetaGameSave.SetSelectedSkin(category, id);

        RefreshSelectedImages();
    }

    private void RefreshAll()
    {
        RefreshDynamicText();
        RefreshButtons();
        RefreshSettingsButtons();
        RefreshTabButtons();
        RefreshSelectedImages();
        MetaGameLocalization.Apply();
        RefreshBackgroundTarget();
    }

    private void RefreshDynamicText()
    {
        MetaGameSave.RefillLives();

        foreach (TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string path = GetPath(text.transform);

            if (text.name == "Lives")
                text.text = $"{MetaGameSave.Lives}/3";
            else if (text.name == "TimerText")
                RefreshTimer(text, path);
            else if (path.Contains("HardCurrencyContainer", StringComparison.OrdinalIgnoreCase))
                text.text = MetaGameSave.FormatCompact(MetaGameSave.HardCurrency);
            else if (path.Contains("SoftCurrencyContainer", StringComparison.OrdinalIgnoreCase))
                text.text = MetaGameSave.FormatCompact(MetaGameSave.SoftCurrency);
            else if (path.Contains("LeaderBoardElementYou", StringComparison.OrdinalIgnoreCase) && text.name == "Score")
                text.text = MetaGameSave.FormatCompact(MetaGameSave.BestScore);
            else if (text.name == "LanguageText")
                text.text = MetaGameLocalization.CurrentLanguageLabel;
            else if (text.name == "CurrentPowerupCount" || text.name == "CountText")
                RefreshShopPowerUpCount(text, path);
        }

        RefreshLanguageButtonText();
    }

    private void RefreshTimer(TMP_Text text, string path)
    {
        if (path.Contains("Leaderboards", StringComparison.OrdinalIgnoreCase)
            || path.Contains("LeaderBoard", StringComparison.OrdinalIgnoreCase))
        {
            text.gameObject.SetActive(true);
            text.text = MetaGameSave.FormatTime(GetLoopTimer(MetaGameSaveKeys.LeaderboardTimerStartTicks, TimeSpan.FromHours(leaderboardTimerHours)));
            return;
        }

        if (path.Contains("StarterPack", StringComparison.OrdinalIgnoreCase))
        {
            text.gameObject.SetActive(true);
            text.text = MetaGameSave.FormatTime(GetLoopTimer(MetaGameSaveKeys.StarterPackTimerStartTicks, TimeSpan.FromHours(starterPackTimerHours)));
            return;
        }

        bool full = MetaGameSave.Lives >= 3;
        text.gameObject.SetActive(!full);

        if (!full)
            text.text = MetaGameSave.FormatTime(MetaGameSave.TimeUntilNextLife);
    }

    private void RefreshShopPowerUpCount(TMP_Text text, string path)
    {
        string tag = DetectPowerUpTag(path);

        if (!string.IsNullOrWhiteSpace(tag))
            text.text = MetaGameSave.GetPowerUpCount(tag).ToString();
    }

    private void RefreshButtons()
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string name = button.name;

            if (name.Contains("RestartGameButton", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Play", StringComparison.OrdinalIgnoreCase)
                || name.Contains("StartGame", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Start", StringComparison.OrdinalIgnoreCase))
            {
                button.interactable = MetaGameSave.Lives > 0;
            }

            if (name == "OpenStarterPack" || GetPath(button.transform).Contains("StarterPack", StringComparison.OrdinalIgnoreCase))
                button.gameObject.SetActive(MetaGameSave.ShouldShowStarterPack());

            if (IsSoftCurrencyBuyButton(button))
                button.interactable = MetaGameSave.SoftCurrency >= ReadPrice(button.transform);
        }
    }

    private void RefreshSettingsButtons()
    {
        RefreshToggleButton("MusicButton", AudioPreferences.MusicEnabled);
        RefreshToggleButton("SoundButton", AudioPreferences.SoundEnabled);
    }

    private void RefreshToggleButton(string name, bool enabled)
    {
        Button button = FindNamed<Button>(name);

        if (button == null)
            return;

        Image[] images = button.GetComponentsInChildren<Image>(true);

        if (images.Length >= 2)
        {
            images[0].gameObject.SetActive(enabled);
            images[1].gameObject.SetActive(!enabled);
        }
    }

    private void RefreshTabButtons()
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!button.name.StartsWith("Button", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!button.TryGetComponent(out Image image))
                continue;

            string tab = button.name.Replace("Button", string.Empty);
            Color normalColor = image.color;
            normalButtonColors.TryGetValue(button, out normalColor);
            image.color = tab.Equals(activeShopTab, StringComparison.OrdinalIgnoreCase)
                ? activeTabColor
                : normalColor;
        }
    }

    private void RefreshSelectedImages()
    {
        foreach (GameObject selected in FindSceneObjects())
        {
            if (selected.name != "SelectedImage")
                continue;

            string path = GetPath(selected.transform.parent);
            string category = DetectSkinCategory(path);
            string id = DetectSkinId(path);
            selected.SetActive(!string.IsNullOrEmpty(category) && MetaGameSave.GetSelectedSkin(category) == id);
        }
    }

    private void SetShowcaseActive(bool active)
    {
        GameObject showcase = FindSceneObject("Showcase");
        Transform container = showcase != null ? FindChild(showcase.transform, "Container") : null;

        if (container != null)
            container.gameObject.SetActive(active);
    }

    private void ShowShowcaseForButton(Button button)
    {
        if (button == null)
            return;

        string path = GetPath(button.transform);

        if (path.Contains("Heart", StringComparison.OrdinalIgnoreCase)
            || path.Contains("HardCurrency", StringComparison.OrdinalIgnoreCase)
            || path.Contains("Gold", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Transform container = GetShowcaseContainer();

        if (container == null)
            return;

        string targetName = DetectShowcaseName(path);

        if (string.IsNullOrWhiteSpace(targetName))
            return;

        foreach (Transform child in container)
            child.gameObject.SetActive(IsShowcaseMatch(child.name, targetName));
    }

    private Transform GetShowcaseContainer()
    {
        GameObject showcase = FindSceneObject("Showcase");
        return showcase != null ? FindChild(showcase.transform, "Container") : null;
    }

    private void RefreshBackgroundTarget()
    {
        if (!IsMenuScene())
            return;

        if (backgroundImage == null)
            backgroundImage = FindBackgroundImage();

        if (panels.TryGetValue("Leaderboards", out GameObject leaderboard) && leaderboard.activeInHierarchy)
            targetBackgroundColor = leaderboardColor;
        else if (panels.TryGetValue("StarterPack", out GameObject starterPack) && starterPack.activeInHierarchy)
            targetBackgroundColor = starterPackColor;
        else if (panels.TryGetValue("Shop", out GameObject shop) && shop.activeInHierarchy)
            targetBackgroundColor = shopColor;
        else
            targetBackgroundColor = menuColor;
    }

    private void SetShopTabPanels(string tab)
    {
        GameObject shop = FindSceneObject("Shop");

        if (shop == null)
            return;

        foreach (Transform child in shop.GetComponentsInChildren<Transform>(true))
        {
            if (child == shop.transform)
                continue;

            string name = child.name;
            bool isKnownTabPanel = name.Equals("PoolSticks", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Boards", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Balls", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Powerups", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Lives", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Gold", StringComparison.OrdinalIgnoreCase)
                || name.Equals("PoolSticksPanel", StringComparison.OrdinalIgnoreCase)
                || name.Equals("BoardsPanel", StringComparison.OrdinalIgnoreCase)
                || name.Equals("BallsPanel", StringComparison.OrdinalIgnoreCase)
                || name.Equals("PowerupsPanel", StringComparison.OrdinalIgnoreCase)
                || name.Equals("LivesPanel", StringComparison.OrdinalIgnoreCase)
                || name.Equals("GoldPanel", StringComparison.OrdinalIgnoreCase);

            if (!isKnownTabPanel)
                continue;

            child.gameObject.SetActive(name.Equals(tab, StringComparison.OrdinalIgnoreCase)
                || name.Equals(tab + "Panel", StringComparison.OrdinalIgnoreCase));
        }
    }

    private void RefreshLanguageButtonText()
    {
        Button button = FindNamed<Button>("LanguageButton");

        if (button == null)
            return;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);

        if (text != null)
            text.text = MetaGameLocalization.CurrentLanguageLabel;
    }

    private TimeSpan GetLoopTimer(string saveKey, TimeSpan duration)
    {
        long startTicks = MetaGameSave.SaveSystem.GetLong(saveKey, 0L);
        DateTime now = DateTime.UtcNow;

        if (startTicks <= 0L)
        {
            startTicks = now.Ticks;
            MetaGameSave.SaveSystem.SetLong(saveKey, startTicks);
            MetaGameSave.SaveSystem.Save();
        }

        TimeSpan elapsed = now - new DateTime(startTicks, DateTimeKind.Utc);

        if (elapsed >= duration)
        {
            long cycles = (long)(elapsed.Ticks / duration.Ticks);
            startTicks += cycles * duration.Ticks;
            MetaGameSave.SaveSystem.SetLong(saveKey, startTicks);
            MetaGameSave.SaveSystem.Save();
            elapsed = now - new DateTime(startTicks, DateTimeKind.Utc);
        }

        TimeSpan remaining = duration - elapsed;
        return remaining > TimeSpan.Zero ? remaining : duration;
    }

    private static Image FindBackgroundImage()
    {
        foreach (Image image in FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (image.name == "BackgroundImage" && GetPath(image.transform).Contains("BackgroundCanvas", StringComparison.OrdinalIgnoreCase))
                return image;
        }

        return FindNamed<Image>("BackgroundImage");
    }

    private static bool IsSoftCurrencyBuyButton(Button button)
    {
        string path = GetPath(button.transform);

        if (!button.name.Contains("Buy", StringComparison.OrdinalIgnoreCase) && !HasChild(button.transform, "PriceText"))
            return false;

        return !path.Contains("Hard", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("Gold", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("RealMoney", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("StarterPack", StringComparison.OrdinalIgnoreCase);
    }

    private static string DetectShowcaseName(string path)
    {
        string[] knownNames =
        {
            "BluePoolStick", "GreenPoolStick", "PinkPoolStick", "PurplePoolStick", "RedPoolStick", "YellowPoolStick", "DefaultPoolStick",
            "BoardBlue", "BoardPurple", "BoardRed", "BoardDefault",
            "PoolBalls", "BallsDefault", "BallsSkin1", "BallsSkin2", "BallsSkin3", "BallsSkin4", "BallsSkin5",
            "Fire", "Earth", "Water", "Wind"
        };

        foreach (string knownName in knownNames)
        {
            if (path.Contains(knownName, StringComparison.OrdinalIgnoreCase))
                return knownName;
        }

        string lower = path.ToLowerInvariant();

        if (lower.Contains("poolstick_blue") || (lower.Contains("poolstick") && lower.Contains("blue")))
            return "BluePoolStick";
        if (lower.Contains("poolstick_green") || (lower.Contains("poolstick") && lower.Contains("green")))
            return "GreenPoolStick";
        if (lower.Contains("poolstick_pink") || (lower.Contains("poolstick") && lower.Contains("pink")))
            return "PinkPoolStick";
        if (lower.Contains("poolstick_purple") || (lower.Contains("poolstick") && lower.Contains("purple")))
            return "PurplePoolStick";
        if (lower.Contains("poolstick_red") || (lower.Contains("poolstick") && lower.Contains("red")))
            return "RedPoolStick";
        if (lower.Contains("poolstick_yellow") || (lower.Contains("poolstick") && lower.Contains("yellow")))
            return "YellowPoolStick";
        if (lower.Contains("poolstick_default") || (lower.Contains("poolstick") && lower.Contains("default")))
            return "DefaultPoolStick";
        if (lower.Contains("board_blue") || (lower.Contains("board") && lower.Contains("blue")))
            return "BoardBlue";
        if (lower.Contains("board_purple") || (lower.Contains("board") && lower.Contains("purple")))
            return "BoardPurple";
        if (lower.Contains("board_red") || (lower.Contains("board") && lower.Contains("red")))
            return "BoardRed";
        if (lower.Contains("board_default") || (lower.Contains("board") && lower.Contains("default")))
            return "BoardDefault";
        if (lower.Contains("balls_skin1"))
            return "BallsSkin1";
        if (lower.Contains("balls_skin2"))
            return "BallsSkin2";
        if (lower.Contains("balls_skin3"))
            return "BallsSkin3";
        if (lower.Contains("balls_skin4"))
            return "BallsSkin4";
        if (lower.Contains("balls_skin5"))
            return "BallsSkin5";
        if (lower.Contains("balls_default") || lower.Contains("poolballs"))
            return "PoolBalls";
        if (lower.Contains("fire"))
            return "Fire";
        if (lower.Contains("earth"))
            return "Earth";
        if (lower.Contains("water"))
            return "Water";
        if (lower.Contains("wind") || lower.Contains("air"))
            return "Wind";

        return string.Empty;
    }

    private static bool IsShowcaseMatch(string childName, string targetName)
    {
        string child = NormalizeShowcaseName(childName);
        string target = NormalizeShowcaseName(targetName);
        return child.Equals(target, StringComparison.OrdinalIgnoreCase)
            || child.Contains(target, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeShowcaseName(string value)
    {
        return value.Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace("(Clone)", string.Empty)
            .Trim();
    }

    private static bool IsMenuScene()
    {
        return SceneManager.GetActiveScene().name.Contains("Menu", StringComparison.OrdinalIgnoreCase);
    }

    private static int ReadPrice(Transform root)
    {
        TMP_Text price = FindChildComponent<TMP_Text>(root, "PriceText");

        if (price == null)
            return 0;

        string digits = string.Empty;

        foreach (char character in price.text)
        {
            if (char.IsDigit(character))
                digits += character;
        }

        return int.TryParse(digits, out int parsed) ? parsed : 0;
    }

    private static string DetectSkinCategory(string path)
    {
        if (path.Contains("PoolStick", StringComparison.OrdinalIgnoreCase))
            return "poolstick";

        if (path.Contains("Board", StringComparison.OrdinalIgnoreCase))
            return "board";

        if (path.Contains("Ball", StringComparison.OrdinalIgnoreCase))
            return "balls";

        return string.Empty;
    }

    private static string DetectSkinId(string path)
    {
        string lower = path.ToLowerInvariant();

        if (lower.Contains("default"))
            return "default";

        string[] names = { "red", "blue", "green", "pink", "purple", "yellow", "skin1", "skin2", "skin3", "skin4", "skin5" };

        foreach (string name in names)
        {
            if (lower.Contains(name.ToLowerInvariant()))
                return name.ToLowerInvariant();
        }

        return "default";
    }

    private static string DetectPowerUpTag(string path)
    {
        string lower = path.ToLowerInvariant();

        if (lower.Contains("fire"))
            return "fire";
        if (lower.Contains("earth"))
            return "earth";
        if (lower.Contains("water"))
            return "water";
        if (lower.Contains("wind") || lower.Contains("air"))
            return "wind";

        return string.Empty;
    }

    private static bool HasChild(Transform root, string childName)
    {
        return FindChild(root, childName) != null;
    }

    private static T FindChildComponent<T>(Transform root, string childName)
        where T : Component
    {
        Transform child = FindChild(root, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindChild(Transform root, string childName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private static T FindNamed<T>(string name)
        where T : Component
    {
        GameObject found = FindSceneObject(name);
        return found != null ? found.GetComponent<T>() : null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (GameObject candidate in FindSceneObjects())
        {
            if (candidate.name == objectName)
                return candidate;
        }

        return null;
    }

    private static IEnumerable<GameObject> FindSceneObjects()
    {
        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate.hideFlags == HideFlags.None && candidate.scene.IsValid())
                yield return candidate;
        }
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

    private static void SimulatePurchasePopup()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Application.OpenURL("market://details?id=com.fake.purchase");
#else
        Debug.Log("Simulated Google Play purchase popup.");
#endif
    }
}
