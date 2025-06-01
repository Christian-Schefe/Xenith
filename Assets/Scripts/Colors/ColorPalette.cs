using UnityEngine;

namespace Colors
{
    [CreateAssetMenu(fileName = "ColorPalette", menuName = "Colors/Color Palette")]
    public class ColorPalette : ScriptableObject
    {
        [SerializeField] private ColorRef primary;
        [SerializeField] private ColorRef secondary;
        [SerializeField] private ColorRef background;
        [SerializeField] private ColorRef surface;
        [SerializeField] private ColorRef textLight;
        [SerializeField] private ColorRef textDark;
        [SerializeField] private ColorRef outline;

        public Color GetColor(ColorPaletteColor color)
        {
            return color switch
            {
                ColorPaletteColor.Clear => Color.clear,
                ColorPaletteColor.White => Color.white,
                ColorPaletteColor.Black => Color.black,
                ColorPaletteColor.Primary => primary.Color,
                ColorPaletteColor.Secondary => secondary.Color,
                ColorPaletteColor.Background => background.Color,
                ColorPaletteColor.Surface => surface.Color,
                ColorPaletteColor.TextLight => textLight.Color,
                ColorPaletteColor.TextDark => textDark.Color,
                ColorPaletteColor.Outline => outline.Color,
                _ => throw new System.ArgumentOutOfRangeException(nameof(color), color, null)
            };
        }
    }

    public enum ColorPaletteColor
    {
        Clear,
        White,
        Black,
        Primary,
        Secondary,
        Background,
        Surface,
        TextLight,
        TextDark,
        Outline,
    }
}
