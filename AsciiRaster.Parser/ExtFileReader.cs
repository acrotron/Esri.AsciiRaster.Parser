using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace AsciiRaster.Parser;

/// <summary>
/// Extension of the regular Esri Ascii Raster reader that will create a list of Coordinates based
/// on the Lambert Conformal Conic projection, typically used by AEDT to generate output files to
/// generate contours.
/// </summary>
public sealed class ExtFileReader
{
    private readonly FileReader _fileReader = new FileReader();

    /// <summary>
    /// Reads an Esri ASCII raster file and returns a list of geographic coordinates
    /// </summary>
    public List<CoordinateM> Read(string filename, double lat, double @long)
    {
        var raster = _fileReader.Read(filename);
        return ReadRaster(lat, @long, raster);
    }

    /// <summary>
    /// Reads an Esri ASCII raster file and returns a grid of geographic coordinates
    /// preserving the [col, row] structure for grid-based contouring.
    /// </summary>
    public CoordinateM[,] ReadGrid(string filename, double lat, double @long)
    {
        var raster = _fileReader.Read(filename);
        return ReadRasterGrid(lat, @long, raster);
    }

    private CoordinateM[,] ReadRasterGrid(double lat, double @long, EsriAsciiRaster raster)
    {
        var lcc = CreateProjection(lat, @long);

        var transformationFactory = new CoordinateTransformationFactory();
        ICoordinateTransformation transformation =
            transformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, lcc);

        var originPoint = new GeoAPI.Geometries.Coordinate(@long, lat);
        var projectedOrigin = transformation.MathTransform.Transform(originPoint);

        var inverse = transformation.MathTransform.Inverse();
        var nodes = new CoordinateM[raster.NCols, raster.NRows];

        for (int row = 0; row < raster.NRows; row++)
        {
            for (int col = 0; col < raster.NCols; col++)
            {
                double projX = projectedOrigin.X + col * raster.CellSize;

                // Esri ASCII raster row 0 = top (northernmost), so invert row for Y
                double projY = projectedOrigin.Y + (raster.NRows - 1 - row) * raster.CellSize;
                double value = raster.Data[col, row];

                var geoPoint = inverse.Transform(new GeoAPI.Geometries.Coordinate(projX, projY));
                nodes[col, row] = new CoordinateM(geoPoint.X, geoPoint.Y, value);
            }
        }

        return nodes;
    }

    private List<CoordinateM> ReadRaster(double lat, double @long, EsriAsciiRaster raster)
    {
        var lcc = CreateProjection(lat, @long);

        var transformationFactory = new CoordinateTransformationFactory();
        ICoordinateTransformation transformation =
            transformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, lcc);

        // Define the origin location (in latitude and longitude)
        var originPoint = new GeoAPI.Geometries.Coordinate(@long, lat);
        var projectedOrigin = transformation.MathTransform.Transform(originPoint);

        return BuildGrid(raster, projectedOrigin, transformation);
    }

    private List<CoordinateM> BuildGrid(EsriAsciiRaster raster, GeoAPI.Geometries.Coordinate projectedOrigin,
        ICoordinateTransformation transformation)
    {
        double cellSpacing = raster.CellSize; // Spacing between cells in meters
        int numRows = raster.NRows; // Number of rows
        int numCols = raster.NCols; // Number of columns

        var geometryFactory = new GeometryFactory();
        var points = new List<Point>();

        // Generate points based on the cell spacing
        for (int row = 0; row < numRows; row++)
        {
            for (int col = 0; col < numCols; col++)
            {
                // Calculate point location in projected coordinates
                // Esri ASCII raster row 0 = top (northernmost), so invert row for Y
                double x = projectedOrigin.X + col * cellSpacing;
                double y = projectedOrigin.Y + (numRows - 1 - row) * cellSpacing;

                double value = raster.Data[col, row];

                // Create a new point in the projected system
                var point = geometryFactory.CreatePoint(new CoordinateM(y, x, value));
                points.Add(point);
            }
        }

        List<CoordinateM> result = new List<CoordinateM>();

        foreach (var point in points)
        {
            var geoPoint = transformation.MathTransform.Inverse()
                .Transform(new GeoAPI.Geometries.Coordinate(point.Y, point.X));

            result.Add(new CoordinateM(geoPoint.X, geoPoint.Y, point.M));
        }

        return result;
    }

    private ICoordinateSystem CreateProjection(double lat, double @long)
    {
        var csFactory = new CoordinateSystemFactory();

        var projection = @"PROJCS[""Custom_Lambert_Conformal_Conic""," +
                         @"GEOGCS[""GCS_WGS_1984"",DATUM[""D_WGS_1984"",SPHEROID[""WGS_1984"",6378137.0,298.257223563]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]]," +
                         @"PROJECTION[""Lambert_Conformal_Conic""]," +
                         @"PARAMETER[""False_Easting"",0.0]," +
                         @"PARAMETER[""False_Northing"",0.0]," +
                         @"PARAMETER[""Central_Meridian""," + @long + "]," +
                         @"PARAMETER[""Standard_Parallel_1""," + lat + "]," +
                         @"PARAMETER[""Standard_Parallel_2""," + lat + "]," +
                         @"PARAMETER[""Latitude_Of_Origin""," + lat + "]," +
                         @"UNIT[""Meter"",1.0]]";

        return csFactory.CreateFromWkt(projection);
    }
}