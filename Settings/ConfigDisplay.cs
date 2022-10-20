namespace CarDodge.Settings;

/// <summary>
/// Items around visual display
/// </summary>
internal class ConfigDisplay
{
    /// <summary>
    /// If TRUE it draws dot where the collision sensors are.
    /// </summary>
    internal bool ShowHitPointsOnCar { get; set; } = false;

    /// <summary>
    /// If TRUE it draws the LIDAR.
    /// </summary>
    internal bool ShowLIDAR { get; set; } = false;

    #region PENS
    /// <summary>
    /// Used to draw the white dotted lines between lanes.
    /// </summary>
    internal readonly Pen DottedRoadLaneLinesPen = new(Color.White, 1);

    /// <summary>
    /// Red out of bounds lines used to stop cars going off the road.
    /// </summary>
    internal readonly Pen RedOutOfBoundsLines = new(Color.Red, 2);
    #endregion

    #region FONTS

    /// <summary>
    /// 
    /// </summary>
    internal readonly Font fontForCarLabel = new("Arial", 8, FontStyle.Bold);
    #endregion

    /// <summary>
    /// Constructor
    /// </summary>
    internal ConfigDisplay()
    {
        float[] dashValues = { 3, 6 };
        DottedRoadLaneLinesPen.DashPattern = dashValues;
    }
}