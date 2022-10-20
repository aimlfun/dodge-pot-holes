using CarDodge.Settings;
using CarDodge.Utilities;

namespace CarDodge.Vision;

/// <summary>
/// Simple vision implementation of a LIDAR.
/// </summary>
internal class MonoLIDAR
{
    /// <summary>
    /// 
    /// </summary>
    readonly int radiusOfCarInPX = 10; // we process in radius, not diameter, so calc once here

    /// <summary>
    /// The AI car needs to know how far it is from the curb and pot holes, after all we're training it to stay on the road
    /// and avoid pot holes.
    /// 
    /// We do this as a LIDAR approach minus the fancy/expensive laser. 
    /// 
    /// We check pixels in the config defined directions.
    /// 
    /// Typically that would be forwards, diagonally forward to inform it as it turns, and to the sides so it knows how
    /// close it is to obstructions
    /// 
    /// The car could be moving in any direction, we have to compute LIDAR hits with the forward line pointing
    /// where the car is going.
    /// 
    ///     \  |  /
    ///      \ | /        __       __
    ///  _____\|/_____     |\  /|\  /|
    ///   +--------+         \  |  /        .¦
    ///   |   AI   |          ) | (      .:¦¦¦
    ///   +--------+          angle      speed
    /// </summary>
    /// <returns>Array of proximities (each 0..1F).</returns>
    /// <param name="image">The image of the road.</param>
    /// <param name="AngleLookingInDegrees">Which way the car is facing.</param>
    /// <param name="location">Where the car is.</param>
    /// <returns>Proximities 0..1 of what it sees</returns>
    /// <exception cref="NotImplementedException"></exception>
    public double[] VisionSensorOutput(Bitmap image, double AngleLookingInDegrees, PointF location)
    {
        ConfigAI aiConfig = Settings.Config.s_settings.AI;

        // e.g 
        // input to the neural network
        //   _ \ | / _   
        //   0 1 2 3 4 
        //        
        double[] LIDAROutput = new double[aiConfig.SamplePoints];

        //   _ \ | / _   
        //   0 1 2 3 4
        //   ^ this
        float LIDARAngleToCheckInDegrees = aiConfig.FieldOfVisionStartInDegrees;

        //   _ \ | / _   
        //   0 1 2 3 4
        //   [-] this
        float LIDARVisionAngleInDegrees = aiConfig.VisionAngleInDegrees;

        int maxSearchDistanceInPixels = aiConfig.DepthOfVisionInPixels + radiusOfCarInPX;
        float sp = (float)aiConfig.SamplePoints / 2F;

        // for each sample point...
        for (int LIDARangleIndex = 0; LIDARangleIndex < aiConfig.SamplePoints; LIDARangleIndex++)
        {
            int searchDistanceInPixels = maxSearchDistanceInPixels;

            if (aiConfig.ReduceVisionDepthAtSides)
            {
                searchDistanceInPixels = (int)(searchDistanceInPixels * (sp - Math.Abs((float)LIDARangleIndex - sp)) / sp);
            }

            //     -45  0  45
            //  -90 _ \ | / _ 90   <-- relative to direction of car, hence + angle car is pointing
            double LIDARangleToCheckInRadians = Utils.DegreesInRadians(AngleLookingInDegrees + LIDARAngleToCheckInDegrees);

            // calculate ONCE per angle, not per radius.
            double cos = Math.Cos(LIDARangleToCheckInRadians);
            double sin = Math.Sin(LIDARangleToCheckInRadians);

            float howCloseToObstructionIsForThisAngle = 0;

            // We don't want the car driving thru the curb or obstacles, so there is little advantage of checking pixels very close to
            // the car. To avoid accidents, we need to check radiating outwards from the vehicle and find the first square
            // of red in that direction. i.e. we don't care if there is red 30 pixels away if there is red right next to the car.

            // Given the car is a blob of 5px radius, the first 5 pixels are the car itself. We'd need to start
            // checking at least 5 pixels away.
            //         .5.        px
            //  |..15..(o)..15..|

            // Based on config, we can look ahead. But be mindful, every pixel we check takes time.
            for (int currentLIDARscanningDistanceRadius = radiusOfCarInPX;
                     currentLIDARscanningDistanceRadius < searchDistanceInPixels;
                     currentLIDARscanningDistanceRadius += 2) // no need to check at 1 pixel resolution
            {
                // simple maths, think circle. We are picking a point at an arbitrary angle at an arbitrary distance from a center point.
                // r = LIDARscanningDistance, angle = LIDARangleToCheckInDegrees

                // we need to convert that into a relative horizontal / vertical position, then add that to the cars location
                // X = r cos angle | y = r sin angle
                int positionOnTrackBeingScannedX = (int)Math.Round(cos * currentLIDARscanningDistanceRadius);
                int positionOnTrackBeingScannedY = (int)Math.Round(sin * currentLIDARscanningDistanceRadius);

                int posX = (int)(location.X + positionOnTrackBeingScannedX);
                int posY = (int)(location.Y + positionOnTrackBeingScannedY);

                // do we see red on that pixel?
                if (posX > 0 && posX < image.Width && posY >= 0 && posY < image.Height)
                {
                    Color c = image.GetPixel(posX, posY);

                    if (Config.s_settings.Display.ShowLIDAR) image.SetPixel(posX, posY, Color.Cyan);

                    if (c.R == 255 && c.G == 0 && c.B == 0) // red
                    {
                        howCloseToObstructionIsForThisAngle = currentLIDARscanningDistanceRadius;
                        break; // we've found the closest pixel in this direction
                    }
                }
            }

            // at this point we have proximity of red objects in a single direction

            // >0 means there is grass within the LIDAR radius
            if (howCloseToObstructionIsForThisAngle > 0)
            {
                howCloseToObstructionIsForThisAngle -= radiusOfCarInPX;

                // the range is 20..30, so we subtract 20 to bring it in the 0-10 range.
                howCloseToObstructionIsForThisAngle /= aiConfig.DepthOfVisionInPixels;
                howCloseToObstructionIsForThisAngle.Clamp(0, 1);

                // the neural network cares about 0..1 for inputs so we scale but
                // but we also need to invert so that "1" needs to mean REALLY close (neuron fires), "0" means no grass
                LIDAROutput[LIDARangleIndex] = 1 - howCloseToObstructionIsForThisAngle;
            }
            else
            {
                LIDAROutput[LIDARangleIndex] = 0; // no grass within this direction
            }

            //   _ \ | / _         _ \ | / _   
            //   0 1 2 3 4         0 1 2 3 4
            //  [-] from this       [-] to this
            LIDARAngleToCheckInDegrees += LIDARVisionAngleInDegrees;
        }

        // an array of float values 0..1 indicating "1" obstacle is really close in that direction to "0" no obstacle.
        return LIDAROutput;
    }
}