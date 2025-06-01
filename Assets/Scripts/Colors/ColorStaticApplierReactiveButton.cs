using ReactiveData.UI;
using UnityEngine;

namespace Colors
{
    public class ColorStaticApplierReactiveButton : MonoBehaviour
    {
        [SerializeField] private ReactiveButton button;
        [SerializeField] private ColorPaletteColor normalColor, hoveredColor, pressedColor;
        [SerializeField] private ColorPaletteColor outlineNormalColor, outlineHoveredColor, outlinePressedColor;
        [SerializeField] private ColorPaletteColor textNormalColor, textHoveredColor, textPressedColor;

        public void Awake()
        {
            var colorSystem = Globals<ColorSystem>.Instance;
            button.normal = colorSystem.GetColor(normalColor);
            button.hovered = colorSystem.GetColor(hoveredColor);
            button.pressed = colorSystem.GetColor(pressedColor);
            button.outlineNormal = colorSystem.GetColor(outlineNormalColor);
            button.outlineHovered = colorSystem.GetColor(outlineHoveredColor);
            button.outlinePressed = colorSystem.GetColor(outlinePressedColor);
            button.textNormal = colorSystem.GetColor(textNormalColor);
            button.textHovered = colorSystem.GetColor(textHoveredColor);
            button.textPressed = colorSystem.GetColor(textPressedColor);
            button.UpdateState();
        }
    }
}
