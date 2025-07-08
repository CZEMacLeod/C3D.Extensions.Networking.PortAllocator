using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace C3D.Extensions.Networking;

/// <summary>
/// Provides functionality to allocate and manage TCP port usage within the application,
/// avoiding conflicts with commonly used or reserved ports and currently active connections.
/// </summary>
public partial class PortAllocator
{
    /// <summary>
    /// Singleton instance of the <see cref="PortAllocator"/> class.
    /// </summary>
    private static readonly PortAllocator? portAllocator;

    /// <summary>
    /// Gets the singleton instance of the <see cref="PortAllocator"/>.
    /// </summary>
    public static readonly PortAllocator Instance = portAllocator ??= new();

    /// <summary>
    /// List of ports to avoid allocating, such as well-known or reserved ports.
    /// </summary>
    /// <remarks>
    /// Based on https://searchfox.org/mozilla-central/source/netwerk/base/nsIOService.cpp
    /// </remarks>
    private static readonly int[] avoidPorts = [
    1,      // tcpmux
    7,      // echo
    9,      // discard
    11,     // systat
    13,     // daytime
    15,     // netstat
    17,     // qotd
    19,     // chargen
    20,     // ftp-data
    21,     // ftp
    22,     // ssh
    23,     // telnet
    25,     // smtp
    37,     // time
    42,     // name
    43,     // nicname
    53,     // domain
    69,     // tftp
    77,     // priv-rjs
    79,     // finger
    87,     // ttylink
    95,     // supdup
    101,    // hostriame
    102,    // iso-tsap
    103,    // gppitnp
    104,    // acr-nema
    109,    // pop2
    110,    // pop3
    111,    // sunrpc
    113,    // auth
    115,    // sftp
    117,    // uucp-path
    119,    // nntp
    123,    // ntp
    135,    // loc-srv / epmap
    137,    // netbios
    139,    // netbios
    143,    // imap2
    161,    // snmp
    179,    // bgp
    389,    // ldap
    427,    // afp (alternate)
    465,    // smtp (alternate)
    512,    // print / exec
    513,    // login
    514,    // shell
    515,    // printer
    526,    // tempo
    530,    // courier
    531,    // chat
    532,    // netnews
    540,    // uucp
    548,    // afp
    554,    // rtsp
    556,    // remotefs
    563,    // nntp+ssl
    587,    // smtp (outgoing)
    601,    // syslog-conn
    636,    // ldap+ssl
    989,    // ftps-data
    990,    // ftps
    993,    // imap+ssl
    995,    // pop3+ssl
    1719,   // h323gatestat
    1720,   // h323hostcall
    1723,   // pptp
    2049,   // nfs
    3659,   // apple-sasl
    4045,   // lockd
    4190,   // sieve
    5060,   // sip
    5061,   // sips
    6000,   // x11
    6566,   // sane-port
    6665,   // irc (alternate)
    6666,   // irc (alternate)
    6667,   // irc (default)
    6668,   // irc (alternate)
    6669,   // irc (alternate)
    6679,   // osaut
    6697,   // irc+tls
    10080,  // amanda
];
    private static BitArray? allocatedPorts;
#if NET9_0_OR_GREATER
    private static readonly System.Threading.Lock @lock = new();
#else
    private static readonly object @lock = new();
#endif

    private PortAllocatorOptions options;
    private readonly ILogger logger;

    private int? randomSeed;
    private Random? random;
    private Random Random => random ??= randomSeed is null ?
#if NET8_0_OR_GREATER
        Random.Shared
#else
        new Random()
#endif
        : new Random(randomSeed.Value);

    /// <summary>
    /// Initializes a new instance of the <see cref="PortAllocator"/> class with a logger and options.
    /// </summary>
    /// <param name="logger">The logger to use for diagnostic messages.</param>
    /// <param name="options">The port allocator options.</param>
    public PortAllocator(ILogger logger, PortAllocatorOptions? options)
    {
        this.options = options ?? new PortAllocatorOptions();
        this.randomSeed = this.options.Seed;
        this.random = null;
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PortAllocator"/> class for dependency injection.
    /// </summary>
    /// <param name="logger">The logger to use for diagnostic messages.</param>
    /// <param name="options">The options monitor for port allocator options.</param>
    [ActivatorUtilitiesConstructor]
    private PortAllocator(ILogger logger, IOptionsMonitor<PortAllocatorOptions>? options) : this(logger, options?.CurrentValue.Clone())
    {
        options?.OnChange(o =>
        {
            if (o.Seed.HasValue && o.Seed != randomSeed)
            {
                this.randomSeed = o.Seed;
                this.random = null;
            }

            if (allocatedPorts is not null)
            {
                lock (@lock)
                {
                    if (!this.options.ExcludeWellKnownPorts && o.ExcludeWellKnownPorts)
                        PortAllocator.ExcludeWellKnownPorts(allocatedPorts);
                    ExcludePorts(allocatedPorts, o.ExcludedPorts.Except(this.options.ExcludedPorts));
                    if (!this.options.ScanInUsePorts && o.ScanInUsePorts)
                        TryScanInUsePorts_Internal(allocatedPorts);
                    if (!this.options.ExcludeEphemeralPorts && o.ExcludeEphemeralPorts)
                        ExcludeEphemeralPorts(allocatedPorts);
                    if (!this.options.ScanExcludedPorts && o.ScanExcludedPorts)
                        ScanExcludedPorts(allocatedPorts);
                }
            }
            this.options = o.Clone();
        });
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PortAllocator"/> class using a new <see cref="Random"/> instance and a no-op logger.
    /// </summary>
    /// <param name="options">The options monitor for port allocator options.</param>
    public PortAllocator(IOptionsMonitor<PortAllocatorOptions>? options = null) : this(NullLogger.Instance, options) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PortAllocator"/> class using a seeded <see cref="Random"/> instance and a no-op logger.
    /// </summary>
    /// <param name="seed">The seed for the random number generator.</param>
    public PortAllocator(int seed) : this(NullLogger.Instance, new PortAllocatorOptions() { Seed = seed }) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PortAllocator"/> class using a seeded <see cref="Random"/> instance and the specified logger.
    /// </summary>
    /// <param name="logger">The logger to use for diagnostic messages.</param>
    /// <param name="seed">The seed for the random number generator.</param>
    public PortAllocator(ILogger<PortAllocator> logger, int seed) : this(logger, new PortAllocatorOptions() { Seed = seed }) { }

    /// <summary>
    /// Gets the <see cref="BitArray"/> representing the allocation status of all ports.
    /// Ports marked as <c>true</c> are considered allocated or unavailable.
    /// </summary>
    private BitArray AllocatedPorts
    {
        get
        {
            lock (@lock)
            {
                if (allocatedPorts is null)
                {
                    allocatedPorts = new BitArray(65536);
                    if (options.ExcludeWellKnownPorts)
                        PortAllocator.ExcludeWellKnownPorts(allocatedPorts);
                    ExcludePorts(allocatedPorts, this.options.ExcludedPorts);
                    if (options.ScanInUsePorts)
                        TryScanInUsePorts_Internal(allocatedPorts);
                    if (options.ExcludeEphemeralPorts)
                        ExcludeEphemeralPorts(allocatedPorts);
                    if (options.ScanExcludedPorts)
                        ScanExcludedPorts(allocatedPorts);
                }
            }
            return allocatedPorts;
        }
    }

    /// <summary>
    /// Marks well-known ports as allocated in the provided <see cref="BitArray"/>.
    /// </summary>
    /// <param name="allocatedPorts">The <see cref="BitArray"/> to update.</param>
    private static void ExcludeWellKnownPorts(BitArray allocatedPorts)
    {
        foreach (var avoidPort in avoidPorts)
        {
            allocatedPorts[avoidPort] = true;
        }
    }

    /// <summary>
    /// Marks the specified ports as allocated in the provided <see cref="BitArray"/>.
    /// </summary>
    /// <param name="allocatedPorts">The <see cref="BitArray"/> to update.</param>
    /// <param name="ports">The ports to exclude.</param>
    private void ExcludePorts(BitArray allocatedPorts, IEnumerable<int> ports)
    {
        foreach (var avoidPort in ports)
        {
            if (avoidPort < 1 || avoidPort > 65535)
            {
                LogErrorExcludePortOutsideRange(logger, avoidPort);
            }
            allocatedPorts[avoidPort] = true;
        }
    }

    /// <summary>
    /// Scans for in-use ports and marks them as allocated.
    /// </summary>
    /// <returns><c>true</c> if the scan succeeded; otherwise, <c>false</c>.</returns>
    public bool TryScanInUsePorts()
    {
        var ap = AllocatedPorts;
        lock (@lock)
        {
            return TryScanInUsePorts_Internal(ap);
        }
    }

    /// <summary>
    /// Scans for in-use ports and marks them as allocated in the provided <see cref="BitArray"/>.
    /// </summary>
    /// <param name="allocatedPorts">The <see cref="BitArray"/> to update.</param>
    /// <returns><c>true</c> if the scan succeeded; otherwise, <c>false</c>.</returns>
    private bool TryScanInUsePorts_Internal(BitArray allocatedPorts)
    {
        try
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            foreach (var tcpci in tcpConnInfoArray)
            {
                allocatedPorts[tcpci.LocalEndPoint.Port] = true;
            }

            var tcpListenerArray = ipGlobalProperties.GetActiveTcpListeners();
            foreach (var tcpl in tcpConnInfoArray)
            {
                allocatedPorts[tcpl.LocalEndPoint.Port] = true;
            }
            return true;
        }
        catch (Exception ex)
        {
            LogErrorCheckingAllocatedPorts(logger, ex, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Excludes ephemeral ports from allocation by marking them as allocated.
    /// </summary>
    /// <param name="allocatedPorts">The <see cref="BitArray"/> to update.</param>
    private void ExcludeEphemeralPorts(BitArray allocatedPorts)
    {
        try
        {
            switch (System.Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    ExcludeEphemeralPorts_Unix(allocatedPorts);
                    break;
                case PlatformID.Win32NT:
                    ExcludeEphemeralPorts_Windows(allocatedPorts);
                    break;
                default:
                    LogUnsupportedPlatformForEphemeralPortExclusion(logger, System.Environment.OSVersion.Platform);
                    break;
            }
        }
        catch (Exception ex)
        {
            LogErrorCheckingEphemeralPorts(logger, ex, ex.Message);
        }
    }

    /// <summary>
    /// Excludes ephemeral ports on Windows by marking them as allocated.
    /// </summary>
    /// <param name="allocatedPorts">The <see cref="BitArray"/> to update.</param>
    private void ExcludeEphemeralPorts_Windows(BitArray allocatedPorts)
    {
        // Use netsh to scan for excluded ports on Windows
        using (var process = new System.Diagnostics.Process())
        {
            process.StartInfo.FileName = "netsh";
            process.StartInfo.Arguments = "int ipv4 show excludedportrange tcp";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            var output = process.StandardOutput.ReadToEnd();
            if (EphemeralPortRangeRegEx_Windows().Match(output) is Match match)
            {
                var start = int.Parse(match.Groups["start"].Value);
                var end = start + int.Parse(match.Groups["count"].Value) - 1;
                for (int i = start; i <= end; i++)
                {
                    allocatedPorts[i] = true;
                }
            }
            else
            {
                LogFailedToParseEphemeralPortRangeWindows(logger, output);
            }
        }
    }

    /// <summary>
    /// Excludes ephemeral ports on Unix by marking them as allocated.
    /// </summary>
    /// <param name="allocatedPorts">The <see cref="BitArray"/> to update.</param>
    private void ExcludeEphemeralPorts_Unix(BitArray allocatedPorts)
    {
        var output = System.IO.File.ReadAllText("/proc/sys/net/ipv4/ip_local_port_range");
        if (EphemeralPortRangeRegEx_Unix().Match(output) is Match match)
        {
            var start = int.Parse(match.Groups["start"].Value);
            var end = int.Parse(match.Groups["end"].Value);
            for (int i = start; i <= end; i++)
            {
                allocatedPorts[i] = true;
            }
        }
        else
        {
            LogFailedToParseEphemeralPortRangeUnix(logger, output);
        }
    }

    /// <summary>
    /// Scans for excluded ports (e.g., reserved by the OS) and marks them as allocated.
    /// </summary>
    /// <param name="allocatedPorts">The <see cref="BitArray"/> to update.</param>
    private void ScanExcludedPorts(BitArray allocatedPorts)
    {
        try
        {
            if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // Use netsh to scan for excluded ports on Windows
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = "netsh";
                    process.StartInfo.Arguments = "int ipv4 show excludedportrange tcp";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();
                    var output = process.StandardOutput.ReadToEnd();
                    ExcludedPortRangeRegex().Matches(output)
                        .Cast<Match>()
                        .Select(m => new
                        {
                            Start = int.Parse(m.Groups["start"].Value),
                            End = int.Parse(m.Groups["end"].Value)
                        })
                        .ToList()
                        .ForEach(range =>
                        {
                            for (int i = range.Start; i <= range.End; i++)
                            {
                                allocatedPorts[i] = true;
                            }
                        });
                }
            }
            else
            {
                LogUnsupportedPlatformForScanExcludedPorts(logger, System.Environment.OSVersion.Platform);
            }
        }
        catch (Exception ex)
        {
            LogErrorCheckingExcludedPorts(logger, ex, ex.Message);
        }
    }

    /// <summary>
    /// Marks the specified port as used (allocated).
    /// </summary>
    /// <param name="port">The port number to mark as used. Must be between 1 and 65535.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="port"/> is outside the valid range.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the port is already allocated.</exception>
    public void MarkPortAsUsed(int port)
    {
        Guard.IsBetween(port, 0, 65536, nameof(port));
        lock (@lock)
        {
            var ap = AllocatedPorts;
            if (ap[port])
            {
                throw new InvalidOperationException($"Port {port} is already allocated");
            }
            ap[port] = true;
        }
        LogPortMarkedAsUsed(logger, port);
    }

    /// <summary>
    /// Marks the specified port as free (available).
    /// </summary>
    /// <param name="port">The port number to mark as free. Must be between 1 and 65535.</param>
    /// <returns>
    /// <c>true</c> if the port was previously allocated and is now marked as free;
    /// <c>false</c> if the port was already free.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="port"/> is outside the valid range.</exception>
    public bool MarkPortAsFree(int port)
    {
        Guard.IsBetween(port, 0, 65536, nameof(port));
        bool used;
        lock (@lock)
        {
            var ap = AllocatedPorts;
            used = ap[port];
            if (used) ap[port] = false;
        }
        if (used)
        {
            LogPortMarkedAsFree(logger, port);
        }
        else
        {
            LogPortAlreadyFree(logger, port);
        }
        return used;
    }

    /// <summary>
    /// Attempts to mark the specified port as used (allocated).
    /// </summary>
    /// <param name="port">The port number to mark as used. Must be between 1 and 65535.</param>
    /// <returns>
    /// <c>true</c> if the port was successfully marked as used;
    /// <c>false</c> if the port was already allocated.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="port"/> is outside the valid range.</exception>
    public bool TryMarkPortAsUsed(int port)
    {
        Guard.IsBetween(port, 0, 65536, nameof(port));
        bool used;
        lock (@lock)
        {
            var ap = AllocatedPorts;
            used = ap[port];
            if (!used) ap[port] = true;
        }
        if (used)
        {
            LogPortAlreadyAllocated(logger, port);
        }
        else
        {
            LogPortSuccessfullyMarkedAsUsed(logger, port);
        }
        return !used;
    }

    /// <summary>
    /// Returns a random free port within the specified range and marks it as used.
    /// </summary>
    /// <param name="minPort">The minimum port number (inclusive).</param>
    /// <param name="maxPort">The maximum port number (inclusive).</param>
    /// <returns>A randomly selected free port number within the specified range.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="minPort"/> or <paramref name="maxPort"/> is outside the valid range (1 to 65535).
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="maxPort"/> is less than <paramref name="minPort"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if all ports in the specified range are marked as used.
    /// </exception>
    public int GetRandomFreePort(int minPort, int maxPort = 65535)
    {
        Guard.IsBetween(minPort, 0, 65536, nameof(minPort));
        Guard.IsBetween(maxPort, 0, 65536, nameof(maxPort));
        Guard.IsGreaterThanOrEqualTo(maxPort, minPort, nameof(maxPort));

        if (minPort < 1000)
        {
            LogMinPortBelowRecommended(logger, minPort);
        }
        int port;
        lock (@lock)
        {
            var ap = AllocatedPorts;
            if (ap.HasAllSet(minPort, maxPort))
            {
                throw new InvalidOperationException("All ports are marked as used");
            }
            do
            {
                port = Random.Next(minPort, maxPort + 1);
            } while (ap[port]);
            ap[port] = true;
        }
        LogAllocatedRandomFreePort(logger, port);
        return port;
    }

    /// <summary>
    /// Attempts to allocate a random free port within the specified range.
    /// </summary>
    /// <param name="minPort">The minimum port number (inclusive).</param>
    /// <param name="maxPort">The maximum port number (inclusive).</param>
    /// <param name="port">
    /// When this method returns, contains the allocated port number if successful; otherwise, <c>-1</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a free port was found and allocated; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="minPort"/> or <paramref name="maxPort"/> is outside the valid range (0 to 65535).
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="maxPort"/> is less than <paramref name="minPort"/>.
    /// </exception>
    public bool TryGetRandomFreePort(int minPort, int maxPort, [MaybeNullWhen(false)] out int port)
    {
        Guard.IsBetween(minPort, 0, 65536, nameof(minPort));
        Guard.IsBetween(maxPort, 0, 65536, nameof(maxPort));
        Guard.IsGreaterThanOrEqualTo(maxPort, minPort, nameof(maxPort));

        if (minPort < 1000)
        {
            LogMinPortBelowRecommended(logger, minPort);
        }
        lock (@lock)
        {
            var ap = AllocatedPorts;
            var ports = Enumerable.Range(minPort, maxPort - minPort + 1)
                .Where(p => !ap[p])
                .ToArray();
            if (ports.Length == 0)
            {
                port = -1;
                return false;
            }
            port = ports[Random.Next(ports.Length)];
            ap[port] = true;
        }
        LogAllocatedRandomFreePort(logger, port);
        return true;
    }

    /// <summary>
    /// Returns a random free port in the range 1000 to 65535 (inclusive of 1000, exclusive of 65536) and marks it as used.
    /// </summary>
    /// <remarks>
    /// This overload uses the commonly available dynamic/private port range.
    /// </remarks>
    /// <returns>
    /// A randomly selected free port number in the default range.
    /// </returns>
    public int GetRandomFreePort() => GetRandomFreePort(options.DefaultMinPort, options.DefaultMaxPort);

    /// <summary>
    /// Gets the number of free (unallocated) ports in the entire port range.
    /// </summary>
    /// <returns>The count of free ports.</returns>
    public int GetFreePortCount() => AllocatedPorts.Length - AllocatedPorts.CountSetBits();

    /// <summary>
    /// Gets the number of free (unallocated) ports in the specified range.
    /// </summary>
    /// <param name="minPort">The minimum port number (inclusive).</param>
    /// <param name="maxPort">The maximum port number (inclusive).</param>
    /// <returns>The count of free ports in the specified range.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="minPort"/> or <paramref name="maxPort"/> is outside the valid range (0 to 65535).
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="maxPort"/> is less than <paramref name="minPort"/>.
    /// </exception>
    public int GetFreePortCount(int minPort, int maxPort)
    {
        Guard.IsBetween(minPort, 0, 65536, nameof(minPort));
        Guard.IsBetween(maxPort, 0, 65536, nameof(maxPort));
        Guard.IsGreaterThanOrEqualTo(maxPort, minPort, nameof(maxPort));
        lock (@lock)
        {
            var ap = AllocatedPorts;
            return Enumerable.Range(minPort, maxPort - minPort + 1)
                .Where(p => !ap[p])
                .Count();
        }
    }

    /// <summary>
    /// Gets an array of all free (unallocated) ports in the specified range.
    /// </summary>
    /// <param name="minPort">The minimum port number (inclusive).</param>
    /// <param name="maxPort">The maximum port number (inclusive).</param>
    /// <returns>An array of free port numbers in the specified range.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="minPort"/> or <paramref name="maxPort"/> is outside the valid range (0 to 65535).
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="maxPort"/> is less than <paramref name="minPort"/>.
    /// </exception>
    public int[] GetFreePorts(int minPort, int maxPort)
    {
        Guard.IsBetween(minPort, 0, 65536, nameof(minPort));
        Guard.IsBetween(maxPort, 0, 65536, nameof(maxPort));
        Guard.IsGreaterThanOrEqualTo(maxPort, minPort, nameof(maxPort));

        lock (@lock)
        {
            var ap = AllocatedPorts;
            return Enumerable.Range(minPort, maxPort - minPort + 1)
                .Where(p => !ap[p])
                .ToArray();
        }
    }
}
