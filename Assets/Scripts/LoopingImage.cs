using UnityEngine;
using UnityEngine.UI;

public class LoopingImage : MonoBehaviour
{
    private RawImage image;
    [SerializeField] private float x, y;

    private void Start()
    {
        image = GetComponent<RawImage>();
    }

    void Update()
    {
        image.uvRect = new Rect(image.uvRect.position + new Vector2(x, y) * Time.deltaTime, image.uvRect.size);
    }
}