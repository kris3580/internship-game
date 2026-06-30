using System;
using UnityEngine;

public static class MetaGameSave
{
    private static ISaveSystem saveSystem;

    public static event Action LivesChanged;

    public static ISaveSystem SaveSystem => saveSystem ??= new JsonSaveSystem();

    public static void EnsureCurrencyDefaults()
    {
        if (SaveSystem.GetInt(MetaGameSaveKeys.CurrencyDefaultsInitialized, 0) == 2)
            return;

        SaveSystem.SetInt(MetaGameSaveKeys.SoftCurrency, 1000000);
        SaveSystem.SetInt(MetaGameSaveKeys.HardCurrency, 100000);
        SaveSystem.SetInt(MetaGameSaveKeys.CurrencyDefaultsInitialized, 2);
        SaveSystem.Save();
    }

    public static int SoftCurrency
    {
        get => SaveSystem.GetInt(MetaGameSaveKeys.SoftCurrency, 0);
        set => SetInt(MetaGameSaveKeys.SoftCurrency, Mathf.Max(0, value));
    }

    public static int HardCurrency
    {
        get => SaveSystem.GetInt(MetaGameSaveKeys.HardCurrency, 0);
        set => SetInt(MetaGameSaveKeys.HardCurrency, Mathf.Max(0, value));
    }

    public static int BestScore
    {
        get => SaveSystem.GetInt(MetaGameSaveKeys.BestScore, 0);
        set => SetInt(MetaGameSaveKeys.BestScore, Mathf.Max(BestScore, value));
    }

    public static int Lives
    {
        get
        {
            RefillLives();
            return SaveSystem.GetInt(MetaGameSaveKeys.Lives, 3);
        }
        set
        {
            int previous = SaveSystem.GetInt(MetaGameSaveKeys.Lives, 3);
            int clamped = Mathf.Max(0, value);
            SetInt(MetaGameSaveKeys.Lives, clamped);

            if (clamped < 3 && SaveSystem.GetLong(MetaGameSaveKeys.LastLifeTicks, 0L) <= 0L)
                SetLong(MetaGameSaveKeys.LastLifeTicks, DateTime.UtcNow.Ticks);

            if (clamped >= 3)
                SetLong(MetaGameSaveKeys.LastLifeTicks, 0L);

            if (previous != clamped)
                LivesChanged?.Invoke();
        }
    }

    public static TimeSpan TimeUntilNextLife
    {
        get
        {
            RefillLives();

            if (SaveSystem.GetInt(MetaGameSaveKeys.Lives, 3) >= 3)
                return TimeSpan.Zero;

            long ticks = SaveSystem.GetLong(MetaGameSaveKeys.LastLifeTicks, DateTime.UtcNow.Ticks);
            TimeSpan elapsed = DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc);
            TimeSpan remaining = TimeSpan.FromHours(1) - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    public static bool TrySpendLife()
    {
        RefillLives();

        if (Lives <= 0)
            return false;

        Lives--;
        return true;
    }

    public static int GetPowerUpCount(string tag)
    {
        return SaveSystem.GetInt(MetaGameSaveKeys.PowerUpCount(tag), 100);
    }

    public static void SetPowerUpCount(string tag, int count)
    {
        SetInt(MetaGameSaveKeys.PowerUpCount(tag), Mathf.Max(0, count));
    }

    public static void AddPowerUps(int amount)
    {
        AddPowerUp("fire", amount);
        AddPowerUp("earth", amount);
        AddPowerUp("water", amount);
        AddPowerUp("wind", amount);
    }

    public static void AddPowerUp(string tag, int amount)
    {
        SetPowerUpCount(tag, GetPowerUpCount(tag) + amount);
    }

    public static string GetSelectedSkin(string category)
    {
        string key = category switch
        {
            "poolstick" => MetaGameSaveKeys.SelectedPoolStick,
            "board" => MetaGameSaveKeys.SelectedBoard,
            "balls" => MetaGameSaveKeys.SelectedBalls,
            _ => $"skin.{category}.selected"
        };

        return SaveSystem.GetString(key, "default");
    }

    public static void SetSelectedSkin(string category, string id)
    {
        string key = category switch
        {
            "poolstick" => MetaGameSaveKeys.SelectedPoolStick,
            "board" => MetaGameSaveKeys.SelectedBoard,
            "balls" => MetaGameSaveKeys.SelectedBalls,
            _ => $"skin.{category}.selected"
        };

        SaveSystem.SetString(key, string.IsNullOrWhiteSpace(id) ? "default" : id);
        SaveSystem.Save();
    }

    public static bool IsSkinOwned(string category, string id)
    {
        return string.IsNullOrWhiteSpace(id)
            || id == "default"
            || SaveSystem.GetInt(MetaGameSaveKeys.OwnedSkin(category, id), 0) == 1;
    }

    public static void OwnSkin(string category, string id)
    {
        if (!string.IsNullOrWhiteSpace(id))
            SetInt(MetaGameSaveKeys.OwnedSkin(category, id), 1);
    }

    public static bool BuyStarterPack()
    {
        if (SaveSystem.GetInt(MetaGameSaveKeys.StarterPackBought, 0) == 1)
            return false;

        SetInt(MetaGameSaveKeys.StarterPackBought, 1);
        SetLong(MetaGameSaveKeys.StarterPackExpireTicks, DateTime.UtcNow.AddDays(1).Ticks);
        HardCurrency += 300;
        Lives = Lives + 12;
        AddPowerUps(10);
        return true;
    }

    public static bool ShouldShowStarterPack()
    {
        long expires = SaveSystem.GetLong(MetaGameSaveKeys.StarterPackExpireTicks, 0L);
        return SaveSystem.GetInt(MetaGameSaveKeys.StarterPackBought, 0) == 0
            || (expires > 0L && DateTime.UtcNow.Ticks < expires);
    }

    public static void RefillLives()
    {
        int previous = SaveSystem.GetInt(MetaGameSaveKeys.Lives, 3);
        int lives = previous;

        if (lives >= 3)
        {
            if (SaveSystem.GetLong(MetaGameSaveKeys.LastLifeTicks, 0L) != 0L)
                SetLong(MetaGameSaveKeys.LastLifeTicks, 0L);

            return;
        }

        long lastTicks = SaveSystem.GetLong(MetaGameSaveKeys.LastLifeTicks, DateTime.UtcNow.Ticks);
        DateTime last = new(lastTicks, DateTimeKind.Utc);
        TimeSpan elapsed = DateTime.UtcNow - last;

        if (elapsed.TotalHours < 1d)
            return;

        int gained = Mathf.FloorToInt((float)elapsed.TotalHours);
        lives = Mathf.Min(3, lives + gained);
        SetInt(MetaGameSaveKeys.Lives, lives);

        if (lives >= 3)
            SetLong(MetaGameSaveKeys.LastLifeTicks, 0L);
        else
            SetLong(MetaGameSaveKeys.LastLifeTicks, last.AddHours(gained).Ticks);

        if (previous != lives)
            LivesChanged?.Invoke();
    }

    public static string FormatCompact(int value)
    {
        if (value < 1000)
            return value.ToString();

        if (value < 1000000)
            return FormatCompact(value / 1000f, "K");

        return FormatCompact(value / 1000000f, "M");
    }

    public static string FormatTime(TimeSpan time)
    {
        int hours = Mathf.FloorToInt((float)time.TotalHours);
        return $"{hours:00}:{time.Minutes:00}:{time.Seconds:00}";
    }

    private static string FormatCompact(float value, string suffix)
    {
        return value >= 100f || Mathf.Approximately(value % 1f, 0f)
            ? $"{Mathf.FloorToInt(value)}{suffix}"
            : $"{value:0.0}{suffix}";
    }

    private static void SetInt(string key, int value)
    {
        SaveSystem.SetInt(key, value);
        SaveSystem.Save();
    }

    private static void SetLong(string key, long value)
    {
        SaveSystem.SetLong(key, value);
        SaveSystem.Save();
    }
}
