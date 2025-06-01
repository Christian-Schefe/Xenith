using TMPro;
using UnityEngine;

namespace Colors
{
    public class ColorStaticApplierText : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private ColorPaletteColor color;

        public void Awake()
        {
            var colorSystem = Globals<ColorSystem>.Instance;
            var col = colorSystem.GetColor(color);
            text.color = col;
        }
    }
}
