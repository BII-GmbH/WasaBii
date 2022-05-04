using BII.WasaBii.Units;
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
        
        public static Units.Angle AngleTo<TRelativity>(this RelativeDirectionLike<TRelativity> lhs, RelativeDirectionLike<TRelativity> rhs) 
            where TRelativity : WithRelativity => Vector3.Angle(lhs.AsVector, rhs.AsVector).Degrees();

        public static Units.Angle SignedAngleTo<TRelativity>(
            this RelativeDirectionLike<TRelativity> lhs, RelativeDirectionLike<TRelativity> rhs, RelativeDirectionLike<TRelativity> axis
        ) where TRelativity : WithRelativity => 
            Vector3.SignedAngle(lhs.AsVector, rhs.AsVector, axis.AsVector).Degrees();

        public static float Dot<T>(this T a, T b) where T : struct, DirectionLike<T> => a.AsVector.Dot(b.AsVector);

        public static T Cross<T>(this T a, T b) where T : struct, DirectionLike<T> => a.CopyWithDifferentValue(a.AsVector.Cross(b.AsVector));

        public static bool PointsInSameDirectionAs<TRelativity>(this RelativeDirectionLike<TRelativity> a, RelativeDirectionLike<TRelativity> b)
            where TRelativity : WithRelativity => a.AsVector.PointsInSameDirectionAs(b.AsVector);
        
    }

}
