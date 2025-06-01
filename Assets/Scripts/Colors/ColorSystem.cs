using UnityEngine;

namespace Colors
{
    public class ColorSystem : MonoBehaviour
    {
        [SerializeField] private ColorPalette colorPalette;

        public Color GetColor(ColorPaletteColor color)
        {
            return colorPalette.GetColor(color);
        }
    }
}
