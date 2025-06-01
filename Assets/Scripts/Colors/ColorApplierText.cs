using ReactiveData.Core;
using UnityEngine;

namespace Colors
{
    public class ColorApplierText : ColorApplier
    {
        [SerializeField] private TMPro.TextMeshProUGUI text;

        [SerializeField] private ColorPaletteColor defaultTextColor;

        protected override System.Collections.Generic.List<ColorPaletteColor> GetDefaultColors() => new() { defaultTextColor };

        public void Bind(IReactive<ColorPaletteColor> textColor)
        {
            Bind(new[] { textColor });
        }

        protected override void ApplyColor(int index, Color color)
        {
            if (index != 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(index), "Index must be 0 for Text color application.");
            }
            text.color = color;
        }
    }
}
