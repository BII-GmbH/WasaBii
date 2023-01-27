using NUnit.Framework.Constraints;

namespace BII.WasaBii.UnitSystem.Tests
{
    public static class UnitValueEqualityExtension
    {

        public static EqualConstraint WithinUnitValue<T>(this EqualConstraint constraint, T tolerance)
        where T : struct, IUnitValue<T> => 
            constraint.Within(tolerance).Using<T>((expected, actual) => 
                expected.IsNearly(actual, tolerance) ? 0 : expected.SiValue.CompareTo(actual.SiValue));

    }
}