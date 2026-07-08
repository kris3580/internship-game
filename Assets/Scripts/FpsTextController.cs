using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class FpsTextController : MonoBehaviour
{
    private const string FpsTextName = "FPSText";
    private const string GameSceneName = "Game";

    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private float refreshInterval = 0.25f;
    [SerializeField] private float missingTextRetryInterval = 1f;

    private float elapsed;
    private float nextResolveTime;
    private int frames;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ResolveText();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (fpsText == null)
        {
            if (SceneManager.GetActiveScene().name != GameSceneName || Time.unscaledTime < nextResolveTime)
                return;

            nextResolveTime = Time.unscaledTime + missingTextRetryInterval;
            ResolveText();
        }

        if (fpsText == null)
            return;

        elapsed += Time.unscaledDeltaTime;
        frames++;

        if (elapsed < refreshInterval)
            return;

        int fps = Mathf.RoundToInt(frames / elapsed);
        fpsText.text = "FPS:" + fps;
        elapsed = 0f;
        frames = 0;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        fpsText = null;
        elapsed = 0f;
        frames = 0;

        if (scene.name == GameSceneName)
            ResolveText();
    }

    private void ResolveText()
    {
        foreach (TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text != null && text.name == FpsTextName)
            {
                fpsText = text;
                fpsText.text = "FPS0";
                return;
            }
        }
    }

    private void OnValidate()
    {
        refreshInterval = Mathf.Max(0.05f, refreshInterval);
        missingTextRetryInterval = Mathf.Max(0.1f, missingTextRetryInterval);
    }
}
