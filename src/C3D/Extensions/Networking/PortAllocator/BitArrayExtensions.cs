namespace System.Collections;

/// <summary>
/// Provides extension methods for <see cref="BitArray"/> to check for set bits.
/// </summary>
internal static class BitArrayExtensions
{
#if !NET8_0_OR_GREATER
    /// <summary>
    /// Determines whether any bit in the <see cref="BitArray"/> is set to <c>true</c>.
    /// </summary>
    /// <param name="bitArray">The <see cref="BitArray"/> to check.</param>
    /// <returns><c>true</c> if at least one bit is set; otherwise, <c>false</c>.</returns>
    internal static bool HasAnySet(this BitArray bitArray)
    {
        for (int i = 0; i < bitArray.Count; i++)
        {
            if (bitArray[i])
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether all bits in the <see cref="BitArray"/> are set to <c>true</c>.
    /// </summary>
    /// <param name="bitArray">The <see cref="BitArray"/> to check.</param>
    /// <returns><c>true</c> if all bits are set; otherwise, <c>false</c>.</returns>
    internal static bool HasAllSet(this BitArray bitArray)
    {
        for (int i = 0; i < bitArray.Count; i++)
        {
            if (!bitArray[i])
            {
                return false;
            }
        }

        return true;
    }
#endif

    /// <summary>
    /// Determines whether all bits in the specified range of the <see cref="BitArray"/> are set to <c>true</c>.
    /// </summary>
    /// <param name="bitArray">The <see cref="BitArray"/> to check.</param>
    /// <param name="min">The inclusive lower bound of the range to check.</param>
    /// <param name="max">The inclusive upper bound of the range to check.</param>
    /// <returns><c>true</c> if all bits in the specified range are set; otherwise, <c>false</c>.</returns>
    internal static bool HasAllSet(this BitArray bitArray, int min, int max)
    {
        for (int i = min; i <= max; i++)
        {
            if (!bitArray[i])
            {
                return false;
            }
        }

        return true;
    }
}
