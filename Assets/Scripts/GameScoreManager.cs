using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameScoreManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private IslandManager islandManager;
    [SerializeField] private int pointsPerBall = 1;

    private int score;
    private int destructions;

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
            islandManager.IslandCleared += OnIslandCleared;
    }

    private void OnDisable()
    {
        if (islandManager != null)
            islandManager.IslandCleared -= OnIslandCleared;
    }

    private void OnIslandCleared(IReadOnlyList<GameObject> island)
    {
        destructions++;
        score += (island?.Count ?? 0) * Mathf.Max(1, pointsPerBall);
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
}
