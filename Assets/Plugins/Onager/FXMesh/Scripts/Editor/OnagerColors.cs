using UnityEngine;

namespace Onager.Utilities
{
    public class Colors
    {
        public static Color OnagerColor = new Color32(240, 190, 85, 255);
        public static Color OnagerGrey = new Color(.2f, .2f, .2f, 1f);
        public static Color OnagerGreyLight = new Color(.3f, .3f, .3f, 1f);
    }

    public static class ColorExtensions
    {
        public static Color Alpha(this Color color, float value)
        {
            return new Color(color.r, color.g, color.b, value);
        }

        public static Color Brightness(this Color color, float value)
        {
            return new Color(color.r * value, color.g * value, color.b * value, color.a);
        }
    }
}
