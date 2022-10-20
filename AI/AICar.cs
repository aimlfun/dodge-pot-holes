using CarDodge.Settings;
using CarDodge.Utilities;
using CarDodge.Vision;
using System.Text;

namespace CarDodge.AI;

/// <summary>
/// Represents the AI driven cars.
/// </summary>
internal class AICar
{
    #region STATIC PROPERIES
    /// <summary>
    /// This provides the "vision" to the car, in the form of a LIDAR.
    /// </summary>
    internal static MonoLIDAR s_vision = new();

    /// <summary>
    /// Drawing all the pixels wouldn't be quick, so we blit this image (rotated as required).
    /// </summary>
    internal static Bitmap s_carBitmap = new("Assets/car.png");

    /// <summary>
    /// How wide the potholes are.
    /// </summary>
    internal static int s_potholeWidth = 44;

    /// <summary>
    /// How tall the potholes are.
    /// </summary>
    internal static int s_potholeHeight = 20;

    /// <summary>
    /// Used to track what AI cars we have created.
    /// </summary>
    private static readonly Dictionary<int, AICar> s_listOfAICars = new();
    #endregion

    #region PROPERTIES
    /// <summary>
    /// true - this car has failed (is eliminated).
    /// </summary>
    internal bool isEliminated = false;

    /// <summary>
    /// Identifier for the car. Used to know which AI it is connected to.
    /// </summary>
    internal int Id = 0;

    /// <summary>
    /// Contains the current location of the AI car.
    /// </summary>
    internal PointF Location = new();

    /// <summary>
    /// Contains the angle the AI car is pointing.
    /// </summary>
    internal float AngleInDegrees = 0;
    #endregion

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="laneWidth"></param>
    internal AICar(int id, int startX, float startY)
    {
        Id = id; // this links to the neural network id, as is used to reference this car.

        // We start the car 50px from left edge, because we eliminate slow cars scrolled off screen.
        // If we started at zero, many would get destroyed before getting to show what they can do.

        Location.X = startX + 50;
        Location.Y = startY;

        // pointing right horizontally
        AngleInDegrees = 0;
    }

    /// <summary>
    /// Clears the car list, and creates new ones reset at their start position.
    /// </summary>
    /// <param name="roadWidth"></param>
    /// <param name="laneWidth"></param>
    internal static void NewGeneration(int roadWidth, int laneWidth)
    {
        s_listOfAICars.Clear();

        for (int i = 0; i < Config.s_settings.AI.NumberOfAICarsToCreate; i++)
        {
            s_listOfAICars.Add(i, new AICar(i, roadWidth / 2, (int)(2 * laneWidth - laneWidth / 2)));
        }
    }

    /// <summary>
    /// Move all the AI cars with collision detection.
    /// </summary>
    /// <param name="road"></param>
    /// <returns></returns>
    internal static bool MoveAICars(Bitmap road, int viewPortX, out float maxCarX, out int idRemainingCar)
    {
        bool allEliminated = true;

        maxCarX = 0;
        int carsRemaining = 0;
        idRemainingCar = -1;

        foreach (AICar aiCar in s_listOfAICars.Values)
        {
            if (aiCar.isEliminated) continue; // no need to move eliminated cars

            aiCar.Move(road, viewPortX);
            aiCar.UpdateFitness();

            if (aiCar.CollisionWithBarrierOrPotHole(road, viewPortX) ||
                // turned too much; primarily we do it to stop it going in circles off driving the wrong way
                aiCar.CarHasRotated90Degrees() ||
                // check to see if car is off screen because it is driving too slow.
                aiCar.CarIsOffScreen(viewPortX))
            {
                aiCar.isEliminated = true;
                continue;
            }

            // if it's not eliminated, we need to know - as mutation occurs when it reaches zero
            allEliminated = false;
            ++carsRemaining;

            // tracking the "id" enables it to show the neural network if one car remains.
            idRemainingCar = aiCar.Id;

            // we weed out slow cars by moving the viewport to the one in the lead (and slow go off
            // screen) and are eliminated
            if (aiCar.Location.X > maxCarX) maxCarX = aiCar.Location.X;
        }

        if (carsRemaining != 1) idRemainingCar = -1; // more than 1 car, which do we display?

        return allEliminated;
    }

    /// <summary>
    /// The car needs to point right. Beyond +/-90 degrees, it's going the wrong way.
    /// </summary>
    /// <returns>TRUE - car needs to be eliminated for bad driving.</returns>
    private bool CarHasRotated90Degrees()
    {
        return !(AngleInDegrees <= 90 || AngleInDegrees >= 270);
    }

    /// <summary>
    /// If cars are too slow they get forced off screen and we eliminate them.
    /// We do it to encourage faster driving.
    /// </summary>
    /// <param name="viewPortX"></param>
    /// <returns></returns>
    private bool CarIsOffScreen(int viewPortX)
    {
        return Location.X < viewPortX;
    }

    /// <summary>
    /// Has the car collided with the top/bottom barriers, or pothole? (something red).
    /// </summary>
    /// <param name="road"></param>
    /// <param name="viewPortX"></param>
    private bool CollisionWithBarrierOrPotHole(Bitmap road, int viewPortX)
    {
        PointF[] points = DetermineHitTestPoints();

        foreach (PointF hitTestPoint in points)
        {
            int posX = (int)(0.5F + hitTestPoint.X) - viewPortX;
            int posY = (int)(0.5F + hitTestPoint.Y);

            if (posX < 0 || posY < 0 || posX >= road.Width || posY >= road.Height) continue;

            Color c = road.GetPixel(posX, posY);

            if (c.R == 255 && c.G == 0 && c.B == 0)
            {
                return true; // collided
            }
        }

        return false; // no collision
    }

    /// <summary>
    /// Draws ALL the AI cars.
    /// </summary>
    /// <param name="g"></param>
    internal static void DrawCars(Graphics g, int viewPortX)
    {
        foreach (int id in s_listOfAICars.Keys)
        {
            AICar aiCar = s_listOfAICars[id];

            if (!aiCar.isEliminated) aiCar.Draw(g, viewPortX);
        }
    }

    /// <summary>
    /// Updates the AI with how well the car fitted the model.
    /// For this demo, it's how far travelled.
    /// </summary>
    internal void UpdateFitness()
    {
        NeuralNetwork.s_networks[Id].Fitness = (int)Math.Round(Location.X);
    }

    /// <summary>
    /// Unreal "physics" of car, but sufficient for the AI demo.
    /// output[1] from AI determines what speed the car travels
    /// output[0] from AI determines how much to *rotate* the car.
    /// </summary>
    /// <param name="road"></param>
    /// <param name="viewPortX"></param>
    internal void Move(Bitmap road, int viewPortX)
    {
        ConfigAI aiConfig = Settings.Config.s_settings.AI;

        double[] neuralNetworkInput = s_vision.VisionSensorOutput(road, AngleInDegrees, new PointF(Location.X - viewPortX, Location.Y)); /// input is the distance sensors of how soon we'll impact

        // ask the neural to use the input and decide what to do with the car
        double[] outputFromNeuralNetwork = NeuralNetwork.s_networks[Id].FeedForward(neuralNetworkInput); // process inputs

        // a biases of 1.5 ensures the car has to move forward, and not stop.
        // so technically the AI decides whether to slow down to minimum speed or speed up.
        float speed = 1.5F + ((float)outputFromNeuralNetwork[ConfigAI.c_throttleNeuron] * aiConfig.SpeedAmplifier).Clamp(0, 1);

        // Remember: speed x angle determines where the car ends up next
        AngleInDegrees += (float)outputFromNeuralNetwork[ConfigAI.c_steeringNeuron] * aiConfig.SteeringAmplifier;

        // it'll work even if we violate this, but let's keep it clean 0..359.999 degrees.
        if (AngleInDegrees < 0) AngleInDegrees += 360;
        if (AngleInDegrees >= 360) AngleInDegrees -= 360;

        // move the car using basic sin/cos math ->  x = r * cos(theta), y = r * sin(theta)
        // in this instance "r" is the speed output, theta is the angle of the car.

        double angleCarIsPointingInRadians = Utils.DegreesInRadians(AngleInDegrees);
        Location.X += (float)Math.Cos(angleCarIsPointingInRadians) * speed;
        Location.Y += (float)Math.Sin(angleCarIsPointingInRadians) * speed;
    }

    /// <summary>
    /// Compute the hit points based on the angle of the car, and its location.
    /// </summary>
    /// <param name="car"></param>
    /// <returns></returns>
    internal PointF[] DetermineHitTestPoints()
    {
        // we work out the points at 0 degrees (car going right)
        PointF[] points = RawHitTestPointsWithCarFacing0Degrees();

        // but the car rotates, so we need to adjust the hit points.

        PointF originOfRotation = new((float)Location.X, (float)Location.Y);

        List<PointF> pointsRotatedForAngleOfCar = new();

        foreach (PointF p in points)
        {
            pointsRotatedForAngleOfCar.Add(Utils.RotatePointAboutOrigin(new PointF(p.X + originOfRotation.X, p.Y + originOfRotation.Y),
                                                                        originOfRotation,
                                                                        AngleInDegrees));
        }

        return pointsRotatedForAngleOfCar.ToArray();
    }

    /// <summary>
    /// Compute the hit points based on the car facing right (we rotate these based on the angle).
    /// We don't check rear, as the car is forward driving only.
    /// </summary>
    /// <returns></returns>
    internal static PointF[] RawHitTestPointsWithCarFacing0Degrees()
    {
        /*              X   X   X
         *             p3  p13   p1
         *   p5 +---------------+ X 
         *      |               |
         *      |        +      | X p12
         *      |               |
         *   p6 +---------------+ X
         *             p4  p24  p2
         *              X   X   X
         */

        PointF bottomRight = new(12, 6);
        PointF bottomLeft = new(-9, bottomRight.Y);
        PointF bottomMiddle = new(0, bottomRight.Y);
        PointF betweenBottomRightAndMiddle = new((bottomRight.X + bottomMiddle.X) / 2, bottomRight.Y);

        PointF topRight = new(bottomRight.X, -8);
        PointF topLeft = new(bottomLeft.X, topRight.Y);
        PointF topMiddle = new(0, topRight.Y);
        PointF betweenTopRightAndMiddle = new((topRight.X + topMiddle.X) / 2, topRight.Y);

        PointF frontMiddle = new(bottomRight.X + 2, (bottomRight.Y + topRight.Y) / 2);

        return new PointF[] { bottomRight, frontMiddle, topRight, betweenTopRightAndMiddle, topMiddle, topLeft, bottomLeft, bottomMiddle, betweenBottomRightAndMiddle };
    }

    /// <summary>
    /// Draws dots where the hit points are. Great for debugging!
    /// </summary>
    /// <param name="g"></param>
    /// <param name="viewPortX"></param>
    /// <exception cref="ArgumentNullException"></exception>
    internal void DrawHitPoints(Graphics g, int viewPortX)
    {
        if (g is null) throw new ArgumentNullException(nameof(g), "this method paints to the graphics, but cannot if one isn't provided.");

        PointF[] points = DetermineHitTestPoints();

        foreach (PointF hitTestPoint in points)
        {
            g.DrawRectangle(Pens.Cyan, (int)(0.5F + hitTestPoint.X - viewPortX), (int)(0.5F + hitTestPoint.Y), 1, 1);
        }
    }

    /// <summary>
    /// Draws the car, with collision detection hit points overlayed.
    /// </summary>
    /// <param name="g"></param>
    /// <param name="viewPortXLeftEdge"></param>
    internal void Draw(Graphics g, int viewPortXLeftEdge)
    {
        Bitmap b = Utils.RotateBitmapWithColoredBackground(s_carBitmap, AngleInDegrees); // do not dispose(), this is shared.

        g.DrawImage(b,
                    new Point((int)Location.X - b.Width / 2 - viewPortXLeftEdge, // everything is from viewport .. viewport + width
                              (int)Location.Y - b.Height / 2));

        if (Config.s_settings.Display.ShowHitPointsOnCar) DrawHitPoints(g, viewPortXLeftEdge); // debug, so you can see where it detects collision.

        g.DrawString(Id.ToString(), Config.s_settings.Display.fontForCarLabel, Brushes.Black, (int)Location.X - viewPortXLeftEdge - 10, (int)Location.Y - 7);
    }

}
