using System.Text.RegularExpressions;

namespace C3D.Extensions.Networking;
partial class PortAllocator
{

    /// <summary>
    /// Gets a regular expression that matches excluded port ranges (start and end) from multiline text.
    /// </summary>
    /// <returns>A <see cref="Regex"/> for matching excluded port ranges.</returns>
#if NET8_0_OR_GREATER
    [GeneratedRegex(@"^\s*(?<start>(\d\d*))\s*(?<end>(\d\d*))\s*\*?\s*$", RegexOptions.Multiline)]
    private static partial Regex ExcludedPortRangeRegex();
#else
    private static Regex ExcludedPortRangeRegex() => excludedPortRangeRegex;
    private static readonly Regex excludedPortRangeRegex = new Regex(@"^\s*(?<start>(\d\d*))\s*(?<end>(\d\d*))\s*\*?\s*$", RegexOptions.Multiline);
#endif

    /// <summary>
    /// Gets a regular expression that matches ephemeral port range output from Windows (netsh) command.
    /// </summary>
    /// <returns>A <see cref="Regex"/> for matching Windows ephemeral port range output.</returns>
#if NET8_0_OR_GREATER
    [GeneratedRegex(@"^*.(?:Start Port\s*:)*\s*(?<start>\d\d*)*.(?:Number of Ports\s*:)*\s*(?<count>\d\d*)*.$", RegexOptions.Singleline)]
    private static partial Regex EphemeralPortRangeRegEx_Windows();
#else
    private static Regex EphemeralPortRangeRegEx_Windows() => ephemeralPortRangeRegEx;
    private static readonly Regex ephemeralPortRangeRegEx = new Regex(@"^*.(?:Start Port\s*:)*\s*(?<start>\d\d*)*.(?:Number of Ports\s*:)*\s*(?<count>\d\d*)*.$", RegexOptions.Multiline);
#endif
    /// <summary>
    /// Gets a regular expression that matches ephemeral port range output from Unix systems (/proc/sys/net/ipv4/ip_local_port_range).
    /// </summary>
    /// <returns>A <see cref="Regex"/> for matching Unix ephemeral port range output.</returns>
#if NET8_0_OR_GREATER
    [GeneratedRegex(@"^\s*(?<start>\d\d*)\s*\s*(?<end>\d\d*)\s*$", RegexOptions.Singleline)]
    private static partial Regex EphemeralPortRangeRegEx_Unix();
#else
    private static Regex EphemeralPortRangeRegEx_Unix() => ephemeralPortRangeRegExUnix;
    private static readonly Regex ephemeralPortRangeRegExUnix = new Regex(@"^\s*(?<start>\d\d*)\s*\s*(?<end>\d\d*)\s*$", RegexOptions.Multiline);
#endif
}
