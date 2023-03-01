using NUnit.Framework.Constraints;

namespace BII.WasaBii.UnitSystem.Tests
{
    public static class UnitValueEqualityExtension
    {

        /// <summary>
        /// Like <see cref="EqualConstraint.Within"/>, but works for <see cref="IUnitValue{T}"/>. Configures
        /// the <see cref="EqualConstraint"/> such that the equality is checked within a <paramref name="tolerance"/>.
        /// </summary>
        /// <example>
        /// assert that duration == 1min ± 1sec:
        /// Assert.That(duration.AsSeconds(), Is.EqualTo(1.0.Minute().AsSeconds()).Within(1.0));
        /// ==
        /// Assert.That(duration, Is.EqualTo(1.0.Minute()).WithinUnitValue(1.0.Seconds()));
        /// </example>
        public static EqualConstraint WithinUnitValue<T>(this EqualConstraint constraint, T tolerance)
        where T : struct, IUnitValue<T> => 
            constraint.Using<T>((expected, actual) => 
                expected.IsNearly(actual, tolerance) ? 0 : expected.SiValue.CompareTo(actual.SiValue));

    }
}