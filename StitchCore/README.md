# Microscope Image Stitching Library

Production-ready C# library for stitching tiled microscope images with geometrically accurate alignment.

## Features

- **Sub-pixel Alignment**: Phase correlation with iterative sub-pixel refinement
- **Position Constraints**: Enforces ±2 pixel maximum deviation from stage coordinates
- **Global Drift Correction**: Translation-only optimization model
- **Seamless Blending**: Multi-band blending with intensity normalization
- **Large Dataset Support**: Handles ≥4000 tiles with efficient memory management
- **Multi-threaded**: Parallel processing for optimal performance
- **Coordinate Export**: Traceability through optimized position export

## Quick Start
'''csharp
using StitchCore; using StitchCore.Models;
// Configure stitching parameters var config = new StitchingConfiguration { MaxPositionDeviation = 2.0, ExpectedOverlap = 0.1, EnableSubPixelAlignment = true, EnableGlobalDriftCorrection = true, EnableSeamBlending = true, MaxDegreeOfParallelism = 8 };
// Load tile metadata var tiles = new List<TileInfo> { new TileInfo { Id = "tile_0", FilePath = "tile_0.tif", StageX = 0, StageY = 0 }, // ... more tiles };
// Perform stitching using var stitcher = new MicroscopeStitcher(config); var result = await stitcher.StitchAsync(tiles, "output.tif");
if (result.Success) { Console.WriteLine($"Stitching completed! Max deviation: {result.MaxDeviation:F3} px"); }
'''


## API Reference

### MicroscopeStitcher

Main class for performing image stitching operations.

**Methods:**
- `Task<StitchingResult> StitchAsync(List<TileInfo>, string, IProgress?)`: Stitches tiles asynchronously

### StitchingConfiguration

Configuration parameters for the stitching process.

**Key Properties:**
- `MaxPositionDeviation`: Maximum allowed deviation from stage position (pixels)
- `ExpectedOverlap`: Expected overlap between adjacent tiles (0.0-1.0)
- `EnableSubPixelAlignment`: Enable sub-pixel precision alignment
- `EnableGlobalDriftCorrection`: Enable global drift correction
- `EnableSeamBlending`: Enable seamless blending
- `MaxDegreeOfParallelism`: Number of parallel threads

### TileInfo

Represents a single microscope image tile.

**Properties:**
- `Id`: Unique tile identifier
- `FilePath`: Path to image file
- `StageX`, `StageY`: Original stage coordinates
- `OptimizedX`, `OptimizedY`: Optimized coordinates after alignment
- `PositionDeviation`: Computed deviation from stage position

## Performance

- **4000 tiles (2048×2048 px)**: ~5-10 minutes on 8-core CPU
- **Memory usage**: Configurable, default 4GB cache
- **Accuracy**: Sub-pixel alignment with <2px deviation constraint

## License

MIT License - see LICENSE file for details

## Third-Party Libraries

- **OpenCvSharp** (Apache 2.0): Computer vision operations
- **Math.NET Numerics** (MIT): Linear algebra and optimization

