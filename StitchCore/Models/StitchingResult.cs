using System;
using System.Collections.Generic;

namespace StitchCore.Models;

/// <summary>
/// Result of the stitching operation
/// </summary>
public class StitchingResult
{
    /// <summary>
    /// Success status of the stitching operation
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Path to the output stitched image
    /// </summary>
    public string? OutputImagePath { get; set; }

    /// <summary>
    /// Path to the exported tile coordinates file
    /// </summary>
    public string? CoordinatesFilePath { get; set; }

    /// <summary>
    /// Total number of tiles processed
    /// </summary>
    public int TotalTiles { get; set; }

    /// <summary>
    /// Number of successfully aligned tiles
    /// </summary>
    public int AlignedTiles { get; set; }

    /// <summary>
    /// Average position deviation in pixels
    /// </summary>
    public double AverageDeviation { get; set; }

    /// <summary>
    /// Maximum position deviation in pixels
    /// </summary>
    public double MaxDeviation { get; set; }

    /// <summary>
    /// Total processing time
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Peak memory usage in megabytes
    /// </summary>
    public long PeakMemoryUsageMB { get; set; }

    /// <summary>
    /// List of error messages if any
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of warning messages
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Updated tile information with optimized positions
    /// </summary>
    public List<TileInfo> OptimizedTiles { get; set; } = new();
}