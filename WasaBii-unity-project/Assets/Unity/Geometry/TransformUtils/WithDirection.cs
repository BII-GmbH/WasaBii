using BII.WasaBii.Units;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {

    public interface DirectionLike<out TRelativity> : VectorLike where TRelativity : WithRelativity { }

    public interface GlobalDirectionLike : DirectionLike<IsGlobal> { }

    public interface LocalDirectionLike : DirectionLike<IsLocal> { }

    public static class DirectionLikeExtensions {
        
        public static Units.Angle AngleTo(this GlobalDirectionLike lhs, GlobalDirectionLike rhs)
            => Vector3.Angle(lhs.AsVector, rhs.AsVector).Degrees();

        public static Units.Angle AngleTo(this LocalDirectionLike lhs, LocalDirectionLike rhs)
            => Vector3.Angle(lhs.AsVector, rhs.AsVector).Degrees();

        public static Units.Angle SignedAngleTo(this GlobalDirectionLike lhs, GlobalDirectionLike rhs, GlobalDirection axis)
            => Vector3.SignedAngle(lhs.AsVector, rhs.AsVector, axis.AsVector).Degrees();

        public static Units.Angle SignedAngleTo(this LocalDirectionLike lhs, LocalDirectionLike rhs, LocalDirection axis)
            => Vector3.SignedAngle(lhs.AsVector, rhs.AsVector, axis.AsVector).Degrees();

        public static float Dot(this GlobalDirectionLike a, GlobalDirectionLike b) => a.AsVector.Dot(b.AsVector);

        public static float Dot(this LocalDirectionLike a, LocalDirectionLike b) => a.AsVector.Dot(b.AsVector);

        public static Vector3 Cross(this GlobalDirectionLike a, GlobalDirectionLike b) => a.AsVector.Cross(b.AsVector);

        public static Vector3 Cross(this LocalDirectionLike a, LocalDirectionLike b) => a.AsVector.Cross(b.AsVector);

        public static bool PointsInSameDirectionAs(this GlobalDirectionLike a, GlobalDirectionLike b) 
            => a.AsVector.PointsInSameDirectionAs(b.AsVector);
        
        public static bool PointsInSameDirectionAs(this LocalDirectionLike a, LocalDirectionLike b) 
            => a.AsVector.PointsInSameDirectionAs(b.AsVector);
        
    }

}
