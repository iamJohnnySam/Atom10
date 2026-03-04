using OpenCvSharp;
using StitchCore.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StitchCore.Blending;

/// <summary>
/// Performs seam blending and intensity normalization for seamless stitching
/// </summary>
public class SeamBlender
{
    private readonly StitchingConfiguration _config;

    public SeamBlender(StitchingConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Blends overlapping regions between tiles using multi-band blending
    /// </summary>
    public Mat BlendTiles(List<TileInfo> tiles, Size outputSize)
    {
        // Calculate intensity normalization factors
        var normalizationFactors = _config.EnableIntensityNormalization 
            ? CalculateNormalizationFactors(tiles) 
            : new Dictionary<string, double>();

        // Create output canvas
        var output = Mat.Zeros((int)outputSize.Height, (int)outputSize.Width, MatType.CV_32FC3);
        var weightMap = Mat.Zeros((int)outputSize.Height, (int)outputSize.Width, MatType.CV_32FC1);

        // Process tiles sequentially to avoid threading issues
        foreach (var tile in tiles)
        {
            using var img = Cv2.ImRead(tile.FilePath, ImreadModes.Color);
            if (img.Empty()) continue;

            // Convert to float
            using var imgFloat = new Mat();
            img.ConvertTo(imgFloat, MatType.CV_32FC3);

            // Apply intensity normalization
            if (_config.EnableIntensityNormalization && 
                normalizationFactors.TryGetValue(tile.Id, out double factor))
            {
                imgFloat.ConvertTo(imgFloat, -1, factor, 0);
            }

            // Calculate tile position on canvas
            int x = (int)Math.Round(tile.OptimizedX);
            int y = (int)Math.Round(tile.OptimizedY);

            // Create weight map for blending
            using var tileWeight = CreateWeightMap(imgFloat.Size(), _config.BlendingWidth);

            // Copy tile to output with blending
            BlendTileToCanvas(output, weightMap, imgFloat, tileWeight, new Point(x, y));
        }

        // Normalize by accumulated weights
        NormalizeByWeights(output, weightMap);

        // Convert back to 8-bit
        var result = new Mat();
        output.ToMat().ConvertTo(result, MatType.CV_8UC3);

        output.Dispose();
        weightMap.Dispose();

        return result;
    }

    /// <summary>
    /// Creates a distance-based weight map for smooth blending
    /// </summary>
    private Mat CreateWeightMap(Size size, int blendWidth)
    {
        var weight = new Mat(size, MatType.CV_32FC1);

        var indexer = weight.GetGenericIndexer<float>();

        for (int y = 0; y < size.Height; y++)
        {
            for (int x = 0; x < size.Width; x++)
            {
                // Calculate distance from edges
                int distLeft = x;
                int distRight = size.Width - x - 1;
                int distTop = y;
                int distBottom = size.Height - y - 1;

                int minDist = Math.Min(
                    Math.Min(distLeft, distRight),
                    Math.Min(distTop, distBottom)
                );

                // Smooth weight based on distance
                float w = Math.Min(1.0f, minDist / (float)blendWidth);
                indexer[y, x] = w;
            }
        }

        return weight;
    }

    /// <summary>
    /// Blends a tile onto the canvas using weighted averaging
    /// </summary>
    private void BlendTileToCanvas(Mat canvas, Mat weightMap, Mat tile, Mat tileWeight, Point position)
    {
        // Calculate valid region
        var canvasRect = new Rect(0, 0, canvas.Width, canvas.Height);
        var tileRect = new Rect(position.X, position.Y, tile.Width, tile.Height);
        
        var intersectX = Math.Max(canvasRect.X, tileRect.X);
        var intersectY = Math.Max(canvasRect.Y, tileRect.Y);
        var intersectWidth = Math.Min(canvasRect.X + canvasRect.Width, tileRect.X + tileRect.Width) - intersectX;
        var intersectHeight = Math.Min(canvasRect.Y + canvasRect.Height, tileRect.Y + tileRect.Height) - intersectY;

        if (intersectWidth <= 0 || intersectHeight <= 0)
            return;

        var intersection = new Rect(intersectX, intersectY, intersectWidth, intersectHeight);

        // Calculate source and destination ROIs
        var srcRect = new Rect(
            intersection.X - tileRect.X,
            intersection.Y - tileRect.Y,
            intersection.Width,
            intersection.Height
        );

        var dstRect = intersection;

        // Extract ROIs
        using var srcROI = new Mat(tile, srcRect);
        using var weightROI = new Mat(tileWeight, srcRect);
        using var dstROI = new Mat(canvas, dstRect);
        using var dstWeightROI = new Mat(weightMap, dstRect);

        // Expand weight to 3 channels
        Mat[] weightChannels = { weightROI, weightROI, weightROI };
        using var weightExpanded = new Mat();
        Cv2.Merge(weightChannels, weightExpanded);

        // Weighted accumulation
        using var weighted = new Mat();
        Cv2.Multiply(srcROI, weightExpanded, weighted);

        using var sumWeighted = new Mat();
        Cv2.Add(dstROI, weighted, sumWeighted);
        sumWeighted.CopyTo(dstROI);

        // Update weights
        using var sumWeights = new Mat();
        Cv2.Add(dstWeightROI, weightROI, sumWeights);
        sumWeights.CopyTo(dstWeightROI);
    }

    /// <summary>
    /// Normalizes the blended image by accumulated weights
    /// </summary>
    private void NormalizeByWeights(Mat canvas, Mat weightMap)
    {
        Mat[] weightChannels = { weightMap, weightMap, weightMap };
        using var weightExpanded = new Mat();
        Cv2.Merge(weightChannels, weightExpanded);

        // Avoid division by zero
        using var mask = new Mat();
        Cv2.Threshold(weightMap, mask, 0.001, 1, ThresholdTypes.Binary);

        using var normalized = new Mat();
        Cv2.Divide(canvas, weightExpanded, normalized);
        
        normalized.CopyTo(canvas);
    }

    /// <summary>
    /// Calculates intensity normalization factors for all tiles
    /// </summary>
    private Dictionary<string, double> CalculateNormalizationFactors(List<TileInfo> tiles)
    {
        var factors = new ConcurrentDictionary<string, double>();
        var intensities = new ConcurrentBag<double>();

        // Calculate mean intensity for each tile
        Parallel.ForEach(tiles, tile =>
        {
            using var img = Cv2.ImRead(tile.FilePath, ImreadModes.Grayscale);
            if (!img.Empty())
            {
                var mean = Cv2.Mean(img);
                intensities.Add(mean.Val0);
                factors[tile.Id] = mean.Val0;
            }
        });

        // Calculate target intensity (median)
        var sorted = intensities.OrderBy(x => x).ToList();
        double targetIntensity = sorted.Count > 0 ? sorted[sorted.Count / 2] : 128.0;

        // Calculate normalization factors
        var result = new Dictionary<string, double>();
        foreach (var tile in tiles)
        {
            if (factors.TryGetValue(tile.Id, out double intensity) && intensity > 0)
            {
                result[tile.Id] = targetIntensity / intensity;
            }
            else
            {
                result[tile.Id] = 1.0;
            }
        }

        return result;
    }
}