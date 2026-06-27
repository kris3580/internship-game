using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public sealed class FatePointsManager : MonoBehaviour
{
    [Serializable]
    private sealed class FateBall
    {
        public string tag;
        public int fatePoints;
        public bool inRotation;

        [HideInInspector] public GameObject element;
        [HideInInspector] public RectTransform progressBar;
        [HideInInspector] public TMP_Text fpText;
        [HideInInspector] public Image iconImage;
        [HideInInspector] public float fullProgressWidth;
        [HideInInspector] public int closestIslandNeed;
        [HideInInspector] public float effectiveWeight;
    }

    private const string PlusTag = "+";
    private const string MinusTag = "-";

    [Header("Panel")]
    [SerializeField] private GameObject fatePointsPanel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button resumeGameButton;
    [SerializeField] private bool pauseGameWhenOpen = true;

    [Header("Rotation")]
    [SerializeField] private List<FateBall> balls = new()
    {
        new FateBall { tag = "2", fatePoints = 1, inRotation = true },
        new FateBall { tag = "3", fatePoints = 1, inRotation = true },
        new FateBall { tag = "4", fatePoints = 2, inRotation = true },
        new FateBall { tag = "5", fatePoints = 3, inRotation = true },
        new FateBall { tag = "6", fatePoints = 3, inRotation = false },
        new FateBall { tag = "7", fatePoints = 4, inRotation = false },
        new FateBall { tag = "8", fatePoints = 4, inRotation = false },
    };
    [SerializeField] private int minimumFatePoints = 1;
    [SerializeField] private List<int> randomBallJoinDestructionCounts = new() { 0, 0, 0, 0, 10, 40, 80 };

    [Header("Manipulative Randomness")]
    [SerializeField] private bool useSubtleIslandBias = true;
    [SerializeField] private int islandBiasNeedThreshold = 2;
    [SerializeField, Range(0f, 1f)] private float immediateIslandChance = 0.18f;
    [SerializeField] private float oneAwayBonusWeight = 0.75f;
    [SerializeField] private float twoAwayBonusWeight = 0.35f;
    [SerializeField] private string mostLikelyBallPreview;
    [SerializeField] private List<string> likelihoodReport = new();

    [Header("Modifier Balls")]
    [SerializeField] private bool enableModifierBalls = true;
    [SerializeField] private int modifierBallsAfterSentCount = 20;
    [SerializeField] private int modifierBallMinSentGap = 7;
    [SerializeField] private int modifierBallMaxSentGap = 14;

    [Header("Feedback")]
    [SerializeField] private Animator newBallJoinAnimator;
    [SerializeField] private string newBallJoinAnimationStateName = "NewBallText";
    [SerializeField] private Image newBallJoinSmallImage;
    [SerializeField] private Image newBallJoinBigImage;
    [SerializeField] private Animator controlFateTextAnimator;
    [SerializeField] private string controlFateAnimationStateName = "ControlFateText";
    [SerializeField] private AudioClip feedbackSwooshClip;
    [SerializeField] private float newBallSwooshDelay = 0.08f;
    [SerializeField] private float controlFateSwooshDelay = 0.08f;
    [SerializeField, Range(0f, 1f)] private float feedbackSwooshVolume = 1f;
    [SerializeField] private ComboBackgroundColorController backgroundColorController;

    private readonly Dictionary<string, FateBall> ballsByTag = new();
    private GameScoreManager scoreManager;
    private IslandManager islandManager;
    private AudioSource feedbackAudioSource;
    private int sentBalls;
    private int nextModifierSentCount;
    private float previousTimeScale = 1f;
    private bool panelOpen;
    private bool unlockFeedbackReady;
    [InjectOptional] private IGameStateMachine gameStateMachine;

    public event Action Changed;

    private void Awake()
    {
        ResolveReferences();
        RebuildLookup();
        RefreshRotationFromDestructions(false);
        ScheduleNextModifierBall();
        RefreshUi();
    }

    private void Start()
    {
        unlockFeedbackReady = true;
    }

    private void OnEnable()
    {
        ResolveReferences();
        AddListeners();

        if (scoreManager != null)
            scoreManager.DestructionsChanged += OnDestructionsChanged;
    }

    private void OnDisable()
    {
        RemoveListeners();

        if (scoreManager != null)
            scoreManager.DestructionsChanged -= OnDestructionsChanged;
    }

    public string PickNextTag()
    {
        RefreshRotationFromDestructions(false);
        RefreshIslandBias();

        string completingTag = TryPickImmediateIslandCompletion();

        if (!string.IsNullOrWhiteSpace(completingTag))
        {
            RefreshInspectorReport(completingTag);
            RefreshUi();
            return completingTag;
        }

        if (ShouldSpawnModifierBall())
        {
            return UnityEngine.Random.value < 0.5f ? PlusTag : MinusTag;
        }

        float totalWeight = 0f;

        foreach (FateBall ball in balls)
        {
            if (!ball.inRotation)
                continue;

            ball.effectiveWeight = GetEffectiveWeight(ball);
            totalWeight += ball.effectiveWeight;
        }

        if (totalWeight <= 0f)
            return "2";

        float roll = UnityEngine.Random.Range(0f, totalWeight);

        foreach (FateBall ball in balls)
        {
            if (!ball.inRotation)
                continue;

            roll -= ball.effectiveWeight;

            if (roll <= 0f)
            {
                RefreshInspectorReport(ball.tag);
                RefreshUi();
                return ball.tag;
            }
        }

        return "2";
    }

    public void PlaySpawnFeedback(string ballTag)
    {
        if (ballTag == PlusTag || ballTag == MinusTag)
            PlayControlFateAnimation();
    }

    public void RegisterSentBall(string ballTag)
    {
        if (string.IsNullOrWhiteSpace(ballTag) || ballTag == PlusTag || ballTag == MinusTag)
            return;

        sentBalls++;

        if (enableModifierBalls &&
            sentBalls >= modifierBallsAfterSentCount &&
            nextModifierSentCount <= 0)
            ScheduleNextModifierBall();
    }

    public void AdjustFatePoints(string ballTag, int delta)
    {
        if (string.IsNullOrWhiteSpace(ballTag))
            return;

        RebuildLookup();

        if (!ballsByTag.TryGetValue(ballTag, out FateBall ball))
            return;

        ball.fatePoints = Mathf.Max(minimumFatePoints, ball.fatePoints + delta);
        ball.inRotation = true;
        RefreshInspectorReport(ballTag);
        RefreshUi();
        Changed?.Invoke();
    }

    public bool IsInRotation(string ballTag)
    {
        RebuildLookup();
        return ballsByTag.TryGetValue(ballTag, out FateBall ball) && ball.inRotation;
    }

    private void OpenPanel()
    {
        ResolveReferences();
        RefreshUi();

        if (fatePointsPanel != null)
            fatePointsPanel.SetActive(true);

        if (pauseGameWhenOpen && !panelOpen)
        {
            previousTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            Time.timeScale = 0f;
        }

        panelOpen = true;
    }

    private void ClosePanel()
    {
        if (fatePointsPanel != null)
            fatePointsPanel.SetActive(false);

        if (pauseGameWhenOpen && panelOpen)
            Time.timeScale = previousTimeScale;

        panelOpen = false;
    }

    private bool ShouldSpawnModifierBall()
    {
        if (!enableModifierBalls || nextModifierSentCount <= 0 || sentBalls < nextModifierSentCount)
            return false;

        ScheduleNextModifierBall();
        return true;
    }

    private void RefreshRotationFromDestructions(bool playFeedback)
    {
        int destructions = scoreManager != null ? scoreManager.Destructions : 0;
        int targetRotationCount = GetRotationCountForDestructions(destructions);
        int currentRotationCount = CountBallsInRotation();

        while (currentRotationCount < targetRotationCount)
        {
            FateBall ball = PickRandomInactiveBall();

            if (ball == null)
                break;

            ball.inRotation = true;
            ball.fatePoints = Mathf.Max(minimumFatePoints, ball.fatePoints);
            currentRotationCount++;

            if (playFeedback)
                PlayNewBallJoinAnimation(ball);
        }
    }

    private int GetRotationCountForDestructions(int destructions)
    {
        if (randomBallJoinDestructionCounts == null || randomBallJoinDestructionCounts.Count == 0)
            return CountBallsInRotation();

        int count = 0;

        foreach (int threshold in randomBallJoinDestructionCounts)
        {
            if (destructions >= threshold)
                count++;
        }

        return Mathf.Clamp(count, 0, balls.Count);
    }

    private int CountBallsInRotation()
    {
        int count = 0;

        foreach (FateBall ball in balls)
        {
            if (ball.inRotation)
                count++;
        }

        return count;
    }

    private FateBall PickRandomInactiveBall()
    {
        List<FateBall> inactive = new();

        foreach (FateBall ball in balls)
        {
            if (!ball.inRotation)
                inactive.Add(ball);
        }

        return inactive.Count > 0 ? inactive[UnityEngine.Random.Range(0, inactive.Count)] : null;
    }

    private void OnDestructionsChanged(int destructions)
    {
        RefreshRotationFromDestructions(unlockFeedbackReady);
        RefreshUi();
    }

    private void RefreshUi()
    {
        ResolveReferences();
        RebuildLookup();
        RefreshIslandBias();

        float totalFatePoints = 0f;

        foreach (FateBall ball in balls)
        {
            if (ball.inRotation)
                totalFatePoints += Mathf.Max(minimumFatePoints, ball.fatePoints);
        }

        foreach (FateBall ball in balls)
        {
            if (ball.element == null)
                continue;

            ball.element.SetActive(ball.inRotation);

            if (!ball.inRotation)
                continue;

            if (ball.fpText != null)
                ball.fpText.text = $"{ball.fatePoints} FP";

            if (ball.progressBar != null)
            {
                float ratio = totalFatePoints > 0f ? ball.fatePoints / totalFatePoints : 0f;
                Vector2 size = ball.progressBar.sizeDelta;
                size.x = Mathf.Max(1f, ball.fullProgressWidth * ratio);
                ball.progressBar.sizeDelta = size;
            }
        }

        RefreshInspectorReport();
    }

    private void RefreshIslandBias()
    {
        if (islandManager == null)
            islandManager = FindFirstObjectByType<IslandManager>();

        foreach (FateBall ball in balls)
        {
            ball.closestIslandNeed = islandManager != null
                ? islandManager.GetClosestIslandNeed(ball.tag)
                : int.MaxValue;
            ball.effectiveWeight = GetEffectiveWeight(ball);
        }
    }

    private float GetEffectiveWeight(FateBall ball)
    {
        float weight = Mathf.Max(minimumFatePoints, ball.fatePoints);

        if (!useSubtleIslandBias || ball.closestIslandNeed <= 0 || ball.closestIslandNeed > islandBiasNeedThreshold)
            return weight;

        if (ball.closestIslandNeed == 1)
            return weight + oneAwayBonusWeight;

        return weight + twoAwayBonusWeight;
    }

    private string TryPickImmediateIslandCompletion()
    {
        if (!useSubtleIslandBias || immediateIslandChance <= 0f || UnityEngine.Random.value > immediateIslandChance)
            return null;

        List<string> completingTags = new();

        foreach (FateBall ball in balls)
        {
            if (ball.inRotation && ball.closestIslandNeed == 1)
                completingTags.Add(ball.tag);
        }

        return completingTags.Count > 0
            ? completingTags[UnityEngine.Random.Range(0, completingTags.Count)]
            : null;
    }

    private void ScheduleNextModifierBall()
    {
        if (!enableModifierBalls)
        {
            nextModifierSentCount = 0;
            return;
        }

        int minGap = Mathf.Max(1, modifierBallMinSentGap);
        int maxGap = Mathf.Max(minGap, modifierBallMaxSentGap);
        nextModifierSentCount = Mathf.Max(modifierBallsAfterSentCount, sentBalls + UnityEngine.Random.Range(minGap, maxGap + 1));
    }

    private void PlayNewBallJoinAnimation(FateBall ball)
    {
        SetNewBallJoinSprites(ball);
        ChangeBackgroundColor();
        PlayAnimatorState(newBallJoinAnimator, newBallJoinAnimationStateName);
        PlayFeedbackSwoosh(newBallSwooshDelay);
    }

    private void PlayControlFateAnimation()
    {
        ChangeBackgroundColor();
        PlayAnimatorState(controlFateTextAnimator, controlFateAnimationStateName);
        PlayFeedbackSwoosh(controlFateSwooshDelay);
    }

    private void ChangeBackgroundColor()
    {
        if (backgroundColorController == null)
            backgroundColorController = FindFirstObjectByType<ComboBackgroundColorController>();

        backgroundColorController?.AdvanceColor();
    }

    private void PlayFeedbackSwoosh(float delay)
    {
        if (feedbackSwooshClip == null || !AudioPreferences.SoundEnabled)
            return;

        if (delay <= 0f)
        {
            PlayFeedbackSwooshNow();
            return;
        }

        StartCoroutine(PlayFeedbackSwooshAfterDelay(delay));
    }

    private IEnumerator PlayFeedbackSwooshAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayFeedbackSwooshNow();
    }

    private void PlayFeedbackSwooshNow()
    {
        if (feedbackSwooshClip != null && AudioPreferences.SoundEnabled)
        {
            EnsureFeedbackAudioSource();
            feedbackAudioSource.PlayOneShot(feedbackSwooshClip, feedbackSwooshVolume);
        }
    }

    private void EnsureFeedbackAudioSource()
    {
        if (feedbackAudioSource != null)
            return;

        feedbackAudioSource = GetComponent<AudioSource>();

        if (feedbackAudioSource == null)
            feedbackAudioSource = gameObject.AddComponent<AudioSource>();

        feedbackAudioSource.playOnAwake = false;
        feedbackAudioSource.loop = false;
        feedbackAudioSource.spatialBlend = 0f;
        feedbackAudioSource.volume = 1f;
    }

    private static void PlayAnimatorState(Animator animator, string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return;

        if (!animator.gameObject.activeSelf)
            animator.gameObject.SetActive(true);

        animator.Play(stateName, 0, 0f);
    }

    private void SetNewBallJoinSprites(FateBall ball)
    {
        if (ball == null)
            return;

        ResolveBallUi(ball);

        Sprite sprite = ball.iconImage != null ? ball.iconImage.sprite : null;

        if (sprite == null)
            return;

        if (newBallJoinSmallImage != null)
            newBallJoinSmallImage.sprite = sprite;

        if (newBallJoinBigImage != null)
            newBallJoinBigImage.sprite = sprite;
    }

    private void RefreshInspectorReport(string pickedTag = null)
    {
        likelihoodReport.Clear();
        FateBall best = null;

        foreach (FateBall ball in balls)
        {
            if (!ball.inRotation)
                continue;

            if (best == null || ball.effectiveWeight > best.effectiveWeight)
                best = ball;

            string needText = ball.closestIslandNeed == int.MaxValue
                ? "no island"
                : ball.closestIslandNeed <= 0
                    ? "ready"
                    : $"{ball.closestIslandNeed} away";
            likelihoodReport.Add($"Ball {ball.tag}: FP {ball.fatePoints}, effective {ball.effectiveWeight:0.##}, {needText}");
        }

        mostLikelyBallPreview = best != null
            ? $"Ball {best.tag} ({best.effectiveWeight:0.##} weight){(string.IsNullOrWhiteSpace(pickedTag) ? string.Empty : $"; picked {pickedTag}")}"
            : "No balls in rotation";
    }

    private void ResolveReferences()
    {
        if (fatePointsPanel == null)
            fatePointsPanel = FindSceneObject("FatePointsPanel");

        if (openButton == null)
            openButton = FindButton("FPOpenButton");

        if (resumeGameButton == null && fatePointsPanel != null)
            resumeGameButton = FindButtonInChildren(fatePointsPanel, "ResumeGameButton");

        if (scoreManager == null)
            scoreManager = FindFirstObjectByType<GameScoreManager>();

        if (islandManager == null)
            islandManager = FindFirstObjectByType<IslandManager>();

        if (newBallJoinAnimator == null)
            newBallJoinAnimator = FindAnimator("NewBallJoinsTheBoard", "NewBallJoinsTheBoardText");

        if (newBallJoinSmallImage == null)
            newBallJoinSmallImage = FindImage("NewBallSpriteSmall");

        if (newBallJoinBigImage == null)
            newBallJoinBigImage = FindImage("NewBallSpriteBig");

        if (controlFateTextAnimator == null)
            controlFateTextAnimator = FindAnimator("ControlFate", "ControlFateText");

        if (backgroundColorController == null)
            backgroundColorController = FindFirstObjectByType<ComboBackgroundColorController>();

        foreach (FateBall ball in balls)
            ResolveBallUi(ball);

        if (fatePointsPanel != null && !panelOpen)
            fatePointsPanel.SetActive(false);
    }

    private void ResolveBallUi(FateBall ball)
    {
        if (string.IsNullOrWhiteSpace(ball.tag))
            return;

        if (ball.element == null)
            ball.element = FindSceneObject($"FPElementGroupBall{ball.tag}");

        if (ball.element == null)
            return;

        if (ball.progressBar == null)
        {
            GameObject progressObject = FindChild(ball.element.transform, "ProgressBar");
            ball.progressBar = progressObject != null ? progressObject.GetComponent<RectTransform>() : null;

            if (ball.progressBar != null && ball.fullProgressWidth <= 0f)
                ball.fullProgressWidth = ball.progressBar.sizeDelta.x;
        }

        if (ball.fpText == null)
        {
            GameObject textObject = FindChild(ball.element.transform, "FPCountText");
            ball.fpText = textObject != null ? textObject.GetComponent<TMP_Text>() : null;
        }

        if (ball.iconImage == null)
        {
            GameObject iconObject = FindChild(ball.element.transform, "BallIcon");
            ball.iconImage = iconObject != null ? iconObject.GetComponent<Image>() : null;
        }
    }

    private void RebuildLookup()
    {
        ballsByTag.Clear();

        foreach (FateBall ball in balls)
        {
            if (!string.IsNullOrWhiteSpace(ball.tag))
                ballsByTag[ball.tag] = ball;
        }
    }

    private void AddListeners()
    {
        openButton?.onClick.RemoveListener(OpenPanel);
        openButton?.onClick.AddListener(OpenPanel);
        resumeGameButton?.onClick.RemoveListener(ClosePanel);
        resumeGameButton?.onClick.AddListener(ClosePanel);
    }

    private void RemoveListeners()
    {
        openButton?.onClick.RemoveListener(OpenPanel);
        resumeGameButton?.onClick.RemoveListener(ClosePanel);
    }

    private static Button FindButton(string objectName)
    {
        GameObject found = FindSceneObject(objectName);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private static Image FindImage(string objectName)
    {
        GameObject found = FindSceneObject(objectName);
        return found != null ? found.GetComponent<Image>() : null;
    }

    private static Button FindButtonInChildren(GameObject parent, string objectName)
    {
        if (parent == null)
            return null;

        GameObject found = FindChild(parent.transform, objectName);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private static GameObject FindChild(Transform parent, string objectName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child.gameObject;
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

    private void OnValidate()
    {
        minimumFatePoints = Mathf.Max(1, minimumFatePoints);
        modifierBallsAfterSentCount = Mathf.Max(0, modifierBallsAfterSentCount);
        modifierBallMinSentGap = Mathf.Max(1, modifierBallMinSentGap);
        modifierBallMaxSentGap = Mathf.Max(modifierBallMinSentGap, modifierBallMaxSentGap);
        islandBiasNeedThreshold = Mathf.Max(0, islandBiasNeedThreshold);
        oneAwayBonusWeight = Mathf.Max(0f, oneAwayBonusWeight);
        twoAwayBonusWeight = Mathf.Max(0f, twoAwayBonusWeight);
        newBallSwooshDelay = Mathf.Max(0f, newBallSwooshDelay);
        controlFateSwooshDelay = Mathf.Max(0f, controlFateSwooshDelay);

        foreach (FateBall ball in balls)
        {
            ball.fatePoints = Mathf.Max(minimumFatePoints, ball.fatePoints);
        }

        if (randomBallJoinDestructionCounts == null)
            randomBallJoinDestructionCounts = new List<int>();

        for (int i = 0; i < randomBallJoinDestructionCounts.Count; i++)
            randomBallJoinDestructionCounts[i] = Mathf.Max(0, randomBallJoinDestructionCounts[i]);
    }

    private static Animator FindAnimator(params string[] objectNames)
    {
        foreach (string objectName in objectNames)
        {
            GameObject found = FindSceneObject(objectName);
            Animator animator = found != null ? found.GetComponent<Animator>() : null;

            if (animator != null)
                return animator;
        }

        return null;
    }
}
