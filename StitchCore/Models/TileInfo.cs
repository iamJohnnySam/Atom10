namespace StitchCore.Models;

public class TileInfo
{
    /// <summary>
    /// Unique identifier for the tile
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// File path to the image
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Original stage X position in micrometers or pixels
    /// </summary>
    public double StageX { get; set; }

    /// <summary>
    /// Original stage Y position in micrometers or pixels
    /// </summary>
    public double StageY { get; set; }
    
    /// <summary>
    /// Image width in pixels
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Image height in pixels
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Pixel size in X direction (optional metadata from POS files)
    /// </summary>
    public double PixelSizeX { get; set; }

    /// <summary>
    /// Pixel size in Y direction (optional metadata from POS files)
    /// </summary>
    public double PixelSizeY { get; set; }
    
    /// <summary>
    /// Aligned X position after optimization (in pixels)
    /// </summary>
    public double AlignedX { get; set; }

    /// <summary>
    /// Aligned Y position after optimization (in pixels)
    /// </summary>
    public double AlignedY { get; set; }

    /// <summary>
    /// Deviation from original stage position after optimization (in pixels)
    /// </summary>
    public double Deviation { get; set; }
}