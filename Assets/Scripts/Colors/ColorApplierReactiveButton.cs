using ReactiveData.Core;
using ReactiveData.UI;
using UnityEngine;

namespace Colors
{
    public class ColorApplierReactiveButton : ColorApplier
    {
        [SerializeField] private ReactiveButton button;

        [SerializeField] private ColorPaletteColor defaultNormalColor;
        [SerializeField] private ColorPaletteColor defaultHoveredColor;
        [SerializeField] private ColorPaletteColor defaultPressedColor;
        [SerializeField] private ColorPaletteColor defaultOutlineNormalColor;
        [SerializeField] private ColorPaletteColor defaultOutlineHoveredColor;
        [SerializeField] private ColorPaletteColor defaultOutlinePressedColor;
        [SerializeField] private ColorPaletteColor defaultTextNormalColor;
        [SerializeField] private ColorPaletteColor defaultTextHoveredColor;
        [SerializeField] private ColorPaletteColor defaultTextPressedColor;

        protected override System.Collections.Generic.List<ColorPaletteColor> GetDefaultColors() => new()
        {
            defaultNormalColor,
            defaultHoveredColor,
            defaultPressedColor,
            defaultOutlineNormalColor,
            defaultOutlineHoveredColor,
            defaultOutlinePressedColor,
            defaultTextNormalColor,
            defaultTextHoveredColor,
            defaultTextPressedColor
        };

        public void Bind(
            IReactive<ColorPaletteColor> normalColor,
            IReactive<ColorPaletteColor> hoveredColor,
            IReactive<ColorPaletteColor> pressedColor,
            IReactive<ColorPaletteColor> outlineNormalColor,
            IReactive<ColorPaletteColor> outlineHoveredColor,
            IReactive<ColorPaletteColor> outlinePressedColor,
            IReactive<ColorPaletteColor> textNormalColor,
            IReactive<ColorPaletteColor> textHoveredColor,
            IReactive<ColorPaletteColor> textPressedColor
        )
        {
            Bind(new[] {
                normalColor,
                hoveredColor,
                pressedColor,
                outlineNormalColor,
                outlineHoveredColor,
                outlinePressedColor,
                textNormalColor,
                textHoveredColor,
                textPressedColor
            });
        }

        protected override void ApplyColor(int index, Color color)
        {
            if (index == 0)
            {
                button.normal = color;
            }
            else if (index == 1)
            {
                button.hovered = color;
            }
            else if (index == 2)
            {
                button.pressed = color;
            }
            else if (index == 3)
            {
                button.outlineNormal = color;
            }
            else if (index == 4)
            {
                button.outlineHovered = color;
            }
            else if (index == 5)
            {
                button.outlinePressed = color;
            }
            else if (index == 6)
            {
                button.textNormal = color;
            }
            else if (index == 7)
            {
                button.textHovered = color;
            }
            else if (index == 8)
            {
                button.textPressed = color;
            }
            else
            {
                throw new System.ArgumentOutOfRangeException(nameof(index), "Index must be between 0 and 8 inclusive.");
            }
            button.UpdateState();
        }
    }
}
