namespace System.Collections;
using System.Numerics;

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

    internal static int CountSetBits(this BitArray bitArray)
    {

#if NET8_0_OR_GREATER
        UInt32[] ints = new UInt32[(bitArray.Count >> 5) + 1];
        bitArray.CopyTo(ints, 0);
        Int32 count = 0;        for (Int32 i = 0; i < ints.Length; i++)
        {
            count += BitOperations.PopCount(ints[i]);
        }
#else
        Int32[] ints = new Int32[(bitArray.Count >> 5) + 1];
        bitArray.CopyTo(ints, 0);
        Int32 count = 0;
        // fix for not truncated bits in last integer that may have been set to true with SetAll()
        ints[ints.Length - 1] &= ~(-1 << (bitArray.Count % 32));

        for (Int32 i = 0; i < ints.Length; i++)
        {

            Int32 c = ints[i];

            // magic (http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel)
            unchecked
            {
                c = c - ((c >> 1) & 0x55555555);
                c = (c & 0x33333333) + ((c >> 2) & 0x33333333);
                c = ((c + (c >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
            }

            count += c;

        }
#endif
        return count;
    }
}
