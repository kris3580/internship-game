using UnityEngine;

[CreateAssetMenu(fileName = "BallPopColorPalette", menuName = "Game/Ball Pop Color Palette")]
public sealed class BallPopColorPalette : ScriptableObject
{
    public SkinColors[] skins =
    {
        new("default"),
        new("skin1"),
        new("skin2"),
        new("skin3"),
        new("skin4"),
        new("skin5")
    };

    public Color GetColor(int skinIndex, string ballTag)
    {
        int ballIndex = 0;

        if (int.TryParse(ballTag, out int number))
            ballIndex = Mathf.Clamp(number - 2, 0, 6);

        skinIndex = Mathf.Clamp(skinIndex, 0, skins.Length - 1);
        SkinColors skin = skins[skinIndex];

        if (skin.colors == null || skin.colors.Length == 0)
            return Color.white;

        return skin.colors[Mathf.Clamp(ballIndex, 0, skin.colors.Length - 1)];
    }

    [System.Serializable]
    public sealed class SkinColors
    {
        public string name;
        public Color[] colors = new Color[7];

        public SkinColors(string name)
        {
            this.name = name;

            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.white;
        }
    }
}
