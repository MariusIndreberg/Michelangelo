using Michelangelo.Types;
using Xunit;

namespace Core.Tests;

public class DiagnosticsLogTests
{
    [Fact]
    public void AppendsEntries()
    {
        var log1 = new DiagnosticsLog();
        log1.Info("One");
        var log2 = new DiagnosticsLog();
        log2.Warn("Two");
        log1.Append(log2);
        Assert.Equal(2, log1.Entries.Count);
        Assert.Contains(log1.Entries, e => e.Message.Contains("One"));
        Assert.Contains(log1.Entries, e => e.Message.Contains("Two"));
    }
}
