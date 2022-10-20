using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace CarDodge.Utilities
{
    internal static class Extensions
    {
        /// <summary>
        /// Makes the graphics object best quality (slower, but looks better).
        /// </summary>
        /// <param name="graphics"></param>
        public static void ToHighQuality(this Graphics graphics)
        {
            if (graphics.InterpolationMode == InterpolationMode.HighQualityBicubic) return; // saves 5 assigns each call

            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            graphics.PixelOffsetMode = PixelOffsetMode.Default;
        }

        /// <summary>
        /// Ensures value is between the min and max.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        internal static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0)
            {
                return min;
            }

            if (val.CompareTo(max) > 0)
            {
                return max;
            }

            return val;
        }
    }
}
