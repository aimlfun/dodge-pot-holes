namespace CarDodge.Settings;

/// <summary>
/// Configuration
/// </summary>
internal class Config
{
    /// <summary>
    /// Singleton instance of the config.
    /// </summary>
    internal static Config s_settings = new();

    /// <summary>
    /// Configuration of the LIDAR (visual detection).
    /// </summary>
    internal ConfigAI AI = new();

    /// <summary>
    /// Configuration of the display.
    /// </summary>
    internal ConfigDisplay Display = new();
}