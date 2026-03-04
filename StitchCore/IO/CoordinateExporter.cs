using StitchCore.Models;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace StitchCore.IO;

/// <summary>
/// Exports optimized tile coordinates for traceability
/// </summary>
public class CoordinateExporter
{
    /// <summary>
    /// Exports tile coordinates to CSV format
    /// </summary>
    public void ExportToCsv(List<TileInfo> tiles, string outputPath)
    {
        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("TileId,FilePath,StageX,StageY,OptimizedX,OptimizedY,Deviation,Width,Height,IsAligned");

        // Data rows
        foreach (var tile in tiles.OrderBy(t => t.Id))
        {
            csv.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2:F4},{3:F4},{4:F4},{5:F4},{6:F4},{7},{8},{9}",
                EscapeCsvField(tile.Id),
                EscapeCsvField(tile.FilePath),
                tile.StageX,
                tile.StageY,
                tile.OptimizedX,
                tile.OptimizedY,
                tile.PositionDeviation,
                tile.Width,
                tile.Height,
                tile.IsAligned));
        }

        File.WriteAllText(outputPath, csv.ToString());
    }

    /// <summary>
    /// Exports tile coordinates to JSON format
    /// </summary>
    public void ExportToJson(List<TileInfo> tiles, string outputPath)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(tiles, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        File.WriteAllText(outputPath, json);
    }

    private string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}