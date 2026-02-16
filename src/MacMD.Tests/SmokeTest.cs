using MacMD.Core.Models;

namespace MacMD.Tests;

public sealed class SmokeTest
{
    [Fact]
    public void DocumentId_RoundTrips()
    {
        var id = new DocumentId("test-123");
        Assert.Equal("test-123", id.Value);
    }
}
