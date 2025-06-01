using ReactiveData.Core;
using ReactiveData.UI;
using UnityEngine;

namespace Colors
{
    public class ColorApplierReactiveToggleButton : ColorApplier
    {
        [SerializeField] private ReactiveToggleButton button;

        [SerializeField] private ColorPaletteColor defaultOffNormalColor;
        [SerializeField] private ColorPaletteColor defaultOffHoveredColor;
        [SerializeField] private ColorPaletteColor defaultOffPressedColor;
        [SerializeField] private ColorPaletteColor defaultOnNormalColor;
        [SerializeField] private ColorPaletteColor defaultOnHoveredColor;
        [SerializeField] private ColorPaletteColor defaultOnPressedColor;
        [SerializeField] private ColorPaletteColor defaultOutlineNormalColor;
        [SerializeField] private ColorPaletteColor defaultOutlineHoveredColor;
        [SerializeField] private ColorPaletteColor defaultOutlinePressedColor;
        [SerializeField] private ColorPaletteColor defaultTextNormalColor;
        [SerializeField] private ColorPaletteColor defaultTextHoveredColor;
        [SerializeField] private ColorPaletteColor defaultTextPressedColor;

        protected override System.Collections.Generic.List<ColorPaletteColor> GetDefaultColors() => new()
        {
            defaultOffNormalColor,
            defaultOffHoveredColor,
            defaultOffPressedColor,
            defaultOnNormalColor,
            defaultOnHoveredColor,
            defaultOnPressedColor,
            defaultOutlineNormalColor,
            defaultOutlineHoveredColor,
            defaultOutlinePressedColor,
            defaultTextNormalColor,
            defaultTextHoveredColor,
            defaultTextPressedColor
        };

        public void Bind(
            IReactive<ColorPaletteColor> normalOffColor,
            IReactive<ColorPaletteColor> hoveredOffColor,
            IReactive<ColorPaletteColor> pressedOffColor,
            IReactive<ColorPaletteColor> normalOnColor,
            IReactive<ColorPaletteColor> hoveredOnColor,
            IReactive<ColorPaletteColor> pressedOnColor,
            IReactive<ColorPaletteColor> outlineNormalColor,
            IReactive<ColorPaletteColor> outlineHoveredColor,
            IReactive<ColorPaletteColor> outlinePressedColor,
            IReactive<ColorPaletteColor> textNormalColor,
            IReactive<ColorPaletteColor> textHoveredColor,
            IReactive<ColorPaletteColor> textPressedColor
        )
        {
            Bind(new[] {
                normalOffColor,
                hoveredOffColor,
                pressedOffColor,
                normalOnColor,
                hoveredOnColor,
                pressedOnColor,
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
                button.offNormal = color;
            }
            else if (index == 1)
            {
                button.offHovered = color;
            }
            else if (index == 2)
            {
                button.offPressed = color;
            }
            else if (index == 3)
            {
                button.normal = color;
            }
            else if (index == 4)
            {
                button.hovered = color;
            }
            else if (index == 5)
            {
                button.pressed = color;
            }
            else if (index == 6)
            {
                button.outlineNormal = color;
            }
            else if (index == 7)
            {
                button.outlineHovered = color;
            }
            else if (index == 8)
            {
                button.outlinePressed = color;
            }
            else if (index == 9)
            {
                button.textNormal = color;
            }
            else if (index == 10)
            {
                button.textHovered = color;
            }
            else if (index == 11)
            {
                button.textPressed = color;
            }
            else
            {
                Debug.LogError($"Color index {index} is out of range for ColorApplierReactiveToggleButton.");
                return;
            }
            button.UpdateState();
        }
    }
}
