#if !NET6_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;

namespace System;
/// <summary>
/// Polyfill for ArgumentOutOfRangeException helper methods that are only available in .NET 6.0 and later.
/// </summary>
internal static class ArgumentOutOfRangeExceptionPolyfill
{
    extension(ArgumentOutOfRangeException)
    {
        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the value is greater than the specified maximum.
        /// </summary>
        /// <param name="value">The argument to validate.</param>
        /// <param name="max">The maximum value (inclusive).</param>
        /// <param name="paramName">The name of the parameter to include in the exception.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="value"/> is greater than <paramref name="max"/>.</exception>
        public static void ThrowIfGreaterThan(int value, int max, [CallerArgumentExpression(nameof(value))] string paramName = null!)
        {
            if (value > max)
            {
                throw new ArgumentOutOfRangeException(paramName, value, $"Argument must be less than or equal to {max}.");
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the value is less than the specified minimum.
        /// </summary>
        /// <param name="value">The argument to validate.</param>
        /// <param name="min">The minimum value (inclusive).</param>
        /// <param name="paramName">The name of the parameter to include in the exception.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="value"/> is less than <paramref name="min"/>.</exception>
        public static void ThrowIfLessThan(int value, int min, [CallerArgumentExpression(nameof(value))] string paramName = null!)
        {
            if (value < min)
            {
                throw new ArgumentOutOfRangeException(paramName, value, $"Argument must be greater than or equal to {min}.");
            }
        }
    }
}

#endif
