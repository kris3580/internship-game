using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class PowerUpHudController : MonoBehaviour
{
    [SerializeField] private BallPlacer ballPlacer;
    [SerializeField] private GameObject firePrefab;
    [SerializeField] private GameObject earthPrefab;
    [SerializeField] private GameObject waterPrefab;
    [SerializeField] private GameObject windPrefab;
    [SerializeField] private int startingCount = 100;

    [InjectOptional] private IBallFactory ballFactory;
    [InjectOptional] private ISaveSystem saveSystem;

    private PowerUpSlot fire;
    private PowerUpSlot earth;
    private PowerUpSlot water;
    private PowerUpSlot wind;

    private void Awake()
    {
        if (ballPlacer == null)
            ballPlacer = FindFirstObjectByType<BallPlacer>();

        ballFactory?.RegisterPrefabs(firePrefab, earthPrefab, waterPrefab, windPrefab);

        fire = new PowerUpSlot("PowerUpFireButton", "fire", LoadCount("fire"));
        earth = new PowerUpSlot("PowerUpEarthButton", "earth", LoadCount("earth"));
        water = new PowerUpSlot("PowerUpWaterButton", "water", LoadCount("water"));
        wind = new PowerUpSlot("PowerUpWindButton", "wind", LoadCount("wind"));

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
        if (slot.Count <= 0 || ballPlacer == null)
            return;

        if (!ballPlacer.QueuePowerUp(slot.BallTag, () => Spend(slot)))
            return;
    }

    private void Spend(PowerUpSlot slot)
    {
        slot.Count = Mathf.Max(0, slot.Count - 1);
        SaveCount(slot);
        slot.Refresh();
    }

    private int LoadCount(string ballTag)
    {
        string key = GetSaveKey(ballTag);
        return saveSystem != null ? saveSystem.GetInt(key, startingCount) : startingCount;
    }

    private void SaveCount(PowerUpSlot slot)
    {
        if (saveSystem == null)
            return;

        saveSystem.SetInt(GetSaveKey(slot.BallTag), slot.Count);
        saveSystem.Save();
    }

    private string GetSaveKey(string ballTag)
    {
        return $"powerup.{ballTag}.count";
    }

    [System.Serializable]
    private sealed class PowerUpSlot
    {
        public PowerUpSlot(string buttonName, string ballTag, int count)
        {
            BallTag = ballTag;
            Count = count;

            GameObject buttonObject = GameObject.Find(buttonName);
            Button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
            CountText = buttonObject != null ? buttonObject.GetComponentInChildren<TMP_Text>() : null;
        }

        public Button Button { get; }
        public TMP_Text CountText { get; }
        public string BallTag { get; }
        public int Count { get; set; }

        public void Refresh()
        {
            if (CountText != null)
                CountText.text = Count.ToString();

            if (Button != null)
                Button.interactable = Count > 0;
        }
    }
}
