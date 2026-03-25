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
        coordinates[0].X.Should().Be(-122.54236349808801);
        // Row 0 in Esri ASCII raster = northernmost; Y should be at the top of the grid
        coordinates[0].Y.Should().BeApproximately(37.7517738420156, 1e-6);
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

        // coordinates X should all be identical at each 20th item.
        coordinates[0].X.Should().Be(coordinates[20].X);
        coordinates[20].X.Should().Be(coordinates[40].X);
        coordinates[40].X.Should().Be(coordinates[60].X);
        coordinates[60].X.Should().Be(coordinates[80].X);
        coordinates[80].X.Should().Be(coordinates[100].X);
    }
}