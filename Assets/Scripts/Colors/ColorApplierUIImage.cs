using ReactiveData.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Colors
{
    public class ColorApplierUIImage : ColorApplierAlpha
    {
        [SerializeField] private UIImage img;

        [SerializeField] private ColorPaletteCol defaultFillColor;
        [SerializeField] private ColorPaletteCol defaultOutlineColor;

        protected override List<ColorPaletteCol> GetDefaultColors() => new() { defaultFillColor, defaultOutlineColor };

        public void Bind(IReactive<ColorPaletteCol> fillColor, IReactive<ColorPaletteCol> outlineColor)
        {
            Bind(new[] { fillColor, outlineColor });
        }

        protected override void ApplyColor(int index, Color color)
        {
            if (index == 0)
            {
                img.color = color;
            }
            else if (index == 1)
            {
                img.outlineColor = color;
            }
            else
            {
                throw new System.ArgumentOutOfRangeException(nameof(index), "Index must be 0 or 1 for UIImage color application.");
            }
        }
    }
}
