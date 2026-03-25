namespace Esri.AsciiRaster.Parser;

public sealed class EsriAsciiRaster
{
    public int NCols { get; internal set; }
    public int NRows { get; internal set; }
    public double XLLCorner { get; internal set; }
    public double YLLCorner { get; internal set; }
    public double XLLCenter { get; internal set; }
    public double YLLCenter { get; internal set; }
    public double CellSize { get; internal set; }
    public double NoDataValue { get; internal set; }
    public double[,] Data { get; internal set; } = new double[0,0];

    public static readonly double DefaultNoData = -9999; // as per Esri specification.

    public void PrintMetadata()
    {
        Console.WriteLine("Metadata:");
        Console.WriteLine($"  ncols: {NCols}");
        Console.WriteLine($"  nrows: {NRows}");

        if (!double.IsNaN(XLLCorner))
        {
            Console.WriteLine($"  xllcorner: {XLLCorner}");
            Console.WriteLine($"  yllcorner: {YLLCorner}");
        }
        else
        {
            Console.WriteLine($"  xllcenter: {XLLCenter}");
            Console.WriteLine($"  yllcenter: {YLLCenter}");
        }

        Console.WriteLine($"  cellsize: {CellSize}");
        Console.WriteLine($"  NODATA_value: {NoDataValue}");
    }
    
    public void PrintData()
    {
        for (int row = 0; row < NRows; row++)
        {
            for (int col = 0; col < NCols; col++)
            {
                Console.Write(Data[row, col] + " ");
            }
            Console.WriteLine();
        }
    }
}