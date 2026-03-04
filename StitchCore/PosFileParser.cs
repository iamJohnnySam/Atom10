using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace StitchCore;

/// <summary>
/// Parser for microscope .POS metadata files containing stage position information
/// </summary>
public class PosFileMetadata
{
    public string Type { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public double PitchX { get; set; }
    public double PitchY { get; set; }
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double CenterZ { get; set; }
    public double LocalCenterX { get; set; }
    public double LocalCenterY { get; set; }
    public double LocalCenterZ { get; set; }
    public DateTime CreateTime { get; set; }
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// Gets the stage position in micrometers (CENTER is the preferred position)
    /// </summary>
    public (double X, double Y) GetStagePosition() => (CenterX, CenterY);

    /// <summary>
    /// Gets the pixel size in micrometers
    /// </summary>
    public (double X, double Y) GetPixelSize() => (PitchX, PitchY);
}

public static class PosFileParser
{
    /// <summary>
    /// Parse a .POS file and extract metadata
    /// </summary>
    public static PosFileMetadata ParsePosFile(string posFilePath)
    {
        if (!File.Exists(posFilePath))
            throw new FileNotFoundException($"POS file not found: {posFilePath}");

        var metadata = new PosFileMetadata();
        var lines = File.ReadAllLines(posFilePath);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.Contains('='))
                continue;

            var parts = line.Split('=', 2);
            var key = parts[0].Trim();
            var value = parts[1].Trim();

            try
            {
                switch (key)
                {
                    case "TYPE":
                        metadata.Type = value;
                        break;

                    case "SIZE":
                        var sizeParts = value.Split(',');
                        if (sizeParts.Length >= 2)
                        {
                            metadata.Width = int.Parse(sizeParts[0].Trim(), CultureInfo.InvariantCulture);
                            metadata.Height = int.Parse(sizeParts[1].Trim(), CultureInfo.InvariantCulture);
                        }
                        break;

                    case "PITCH":
                        var pitchParts = value.Split(',');
                        if (pitchParts.Length >= 2)
                        {
                            metadata.PitchX = double.Parse(pitchParts[0].Trim(), CultureInfo.InvariantCulture);
                            metadata.PitchY = double.Parse(pitchParts[1].Trim(), CultureInfo.InvariantCulture);
                        }
                        break;

                    case "CENTER":
                        var centerParts = value.Split(',');
                        if (centerParts.Length >= 3)
                        {
                            metadata.CenterX = double.Parse(centerParts[0].Trim(), CultureInfo.InvariantCulture);
                            metadata.CenterY = double.Parse(centerParts[1].Trim(), CultureInfo.InvariantCulture);
                            metadata.CenterZ = double.Parse(centerParts[2].Trim(), CultureInfo.InvariantCulture);
                        }
                        break;

                    case "LCENTER":
                        var lcenterParts = value.Split(',');
                        if (lcenterParts.Length >= 3)
                        {
                            metadata.LocalCenterX = double.Parse(lcenterParts[0].Trim(), CultureInfo.InvariantCulture);
                            metadata.LocalCenterY = double.Parse(lcenterParts[1].Trim(), CultureInfo.InvariantCulture);
                            metadata.LocalCenterZ = double.Parse(lcenterParts[2].Trim(), CultureInfo.InvariantCulture);
                        }
                        break;

                    case "CREATE":
                        // Parse date: "Wed Dec 10 11:05:37 2025"
                        if (DateTime.TryParseExact(value, "ddd MMM dd HH:mm:ss yyyy",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out var createTime))
                        {
                            metadata.CreateTime = createTime;
                        }
                        break;

                    case "COMMENT":
                        metadata.Comment = value;
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log warning but continue parsing
                Console.WriteLine($"Warning: Failed to parse {key}={value}: {ex.Message}");
            }
        }

        return metadata;
    }

    /// <summary>
    /// Automatically discover .POS files for image files in a directory
    /// </summary>
    public static Dictionary<string, PosFileMetadata> DiscoverPosFiles(string directory, string imagePattern = "*.*")
    {
        var results = new Dictionary<string, PosFileMetadata>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(directory))
            return results;

        // Find all image files
        var imageExtensions = new[] { ".tif", ".tiff", ".png", ".jpg", ".jpeg", ".bmp" };
        var imageFiles = Directory.GetFiles(directory, imagePattern, SearchOption.TopDirectoryOnly)
            .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();

        foreach (var imageFile in imageFiles)
        {
            // Look for corresponding .POS file
            var posFile = Path.ChangeExtension(imageFile, ".POS");
            
            if (File.Exists(posFile))
            {
                try
                {
                    var metadata = ParsePosFile(posFile);
                    results[imageFile] = metadata;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to parse POS file for {Path.GetFileName(imageFile)}: {ex.Message}");
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Convert POS file metadata to TileInfo for stitching
    /// </summary>
    public static Models.TileInfo ConvertToTileInfo(string imageFilePath, PosFileMetadata posMetadata)
    {
        var (stageX, stageY) = posMetadata.GetStagePosition();
        
        return new Models.TileInfo
        {
            Id = Path.GetFileNameWithoutExtension(imageFilePath),
            FilePath = imageFilePath,
            StageX = stageX,
            StageY = stageY,
            Width = posMetadata.Width,
            Height = posMetadata.Height,
            PixelSizeX = posMetadata.PitchX,
            PixelSizeY = posMetadata.PitchY
        };
    }
}