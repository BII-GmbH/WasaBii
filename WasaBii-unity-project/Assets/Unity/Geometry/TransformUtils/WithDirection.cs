using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Units;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {

    public interface DirectionLike<TSelf> : VectorLike<TSelf>
        where TSelf : struct, DirectionLike<TSelf> { }

    public interface RelativeDirectionLike<out TRelativity> : VectorLike 
        where TRelativity : WithRelativity { }

    public interface DirectionLike<TSelf, out TRelativity> : VectorLike<TSelf>, DirectionLike<TSelf>, RelativeDirectionLike<TRelativity>
        where TSelf : struct, DirectionLike<TSelf, TRelativity> 
        where TRelativity : WithRelativity { }

    public interface GlobalDirectionLike<TSelf> : DirectionLike<TSelf, IsGlobal> where TSelf : struct, DirectionLike<TSelf, IsGlobal> { }

    public interface LocalDirectionLike<TSelf> : DirectionLike<TSelf, IsLocal> where TSelf : struct, DirectionLike<TSelf, IsLocal> { }

    public static class DirectionLikeExtensions {
        
        [Pure] public static Units.Angle AngleTo<TRelativity>(this RelativeDirectionLike<TRelativity> lhs, RelativeDirectionLike<TRelativity> rhs) 
            where TRelativity : WithRelativity => Vector3.Angle(lhs.AsVector, rhs.AsVector).Degrees();

        [Pure] public static Units.Angle SignedAngleTo<TRelativity>(
            this RelativeDirectionLike<TRelativity> lhs, RelativeDirectionLike<TRelativity> rhs, RelativeDirectionLike<TRelativity> axis
        ) where TRelativity : WithRelativity => 
            Vector3.SignedAngle(lhs.AsVector, rhs.AsVector, axis.AsVector).Degrees();

        [Pure] public static double Dot<T>(this T a, T b) where T : struct, DirectionLike<T> => a.AsVector.Dot(b.AsVector);

        [Pure] public static T Cross<T>(this T a, T b) where T : struct, DirectionLike<T> => a.CopyWithDifferentValue(a.AsVector.Cross(b.AsVector));

        [Pure] public static bool PointsInSameDirectionAs<TRelativity>(this RelativeDirectionLike<TRelativity> a, RelativeDirectionLike<TRelativity> b)
            where TRelativity : WithRelativity => a.AsVector.PointsInSameDirectionAs(b.AsVector);
        
        [Pure] public static T Add<T>(this T a, T b) where T : struct, DirectionLike<T>, HasMagnitude<T>
            => a.CopyWithDifferentValue(a.AsVector + b.AsVector);
        
        [Pure] public static T Sum<T>(this IEnumerable<T> elements) where T : struct, DirectionLike<T>, HasMagnitude<T>
            => elements.Aggregate(Add);

    }

}
