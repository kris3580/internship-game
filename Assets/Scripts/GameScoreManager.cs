using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameScoreManager : MonoBehaviour
{
    private const string MoneySpriteName = "MoneySprite";

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private IslandManager islandManager;
    [SerializeField] private GameObject moneyPopUpCanvasPrefab;
    [SerializeField] private Transform moneyPopUpParent;
    [SerializeField] private float moneyPopUpLifetime = 1.2f;
    [SerializeField] private float moneyPopUpAnchoredX = 0.5f;
    [SerializeField] private GameObject popBallParticlePrefab;
    [SerializeField] private BallPopColorPalette popColorPalette;
    [SerializeField] private float popParticleLifetime = 4f;

    private int score;
    private int destructions;
    private readonly Queue<GameObject> moneyPopUpPool = new();
    private readonly Queue<GameObject> popParticlePool = new();

    public int Score => score;
    public int Destructions => destructions;
    public event Action<int> DestructionsChanged;

    private void Awake()
    {
        if (scoreText == null)
            scoreText = GetComponent<TMP_Text>() ?? FindTextByName("Score&DestructionsText");

        if (islandManager == null)
            islandManager = FindFirstObjectByType<IslandManager>();

        LoadOptionalAssets();
        RefreshText();
    }

    private void OnEnable()
    {
        if (islandManager != null)
        {
            islandManager.IslandCleared += OnIslandCleared;
            islandManager.BallPopped += OnBallPopped;
        }
    }

    private void OnDisable()
    {
        if (islandManager != null)
        {
            islandManager.IslandCleared -= OnIslandCleared;
            islandManager.BallPopped -= OnBallPopped;
        }
    }

    private void OnIslandCleared(IReadOnlyList<GameObject> island)
    {
        destructions++;
        DestructionsChanged?.Invoke(destructions);
        RefreshText();
    }

    private void OnBallPopped(Vector3 popPosition, bool isFinalDestructionPop, int comboCount, string ballTag)
    {
        int comboMultiplier = Mathf.Max(1, comboCount);
        int amount = comboMultiplier + (isFinalDestructionPop ? 1 : 0);
        score += amount;
        MetaGameSave.BestScore = score;

        Color popColor = GetPopColor(ballTag);
        ShowMoneyPopUp(amount, popPosition, popColor);
        ShowPopParticle(popPosition, popColor);
        RefreshText();
    }

    private void RefreshText()
    {
        if (scoreText != null)
        {
            string scoreLabel = MetaGameLocalization.TranslateForFont(scoreText, "Score");
            string destructionsLabel = MetaGameLocalization.TranslateForFont(scoreText, "Destructions");
            scoreText.text = $"{scoreLabel}\n{score}\n{destructionsLabel}\n{destructions}";
        }
    }

    private TMP_Text FindTextByName(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private void ShowMoneyPopUp(int amount, Vector3 popPosition, Color popColor)
    {
        if (moneyPopUpCanvasPrefab == null)
            return;

        GameObject instance = GetMoneyPopUp();

        Transform parent = moneyPopUpParent != null && moneyPopUpParent is not RectTransform
            ? moneyPopUpParent
            : null;

        instance.transform.SetParent(parent, true);
        instance.transform.position = new Vector3(moneyPopUpAnchoredX, popPosition.y, popPosition.z);

        Canvas canvas = instance.GetComponent<Canvas>();

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
        }

        TMP_Text text = instance.GetComponentInChildren<TMP_Text>(true);

        if (text != null)
        {
            text.text = $"+{amount}";
            text.color = popColor;
        }

        foreach (Image image in instance.GetComponentsInChildren<Image>(true))
        {
            if (image.name == MoneySpriteName)
                continue;

            image.color = popColor;
        }

        instance.SetActive(true);

        Animator animator = instance.GetComponent<Animator>();

        if (animator != null)
            animator.Play(0, 0, 0f);

        StartCoroutine(ReturnMoneyPopUpAfterDelay(instance));
    }

    private void ShowPopParticle(Vector3 popPosition, Color popColor)
    {
        if (popBallParticlePrefab == null)
            return;

        GameObject instance = GetPopParticle();
        instance.transform.SetPositionAndRotation(popPosition, Quaternion.identity);
        instance.SetActive(true);

        foreach (ParticleSystem particle in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particle.main;
            main.startColor = popColor;
            particle.Clear(true);
            particle.Play(true);
        }

        StartCoroutine(ReturnPopParticleAfterDelay(instance));
    }

    private GameObject GetMoneyPopUp()
    {
        while (moneyPopUpPool.Count > 0)
        {
            GameObject pooled = moneyPopUpPool.Dequeue();

            if (pooled != null)
                return pooled;
        }

        return Instantiate(moneyPopUpCanvasPrefab, moneyPopUpParent);
    }

    private GameObject GetPopParticle()
    {
        while (popParticlePool.Count > 0)
        {
            GameObject pooled = popParticlePool.Dequeue();

            if (pooled != null)
                return pooled;
        }

        return Instantiate(popBallParticlePrefab);
    }

    private IEnumerator ReturnMoneyPopUpAfterDelay(GameObject instance)
    {
        yield return new WaitForSeconds(moneyPopUpLifetime);

        if (instance == null)
            yield break;

        instance.SetActive(false);
        moneyPopUpPool.Enqueue(instance);
    }

    private IEnumerator ReturnPopParticleAfterDelay(GameObject instance)
    {
        yield return new WaitForSeconds(popParticleLifetime);

        if (instance == null)
            yield break;

        instance.SetActive(false);
        popParticlePool.Enqueue(instance);
    }

    private Color GetPopColor(string ballTag)
    {
        if (popColorPalette == null)
            return Color.white;

        return popColorPalette.GetColor(BallSkinUtility.GetCurrentSkinIndex(), ballTag);
    }

    private void LoadOptionalAssets()
    {
#if UNITY_EDITOR
        if (popBallParticlePrefab == null)
            popBallParticlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Particles/PopBallParticle.prefab");

        if (popColorPalette == null)
            popColorPalette = AssetDatabase.LoadAssetAtPath<BallPopColorPalette>("Assets/Settings/BallPopColorPalette.asset");
#endif
    }

    private void OnValidate()
    {
        moneyPopUpLifetime = Mathf.Max(0.01f, moneyPopUpLifetime);
        popParticleLifetime = Mathf.Max(0.01f, popParticleLifetime);
    }
}
