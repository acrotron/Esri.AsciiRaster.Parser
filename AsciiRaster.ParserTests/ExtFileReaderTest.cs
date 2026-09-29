using AsciiRaster.Parser;
using AwesomeAssertions;

namespace AsciiRaster.ParserTests;

[TestClass]
public class ExtFileReaderTest
{
    [TestMethod]
    [DeploymentItem("INMASCIINoiseValues_J1.asc")]
    public void BasicExtFileReaderTest()
    {
        string filename = "INMASCIINoiseValues_J1.asc";

        double lat = 37.4853922964778;
        double @long = -122.542363498088;

        ExtFileReader reader = new ExtFileReader();
        var coordinates = reader.Read(filename, lat, @long);

        coordinates.Should().NotBeNull();
        coordinates.Count.Should().Be(9604);

        coordinates.All(x => !double.IsNaN(x.M)).Should().BeTrue();
        coordinates.Min(x => x.M).Should().Be(6.22);
        coordinates.Max(x => x.M).Should().Be(83.42);
        // xllcenter is 0, so column 0 lies on the central meridian
        coordinates[0].X.Should().BeApproximately(-122.542363498088, 1e-9);
        // Row 0 in Esri ASCII raster = northernmost; Y should be at the top of the grid
        coordinates[0].Y.Should().BeApproximately(37.7517738420156, 1e-6);
    }

    [TestMethod]
    [DeploymentItem("EsriParserValidation.asc")]
    public void ReadGrid_CellCenters_ProjectBackToHeaderPositions()
    {
        // Arrange - the file declares xllcenter/yllcenter -5556 and cellsize 555.6 (20 columns, 6 rows)
        double lat = 38.9519444444;
        double @long = -77.4480555556;
        var lcc = new LambertConformalConic(lat, @long);

        // Act
        var grid = new ExtFileReader().ReadGrid("EsriParserValidation.asc", lat, @long);

        // Assert - cell (col, row) sits at (-5556 + col * 555.6, -5556 + (5 - row) * 555.6) in LCC meters
        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 20; col++)
            {
                var (x, y) = lcc.Forward(grid[col, row].X, grid[col, row].Y);
                x.Should().BeApproximately(-5556 + col * 555.6, 1e-6);
                y.Should().BeApproximately(-5556 + (5 - row) * 555.6, 1e-6);
            }
        }
    }

    [TestMethod]
    [DeploymentItem("EsriParserValidation.asc")]
    [DataRow(-33.9461, 151.1772)] // southern hemisphere
    [DataRow(-23.4356, -46.4731)] // southern and western hemisphere
    [DataRow(64.13, -21.94)]
    public void ReadGrid_AnyHemisphere_ProjectsBackToHeaderPositions(double lat, double @long)
    {
        // Arrange
        var lcc = new LambertConformalConic(lat, @long);

        // Act
        var grid = new ExtFileReader().ReadGrid("EsriParserValidation.asc", lat, @long);

        // Assert
        var (x, y) = lcc.Forward(grid[19, 0].X, grid[19, 0].Y);
        x.Should().BeApproximately(-5556 + 19 * 555.6, 1e-6);
        y.Should().BeApproximately(-5556 + 5 * 555.6, 1e-6);
        grid[19, 0].X.Should().NotBe(grid[0, 0].X);
        grid[0, 0].Y.Should().BeGreaterThan(grid[0, 5].Y, "row 0 is the northern row");
    }

    [TestMethod]
    [DeploymentItem("EsriParserValidation.asc")]
    public void ReadGrid_CommaDecimalCulture_DoesNotThrow()
    {
        // Arrange - 1.0.1 built its projection WKT with the current culture and failed on "52,3676"
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("nl-NL");

        try
        {
            // Act
            var grid = new ExtFileReader().ReadGrid("EsriParserValidation.asc", 52.3676, 4.9041);

            // Assert
            grid.GetLength(0).Should().Be(20);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [TestMethod]
    [DeploymentItem("EsriParserValidation.asc")]
    public void test_columns_and_rows_reading()
    {
        string filename = "EsriParserValidation.asc";

        double lat = 38.9519444444;
        double @long = -77.4480555556;

        ExtFileReader reader = new ExtFileReader();
        var coordinates = reader.Read(filename, lat, @long);
        coordinates.Should().NotBeEmpty();

        // coordinates should have update value at each 20th item.
        coordinates[0].M.Should().Be(1);
        coordinates[20].M.Should().Be(2);
        coordinates[40].M.Should().Be(3);
        coordinates[60].M.Should().Be(4);
        coordinates[80].M.Should().Be(5);
        coordinates[100].M.Should().Be(6);

        // Every 20th item is in the same column, so it projects back to the same easting. (Its longitude varies
        // slightly by row, because the column lies 5556 m west of the central meridian and meridians converge.)
        var lcc = new LambertConformalConic(lat, @long);
        for (int i = 0; i <= 100; i += 20)
        {
            lcc.Forward(coordinates[i].X, coordinates[i].Y).Easting.Should().BeApproximately(-5556, 1e-6);
        }
    }
}