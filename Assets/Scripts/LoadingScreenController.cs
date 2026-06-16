using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "Game";
    [SerializeField] private float minimumDisplaySeconds = 2f;
    [SerializeField] private Image progressFill;

    private float displayedProgress;
    [InjectOptional] private ISceneLoader sceneLoader;

    private void Awake()
    {
        if (progressFill == null)
            progressFill = GetComponentInChildren<Image>(true);

        SetProgress(0f);
    }

    private void Start()
    {
        StartCoroutine(LoadTargetScene());
    }

    /// <summary>
    /// Loads the target scene while keeping the loading screen visible for the configured minimum time.
    /// </summary>
    public IEnumerator LoadTargetScene()
    {
        AsyncOperation loadOperation = sceneLoader != null
            ? sceneLoader.LoadSceneAsync(targetSceneName)
            : UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(targetSceneName);

        if (loadOperation == null)
        {
            Debug.LogError($"Could not start loading scene '{targetSceneName}'.", this);
            yield break;
        }

        loadOperation.allowSceneActivation = false;

        displayedProgress = 0f;
        float elapsed = 0f;

        while (!loadOperation.isDone)
        {
            elapsed += Time.deltaTime;

            float sceneProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);
            float timeLimit = GetStagedTimeLimit(elapsed);
            float targetProgress = Mathf.Min(sceneProgress, timeLimit);

            AdvanceVisibleProgress(targetProgress);

            if (elapsed >= minimumDisplaySeconds && loadOperation.progress >= 0.9f)
            {
                SetProgress(1f);
                loadOperation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void AdvanceVisibleProgress(float targetProgress)
    {
        if (targetProgress <= displayedProgress)
            return;

        float speed = GetProgressSpeed(displayedProgress);
        displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, speed * Time.deltaTime);
        SetProgress(displayedProgress);
    }

    private float GetStagedTimeLimit(float elapsed)
    {
        if (minimumDisplaySeconds <= 0f)
            return 1f;

        float time = Mathf.Clamp01(elapsed / minimumDisplaySeconds);

        if (time < 0.1f)
            return SmoothStep01(time / 0.1f) * 0.16f;

        if (time < 0.16f)
            return 0.16f;

        if (time < 0.34f)
            return 0.16f + SmoothStep01((time - 0.16f) / 0.18f) * 0.27f;

        if (time < 0.42f)
            return 0.43f;

        if (time < 0.62f)
            return 0.43f + SmoothStep01((time - 0.42f) / 0.2f) * 0.2f;

        if (time < 0.68f)
            return 0.63f;

        if (time < 0.84f)
            return 0.63f + SmoothStep01((time - 0.68f) / 0.16f) * 0.2f;

        if (time < 0.9f)
            return 0.83f;

        return 0.83f + SmoothStep01((time - 0.9f) / 0.1f) * 0.17f;
    }

    private float GetProgressSpeed(float progress)
    {
        if (progress < 0.18f)
            return 1.6f;

        if (progress < 0.42f)
            return 0.95f;

        if (progress < 0.64f)
            return 0.55f;

        if (progress < 0.82f)
            return 0.38f;

        return 0.8f;
    }

    private float SmoothStep01(float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        return clampedValue * clampedValue * (3f - 2f * clampedValue);
    }

    private void SetProgress(float progress)
    {
        displayedProgress = Mathf.Clamp01(progress);

        if (progressFill != null)
            progressFill.fillAmount = displayedProgress;
    }

    private void OnValidate()
    {
        minimumDisplaySeconds = Mathf.Max(0f, minimumDisplaySeconds);
    }
}
