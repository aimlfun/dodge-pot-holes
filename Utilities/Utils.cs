using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace CarDodge.Utilities;

/// <summary>
/// Utility functions.
/// </summary>
internal static class Utils
{
    /// <summary>
    /// Determine a point rotated by an angle around an origin.
    /// </summary>
    /// <param name="point"></param>
    /// <param name="origin"></param>
    /// <param name="angleInDegrees"></param>
    /// <returns></returns>
    internal static PointF RotatePointAboutOrigin(PointF point, PointF origin, double angleInDegrees)
    {
        return RotatePointAboutOriginInRadians(point, origin, DegreesInRadians(angleInDegrees));
    }

    /// <summary>
    /// Determine a point rotated by an angle around an origin.
    /// </summary>
    /// <param name="point"></param>
    /// <param name="origin"></param>
    /// <param name="angleInRadians"></param>
    /// <returns></returns>
    internal static PointF RotatePointAboutOriginInRadians(PointF point, PointF origin, double angleInRadians)
    {
        double cos = Math.Cos(angleInRadians);
        double sin = Math.Sin(angleInRadians);
        float dx = point.X - origin.X;
        float dy = point.Y - origin.Y;

        // standard maths for rotation.
        return new PointF((float)(cos * dx - sin * dy + origin.X),
                          (float)(sin * dx + cos * dy + origin.Y)
        );
    }

    /// <summary>
    /// Similar to RotateBitmap, but with the background coloured.
    /// </summary>
    /// <param name="bitmap"></param>
    /// <param name="angleInDegrees"></param>
    /// <returns></returns>
    internal static Bitmap RotateBitmapWithColoredBackground(Bitmap bitmap, double angleInDegrees)
    {
        Bitmap returnBitmap = new(bitmap.Width, bitmap.Height, PixelFormat.Format32bppPArgb);

        using Graphics graphics = Graphics.FromImage(returnBitmap);

        Color c = bitmap.GetPixel(0, 0);

        graphics.InterpolationMode = InterpolationMode.NearestNeighbor; // rough quality
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.TranslateTransform((float)bitmap.Width / 2, (float)bitmap.Height / 2); // to center about middle, we need to move the point of rotation to middle
        graphics.RotateTransform((float)angleInDegrees);
        graphics.TranslateTransform(-(float)bitmap.Width / 2, -(float)bitmap.Height / 2); // undo the point of rotation

        var z = graphics.CompositingMode;
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.DrawImageUnscaled(bitmap, new Point(0, 0));
        graphics.CompositingMode = z;

        return returnBitmap;
    }

    /// <summary>
    /// Logic requires radians but we track angles in degrees, this converts.
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    internal static double DegreesInRadians(double angle)
    {
        return Math.PI * angle / 180;
    }

    /// <summary>
    /// Resizes an image.
    /// </summary>
    /// <param name="canvasWidth"></param>
    /// <param name="canvasHeight"></param>
    /// <returns></returns>
    internal static Image ResizeImage(Image image, int canvasWidth, int canvasHeight)
    {
        int originalWidth = image.Width;
        int originalHeight = image.Height;

        Image thumbnail = new Bitmap(canvasWidth, canvasHeight); // changed parm names
        using (Graphics graphic = Graphics.FromImage(thumbnail))
        {
            graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphic.SmoothingMode = SmoothingMode.HighQuality;
            graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphic.CompositingQuality = CompositingQuality.HighQuality;

            // Figure out the ratio
            double ratioX = canvasWidth / (double)originalWidth;
            double ratioY = canvasHeight / (double)originalHeight;

            double ratio = ratioX < ratioY ? ratioX : ratioY; // use whichever multiplier is smaller

            // now we can get the new height and width
            int newHeight = Convert.ToInt32(originalHeight * ratio);
            int newWidth = Convert.ToInt32(originalWidth * ratio);

            // Now calculate the X,Y position of the upper-left corner 
            // (one of these will always be zero)
            int posX = Convert.ToInt32((canvasWidth - originalWidth * ratio) / 2);
            int posY = Convert.ToInt32((canvasHeight - originalHeight * ratio) / 2);

            graphic.Clear(Color.Transparent); // white padding
            graphic.DrawImage(image, posX, posY, newWidth, newHeight);
        }

        return thumbnail;
    }
}