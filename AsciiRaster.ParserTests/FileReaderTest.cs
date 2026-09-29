using AsciiRaster.Parser;
using AwesomeAssertions;

namespace AsciiRaster.ParserTests;

[TestClass]
public class FileReaderTest
{
    private readonly List<string> _files = [];

    [TestCleanup]
    public void Cleanup()
    {
        foreach (string file in _files)
        {
            File.Delete(file);
        }
    }

    [TestMethod]
    [DeploymentItem("INMASCIINoiseValues_J1.asc")]
    public void FileReaderReadTest()
    {
        string filename = "INMASCIINoiseValues_J1.asc";

        FileReader fileReader = new FileReader();

        Action action = () =>
        {
            var asciiRaster = fileReader.Read(filename);
            asciiRaster.PrintMetadata();
            asciiRaster.PrintData();
        };

        action.Should().NotThrow();
    }

    [TestMethod]
    public void Read_CornerHeader_LeavesCenterNaN()
    {
        // Arrange
        string file = WriteFile("ncols 4", "nrows 3", "xllcorner -150", "yllcorner -250", "cellsize 100",
            "NODATA_value -9999", "1 2 3 4", "5 6 7 8", "9 10 11 12");

        // Act
        EsriAsciiRaster raster = new FileReader().Read(file);

        // Assert
        raster.XLLCorner.Should().Be(-150);
        raster.YLLCorner.Should().Be(-250);
        raster.XLLCenter.Should().Be(double.NaN);
        raster.YLLCenter.Should().Be(double.NaN);
        raster.LowerLeftCellCenterX.Should().Be(-100);
        raster.LowerLeftCellCenterY.Should().Be(-200);
    }

    [TestMethod]
    public void Read_CenterHeader_LeavesCornerNaN()
    {
        // Arrange
        string file = WriteFile("ncols 2", "nrows 1", "xllcenter 10", "yllcenter 20", "cellsize 5", "1 2");

        // Act
        EsriAsciiRaster raster = new FileReader().Read(file);

        // Assert
        raster.XLLCorner.Should().Be(double.NaN);
        raster.YLLCorner.Should().Be(double.NaN);
        raster.LowerLeftCellCenterX.Should().Be(10);
        raster.LowerLeftCellCenterY.Should().Be(20);
        raster.NoDataValue.Should().Be(EsriAsciiRaster.DefaultNoData);
    }

    [TestMethod]
    public void Read_NonSquareRaster_IndexesDataByColumnThenRow()
    {
        // Arrange - 4 columns x 3 rows
        string file = WriteFile("ncols 4", "nrows 3", "xllcorner 0", "yllcorner 0", "cellsize 1",
            "1 2 3 4", "5 6 7 8", "9 10 11 12");

        // Act
        EsriAsciiRaster raster = new FileReader().Read(file);
        Action print = () => raster.PrintData();

        // Assert
        raster.Data[3, 0].Should().Be(4);
        raster.Data[0, 2].Should().Be(9);
        print.Should().NotThrow("1.0.1 indexed Data[row, col] and threw on non-square rasters");
    }

    [TestMethod]
    public void Read_TabsAnyOrderAndWrappedRows_AreAccepted()
    {
        // Arrange - tab separators, lower-case/upper-case keys in a different order, rows wrapped across lines
        string file = WriteFile("NROWS\t3", "ncols\t4", "CellSize\t1", "yllcorner\t0", "xllcorner\t0",
            "1 2 3 4 5 6", "7 8", "9\t10 11 12");

        // Act
        EsriAsciiRaster raster = new FileReader().Read(file);

        // Assert
        raster.NCols.Should().Be(4);
        raster.NRows.Should().Be(3);
        raster.Data[1, 1].Should().Be(6);
        raster.Data[3, 2].Should().Be(12);
    }

    [TestMethod]
    [DataRow(new[] { "nrows 1", "xllcorner 0", "yllcorner 0", "cellsize 1", "1" }, DisplayName = "missing ncols")]
    [DataRow(new[] { "ncols 1", "nrows 1", "cellsize 1", "1" }, DisplayName = "missing origin")]
    [DataRow(new[] { "ncols 1", "nrows 1", "xllcorner 0", "yllcorner 0", "1" }, DisplayName = "missing cellsize")]
    [DataRow(new[] { "ncols 0", "nrows 1", "xllcorner 0", "yllcorner 0", "cellsize 1" }, DisplayName = "zero ncols")]
    [DataRow(new[] { "ncols 2", "nrows 1", "xllcorner 0", "yllcorner 0", "cellsize 1", "1" }, DisplayName = "too few values")]
    [DataRow(new[] { "ncols 1", "nrows 1", "xllcorner 0", "yllcorner 0", "cellsize 1", "1 2" }, DisplayName = "too many values")]
    [DataRow(new[] { "ncols 1", "nrows 1", "xllcorner 0", "yllcorner 0", "cellsize 1", "1,5" }, DisplayName = "comma decimal")]
    public void Read_InvalidFile_Throws(string[] lines)
    {
        // Arrange
        string file = WriteFile(lines);

        // Act
        Action act = () => new FileReader().Read(file);

        // Assert
        act.Should().Throw<InvalidDataException>();
    }

    private string WriteFile(params string[] lines)
    {
        string file = Path.GetTempFileName();
        _files.Add(file);
        File.WriteAllLines(file, lines);
        return file;
    }
}
