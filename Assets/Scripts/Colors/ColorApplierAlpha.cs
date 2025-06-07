using ReactiveData.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Colors
{
    public abstract class ColorApplierAlpha : MonoBehaviour
    {
        private readonly List<IReactive<ColorPaletteCol>> colors = new();
        private readonly List<System.Action<ColorPaletteCol>> actions = new();

        protected abstract List<ColorPaletteCol> GetDefaultColors();

        private bool isBound = false;

        private void Awake()
        {
            if (isBound) return;
            var defaultColors = GetDefaultColors();
            for (int i = 0; i < defaultColors.Count; i++)
            {
                var colorSystem = Globals<ColorSystem>.Instance;
                var col = colorSystem.GetColor(defaultColors[i].color);
                col.a = defaultColors[i].alpha;
                ApplyColor(i, col);
            }
        }

        public void Bind(IEnumerable<IReactive<ColorPaletteCol>> colors)
        {
            this.colors.Clear();
            this.colors.AddRange(colors);
            actions.Clear();
            var defaultColors = GetDefaultColors();
            for (int i = 0; i < this.colors.Count; i++)
            {
                int index = i;
                actions.Add(color =>
                {
                    var colorSystem = Globals<ColorSystem>.Instance;
                    var col = colorSystem.GetColor(color.color);
                    col.a = color.alpha;
                    ApplyColor(index, col);
                });
                if (this.colors[i] != null)
                {
                    this.colors[i].OnChanged += actions[i];
                }
                else
                {
                    actions[i].Invoke(defaultColors[i]);
                }
            }
            isBound = true;
        }

        public void Unbind()
        {
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i] != null)
                {
                    colors[i].OnChanged -= actions[i];
                }
            }

            colors.Clear();
            actions.Clear();
            isBound = false;
        }

        protected abstract void ApplyColor(int index, Color color);
    }
}
