using UnityEngine;

[CreateAssetMenu(fileName = "MetaGameMenuBackgroundSettings", menuName = "Game/Meta Game Menu Background Settings")]
public sealed class MetaGameMenuBackgroundSettings : ScriptableObject
{
    public const string ResourceName = "MetaGameMenuBackgroundSettings";

    [SerializeField] private Color defaultColor = Color.green;
    [SerializeField] private Color leaderboardColor = Color.red;
    [SerializeField] private Color starterPackColor = Color.blue;
    [SerializeField] private Color shopColor = Color.yellow;
    [SerializeField, Min(0f)] private float lerpSpeed = 7f;

    public float LerpSpeed => Mathf.Max(0f, lerpSpeed);

    public Color GetColor(string panelName)
    {
        return panelName switch
        {
            "Leaderboards" => leaderboardColor,
            "StarterPack" => starterPackColor,
            "Shop" => shopColor,
            _ => defaultColor
        };
    }

    private void OnValidate()
    {
        lerpSpeed = Mathf.Max(0f, lerpSpeed);
    }
}
