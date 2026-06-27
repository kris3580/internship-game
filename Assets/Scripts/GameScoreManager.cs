using System.Collections.Generic;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameScoreManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private IslandManager islandManager;
    [SerializeField] private GameObject moneyPopUpCanvasPrefab;
    [SerializeField] private Transform moneyPopUpParent;
    [SerializeField] private float moneyPopUpLifetime = 1.2f;
    [SerializeField] private float moneyPopUpAnchoredX = 0.5f;

    private int score;
    private int destructions;
    private readonly Queue<GameObject> moneyPopUpPool = new();

    public int Destructions => destructions;
    public event Action<int> DestructionsChanged;

    private void Awake()
    {
        if (scoreText == null)
            scoreText = GetComponent<TMP_Text>() ?? FindTextByName("Score&DestructionsText");

        if (islandManager == null)
            islandManager = FindFirstObjectByType<IslandManager>();

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

    private void OnBallPopped(Vector3 popPosition, bool causedDestruction, int comboCount)
    {
        int amount = Mathf.Max(1, comboCount) + (causedDestruction ? 1 : 0);
        score += amount;
        ShowMoneyPopUp(amount, popPosition);
        RefreshText();
    }

    private void RefreshText()
    {
        if (scoreText != null)
            scoreText.text = $"Score\n{score}\nDestructions\n{destructions}";
    }

    private TMP_Text FindTextByName(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private void ShowMoneyPopUp(int amount, Vector3 popPosition)
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
            text.text = $"+{amount}";

        instance.SetActive(true);

        Animator animator = instance.GetComponent<Animator>();

        if (animator != null)
            animator.Play(0, 0, 0f);

        StartCoroutine(ReturnMoneyPopUpAfterDelay(instance));
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

    private IEnumerator ReturnMoneyPopUpAfterDelay(GameObject instance)
    {
        yield return new WaitForSeconds(moneyPopUpLifetime);

        if (instance == null)
            yield break;

        instance.SetActive(false);
        moneyPopUpPool.Enqueue(instance);
    }

    private void OnValidate()
    {
        moneyPopUpLifetime = Mathf.Max(0.01f, moneyPopUpLifetime);
    }
}
