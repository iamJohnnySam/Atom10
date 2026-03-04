using StitchCore;
using StitchCore.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StitchConsole;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== Microscope Image Stitching Tool ===");
        Console.WriteLine("Production-ready tile stitching with sub-pixel alignment\n");

        try
        {
            // Parse command line arguments
            if (args.Length < 2)
            {
                PrintUsage();
                return 1;
            }

            string inputPath = args[0];
            string outputImagePath = args[1];

            // Load configuration
            var config = new StitchingConfiguration
            {
                MaxPositionDeviation = 2.0,
                ExpectedOverlap = 0.1,
                EnableSubPixelAlignment = true,
                EnableGlobalDriftCorrection = true,
                EnableSeamBlending = true,
                EnableIntensityNormalization = true,
                BlendingWidth = 50,
                MaxMemoryUsageMB = 4096,
                PhaseCorrelationThreshold = 0.3,
                InitialAlignmentDownsample = 2,
                OutputFormat = ImageFormat.Tiff,
                ExportTileCoordinates = true,
                MaxDegreeOfParallelism = 0 // Auto-detect
            };

            // Override config from command line
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--max-deviation" && i + 1 < args.Length)
                    config.MaxPositionDeviation = double.Parse(args[++i], CultureInfo.InvariantCulture);
                else if (args[i] == "--overlap" && i + 1 < args.Length)
                    config.ExpectedOverlap = double.Parse(args[++i], CultureInfo.InvariantCulture);
                else if (args[i] == "--threads" && i + 1 < args.Length)
                    config.MaxDegreeOfParallelism = int.Parse(args[++i], CultureInfo.InvariantCulture);
                else if (args[i] == "--no-blend")
                    config.EnableSeamBlending = false;
                else if (args[i] == "--no-normalize")
                    config.EnableIntensityNormalization = false;
            }

            // Load tile metadata (auto-detect .POS files or use CSV)
            Console.WriteLine($"Loading tile metadata from: {inputPath}");
            var tiles = LoadTileMetadata(inputPath);
            Console.WriteLine($"Loaded {tiles.Count} tiles with position metadata");

            if (tiles.Count == 0)
            {
                Console.WriteLine("Error: No tiles loaded from metadata");
                return 1;
            }

            // Display metadata summary
            DisplayMetadataSummary(tiles);

            // Create stitcher
            using var stitcher = new MicroscopeStitcher(config);

            // Progress reporting
            var progress = new Progress<(string stage, int current, int total)>(p =>
            {
                if (p.total > 0)
                {
                    Console.Write($"\r{p.stage}: {p.current}/{p.total} ({100.0 * p.current / p.total:F1}%)    ");
                }
                else
                {
                    Console.Write($"\r{p.stage}...    ");
                }
            });

            // Perform stitching
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine("\nStarting stitching process...\n");
            
            var result = await stitcher.StitchAsync(tiles, outputImagePath, progress);
            
            stopwatch.Stop();
            Console.WriteLine("\n");

            // Print results
            if (result.Success)
            {
                Console.WriteLine("✓ Stitching completed successfully!");
                Console.WriteLine($"\nOutput: {result.OutputImagePath}");
                if (!string.IsNullOrEmpty(result.CoordinatesFilePath))
                    Console.WriteLine($"Coordinates: {result.CoordinatesFilePath}");
                
                Console.WriteLine($"\nStatistics:");
                Console.WriteLine($"  Total tiles: {result.TotalTiles}");
                Console.WriteLine($"  Aligned tiles: {result.AlignedTiles}");
                Console.WriteLine($"  Average deviation: {result.AverageDeviation:F3} px");
                Console.WriteLine($"  Maximum deviation: {result.MaxDeviation:F3} px");
                Console.WriteLine($"  Processing time: {result.ProcessingTime.TotalSeconds:F2} seconds");
                Console.WriteLine($"  Peak memory: {result.PeakMemoryUsageMB} MB");

                if (result.Warnings.Count > 0)
                {
                    Console.WriteLine($"\nWarnings:");
                    foreach (var warning in result.Warnings)
                        Console.WriteLine($"  ⚠ {warning}");
                }

                return 0;
            }
            else
            {
                Console.WriteLine("✗ Stitching failed!");
                foreach (var error in result.Errors)
                    Console.WriteLine($"  Error: {error}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage: StitchConsole <input> <output_image> [options]");
        Console.WriteLine("\nArguments:");
        Console.WriteLine("  input            CSV metadata file OR directory containing images with .POS files");
        Console.WriteLine("  output_image     Path for the stitched output image");
        Console.WriteLine("\nOptions:");
        Console.WriteLine("  --max-deviation <px>    Maximum position deviation (default: 2.0)");
        Console.WriteLine("  --overlap <ratio>       Expected overlap ratio (default: 0.1)");
        Console.WriteLine("  --threads <n>           Number of threads (default: auto)");
        Console.WriteLine("  --no-blend              Disable seam blending");
        Console.WriteLine("  --no-normalize          Disable intensity normalization");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  StitchConsole tiles.csv output.tif --threads 8");
        Console.WriteLine("  StitchConsole ./images/ output.tif");
        Console.WriteLine("  StitchConsole ./microscope_scan/ stitched.tif --max-deviation 5.0");
    }

    static List<TileInfo> LoadTileMetadata(string path)
    {
        var tiles = new List<TileInfo>();

        // Check if input is a directory (auto-discover .POS files)
        if (Directory.Exists(path))
        {
            Console.WriteLine("Scanning directory for images with .POS files...");
            var posFiles = PosFileParser.DiscoverPosFiles(path);

            if (posFiles.Count == 0)
            {
                Console.WriteLine($"Warning: No .POS files found in directory: {path}");
                Console.WriteLine("Looking for CSV metadata file instead...");
                
                // Try to find CSV in the directory
                var csvFiles = Directory.GetFiles(path, "*.csv");
                if (csvFiles.Length > 0)
                {
                    return LoadTileMetadataFromCsv(csvFiles[0]);
                }
                
                return tiles;
            }

            foreach (var kvp in posFiles)
            {
                var tile = PosFileParser.ConvertToTileInfo(kvp.Key, kvp.Value);
                tiles.Add(tile);
                Console.WriteLine($"  Loaded: {tile.Id} at ({tile.StageX:F3}, {tile.StageY:F3}) μm");
            }

            // Sort by stage position for better processing order
            tiles = tiles.OrderBy(t => t.StageY).ThenBy(t => t.StageX).ToList();
        }
        // Check if input is a CSV file
        else if (File.Exists(path) && Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            tiles = LoadTileMetadataFromCsv(path);
        }
        else
        {
            Console.WriteLine($"Error: Invalid input path: {path}");
            Console.WriteLine("Expected a directory with .POS files or a CSV metadata file");
        }

        return tiles;
    }

    static List<TileInfo> LoadTileMetadataFromCsv(string path)
    {
        var tiles = new List<TileInfo>();
        
        if (!File.Exists(path))
        {
            Console.WriteLine($"Error: CSV file not found: {path}");
            return tiles;
        }

        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            Console.WriteLine("Error: CSV file is empty");
            return tiles;
        }

        // Skip header if present
        int startLine = lines[0].Contains("Id") || lines[0].Contains("FilePath") ? 1 : 0;

        for (int i = startLine; i < lines.Length; i++)
        {
            try
            {
                var parts = lines[i].Split(',');
                if (parts.Length < 4) continue;

                var tile = new TileInfo
                {
                    Id = parts[0].Trim().Trim('"'),
                    FilePath = parts[1].Trim().Trim('"'),
                    StageX = double.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                    StageY = double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture)
                };

                // Check for corresponding .POS file to get additional metadata
                var posFile = Path.ChangeExtension(tile.FilePath, ".POS");
                if (File.Exists(posFile))
                {
                    try
                    {
                        var posMetadata = PosFileParser.ParsePosFile(posFile);
                        tile.Width = posMetadata.Width;
                        tile.Height = posMetadata.Height;
                        tile.PixelSizeX = posMetadata.PitchX;
                        tile.PixelSizeY = posMetadata.PitchY;
                        
                        // Use POS file coordinates if CSV values are zero or missing
                        if (tile.StageX == 0 && tile.StageY == 0)
                        {
                            var (posX, posY) = posMetadata.GetStagePosition();
                            tile.StageX = posX;
                            tile.StageY = posY;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not read .POS file for {tile.Id}: {ex.Message}");
                    }
                }

                tiles.Add(tile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to parse line {i + 1}: {ex.Message}");
            }
        }

        return tiles;
    }

    static void DisplayMetadataSummary(List<TileInfo> tiles)
    {
        if (tiles.Count == 0) return;

        var minX = tiles.Min(t => t.StageX);
        var maxX = tiles.Max(t => t.StageX);
        var minY = tiles.Min(t => t.StageY);
        var maxY = tiles.Max(t => t.StageY);

        Console.WriteLine($"\nMetadata Summary:");
        Console.WriteLine($"  Tile count: {tiles.Count}");
        Console.WriteLine($"  Stage range X: {minX:F3} to {maxX:F3} μm (span: {maxX - minX:F3} μm)");
        Console.WriteLine($"  Stage range Y: {minY:F3} to {maxY:F3} μm (span: {maxY - minY:F3} μm)");

        // Check if we have pixel size information
        var tilesWithPixelSize = tiles.Where(t => t.PixelSizeX > 0).ToList();
        if (tilesWithPixelSize.Any())
        {
            var avgPixelSizeX = tilesWithPixelSize.Average(t => t.PixelSizeX);
            var avgPixelSizeY = tilesWithPixelSize.Average(t => t.PixelSizeY);
            Console.WriteLine($"  Average pixel size: {avgPixelSizeX:F6} x {avgPixelSizeY:F6} μm/px");
        }

        // Estimate grid structure
        var distinctX = tiles.Select(t => t.StageX).Distinct().Count();
        var distinctY = tiles.Select(t => t.StageY).Distinct().Count();
        Console.WriteLine($"  Estimated grid: {distinctX} x {distinctY}");
    }
}