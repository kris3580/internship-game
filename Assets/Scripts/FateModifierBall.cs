using TMPro;
using UnityEngine;

public sealed class FateModifierBall : MonoBehaviour
{
    private const string PlusTag = "+";
    private const string MinusTag = "-";

    [SerializeField] private AudioClip plusClip;
    [SerializeField] private AudioClip minusClip;
    [Range(0f, 1f)]
    [SerializeField] private float plusVolume = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float minusVolume = 0.7f;
    [Header("Fate Point Modifier")]
    private int plusMinDelta = 1;
    private int plusMaxDelta = 10;
    private int minusMinDelta = -10;
    private int minusMaxDelta = -1;
    [SerializeField] private TMP_Text modifierText;

    private FatePointsManager fatePointsManager;
    private int currentDelta;
    private bool consumed;

    private void Awake()
    {
        fatePointsManager = FindFirstObjectByType<FatePointsManager>();
        ResolveModifierText();
    }

    private void OnEnable()
    {
        consumed = false;

        if (fatePointsManager == null)
            fatePointsManager = FindFirstObjectByType<FatePointsManager>();

        currentDelta = RollDelta();
        RefreshModifierText();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryApplyTo(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryApplyTo(other);
    }

    private void TryApplyTo(Collider other)
    {
        if (consumed || other == null)
            return;

        GameObject numericBall = FindNumericBall(other.transform);

        if (numericBall == null || numericBall == gameObject)
            return;

        if (fatePointsManager == null)
            fatePointsManager = FindFirstObjectByType<FatePointsManager>();

        int delta = currentDelta != 0 ? currentDelta : RollDelta();

        if (delta == 0)
            return;

        if (fatePointsManager == null || !fatePointsManager.TryAdjustFatePoints(numericBall.tag, delta))
            return;

        PlayImpactSound(delta);
        consumed = true;
        Destroy(gameObject);
    }

    private int RollDelta()
    {
        NormalizeRanges();

        if (CompareTag(PlusTag))
            return Random.Range(plusMinDelta, plusMaxDelta + 1);

        if (CompareTag(MinusTag))
            return Random.Range(minusMinDelta, minusMaxDelta + 1);

        return 0;
    }

    private void RefreshModifierText()
    {
        ResolveModifierText();

        if (modifierText == null)
            return;

        modifierText.text = currentDelta > 0
            ? $"+{currentDelta}"
            : currentDelta.ToString();
    }

    private void ResolveModifierText()
    {
        if (modifierText != null)
            return;

        Transform canvas = FindChild(transform, "FPModifierCanvas");
        modifierText = canvas != null
            ? canvas.GetComponentInChildren<TMP_Text>(true)
            : GetComponentInChildren<TMP_Text>(true);
    }

    private void PlayImpactSound(int delta)
    {
        AudioClip clip = delta > 0 ? plusClip : minusClip;
        float volume = delta > 0 ? plusVolume : minusVolume;

        if (clip != null && AudioPreferences.SoundEnabled)
            AudioSource.PlayClipAtPoint(clip, transform.position, Mathf.Max(0f, volume));
    }

    private static GameObject FindNumericBall(Transform start)
    {
        Transform current = start;

        while (current != null)
        {
            if (int.TryParse(current.tag, out _))
                return current.gameObject;

            current = current.parent;
        }

        return null;
    }

    private static Transform FindChild(Transform parent, string objectName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child;
        }

        return null;
    }

    private void OnValidate()
    {
        plusVolume = Mathf.Max(0f, plusVolume);
        minusVolume = Mathf.Max(0f, minusVolume);
        NormalizeRanges();
    }

    private void NormalizeRanges()
    {
        int positiveA = Mathf.Max(1, plusMinDelta);
        int positiveB = Mathf.Max(1, plusMaxDelta);
        plusMinDelta = Mathf.Min(positiveA, positiveB);
        plusMaxDelta = Mathf.Max(positiveA, positiveB);

        int negativeA = Mathf.Min(-1, minusMinDelta);
        int negativeB = Mathf.Min(-1, minusMaxDelta);
        minusMinDelta = Mathf.Min(negativeA, negativeB);
        minusMaxDelta = Mathf.Max(negativeA, negativeB);
    }
}
