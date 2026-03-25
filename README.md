# Esri.AsciiRaster.Parser

A .NET library for reading and parsing [Esri ASCII raster](https://en.wikipedia.org/wiki/Esri_grid) (`.asc`) files, with optional coordinate transformation using Lambert Conformal Conic projection.

## Installation

```shell
dotnet add package Esri.AsciiRaster.Parser
```

## Usage

### Reading an ASCII raster file

```csharp
using Esri.AsciiRaster.Parser;

var reader = new FileReader();
EsriAsciiRaster raster = reader.Read("path/to/file.asc");

Console.WriteLine($"Columns: {raster.NCols}, Rows: {raster.NRows}");
Console.WriteLine($"Cell size: {raster.CellSize}");

// Access raster data
double value = raster.Data[col, row];
```

### Reading with coordinate transformation

Use `ExtFileReader` to transform projected coordinates into geographic (WGS 84) coordinates via a Lambert Conformal Conic projection centered on a given latitude/longitude.

```csharp
using Esri.AsciiRaster.Parser;

var reader = new ExtFileReader();

// Returns a flat list of CoordinateM (lon, lat, value)
List<CoordinateM> points = reader.Read("path/to/file.asc", lat: 40.0, @long: -74.0);

// Or preserve the grid structure
CoordinateM[,] grid = reader.ReadGrid("path/to/file.asc", lat: 40.0, @long: -74.0);
```

## Esri ASCII raster format

The Esri ASCII raster format is used to transfer information to and from cell-based or raster systems. The file begins with header information that defines properties of the raster, followed by cell value data in space-delimited row-major order.

The spatial location of the raster is specified by the location of the lower left cell, using either its center or its lower left corner:

```
ncols         480
nrows         450
xllcorner     378923
yllcorner     4072345
cellsize      30
nodata_value  -32768
43 2 45 7 3 56 2 5 23 65 34 6 32 54 57 34 2 2 54 6
35 45 65 34 2 6 78 4 2 6 89 3 2 7 45 23 5 8 4 1 62 ...
```

### Header keywords

| Parameter | Description | Requirements |
|-----------|-------------|--------------|
| `NCOLS` | Number of cell columns | Integer greater than 0 |
| `NROWS` | Number of cell rows | Integer greater than 0 |
| `XLLCENTER` or `XLLCORNER` | X-coordinate of the origin (by center or lower left corner of the cell) | Must match y-coordinate type |
| `YLLCENTER` or `YLLCORNER` | Y-coordinate of the origin (by center or lower left corner of the cell) | Must match x-coordinate type |
| `CELLSIZE` | Cell size | Greater than 0 |
| `NODATA_VALUE` | The input values to be NoData in the output raster | Optional. Default is -9999 |

### Data format

- Cell values are delimited by spaces.
- Carriage returns at the end of each row are not required. The number of columns in the header determines when a new row begins.
- Row 1 of the data is at the top of the raster, row 2 is just under row 1, and so on.

## License

This project is licensed under the MIT License.
