using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    public interface DirectionLike {
        System.Numerics.Vector3 AsNumericsVector { get; }
    }

    public interface RelativeDirectionLike<out TRelativity> : DirectionLike where TRelativity : WithRelativity { }

    public interface DirectionLike<TSelf, out TRelativity> : RelativeDirectionLike<TRelativity>
        where TSelf : struct, DirectionLike<TSelf, TRelativity>, TRelativity
        where TRelativity : WithRelativity { }

    public interface GlobalDirectionLike<TSelf> : DirectionLike<TSelf, IsGlobal>, IsGlobal where TSelf : struct, GlobalDirectionLike<TSelf> { }

    public interface LocalDirectionLike<TSelf> : DirectionLike<TSelf, IsLocal>, IsLocal where TSelf : struct, LocalDirectionLike<TSelf> { }

    public static class DirectionLikeExtensions {
        
        [Pure] public static Angle AngleTo<TRelativity>(this RelativeDirectionLike<TRelativity> lhs, RelativeDirectionLike<TRelativity> rhs) 
            where TRelativity : WithRelativity => Vector3.Angle(lhs.AsVector, rhs.AsVector).Degrees();

        [Pure] public static Angle SignedAngleTo<TRelativity>(
            this RelativeDirectionLike<TRelativity> lhs, RelativeDirectionLike<TRelativity> rhs, RelativeDirectionLike<TRelativity> axis
        ) where TRelativity : WithRelativity => 
            Vector3.SignedAngle(lhs.AsVector, rhs.AsVector, axis.AsVector).Degrees();

        [Pure] public static bool PointsInSameDirectionAs<TRelativity>(this RelativeDirectionLike<TRelativity> a, RelativeDirectionLike<TRelativity> b)
            where TRelativity : WithRelativity => a.AsVector.PointsInSameDirectionAs(b.AsVector);
        
        [Pure] public static T Sum<T>(this IEnumerable<T> elements) where T : struct, DirectionLike, HasMagnitude<T>
            => elements.Aggregate(Add);

    }

}
