using System.Globalization;

namespace AsciiRaster.Parser;

public sealed class FileReader
{
    public EsriAsciiRaster Read(string filePath)
    {
        EsriAsciiRaster raster = new EsriAsciiRaster();
        var hasNoDataSpecified = false;

        using StreamReader reader = new StreamReader(filePath);
        {
            hasNoDataSpecified = ParseHeader(reader, ref raster);
        }

        using StreamReader dataReader = new StreamReader(filePath);
        {
            ParseData(dataReader, hasNoDataSpecified, ref raster);
        }

        return raster;
    }

    private bool ParseHeader(StreamReader reader, ref EsriAsciiRaster raster)
    {
        var headerBuffer = new List<string>();
        int count = 0;

        // read the first five lines.
        while (reader.ReadLine() is { } line && count < 6)
        {
            headerBuffer.Add(line);
            count++;
        }

        if (count != 6)
        {
            throw new InvalidDataException($"Invalid header.");
        }

        TryParseHeaderValue<int>(headerBuffer[0], "ncols", out var nCols);
        raster.NCols = nCols;

        TryParseHeaderValue<int>(headerBuffer[1], "nrows", out var nRows);
        raster.NRows = nRows;

        if (TryParseHeaderValue<double>(headerBuffer[2], "xllcorner", out var xllCorner) && TryParseHeaderValue<double>(headerBuffer[3], "yllcorner", out var yllCorner))
        {
            raster.XLLCorner = xllCorner;
            raster.YLLCorner = yllCorner;
        }
        else
        {
            TryParseHeaderValue<double>(headerBuffer[2], "xllcenter", out var xllCenter);
            TryParseHeaderValue<double>(headerBuffer[3], "yllcenter", out var yllCenter);
            raster.XLLCenter = xllCenter;
            raster.YLLCenter = yllCenter;
        }

        TryParseHeaderValue<double>(headerBuffer[4], "cellsize", out var cellSize);
        raster.CellSize = cellSize;

        string noDataValueLine = headerBuffer[5];

        if (noDataValueLine.StartsWith("nodata_value", StringComparison.OrdinalIgnoreCase))
        {
            TryParseHeaderValue<double>(noDataValueLine, "nodata_value", out var nodataValue);
            raster.NoDataValue = nodataValue;
            return true;
        }
        else
        {
            raster.NoDataValue = EsriAsciiRaster.DefaultNoData;
            return false;
        }
    }

    private static bool TryParseHeaderValue<T>(string line, string expectedKey, out T? output)
    {
        string[] parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2 || !parts[0].Equals(expectedKey, StringComparison.OrdinalIgnoreCase))
        {
            output = default;
            return false;
        }

        output = (T)Convert.ChangeType(parts[1], typeof(T), CultureInfo.InvariantCulture);
        return true;
    }

    private void ParseData(StreamReader reader, bool hasNoDataSpecified, ref EsriAsciiRaster raster)
    {
        // skip header.
        int header = hasNoDataSpecified ? 5 : 4;
        int count = 0;

        while ((_ = reader.ReadLine()) != null && count < header)
        {
            count++;
        }

        if (count != header)
        {
            throw new InvalidDataException($"Invalid header.");
        }

        raster.Data = new double[raster.NCols, raster.NRows];

        for (int row = 0; row < raster.NRows; row++)
        {
            string? line = reader.ReadLine();

            if (line == null)
            {
                throw new InvalidDataException("Unexpected end of file while reading raster data.");
            }

            string[] values = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (values.Length != raster.NCols)
            {
                throw new InvalidDataException($"Expected {raster.NCols} values on row {row}, but got {values.Length}.");
            }

            for (int col = 0; col < raster.NCols; col++)
            {
                raster.Data[col, row] = double.Parse(values[col], CultureInfo.InvariantCulture);
            }
        }
    }
}