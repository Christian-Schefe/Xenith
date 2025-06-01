using ReactiveData.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Colors
{
    public class ColorStaticApplierGraphic : MonoBehaviour
    {
        [SerializeField] private Graphic graphic;
        [SerializeField] private ColorPaletteColor color;

        public void Awake()
        {
            var colorSystem = Globals<ColorSystem>.Instance;
            var col = colorSystem.GetColor(color);
            graphic.color = col;
        }
    }
}
