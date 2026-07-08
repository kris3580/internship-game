using UnityEngine;

public sealed class FPSUnlock : MonoBehaviour
{
    private const string HighFpsPlayerPrefsKey = "settings.high_fps";

    [SerializeField] private bool highFpsDefault = true;
    [SerializeField] private bool disableVSync = true;
    [SerializeField] private int fallbackTargetFrameRate = 60;
    [SerializeField] private int batterySaverTargetFrameRate = 30;
    [SerializeField] private bool logTargetFrameRate;

    private void Awake()
    {
        Apply();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            Apply();
    }

    public void SetHighFpsEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(HighFpsPlayerPrefsKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        Apply();
    }

    public void Apply()
    {
        if (disableVSync)
            QualitySettings.vSyncCount = 0;

        Application.targetFrameRate = GetTargetFrameRate();

        if (logTargetFrameRate)
            Debug.Log($"Target FPS: {Application.targetFrameRate}", this);
    }

    private int GetTargetFrameRate()
    {
        if (!IsHighFpsEnabled())
            return Mathf.Max(1, batterySaverTargetFrameRate);

        int refreshRate = GetScreenRefreshRate();
        return refreshRate > 0 ? refreshRate : Mathf.Max(1, fallbackTargetFrameRate);
    }

    private bool IsHighFpsEnabled()
    {
        int defaultValue = highFpsDefault ? 1 : 0;
        return PlayerPrefs.GetInt(HighFpsPlayerPrefsKey, defaultValue) == 1;
    }

    private static int GetScreenRefreshRate()
    {
        double refreshRate = Screen.currentResolution.refreshRateRatio.value;
        return refreshRate > 0d ? Mathf.RoundToInt((float)refreshRate) : 0;
    }

    private void OnValidate()
    {
        fallbackTargetFrameRate = Mathf.Max(1, fallbackTargetFrameRate);
        batterySaverTargetFrameRate = Mathf.Max(1, batterySaverTargetFrameRate);
    }
}
