public static class MetaGameSaveKeys
{
    public const string SoftCurrency = "currency.soft";
    public const string HardCurrency = "currency.hard";
    public const string CurrencyDefaultsInitialized = "currency.defaultsInitialized";
    public const string Lives = "lives.current";
    public const string LastLifeTicks = "lives.lastTicks";
    public const string BestScore = "score.best";
    public const string Language = "settings.language";
    public const string StarterPackBought = "starterPack.bought";
    public const string StarterPackExpireTicks = "starterPack.expireTicks";
    public const string LeaderboardTimerStartTicks = "leaderboard.timerStartTicks";
    public const string StarterPackTimerStartTicks = "starterPack.timerStartTicks";
    public const string SelectedPoolStick = "skin.poolstick.selected";
    public const string SelectedBoard = "skin.board.selected";
    public const string SelectedBalls = "skin.balls.selected";

    public static string PowerUpCount(string tag)
    {
        return $"powerup.{tag}.count";
    }

    public static string OwnedSkin(string category, string id)
    {
        return $"skin.{category}.{id}.owned";
    }
}
