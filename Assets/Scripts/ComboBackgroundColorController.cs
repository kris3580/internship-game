using UnityEngine;
using UnityEngine.UI;

public sealed class ComboBackgroundColorController : MonoBehaviour
{
    [SerializeField] private IslandManager islandManager;
    [SerializeField] private Image targetImage;
    [SerializeField] private RawImage targetRawImage;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float lerpSpeed = 3f;
    [SerializeField] private Color[] comboColors =
    {
        new(0.95f, 0.36f, 0.42f, 1f),
        new(0.98f, 0.56f, 0.22f, 1f),
        new(0.98f, 0.78f, 0.24f, 1f),
        new(0.64f, 0.86f, 0.28f, 1f),
        new(0.24f, 0.78f, 0.44f, 1f),
        new(0.18f, 0.78f, 0.72f, 1f),
        new(0.22f, 0.64f, 0.96f, 1f),
        new(0.40f, 0.48f, 0.98f, 1f),
        new(0.62f, 0.40f, 0.96f, 1f),
        new(0.86f, 0.38f, 0.92f, 1f),
        new(0.96f, 0.42f, 0.70f, 1f),
        new(0.42f, 0.92f, 0.62f, 1f),
        new(0.36f, 0.88f, 0.96f, 1f),
        new(0.92f, 0.64f, 0.32f, 1f),
        new(0.78f, 0.90f, 0.34f, 1f),
    };

    private Color targetColor;
    [SerializeField, HideInInspector] private int previewIndex;

    private void Awake()
    {
        if (islandManager == null)
            islandManager = FindFirstObjectByType<IslandManager>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetRawImage == null)
            targetRawImage = FindRawImage("BackgroundImage");

        targetColor = GetCurrentColor();
    }

    private void OnEnable()
    {
        if (islandManager != null)
            islandManager.ComboChanged += OnComboChanged;
    }

    private void OnDisable()
    {
        if (islandManager != null)
            islandManager.ComboChanged -= OnComboChanged;
    }

    private void Update()
    {
        Color current = GetCurrentColor();
        Color next = Color.Lerp(current, targetColor, Mathf.Clamp01(lerpSpeed * Time.deltaTime));

        if (targetImage != null)
            targetImage.color = next;

        if (targetRawImage != null)
            targetRawImage.color = next;

        if (targetCamera != null)
            targetCamera.backgroundColor = next;
    }

    private void OnComboChanged(int comboCount)
    {
        if (comboCount >= 2)
            AdvanceColor();
    }

    public void AdvanceColor()
    {
        if (comboColors == null || comboColors.Length == 0)
            return;

        previewIndex = (previewIndex + 1) % comboColors.Length;
        SetTargetByIndex(previewIndex);
    }

    public void PreviewNextColor()
    {
        AdvanceColor();
    }

    public void PreviewRandomColor()
    {
        if (comboColors == null || comboColors.Length == 0)
            return;

        previewIndex = Random.Range(0, comboColors.Length);
        SetTargetByIndex(previewIndex);
    }

    public void ApplyPreviewImmediately()
    {
        Color current = targetColor;

        if (targetImage != null)
            targetImage.color = current;

        if (targetRawImage != null)
            targetRawImage.color = current;

        if (targetCamera != null)
            targetCamera.backgroundColor = current;
    }

    private void SetTargetByIndex(int index)
    {
        if (comboColors == null || comboColors.Length == 0)
            return;

        targetColor = comboColors[Mathf.Abs(index) % comboColors.Length];
    }

    private Color GetCurrentColor()
    {
        if (targetImage != null)
            return targetImage.color;

        if (targetRawImage != null)
            return targetRawImage.color;

        if (targetCamera != null)
            return targetCamera.backgroundColor;

        return Color.black;
    }

    private void OnValidate()
    {
        lerpSpeed = Mathf.Max(0f, lerpSpeed);
    }

    private static RawImage FindRawImage(string objectName)
    {
        foreach (RawImage image in Resources.FindObjectsOfTypeAll<RawImage>())
        {
            if (image.name == objectName &&
                image.hideFlags == HideFlags.None &&
                image.gameObject.scene.IsValid())
            {
                return image;
            }
        }

        return null;
    }
}
