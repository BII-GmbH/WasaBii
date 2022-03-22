using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace BII.WasaBii.Core {
    public static class LazyContract {
        /// Since <see cref="Contract.Assert(bool, string)"/> always builds the string,
        /// even if the condition holds true, performance measurements can get inaccurate.
        /// (e.g. Lots of GC alloc, string buffer overhead)
        /// Therefore, this utility can be used in frequently called code,
        /// which behaves the same as <see cref="Contract.Assert(bool, string)"/>
        /// but it builds the string lazily.
        // Note DG: These attributes are the same used for Contract.Assert.
        // They ensure that the method is only compiled and called
        // when the "DEBUG" or "CONTRACTS_FULL" symbols are defined.
        // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.conditionalattribute?view=net-5.0
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        public static void Assert(bool condition, Func<string> errorMessage) {
            if(!condition) Contract.Assert(condition, errorMessage());
        }
    }
}