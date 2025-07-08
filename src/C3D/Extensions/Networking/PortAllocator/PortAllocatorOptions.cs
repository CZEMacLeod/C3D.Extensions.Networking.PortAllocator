using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace C3D.Extensions.Networking;

/// <summary>
/// Options for configuring the behaviour of the <c>PortAllocator</c>.
/// </summary>
public class PortAllocatorOptions : IValidateOptions<PortAllocatorOptions>
{
    /// <summary>
    /// Gets or sets the random seed for the port allocation.
    /// </summary>
    public int? Seed { get; set; } = null;

    /// <summary>
    /// Gets the list of ports to exclude from the allocation.
    /// </summary>
    public List<int> ExcludedPorts { get; private set; } = new List<int>();

    /// <summary>
    /// Gets or sets a value indicating whether to exclude well-known or reserved ports from the allocation.
    /// </summary>
    /// <remarks>
    /// Based on https://searchfox.org/mozilla-central/source/netwerk/base/nsIOService.cpp
    /// </remarks>
    public bool ExcludeWellKnownPorts { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to scan for in-use ports.
    /// </summary>
    public bool ScanInUsePorts { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to exclude ephemeral ports from the allocation.
    /// </summary>
    public bool ExcludeEphemeralPorts { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to use netsh to scan for excluded ports. (Only works on Windows)
    /// </summary>
    /// <remarks>
    /// netsh int ipv4 show excludedportrange tcp
    /// </remarks>
    public bool ScanExcludedPorts { get; set; } = System.Environment.OSVersion.Platform==System.PlatformID.Win32NT;

    /// <summary>
    /// Gets or sets the default minimum port number to allocate when allocating a random port.
    /// </summary>
    public int DefaultMinPort { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the default maximum port number to allocate when allocating a random port.
    /// </summary>
    public int DefaultMaxPort { get; set; } = 65535;

    /// <summary>
    /// Validates the <see cref="PortAllocatorOptions"/> instance.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>A <see cref="ValidateOptionsResult"/> indicating the result of the validation.</returns>
    public ValidateOptionsResult Validate(string? name, PortAllocatorOptions options)
    {
        if (options.DefaultMaxPort < options.DefaultMinPort)
        {
            return ValidateOptionsResult.Fail(
                $"DefaultMaxPort ({options.DefaultMaxPort}) must be greater than or equal to DefaultMinPort ({options.DefaultMinPort}).");
        }
        if (options.DefaultMinPort < 1 || options.DefaultMinPort > 65535)
        {
            return ValidateOptionsResult.Fail(
                $"DefaultMinPort ({options.DefaultMinPort}) must be between 1 and 65535.");
        }
        if (options.DefaultMaxPort < 1 || options.DefaultMaxPort > 65535)
        {
            return ValidateOptionsResult.Fail(
                $"DefaultMaxPort ({options.DefaultMaxPort}) must be between 1 and 65535.");
        }
        if (options.Seed.HasValue && options.Seed.Value < 0)
        {
            return ValidateOptionsResult.Fail("Seed must be a non-negative integer.");
        }
        if (options.ExcludedPorts != null && options.ExcludedPorts.Count > 0)
        {
            foreach (var port in options.ExcludedPorts)
            {
                if (port < 1 || port > 65535)
                {
                    return ValidateOptionsResult.Fail($"Excluded port {port} is out of range (1-65535).");
                }
            }
        }
        return ValidateOptionsResult.Success;
    }

    /// <summary>
    /// Creates a deep copy of the current <see cref="PortAllocatorOptions"/> instance.
    /// </summary>
    /// <returns>A new <see cref="PortAllocatorOptions"/> instance with the same values.</returns>
    internal PortAllocatorOptions Clone()
    {
        return new PortAllocatorOptions
        {
            Seed = this.Seed,
            ExcludedPorts = new List<int>(this.ExcludedPorts),
            ExcludeWellKnownPorts = this.ExcludeWellKnownPorts,
            ScanInUsePorts = this.ScanInUsePorts,
            ExcludeEphemeralPorts = this.ExcludeEphemeralPorts,
            ScanExcludedPorts = this.ScanExcludedPorts,
            DefaultMinPort = this.DefaultMinPort,
            DefaultMaxPort = this.DefaultMaxPort
        };
    }
}
