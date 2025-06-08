using ReactiveData.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Colors
{
    public class ColorApplierGraphic : ColorApplierAlpha
    {
        [SerializeField] private Graphic img;

        [SerializeField] private ColorPaletteCol defaultColor;

        protected override List<ColorPaletteCol> GetDefaultColors() => new() { defaultColor };

        public void Bind(IReactive<ColorPaletteCol> color)
        {
            Bind(new[] { color });
        }

        protected override void ApplyColor(int index, Color color)
        {
            img.color = color;
        }
    }
}
