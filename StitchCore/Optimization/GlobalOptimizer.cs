using MathNet.Numerics.LinearAlgebra;
using StitchCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StitchCore.Optimization;

/// <summary>
/// Performs global drift correction using translation-only optimization
/// </summary>
public class GlobalOptimizer
{
    private readonly StitchingConfiguration _config;

    public GlobalOptimizer(StitchingConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Optimizes tile positions globally while constraining deviation from stage positions
    /// </summary>
    public List<TileInfo> OptimizePositions(
        List<TileInfo> tiles,
        List<(TileInfo tile1, TileInfo tile2, double dx, double dy, double confidence)> pairwiseAlignments)
    {
        if (tiles.Count == 0) return tiles;

        // Initialize positions from stage coordinates
        var n = tiles.Count;
        var tileIndexMap = tiles.Select((t, i) => new { t.Id, Index = i }).ToDictionary(x => x.Id, x => x.Index);

        if (pairwiseAlignments.Count == 0)
        {
            // No alignments, use stage positions
            foreach (var tile in tiles)
            {
                tile.OptimizedX = tile.StageX;
                tile.OptimizedY = tile.StageY;
                tile.IsAligned = true;
            }
            return tiles;
        }

        // Build system of equations for optimization
        var numConstraints = pairwiseAlignments.Count * 2;
        var A = Matrix<double>.Build.Dense(numConstraints + n * 2, n * 2);
        var b = Vector<double>.Build.Dense(numConstraints + n * 2);

        int row = 0;

        // Add pairwise alignment constraints
        foreach (var alignment in pairwiseAlignments)
        {
            if (!tileIndexMap.TryGetValue(alignment.tile1.Id, out int idx1) ||
                !tileIndexMap.TryGetValue(alignment.tile2.Id, out int idx2))
                continue;

            // Weight by confidence
            double weight = Math.Max(0.1, alignment.confidence);

            // X constraint: x2 - x1 = dx
            A[row, idx1] = -weight;
            A[row, idx2] = weight;
            b[row] = alignment.dx * weight;
            row++;

            // Y constraint: y2 - y1 = dy
            A[row, n + idx1] = -weight;
            A[row, n + idx2] = weight;
            b[row] = alignment.dy * weight;
            row++;
        }

        // Add regularization to prevent deviation from stage positions
        double regularizationWeight = 1.0;

        for (int i = 0; i < n; i++)
        {
            // X regularization
            A[row, i] = regularizationWeight;
            b[row] = tiles[i].StageX * regularizationWeight;
            row++;

            // Y regularization
            A[row, n + i] = regularizationWeight;
            b[row] = tiles[i].StageY * regularizationWeight;
            row++;
        }

        // Solve using least squares (A^T * A * x = A^T * b)
        try
        {
            var AtA = A.TransposeThisAndMultiply(A);
            var Atb = A.TransposeThisAndMultiply(b);
            var solution = AtA.Solve(Atb);

            // Update tile positions with constraint checking
            for (int i = 0; i < n; i++)
            {
                double newX = solution[i];
                double newY = solution[n + i];

                // Enforce maximum deviation constraint
                double dx = newX - tiles[i].StageX;
                double dy = newY - tiles[i].StageY;
                double deviation = Math.Sqrt(dx * dx + dy * dy);

                if (deviation > _config.MaxPositionDeviation)
                {
                    // Scale back to maximum allowed deviation
                    double scale = _config.MaxPositionDeviation / deviation;
                    newX = tiles[i].StageX + dx * scale;
                    newY = tiles[i].StageY + dy * scale;
                }

                tiles[i].OptimizedX = newX;
                tiles[i].OptimizedY = newY;
                tiles[i].IsAligned = true;
            }
        }
        catch
        {
            // If optimization fails, use stage positions
            foreach (var tile in tiles)
            {
                tile.OptimizedX = tile.StageX;
                tile.OptimizedY = tile.StageY;
                tile.IsAligned = true;
            }
        }

        return tiles;
    }

    /// <summary>
    /// Estimates global translation drift across all tiles
    /// </summary>
    public (double driftX, double driftY) EstimateGlobalDrift(List<TileInfo> tiles)
    {
        if (tiles.Count == 0) return (0, 0);

        double sumDriftX = 0;
        double sumDriftY = 0;
        int count = 0;

        foreach (var tile in tiles.Where(t => t.IsAligned))
        {
            sumDriftX += tile.OptimizedX - tile.StageX;
            sumDriftY += tile.OptimizedY - tile.StageY;
            count++;
        }

        return count > 0 
            ? (sumDriftX / count, sumDriftY / count) 
            : (0, 0);
    }
}