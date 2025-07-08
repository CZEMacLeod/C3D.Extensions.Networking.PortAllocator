using Microsoft.Extensions.Logging;
using System;

namespace C3D.Extensions.Networking;

partial class PortAllocator
{

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Port {Port} marked as used."
    )]
    private static partial void LogPortMarkedAsUsed(ILogger logger, int port);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Port {Port} marked as free."
    )]
    private static partial void LogPortMarkedAsFree(ILogger logger, int port);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Port {Port} is already free."
    )]
    private static partial void LogPortAlreadyFree(ILogger logger, int port);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Port {Port} is already allocated."
    )]
    private static partial void LogPortAlreadyAllocated(ILogger logger, int port);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Port {Port} successfully marked as used."
    )]
    private static partial void LogPortSuccessfullyMarkedAsUsed(ILogger logger, int port);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Allocated random free port: {Port}"
    )]
    private static partial void LogAllocatedRandomFreePort(ILogger logger, int port);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Warning,
        Message = "Minimum port {MinPort} is below the recommended range of 1000."
    )]
    private static partial void LogMinPortBelowRecommended(ILogger logger, int minPort);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message = "Error while checking allocated ports: {Message}"
    )]
    private static partial void LogErrorCheckingAllocatedPorts(ILogger logger, Exception exception, string message);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Error,
        Message = "Unsupported platform for ephemeral port exclusion: {Platform}"
    )]
    private static partial void LogUnsupportedPlatformForEphemeralPortExclusion(ILogger logger, PlatformID platform);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Error,
        Message = "Unsupported platform for scanning excluded ports: {Platform}"
    )]
    private static partial void LogUnsupportedPlatformForScanExcludedPorts(ILogger logger, PlatformID platform);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Warning,
        Message = "Error while checking ephemeral ports: {Message}"
    )]
    private static partial void LogErrorCheckingEphemeralPorts(ILogger logger, Exception exception, string message);

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Warning,
        Message = "Error while checking excluded ports: {Message}"
    )]
    private static partial void LogErrorCheckingExcludedPorts(ILogger logger, Exception exception, string message);


    [LoggerMessage(
        EventId = 13,
        Level = LogLevel.Warning,
        Message = "Excluded Port {Port} is outside the valid range (1-65535)."
    )]
    private static partial void LogErrorExcludePortOutsideRange(ILogger logger, int port);

    [LoggerMessage(
        EventId = 14,
        Level = LogLevel.Warning,
        Message = "Failed to parse ephemeral port range from netsh output: {Output}"
    )]
    private static partial void LogFailedToParseEphemeralPortRangeWindows(ILogger logger, string output);

    [LoggerMessage(
        EventId = 15,
        Level = LogLevel.Warning,
        Message = "Failed to parse ephemeral port range from /proc/sys/net/ipv4/ip_local_port_range: {Output}"
    )]
    private static partial void LogFailedToParseEphemeralPortRangeUnix(ILogger logger, string output);
}
