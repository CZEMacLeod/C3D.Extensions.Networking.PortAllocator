using C3D.Extensions.Networking;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace PortAllocatorTests;

public class PortAllocatorConstructorTest
{
    [Fact]
    public void CanConstructWithLoggerAndOptions()
    {
        var options = new PortAllocatorOptions { Seed = 123 };
        var allocator = new PortAllocator(NullLogger.Instance, options);
        Assert.NotNull(allocator);
    }

    [Fact]
    public void CanConstructWithOptionsMonitor()
    {
        var options = new PortAllocatorOptions { Seed = 456 };
        var monitor = new TestOptionsMonitor(options);
        var allocator = new PortAllocator(monitor);
        Assert.NotNull(allocator);
    }

    [Fact]
    public void CanConstructWithSeed()
    {
        var allocator = new PortAllocator(789);
        Assert.NotNull(allocator);
    }

    [Fact]
    public void CanConstructWithLoggerAndSeed()
    {
        var allocator = new PortAllocator(new NullLogger<PortAllocator>(), 321);
        Assert.NotNull(allocator);
    }

    private class TestOptionsMonitor : IOptionsMonitor<PortAllocatorOptions>
    {
        private PortAllocatorOptions _current;
        public TestOptionsMonitor(PortAllocatorOptions current) => _current = current;
        public PortAllocatorOptions CurrentValue => _current;
        public PortAllocatorOptions Get(string? name) => _current;
        public IDisposable OnChange(Action<PortAllocatorOptions, string> listener) => null!;
    }
}