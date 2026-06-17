using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpHudController : MonoBehaviour
{
    [SerializeField] private BallPlacer ballPlacer;
    [SerializeField] private GameObject firePrefab;
    [SerializeField] private GameObject earthPrefab;
    [SerializeField] private GameObject waterPrefab;
    [SerializeField] private GameObject windPrefab;
    [SerializeField] private int startingCount = 3;

    private PowerUpSlot fire;
    private PowerUpSlot earth;
    private PowerUpSlot water;
    private PowerUpSlot wind;

    private void Awake()
    {
        if (ballPlacer == null)
            ballPlacer = FindFirstObjectByType<BallPlacer>();

        fire = new PowerUpSlot("PowerUpFireButton", firePrefab, startingCount);
        earth = new PowerUpSlot("PowerUpEarthButton", earthPrefab, startingCount);
        water = new PowerUpSlot("PowerUpWaterButton", waterPrefab, startingCount);
        wind = new PowerUpSlot("PowerUpWindButton", windPrefab, startingCount);

        Bind(fire);
        Bind(earth);
        Bind(water);
        Bind(wind);
    }

    public void SelectFire()
    {
        TryUse(fire);
    }

    public void SelectEarth()
    {
        TryUse(earth);
    }

    public void SelectWater()
    {
        TryUse(water);
    }

    public void SelectWind()
    {
        TryUse(wind);
    }

    private void Bind(PowerUpSlot slot)
    {
        if (slot.Button == null)
            return;

        slot.Refresh();
        slot.Button.onClick.AddListener(() => TryUse(slot));
    }

    private void TryUse(PowerUpSlot slot)
    {
        if (slot.Count <= 0 || ballPlacer == null || slot.Prefab == null)
            return;

        if (!ballPlacer.QueuePowerUp(slot.Prefab, () => Spend(slot)))
            return;
    }

    private void Spend(PowerUpSlot slot)
    {
        slot.Count = Mathf.Max(0, slot.Count - 1);
        slot.Refresh();
    }

    [System.Serializable]
    private sealed class PowerUpSlot
    {
        public PowerUpSlot(string buttonName, GameObject prefab, int count)
        {
            Prefab = prefab;
            Count = count;

            GameObject buttonObject = GameObject.Find(buttonName);
            Button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
            CountText = buttonObject != null ? buttonObject.GetComponentInChildren<TMP_Text>() : null;
        }

        public Button Button { get; }
        public TMP_Text CountText { get; }
        public GameObject Prefab { get; }
        public int Count { get; set; }

        public void Refresh()
        {
            if (CountText != null)
                CountText.text = Count.ToString();

            if (Button != null)
                Button.interactable = Count > 0 && Prefab != null;
        }
    }
}
