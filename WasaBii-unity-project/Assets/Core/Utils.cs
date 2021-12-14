using System.Diagnostics.Contracts;

namespace BII.WasaBii.Core {

    /// <summary>
    /// Author: Cameron Reuschel, David Schantz
    /// <br/><br/>
    /// This class serves as a namespace for every
    /// non-specific utility function, such as 
    /// <see cref="Mod"/> and <see cref="IsNull{T}"/>
    /// </summary>
    public static class Util {
        
        /// <summary>
        /// Constructs an IEnumerable from a variable number of arguments.
        /// This is an alternative to writing <code>new [] { a, b, c }</code>
        /// when the number of arguments for an IEnumerable parameter is known.
        /// </summary>
        /// <returns>An array consisting of all the passed values.</returns>
        [Pure] public static T[] Seq<T>(params T[] args) => args;

        /// Mathematically correct modulus.
        /// Always positive for any x as long as m > 0.
        [Pure] public static int Mod(int x, int m) => (x % m + m) % m;

    }
    
}