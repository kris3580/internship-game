using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PauseMenuController : MonoBehaviour
{
    private const string MenuSceneName = "Menu";

    [Header("Objects")]
    [SerializeField] private GameObject pauseMenu;

    [Header("Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button musicButton;
    [SerializeField] private Button soundButton;

    private Image musicDisabledImage;
    private Image soundDisabledImage;

    private void Awake()
    {
        ResolveReferences();
        AddListeners();
        SetPaused(false);
        RefreshAudioIndicators();
    }

    private void OnDestroy()
    {
        RemoveListeners();

        if (Time.timeScale == 0f)
            Time.timeScale = 1f;
    }

    public void ShowPauseMenu()
    {
        SetPaused(true);
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuSceneName);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ToggleMusic()
    {
        AudioPreferences.MusicEnabled = !AudioPreferences.MusicEnabled;
        RefreshAudioIndicators();
    }

    public void ToggleSound()
    {
        AudioPreferences.SoundEnabled = !AudioPreferences.SoundEnabled;
        RefreshAudioIndicators();
    }

    private void SetPaused(bool paused)
    {
        GameObject menu = GetPauseMenu();

        try
        {
            if (menu != null)
                menu.SetActive(paused);
        }
        catch (MissingReferenceException)
        {
            pauseMenu = FindSceneObject("PauseMenu");

            if (pauseMenu != null)
                pauseMenu.SetActive(paused);
        }

        Time.timeScale = paused ? 0f : 1f;
    }

    private void ResolveReferences()
    {
        if (pauseMenu == null)
            pauseMenu = FindSceneObject("PauseMenu");

        pauseButton ??= FindButton("PauseButton");
        resumeButton ??= FindButton("ResumeGameButton");
        mainMenuButton ??= FindButton("GoToMainMenuButton");
        restartButton ??= FindButton("RestartGameButton");
        musicButton ??= FindButton("MusicButton");
        soundButton ??= FindButton("SoundButton");

        musicDisabledImage = FindChildImage(musicButton);
        soundDisabledImage = FindChildImage(soundButton);
    }

    private void AddListeners()
    {
        pauseButton?.onClick.AddListener(ShowPauseMenu);
        resumeButton?.onClick.AddListener(ResumeGame);
        mainMenuButton?.onClick.AddListener(GoToMainMenu);
        restartButton?.onClick.AddListener(RestartGame);
        musicButton?.onClick.AddListener(ToggleMusic);
        soundButton?.onClick.AddListener(ToggleSound);
    }

    private void RemoveListeners()
    {
        pauseButton?.onClick.RemoveListener(ShowPauseMenu);
        resumeButton?.onClick.RemoveListener(ResumeGame);
        mainMenuButton?.onClick.RemoveListener(GoToMainMenu);
        restartButton?.onClick.RemoveListener(RestartGame);
        musicButton?.onClick.RemoveListener(ToggleMusic);
        soundButton?.onClick.RemoveListener(ToggleSound);
    }

    private void RefreshAudioIndicators()
    {
        if (musicDisabledImage != null)
            musicDisabledImage.gameObject.SetActive(!AudioPreferences.MusicEnabled);

        if (soundDisabledImage != null)
            soundDisabledImage.gameObject.SetActive(!AudioPreferences.SoundEnabled);
    }

    private GameObject GetPauseMenu()
    {
        if (pauseMenu == null)
            pauseMenu = FindSceneObject("PauseMenu");

        return pauseMenu;
    }

    private static Button FindButton(string objectName)
    {
        GameObject found = FindSceneObject(objectName);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private static Image FindChildImage(Button button)
    {
        if (button == null)
            return null;

        foreach (Image image in button.GetComponentsInChildren<Image>(true))
        {
            if (image.gameObject != button.gameObject)
                return image;
        }

        return null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate.name == objectName &&
                candidate.hideFlags == HideFlags.None &&
                candidate.scene.IsValid())
            {
                return candidate;
            }
        }

        return null;
    }
}
