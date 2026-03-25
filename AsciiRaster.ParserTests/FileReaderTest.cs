using AsciiRaster.Parser;
using AwesomeAssertions;

namespace AsciiRaster.ParserTests;

[TestClass]
public class FileReaderTest
{
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
}