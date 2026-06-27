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

    private FatePointsManager fatePointsManager;
    private bool consumed;

    private void Awake()
    {
        fatePointsManager = FindFirstObjectByType<FatePointsManager>();
    }

    private void OnEnable()
    {
        consumed = false;

        if (fatePointsManager == null)
            fatePointsManager = FindFirstObjectByType<FatePointsManager>();
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

        int delta = CompareTag(PlusTag) ? 1 : CompareTag(MinusTag) ? -1 : 0;

        if (delta == 0)
            return;

        consumed = true;
        PlayImpactSound(delta);
        fatePointsManager?.AdjustFatePoints(numericBall.tag, delta);
        Destroy(gameObject);
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

    private void OnValidate()
    {
        plusVolume = Mathf.Max(0f, plusVolume);
        minusVolume = Mathf.Max(0f, minusVolume);
    }
}
