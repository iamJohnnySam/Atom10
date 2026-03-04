using OpenCvSharp;
using StitchCore.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace StitchCore.Alignment;

/// <summary>
/// Performs phase correlation-based image alignment with sub-pixel precision
/// </summary>
public class PhaseCorrelationAligner : IDisposable
{
    private readonly StitchingConfiguration _config;
    private readonly ConcurrentDictionary<string, Mat> _imageCache;
    private long _currentMemoryUsage;
    private readonly object _memoryLock = new();

    public PhaseCorrelationAligner(StitchingConfiguration config)
    {
        _config = config;
        _imageCache = new ConcurrentDictionary<string, Mat>();
    }

    /// <summary>
    /// Aligns two overlapping tiles using phase correlation
    /// </summary>
    public (double dx, double dy, double confidence) AlignTiles(
        TileInfo tile1, 
        TileInfo tile2, 
        Rect overlap1, 
        Rect overlap2)
    {
        using var img1 = LoadTileRegion(tile1, overlap1);
        using var img2 = LoadTileRegion(tile2, overlap2);

        if (img1 == null || img2 == null)
            return (0, 0, 0);

        // Convert to grayscale if needed
        using var gray1 = ConvertToGrayscale(img1);
        using var gray2 = ConvertToGrayscale(img2);

        // Ensure same size for phase correlation
        var minWidth = Math.Min(gray1.Width, gray2.Width);
        var minHeight = Math.Min(gray1.Height, gray2.Height);
        
        using var crop1 = new Mat(gray1, new Rect(0, 0, minWidth, minHeight));
        using var crop2 = new Mat(gray2, new Rect(0, 0, minWidth, minHeight));

        // Convert to float for phase correlation
        using var float1 = new Mat();
        using var float2 = new Mat();
        crop1.ConvertTo(float1, MatType.CV_32F);
        crop2.ConvertTo(float2, MatType.CV_32F);

        // Downsample for initial alignment if configured
        using var down1 = _config.InitialAlignmentDownsample > 1 
            ? DownsampleImage(float1, _config.InitialAlignmentDownsample) 
            : float1.Clone();
        using var down2 = _config.InitialAlignmentDownsample > 1 
            ? DownsampleImage(float2, _config.InitialAlignmentDownsample) 
            : float2.Clone();

        // Perform phase correlation
        var (dx, dy, response) = PhaseCorrelate(down1, down2);

        // Scale back if downsampled
        if (_config.InitialAlignmentDownsample > 1)
        {
            dx *= _config.InitialAlignmentDownsample;
            dy *= _config.InitialAlignmentDownsample;
        }

        // Refine with sub-pixel precision if enabled
        if (_config.EnableSubPixelAlignment && response > _config.PhaseCorrelationThreshold)
        {
            (dx, dy, response) = RefineSubPixel(float1, float2, dx, dy);
        }

        return (dx, dy, response);
    }

    /// <summary>
    /// Performs phase correlation between two images
    /// </summary>
    private (double dx, double dy, double response) PhaseCorrelate(Mat img1, Mat img2)
    {
        // Pass 'null' for the optional window parameter
        Point2d shift = Cv2.PhaseCorrelate(img1, img2, null, out double response);
        return (shift.X, shift.Y, response);
    }

    /// <summary>
    /// Refines alignment to sub-pixel precision using iterative optimization
    /// </summary>
    private (double dx, double dy, double confidence) RefineSubPixel(
        Mat img1, 
        Mat img2, 
        double initialDx, 
        double initialDy)
    {
        const double stepSize = 0.1;
        const int maxIterations = 10;
        
        double bestDx = initialDx;
        double bestDy = initialDy;
        double bestScore = EvaluateAlignment(img1, img2, initialDx, initialDy);

        for (int iter = 0; iter < maxIterations; iter++)
        {
            bool improved = false;

            // Test 8 directions around current position
            for (double deltaX = -stepSize; deltaX <= stepSize; deltaX += stepSize)
            {
                for (double deltaY = -stepSize; deltaY <= stepSize; deltaY += stepSize)
                {
                    if (Math.Abs(deltaX) < 0.01 && Math.Abs(deltaY) < 0.01) continue;

                    double testDx = bestDx + deltaX;
                    double testDy = bestDy + deltaY;
                    double score = EvaluateAlignment(img1, img2, testDx, testDy);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestDx = testDx;
                        bestDy = testDy;
                        improved = true;
                    }
                }
            }

            if (!improved) break;
        }

        return (bestDx, bestDy, bestScore);
    }

    /// <summary>
    /// Evaluates alignment quality using normalized cross-correlation
    /// </summary>
    private double EvaluateAlignment(Mat img1, Mat img2, double dx, double dy)
    {
        using var transform = Mat.Eye(2, 3, MatType.CV_64F).ToMat();
        transform.Set<double>(0, 2, dx);
        transform.Set<double>(1, 2, dy);    

        // Apply transformation
        using var shifted = new Mat();
        Cv2.WarpAffine(img2, shifted, transform, img2.Size(), InterpolationFlags.Cubic);

        // Calculate overlap region
        int validWidth = (int)(img1.Width - Math.Abs(dx));
        int validHeight = (int)(img1.Height - Math.Abs(dy));
        
        if (validWidth <= 0 || validHeight <= 0)
            return 0;

        int startX = (int)Math.Max(0, dx);
        int startY = (int)Math.Max(0, dy);
        
        var rect = new Rect(startX, startY, validWidth, validHeight);
        
        using var roi1 = new Mat(img1, rect);
        using var roi2 = new Mat(shifted, rect);

        // Calculate normalized cross-correlation
        using var result = new Mat();
        Cv2.MatchTemplate(roi1, roi2, result, TemplateMatchModes.CCoeffNormed);
        
        Cv2.MinMaxLoc(result, out _, out double maxVal);
        return maxVal;
    }

    private Mat? LoadTileRegion(TileInfo tile, Rect region)
    {
        var img = LoadTileWithCache(tile);
        if (img == null) return null;

        // Ensure region is within bounds
        var safeRegion = new Rect(
            Math.Max(0, region.X),
            Math.Max(0, region.Y),
            Math.Min(region.Width, img.Width - Math.Max(0, region.X)),
            Math.Min(region.Height, img.Height - Math.Max(0, region.Y))
        );

        if (safeRegion.Width <= 0 || safeRegion.Height <= 0)
            return null;

        return new Mat(img, safeRegion).Clone();
    }

    private Mat? LoadTileWithCache(TileInfo tile)
    {
        if (_imageCache.TryGetValue(tile.FilePath, out var cached))
            return cached;

        // Check memory usage
        var fileInfo = new FileInfo(tile.FilePath);
        if (!fileInfo.Exists) return null;
        
        if (!CanAllocateMemory(fileInfo.Length))
        {
            // Evict oldest entries
            EvictCache(fileInfo.Length);
        }

        var img = Cv2.ImRead(tile.FilePath, ImreadModes.AnyColor | ImreadModes.AnyDepth);
        if (img.Empty())
        {
            img.Dispose();
            return null;
        }

        _imageCache[tile.FilePath] = img;
        
        lock (_memoryLock)
        {
            _currentMemoryUsage += fileInfo.Length;
        }

        return img;
    }

    private bool CanAllocateMemory(long bytes)
    {
        lock (_memoryLock)
        {
            return (_currentMemoryUsage + bytes) < (_config.MaxMemoryUsageMB * 1024 * 1024);
        }
    }

    private void EvictCache(long requiredBytes)
    {
        var toRemove = new List<string>();
        long freedMemory = 0;

        foreach (var kvp in _imageCache)
        {
            toRemove.Add(kvp.Key);
            kvp.Value.Dispose();
            
            var fileInfo = new FileInfo(kvp.Key);
            if (fileInfo.Exists)
                freedMemory += fileInfo.Length;

            if (freedMemory >= requiredBytes)
                break;
        }

        foreach (var key in toRemove)
        {
            _imageCache.TryRemove(key, out _);
        }

        lock (_memoryLock)
        {
            _currentMemoryUsage -= freedMemory;
        }
    }

    private Mat ConvertToGrayscale(Mat img)
    {
        if (img.Channels() == 1) return img.Clone();
        
        var gray = new Mat();
        Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);
        return gray;
    }

    private Mat DownsampleImage(Mat img, int factor)
    {
        var downsampled = new Mat();
        var newSize = new Size(img.Width / factor, img.Height / factor);
        Cv2.Resize(img, downsampled, newSize, 0, 0, InterpolationFlags.Area);
        return downsampled;
    }

    public void Dispose()
    {
        foreach (var img in _imageCache.Values)
        {
            img?.Dispose();
        }
        _imageCache.Clear();
    }
}