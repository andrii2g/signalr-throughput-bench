using SignalRThroughputBench.Runner.Reports;

namespace SignalRThroughputBench.Tests;

public sealed class ReportWriterTests
{
    [Fact]
    public void CsvEscapesFields()
    {
        Assert.Equal("\"a,b\"", CsvReportWriter.Escape("a,b"));
        Assert.Equal("\"a\"\"b\"", CsvReportWriter.Escape("a\"b"));
    }
}
