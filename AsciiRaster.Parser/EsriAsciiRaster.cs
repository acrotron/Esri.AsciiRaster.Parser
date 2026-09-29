using System.Globalization;

namespace AsciiRaster.Parser;

/// <summary>
/// An Esri ASCII raster: header values and cell data.
/// </summary>
public sealed class EsriAsciiRaster
{
    /// <summary>
    /// Default NODATA value when the file does not specify one (per the Esri specification).
    /// </summary>
    public static readonly double DefaultNoData = -9999;

    /// <summary>
    /// Number of columns.
    /// </summary>
    public int NCols { get; internal set; }

    /// <summary>
    /// Number of rows.
    /// </summary>
    public int NRows { get; internal set; }

    /// <summary>
    /// X of the lower-left corner of the lower-left cell, or NaN when the file uses <c>xllcenter</c>.
    /// </summary>
    public double XLLCorner { get; internal set; } = double.NaN;

    /// <summary>
    /// Y of the lower-left corner of the lower-left cell, or NaN when the file uses <c>yllcenter</c>.
    /// </summary>
    public double YLLCorner { get; internal set; } = double.NaN;

    /// <summary>
    /// X of the center of the lower-left cell, or NaN when the file uses <c>xllcorner</c>.
    /// </summary>
    public double XLLCenter { get; internal set; } = double.NaN;

    /// <summary>
    /// Y of the center of the lower-left cell, or NaN when the file uses <c>yllcorner</c>.
    /// </summary>
    public double YLLCenter { get; internal set; } = double.NaN;

    /// <summary>
    /// Cell size, in the units of the raster's coordinate system.
    /// </summary>
    public double CellSize { get; internal set; }

    /// <summary>
    /// Value marking cells without data.
    /// </summary>
    public double NoDataValue { get; internal set; } = DefaultNoData;

    /// <summary>
    /// Cell values indexed as <c>[col, row]</c>; row 0 is the top (northernmost) row of the file.
    /// </summary>
    public double[,] Data { get; internal set; } = new double[0, 0];

    /// <summary>
    /// X of the center of the lower-left cell, whichever of <c>xllcorner</c>/<c>xllcenter</c> the file declares.
    /// </summary>
    public double LowerLeftCellCenterX => double.IsNaN(XLLCenter) ? XLLCorner + CellSize / 2.0 : XLLCenter;

    /// <summary>
    /// Y of the center of the lower-left cell, whichever of <c>yllcorner</c>/<c>yllcenter</c> the file declares.
    /// </summary>
    public double LowerLeftCellCenterY => double.IsNaN(YLLCenter) ? YLLCorner + CellSize / 2.0 : YLLCenter;

    /// <summary>
    /// Writes the header values to the console.
    /// </summary>
    public void PrintMetadata()
    {
        var inv = CultureInfo.InvariantCulture;

        Console.WriteLine("Metadata:");
        Console.WriteLine($"  ncols: {NCols}");
        Console.WriteLine($"  nrows: {NRows}");

        if (!double.IsNaN(XLLCorner))
        {
            Console.WriteLine($"  xllcorner: {XLLCorner.ToString(inv)}");
            Console.WriteLine($"  yllcorner: {YLLCorner.ToString(inv)}");
        }
        else
        {
            Console.WriteLine($"  xllcenter: {XLLCenter.ToString(inv)}");
            Console.WriteLine($"  yllcenter: {YLLCenter.ToString(inv)}");
        }

        Console.WriteLine($"  cellsize: {CellSize.ToString(inv)}");
        Console.WriteLine($"  NODATA_value: {NoDataValue.ToString(inv)}");
    }

    /// <summary>
    /// Writes the cell values to the console, one raster row per line (top row first).
    /// </summary>
    public void PrintData()
    {
        for (int row = 0; row < NRows; row++)
        {
            for (int col = 0; col < NCols; col++)
            {
                Console.Write(Data[col, row].ToString(CultureInfo.InvariantCulture) + " ");
            }
            Console.WriteLine();
        }
    }
}
