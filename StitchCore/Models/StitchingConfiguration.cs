namespace StitchCore.Models;

/// <summary>
/// Configuration parameters for the stitching process
/// </summary>
public class StitchingConfiguration
{
    /// <summary>
    /// Maximum allowed deviation from stage position in pixels (default: 2.0)
    /// </summary>
    public double MaxPositionDeviation { get; set; } = 2.0;

    /// <summary>
    /// Expected overlap percentage between adjacent tiles (0.0 - 1.0)
    /// </summary>
    public double ExpectedOverlap { get; set; } = 0.1;

    /// <summary>
    /// Number of threads for parallel processing (0 = auto-detect)
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 0;

    /// <summary>
    /// Enable sub-pixel alignment precision
    /// </summary>
    public bool EnableSubPixelAlignment { get; set; } = true;

    /// <summary>
    /// Enable global drift correction
    /// </summary>
    public bool EnableGlobalDriftCorrection { get; set; } = true;

    /// <summary>
    /// Enable seam blending
    /// </summary>
    public bool EnableSeamBlending { get; set; } = true;

    /// <summary>
    /// Enable intensity normalization across tiles
    /// </summary>
    public bool EnableIntensityNormalization { get; set; } = true;

    /// <summary>
    /// Blending width in pixels for seam blending
    /// </summary>
    public int BlendingWidth { get; set; } = 50;

    /// <summary>
    /// Maximum memory usage in megabytes for tile caching
    /// </summary>
    public long MaxMemoryUsageMB { get; set; } = 4096;

    /// <summary>
    /// Phase correlation threshold for alignment acceptance (0.0 - 1.0)
    /// </summary>
    public double PhaseCorrelationThreshold { get; set; } = 0.3;

    /// <summary>
    /// Downsample factor for initial alignment (1 = no downsampling)
    /// </summary>
    public int InitialAlignmentDownsample { get; set; } = 2;

    /// <summary>
    /// Output format for the stitched image
    /// </summary>
    public ImageFormat OutputFormat { get; set; } = ImageFormat.Tiff;

    /// <summary>
    /// Export optimized tile coordinates
    /// </summary>
    public bool ExportTileCoordinates { get; set; } = true;

    /// <summary>
    /// Path for exporting tile coordinates (CSV format)
    /// </summary>
    public string? CoordinatesOutputPath { get; set; }
}

public enum ImageFormat
{
    Tiff,
    Png,
    Jpeg,
    BigTiff
}