using OpenCvSharp;
using StitchCore.Alignment;
using StitchCore.Blending;
using StitchCore.IO;
using StitchCore.Models;
using StitchCore.Optimization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StitchCore;

/// <summary>
/// Main API for microscope image stitching with production-ready features
/// </summary>
public class MicroscopeStitcher : IDisposable
{
    private readonly StitchingConfiguration _config;
    private PhaseCorrelationAligner? _aligner;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the MicroscopeStitcher
    /// </summary>
    /// <param name="config">Stitching configuration parameters</param>
    public MicroscopeStitcher(StitchingConfiguration? config = null)
    {
        _config = config ?? new StitchingConfiguration();
    }

    /// <summary>
    /// Stitches microscope image tiles into a mosaic
    /// </summary>
    /// <param name="tiles">List of tile information with stage positions</param>
    /// <param name="outputPath">Path for the output stitched image</param>
    /// <param name="progress">Optional progress callback</param>
    /// <returns>Stitching result with metadata</returns>
    public async Task<StitchingResult> StitchAsync(
        List<TileInfo> tiles,
        string outputPath,
        IProgress<(string stage, int current, int total)>? progress = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new StitchingResult { TotalTiles = tiles.Count };

        try
        {
            // Validate inputs
            if (!ValidateInput(tiles, result))
                return result;

            // Step 1: Load and validate tile dimensions
            progress?.Report(("Loading tiles", 0, tiles.Count));
            LoadTileDimensions(tiles);

            // Step 2: Perform pairwise alignment
            progress?.Report(("Aligning tiles", 0, tiles.Count));
            var alignments = await PerformPairwiseAlignmentAsync(tiles, progress);

            // Step 3: Global optimization
            progress?.Report(("Optimizing positions", 0, 1));
            var optimizer = new GlobalOptimizer(_config);
            var optimizedTiles = optimizer.OptimizePositions(tiles, alignments);

            // Step 4: Apply global drift correction if enabled
            if (_config.EnableGlobalDriftCorrection)
            {
                progress?.Report(("Correcting drift", 0, 1));
                var (driftX, driftY) = optimizer.EstimateGlobalDrift(optimizedTiles);
                result.Warnings.Add($"Global drift detected: X={driftX:F2}px, Y={driftY:F2}px");
            }

            // Step 5: Calculate output canvas size
            var (canvasSize, offset) = CalculateCanvasSize(optimizedTiles);
            AdjustTilePositions(optimizedTiles, offset);

            // Step 6: Blend and render
            progress?.Report(("Blending tiles", 0, tiles.Count));
            using var stitchedImage = BlendTiles(optimizedTiles, canvasSize);

            // Step 7: Save output
            progress?.Report(("Saving output", 0, 1));
            SaveImage(stitchedImage, outputPath);
            result.OutputImagePath = outputPath;

            // Step 8: Export coordinates if enabled
            if (_config.ExportTileCoordinates)
            {
                var coordPath = _config.CoordinatesOutputPath 
                    ?? Path.ChangeExtension(outputPath, ".coords.csv");
                var exporter = new CoordinateExporter();
                exporter.ExportToCsv(optimizedTiles, coordPath);
                result.CoordinatesFilePath = coordPath;
            }

            // Calculate statistics
            result.AlignedTiles = optimizedTiles.Count(t => t.IsAligned);
            result.AverageDeviation = optimizedTiles.Average(t => t.PositionDeviation);
            result.MaxDeviation = optimizedTiles.Max(t => t.PositionDeviation);
            result.OptimizedTiles = optimizedTiles;
            result.Success = true;

            // Validate constraint
            if (result.MaxDeviation > _config.MaxPositionDeviation)
            {
                result.Warnings.Add(
                    $"Maximum deviation ({result.MaxDeviation:F2}px) exceeds configured limit ({_config.MaxPositionDeviation}px)");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Stitching failed: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;
            result.PeakMemoryUsageMB = GC.GetTotalMemory(false) / (1024 * 1024);
        }

        return result;
    }

    private bool ValidateInput(List<TileInfo> tiles, StitchingResult result)
    {
        if (tiles == null || tiles.Count == 0)
        {
            result.Errors.Add("No tiles provided");
            return false;
        }

        foreach (var tile in tiles)
        {
            if (!File.Exists(tile.FilePath))
            {
                result.Errors.Add($"Tile file not found: {tile.FilePath}");
                return false;
            }
        }

        return true;
    }

    private void LoadTileDimensions(List<TileInfo> tiles)
    {
        Parallel.ForEach(tiles, tile =>
        {
            using var img = Cv2.ImRead(tile.FilePath, ImreadModes.AnyColor);
            if (!img.Empty())
            {
                tile.Width = img.Width;
                tile.Height = img.Height;
            }
        });
    }

    private async Task<List<(TileInfo, TileInfo, double, double, double)>> PerformPairwiseAlignmentAsync(
        List<TileInfo> tiles,
        IProgress<(string, int, int)>? progress)
    {
        _aligner = new PhaseCorrelationAligner(_config);
        var alignments = new List<(TileInfo, TileInfo, double, double, double)>();

        // Find overlapping tile pairs
        var pairs = FindOverlappingPairs(tiles);

        int completed = 0;
        var lockObj = new object();

        await Task.Run(() =>
        {
            Parallel.ForEach(pairs, new ParallelOptions 
            { 
                MaxDegreeOfParallelism = _config.MaxDegreeOfParallelism == 0 
                    ? Environment.ProcessorCount 
                    : _config.MaxDegreeOfParallelism 
            }, pair =>
            {
                var (overlap1, overlap2) = CalculateOverlapRegions(pair.tile1, pair.tile2);
                var (dx, dy, confidence) = _aligner.AlignTiles(pair.tile1, pair.tile2, overlap1, overlap2);

                if (confidence > _config.PhaseCorrelationThreshold)
                {
                    lock (lockObj)
                    {
                        alignments.Add((pair.tile1, pair.tile2, dx, dy, confidence));
                    }
                }

                Interlocked.Increment(ref completed);
                progress?.Report(("Aligning tiles", completed, pairs.Count));
            });
        });

        return alignments;
    }

    private List<(TileInfo tile1, TileInfo tile2)> FindOverlappingPairs(List<TileInfo> tiles)
    {
        var pairs = new List<(TileInfo, TileInfo)>();

        for (int i = 0; i < tiles.Count; i++)
        {
            for (int j = i + 1; j < tiles.Count; j++)
            {
                if (TilesOverlap(tiles[i], tiles[j]))
                {
                    pairs.Add((tiles[i], tiles[j]));
                }
            }
        }

        return pairs;
    }

    private bool TilesOverlap(TileInfo tile1, TileInfo tile2)
    {
        double minOverlap = Math.Min(tile1.Width, tile1.Height) * _config.ExpectedOverlap;

        return Math.Abs(tile1.StageX - tile2.StageX) < (tile1.Width - minOverlap) &&
               Math.Abs(tile1.StageY - tile2.StageY) < (tile1.Height - minOverlap);
    }

    private (Rect overlap1, Rect overlap2) CalculateOverlapRegions(TileInfo tile1, TileInfo tile2)
    {
        double overlapX = Math.Max(0, Math.Min(tile1.StageX + tile1.Width, tile2.StageX + tile2.Width) 
            - Math.Max(tile1.StageX, tile2.StageX));
        double overlapY = Math.Max(0, Math.Min(tile1.StageY + tile1.Height, tile2.StageY + tile2.Height) 
            - Math.Max(tile1.StageY, tile2.StageY));

        var overlap1 = new Rect(
            (int)(Math.Max(0, tile2.StageX - tile1.StageX)),
            (int)(Math.Max(0, tile2.StageY - tile1.StageY)),
            (int)overlapX,
            (int)overlapY);

        var overlap2 = new Rect(
            (int)(Math.Max(0, tile1.StageX - tile2.StageX)),
            (int)(Math.Max(0, tile1.StageY - tile2.StageY)),
            (int)overlapX,
            (int)overlapY);

        return (overlap1, overlap2);
    }

    private (Size size, Point offset) CalculateCanvasSize(List<TileInfo> tiles)
    {
        double minX = tiles.Min(t => t.OptimizedX);
        double minY = tiles.Min(t => t.OptimizedY);
        double maxX = tiles.Max(t => t.OptimizedX + t.Width);
        double maxY = tiles.Max(t => t.OptimizedY + t.Height);

        var size = new Size((int)Math.Ceiling(maxX - minX), (int)Math.Ceiling(maxY - minY));
        var offset = new Point((int)Math.Round(minX), (int)Math.Round(minY));

        return (size, offset);
    }

    private void AdjustTilePositions(List<TileInfo> tiles, Point offset)
    {
        foreach (var tile in tiles)
        {
            tile.OptimizedX -= offset.X;
            tile.OptimizedY -= offset.Y;
        }
    }

    private Mat BlendTiles(List<TileInfo> tiles, Size canvasSize)
    {
        var blender = new SeamBlender(_config);
        return blender.BlendTiles(tiles, canvasSize);
    }

    private void SaveImage(Mat image, string outputPath)
    {
        var extension = Path.GetExtension(outputPath).ToLowerInvariant();
        
        switch (_config.OutputFormat)
        {
            case ImageFormat.Tiff:
            case ImageFormat.BigTiff:
                Cv2.ImWrite(outputPath, image);
                break;
            case ImageFormat.Png:
                Cv2.ImWrite(outputPath, image, new ImageEncodingParam(ImwriteFlags.PngCompression, 9));
                break;
            case ImageFormat.Jpeg:
                Cv2.ImWrite(outputPath, image, new ImageEncodingParam(ImwriteFlags.JpegQuality, 95));
                break;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _aligner?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}