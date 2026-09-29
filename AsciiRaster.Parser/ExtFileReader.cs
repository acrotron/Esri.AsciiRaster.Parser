using NetTopologySuite.Geometries;

namespace AsciiRaster.Parser;

/// <summary>
/// Reads an Esri ASCII raster whose coordinates are Lambert Conformal Conic meters relative to an origin (as in
/// AEDT noise grids) and returns the cell centers as WGS84 longitude/latitude.
/// </summary>
/// <remarks>
/// The projection is the tangential LCC used by AEDT: both standard parallels and the latitude of origin are the
/// origin latitude, the central meridian is the origin longitude. Cell (col, row) is placed at its center,
/// <c>(llx + col * cellsize, lly + (nrows - 1 - row) * cellsize)</c>, where (llx, lly) is the center of the
/// lower-left cell from the header. NODATA cells are returned with the NODATA value as M.
/// </remarks>
public sealed class ExtFileReader
{
    private readonly FileReader _fileReader = new FileReader();

    /// <summary>
    /// Reads a raster and returns its cell centers as a list, top row first, each row west to east.
    /// </summary>
    /// <param name="filename">Path of the <c>.asc</c> file.</param>
    /// <param name="lat">Latitude of the projection origin in degrees (non-zero, between -90 and 90).</param>
    /// <param name="long">Longitude of the projection origin in degrees.</param>
    /// <returns>Cell centers with X = longitude, Y = latitude, M = cell value.</returns>
    public List<CoordinateM> Read(string filename, double lat, double @long)
    {
        CoordinateM[,] grid = ReadGrid(filename, lat, @long);
        var result = new List<CoordinateM>(grid.Length);

        for (int row = 0; row < grid.GetLength(1); row++)
        {
            for (int col = 0; col < grid.GetLength(0); col++)
            {
                result.Add(grid[col, row]);
            }
        }

        return result;
    }

    /// <summary>
    /// Reads a raster and returns its cell centers as a grid indexed <c>[col, row]</c> (row 0 is the top row).
    /// </summary>
    /// <param name="filename">Path of the <c>.asc</c> file.</param>
    /// <param name="lat">Latitude of the projection origin in degrees (non-zero, between -90 and 90).</param>
    /// <param name="long">Longitude of the projection origin in degrees.</param>
    /// <returns>Cell centers with X = longitude, Y = latitude, M = cell value.</returns>
    public CoordinateM[,] ReadGrid(string filename, double lat, double @long)
    {
        EsriAsciiRaster raster = _fileReader.Read(filename);
        var lcc = new LambertConformalConic(lat, @long);

        double llx = raster.LowerLeftCellCenterX;
        double lly = raster.LowerLeftCellCenterY;
        var nodes = new CoordinateM[raster.NCols, raster.NRows];

        for (int row = 0; row < raster.NRows; row++)
        {
            // Row 0 is the top (northernmost) row.
            double y = lly + (raster.NRows - 1 - row) * raster.CellSize;

            for (int col = 0; col < raster.NCols; col++)
            {
                double x = llx + col * raster.CellSize;
                var (lon, latitude) = lcc.Inverse(x, y);
                nodes[col, row] = new CoordinateM(lon, latitude, raster.Data[col, row]);
            }
        }

        return nodes;
    }
}
