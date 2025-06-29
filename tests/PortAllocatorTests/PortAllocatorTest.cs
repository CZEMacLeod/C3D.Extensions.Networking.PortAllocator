using C3D.Extensions.Networking;
using Xunit.Abstractions;

namespace PortAllocatorTests;

[Collection("PortAllocator")]   // Ensure tests run sequentially to avoid port conflicts
public class PortAllocatorTest(ITestOutputHelper outputHelper)
{
    [Fact]
    public void GetRandomFreePort_AllocatesPortInDefaultRange()
    {
        var allocator = new PortAllocator();
        int port = allocator.GetRandomFreePort();
        Assert.InRange(port, 1000, 65535);
        allocator.MarkPortAsFree(port); // Clean up
    }

    [Fact]
    public void GetRandomFreePort_AllocatesPortInCustomRange()
    {
        var allocator = new PortAllocator();
        int port = allocator.GetRandomFreePort(8000, 9000);
        Assert.InRange(port, 8000, 9000);
        allocator.MarkPortAsFree(port); // Clean up
    }

    [Fact]
    public void GetRandomFreePort_ThrowsIfMaxIsLessThanMin()
    {
        var allocator = new PortAllocator();
        Assert.Throws<ArgumentOutOfRangeException>(() => allocator.GetRandomFreePort(9000, 8000));
    }

    [Fact]
    public void MarkPortAsUsed_ThrowsIfAlreadyUsed()
    {
        var allocator = new PortAllocator();
        int port = allocator.GetRandomFreePort();
        Assert.Throws<InvalidOperationException>(() => allocator.MarkPortAsUsed(port));
        allocator.MarkPortAsFree(port); // Clean up
    }

    [Fact]
    public void MarkPortAsFree_MakesPortAvailableAgain()
    {
        var allocator = new PortAllocator();
        int port = allocator.GetRandomFreePort();
        bool freed = allocator.MarkPortAsFree(port);
        Assert.True(freed);
        // Should be able to mark as used again
        allocator.MarkPortAsUsed(port);
        allocator.MarkPortAsFree(port); // Clean up
    }

    [Fact]
    public void MarkPortAsFree_ReturnsFalseIfPortAlreadyFree()
    {
        var allocator = new PortAllocator();
        int port = 12345;
        // Ensure port is free
        allocator.MarkPortAsFree(port);
        bool result = allocator.MarkPortAsFree(port);
        Assert.False(result);
    }

    [Fact]
    public void TryMarkPortAsUsed_ReturnsFalseIfAlreadyUsed()
    {
        var allocator = new PortAllocator();
        int port = allocator.GetRandomFreePort();
        bool result = allocator.TryMarkPortAsUsed(port);
        Assert.False(result);
        allocator.MarkPortAsFree(port); // Clean up
    }

    [Fact]
    public void TryMarkPortAsUsed_ReturnsTrueIfPortIsFree()
    {
        var allocator = new PortAllocator();
        int port = 23456;
        allocator.MarkPortAsFree(port); // Ensure port is free
        bool result = allocator.TryMarkPortAsUsed(port);
        Assert.True(result);
        allocator.MarkPortAsFree(port); // Clean up
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void MarkPortAsUsed_ThrowsArgumentOutOfRange(int port)
    {
        var allocator = new PortAllocator();
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => allocator.MarkPortAsUsed(port));
    }

    [Fact]
    public void GetRandomFreePort_ThrowsIfAllPortsUsed()
    {
        var allocator = new PortAllocator();
        // Use a small range for test speed
        int min = 50000, max = 50002;
        for (int port = min; port <= max; port++)
            allocator.MarkPortAsUsed(port);

        Assert.Throws<InvalidOperationException>(() => allocator.GetRandomFreePort(min, max));

        // We need to clean up the ports we allocated as the port allocator port list is static
        for (int port = min; port <= max; port++)
            allocator.MarkPortAsFree(port); // Clean up
    }

    [Theory]
    [InlineData(5060)]
    [InlineData(5061)]
    [InlineData(6000)]
    [InlineData(6566)]
    [InlineData(6665)]
    [InlineData(6666)]
    [InlineData(6667)]
    [InlineData(6668)]
    [InlineData(6669)]
    [InlineData(6679)]
    [InlineData(6697)]
    [InlineData(10080)]
    public void MarkPortAsUsed_ThrowsIfAvoidPort(int port)
    {
        var allocator = new PortAllocator();
        Assert.ThrowsAny<InvalidOperationException>(() => allocator.MarkPortAsUsed(port));
    }

    [Fact]
    public void GetRandomFreePort_UsesProvidedSeed()
    {
        var seed = 666;
        var allocator1 = new PortAllocator(seed);
        var ports1 = Enumerable.Range(0,10).Select(_=> allocator1.GetRandomFreePort()).ToList();
        ports1.ForEach(port=>allocator1.MarkPortAsFree(port)); // Clean up

        outputHelper.WriteLine($"Ports allocated 1 with seed {seed}: {string.Join(", ", ports1)}");

        var allocator2 = new PortAllocator(seed);
        var ports2 = Enumerable.Range(0, 10).Select(_ => allocator2.GetRandomFreePort()).ToList();
        ports2.ForEach(port => allocator1.MarkPortAsFree(port)); // Clean up

        outputHelper.WriteLine($"Ports allocated 2 with seed {seed}: {string.Join(", ", ports1)}");

        Assert.Equal(ports1, ports2); // Should return the same sequence of ports for the same seed
    }
}