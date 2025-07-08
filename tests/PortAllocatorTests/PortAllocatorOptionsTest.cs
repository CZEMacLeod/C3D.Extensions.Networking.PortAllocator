using C3D.Extensions.Networking;

namespace PortAllocatorTests;

public class PortAllocatorOptionsTest
{
    [Fact]
    public void DefaultOptions_AreValid()
    {
        var options = new PortAllocatorOptions();
        var result = options.Validate(null, options);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void InvalidPortRange_ThrowsValidationError()
    {
        var options = new PortAllocatorOptions { DefaultMinPort = 2000, DefaultMaxPort = 1000 };
        var result = options.Validate(null, options);
        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void ExcludedPorts_OutOfRange_ThrowsValidationError(int port)
    {
        var options = new PortAllocatorOptions();
        options.ExcludedPorts.Add(port);
        var result = options.Validate(null, options);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void NegativeSeed_ThrowsValidationError()
    {
        var options = new PortAllocatorOptions { Seed = -1 };
        var result = options.Validate(null, options);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Clone_CreatesDeepCopy()
    {
        var options = new PortAllocatorOptions
        {
            Seed = 42,
            ExcludeWellKnownPorts = false,
            ScanInUsePorts = false,
            ExcludeEphemeralPorts = false,
            ScanExcludedPorts = false,
            DefaultMinPort = 2000,
            DefaultMaxPort = 3000
        };
        options.ExcludedPorts.Add(1234);
        var clone = options.Clone();
        Assert.NotSame(options, clone);
        Assert.Equal(options.Seed, clone.Seed);
        Assert.Equal(options.ExcludeWellKnownPorts, clone.ExcludeWellKnownPorts);
        Assert.Equal(options.ScanInUsePorts, clone.ScanInUsePorts);
        Assert.Equal(options.ExcludeEphemeralPorts, clone.ExcludeEphemeralPorts);
        Assert.Equal(options.ScanExcludedPorts, clone.ScanExcludedPorts);
        Assert.Equal(options.DefaultMinPort, clone.DefaultMinPort);
        Assert.Equal(options.DefaultMaxPort, clone.DefaultMaxPort);
        Assert.Equal(options.ExcludedPorts, clone.ExcludedPorts);
        clone.ExcludedPorts.Add(5678);
        Assert.DoesNotContain(5678, options.ExcludedPorts);
    }
}
