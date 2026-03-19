using Gateway.Core.Security;
using Xunit;

namespace Gateway.Tests;

public class PayloadSanitizerTests
{
    private readonly PayloadSanitizer _sanitizer;

    public PayloadSanitizerTests()
    {
        // Arrange: Instantiate the class under test
        _sanitizer = new PayloadSanitizer();
    }

    [Theory]
    [InlineData("{\"ZoneId\": \"Main Lobby\", \"OccupancyDelta\": 5}")]
    [InlineData("Just a standard string payload with no SQL.")]
    [InlineData("")]
    [InlineData("   ")]
    public void IsSafeTelemetry_WithSafePayloads_ReturnsTrue(string payload)
    {
        // Act
        var result = _sanitizer.IsSafeTelemetry(payload);

        // Assert
        Assert.True(result, $"Expected payload to be marked as SAFE, but it was flagged as malicious: {payload}");
    }

    [Theory]
    [InlineData("SELECT * FROM Users;")]
    [InlineData("DROP TABLE SecurityEvents;")]
    [InlineData("{\"ZoneId\": \"Main Lobby\", \"OccupancyDelta\": 5}; DELETE FROM AuditLogs;")]
    // The ultimate AST test: Regex would likely miss this due to the injected comment formatting
    [InlineData("UNION ALL /* bypass */ SELECT username, password FROM Admins--")]
    public void IsSafeTelemetry_WithMaliciousSqlPayloads_ReturnsFalse(string payload)
    {
        // Act
        var result = _sanitizer.IsSafeTelemetry(payload);

        // Assert
        Assert.False(result, $"Expected payload to be flagged as MALICIOUS, but it passed validation: {payload}");
    }
}