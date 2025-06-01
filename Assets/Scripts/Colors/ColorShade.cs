using UnityEngine;

namespace Colors
{
    [CreateAssetMenu(fileName = "ColorShade", menuName = "Colors/Color Shade")]
    public class ColorShade : ScriptableObject
    {
        [SerializeField] private Color c50;
        [SerializeField] private Color c100;
        [SerializeField] private Color c200;
        [SerializeField] private Color c300;
        [SerializeField] private Color c400;
        [SerializeField] private Color c500;
        [SerializeField] private Color c600;
        [SerializeField] private Color c700;
        [SerializeField] private Color c800;
        [SerializeField] private Color c900;
        [SerializeField] private Color c950;

        public Color GetColor(ColorShadeStep step)
        {
            return step switch
            {
                ColorShadeStep.C50 => c50,
                ColorShadeStep.C100 => c100,
                ColorShadeStep.C200 => c200,
                ColorShadeStep.C300 => c300,
                ColorShadeStep.C400 => c400,
                ColorShadeStep.C500 => c500,
                ColorShadeStep.C600 => c600,
                ColorShadeStep.C700 => c700,
                ColorShadeStep.C800 => c800,
                ColorShadeStep.C900 => c900,
                ColorShadeStep.C950 => c950,
                _ => throw new System.ArgumentOutOfRangeException(nameof(step), step, null)
            };
        }
    }

    public enum ColorShadeStep
    {
        C50,
        C100,
        C200,
        C300,
        C400,
        C500,
        C600,
        C700,
        C800,
        C900,
        C950
    }

    [System.Serializable]
    public struct ColorRef
    {
        public ColorShade shade;
        public ColorShadeStep step;
        public float alpha;

        public readonly Color Color
        {
            get
            {
                var col = shade.GetColor(step);
                col.a = alpha;
                return col;
            }
        }

        public readonly ColorRef Brighter => new(shade, GetBrighterStep(step), alpha);
        public readonly ColorRef Darker => new(shade, GetDarkerStep(step), alpha);

        public ColorRef(ColorShade shade, ColorShadeStep step, float alpha)
        {
            this.shade = shade;
            this.step = step;
            this.alpha = alpha;
        }

        private static ColorShadeStep GetBrighterStep(ColorShadeStep step)
        {
            return step switch
            {
                ColorShadeStep.C50 => ColorShadeStep.C100,
                ColorShadeStep.C100 => ColorShadeStep.C200,
                ColorShadeStep.C200 => ColorShadeStep.C300,
                ColorShadeStep.C300 => ColorShadeStep.C400,
                ColorShadeStep.C400 => ColorShadeStep.C500,
                ColorShadeStep.C500 => ColorShadeStep.C600,
                ColorShadeStep.C600 => ColorShadeStep.C700,
                ColorShadeStep.C700 => ColorShadeStep.C800,
                ColorShadeStep.C800 => ColorShadeStep.C900,
                ColorShadeStep.C900 => ColorShadeStep.C950,
                _ => step
            };
        }

        private static ColorShadeStep GetDarkerStep(ColorShadeStep step)
        {
            return step switch
            {
                ColorShadeStep.C950 => ColorShadeStep.C900,
                ColorShadeStep.C900 => ColorShadeStep.C800,
                ColorShadeStep.C800 => ColorShadeStep.C700,
                ColorShadeStep.C700 => ColorShadeStep.C600,
                ColorShadeStep.C600 => ColorShadeStep.C500,
                ColorShadeStep.C500 => ColorShadeStep.C400,
                ColorShadeStep.C400 => ColorShadeStep.C300,
                ColorShadeStep.C300 => ColorShadeStep.C200,
                ColorShadeStep.C200 => ColorShadeStep.C100,
                ColorShadeStep.C100 => ColorShadeStep.C50,
                _ => step
            };
        }
    }
}
