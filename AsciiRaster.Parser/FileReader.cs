using System.Globalization;

namespace AsciiRaster.Parser;

/// <summary>
/// Reads Esri ASCII raster (<c>.asc</c>) files.
/// </summary>
public sealed class FileReader
{
    private static readonly char[] Whitespace = [' ', '\t'];

    private static readonly HashSet<string> HeaderKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "ncols", "nrows", "xllcorner", "yllcorner", "xllcenter", "yllcenter", "cellsize", "nodata_value"
    };

    /// <summary>
    /// Reads an Esri ASCII raster file.
    /// </summary>
    /// <remarks>
    /// Header keys are case-insensitive, may come in any order and may be separated from their values by spaces or
    /// tabs. <c>ncols</c>, <c>nrows</c>, <c>cellsize</c> and either <c>xllcorner</c>/<c>yllcorner</c> or
    /// <c>xllcenter</c>/<c>yllcenter</c> are required; <c>nodata_value</c> defaults to
    /// <see cref="EsriAsciiRaster.DefaultNoData"/>. Cell values are read as one whitespace-separated stream in
    /// row-major order, so rows may wrap across lines.
    /// </remarks>
    /// <param name="filePath">Path of the <c>.asc</c> file.</param>
    /// <exception cref="InvalidDataException">The header is incomplete or invalid, or the number of values is wrong.</exception>
    public EsriAsciiRaster Read(string filePath)
    {
        using StreamReader reader = new StreamReader(filePath);

        var header = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? line;

        // Header lines start with a known key; the first line that doesn't is the first data line.
        while ((line = reader.ReadLine()) != null)
        {
            string[] parts = line.Split(Whitespace, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0) continue;
            if (!HeaderKeys.Contains(parts[0])) break;

            if (parts.Length != 2)
            {
                throw new InvalidDataException($"Invalid header line \"{line}\": expected a key and one value.");
            }

            if (!header.TryAdd(parts[0], parts[1]))
            {
                throw new InvalidDataException($"Duplicate header key \"{parts[0]}\".");
            }
        }

        EsriAsciiRaster raster = CreateRaster(header);

        ReadData(reader, line, raster);

        return raster;
    }

    private static EsriAsciiRaster CreateRaster(Dictionary<string, string> header)
    {
        var raster = new EsriAsciiRaster
        {
            NCols = ParseInt(header, "ncols"),
            NRows = ParseInt(header, "nrows"),
            CellSize = ParseDouble(header, "cellsize")
        };

        if (raster.NCols <= 0 || raster.NRows <= 0)
        {
            throw new InvalidDataException($"ncols and nrows must be positive, got {raster.NCols} and {raster.NRows}.");
        }

        if (!(raster.CellSize > 0))
        {
            throw new InvalidDataException($"cellsize must be positive, got {raster.CellSize}.");
        }

        if (header.ContainsKey("xllcorner") || header.ContainsKey("yllcorner"))
        {
            raster.XLLCorner = ParseDouble(header, "xllcorner");
            raster.YLLCorner = ParseDouble(header, "yllcorner");
        }
        else if (header.ContainsKey("xllcenter") || header.ContainsKey("yllcenter"))
        {
            raster.XLLCenter = ParseDouble(header, "xllcenter");
            raster.YLLCenter = ParseDouble(header, "yllcenter");
        }
        else
        {
            throw new InvalidDataException("Missing header: xllcorner/yllcorner or xllcenter/yllcenter.");
        }

        if (header.ContainsKey("xllcorner") && header.ContainsKey("xllcenter"))
        {
            throw new InvalidDataException("The header declares both xllcorner and xllcenter.");
        }

        raster.NoDataValue = header.ContainsKey("nodata_value")
            ? ParseDouble(header, "nodata_value")
            : EsriAsciiRaster.DefaultNoData;

        return raster;
    }

    private static void ReadData(StreamReader reader, string? firstDataLine, EsriAsciiRaster raster)
    {
        int nCols = raster.NCols;
        long expected = (long)nCols * raster.NRows;
        var data = new double[nCols, raster.NRows];
        long index = 0;

        for (string? line = firstDataLine; line != null; line = reader.ReadLine())
        {
            foreach (string token in line.Split(Whitespace, StringSplitOptions.RemoveEmptyEntries))
            {
                if (index == expected)
                {
                    throw new InvalidDataException($"More than the expected {expected} (ncols x nrows) values.");
                }

                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    throw new InvalidDataException($"\"{token}\" is not a number (value {index + 1}).");
                }

                data[index % nCols, index / nCols] = value;
                index++;
            }
        }

        if (index != expected)
        {
            throw new InvalidDataException($"Expected {expected} (ncols x nrows) values but found {index}.");
        }

        raster.Data = data;
    }

    private static int ParseInt(Dictionary<string, string> header, string key)
    {
        if (!header.TryGetValue(key, out string? text))
        {
            throw new InvalidDataException($"Missing header: {key}.");
        }

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            throw new InvalidDataException($"Header {key} \"{text}\" is not an integer.");
        }

        return value;
    }

    private static double ParseDouble(Dictionary<string, string> header, string key)
    {
        if (!header.TryGetValue(key, out string? text))
        {
            throw new InvalidDataException($"Missing header: {key}.");
        }

        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            throw new InvalidDataException($"Header {key} \"{text}\" is not a number.");
        }

        return value;
    }
}
