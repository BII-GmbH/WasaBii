using System;
using BII.WasaBii.Units;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {

    /// Untyped interface that is only used by utilities which don't care about the specific type.
    /// Should never be implemented directly, use the generic version instead.
    public interface VectorLike {
        Vector3 AsVector { get; }
    }
    
    /// Supertype for all transform utils that essentially wrap a vector.
    public interface VectorLike<TSelf> : VectorLike, GeometryHelper<TSelf>
    where TSelf : struct, VectorLike<TSelf> {
        
        /// Constructs a new instance of this type with the given value.
        /// Needed by utilities like `Map`.
        TSelf CopyWithDifferentValue(Vector3 newValue);

        [Pure] TSelf GeometryHelper<TSelf>.LerpTo(TSelf target, double progress, bool shouldClamp) =>
            CopyWithDifferentValue(shouldClamp 
                ? Vector3.Lerp(AsVector, target.AsVector, (float)progress) 
                : Vector3.LerpUnclamped(AsVector, target.AsVector, (float)progress)
            );

        [Pure] TSelf GeometryHelper<TSelf>.SlerpTo(TSelf target, double progress, bool shouldClamp)  =>
            CopyWithDifferentValue(shouldClamp 
                ? Vector3.Slerp(AsVector, target.AsVector, (float)progress) 
                : Vector3.SlerpUnclamped(AsVector, target.AsVector, (float)progress)
            );
    }
    
    /// Signals that the wrapped vector's magnitude is actually meaningful.
    /// Essentially ensures that some utilities like `Min` and 'Max' are not
    /// available for types without a magnitude, e.g. <see cref="LocalDirection"/>.
    /// Be sure to implement this if you want these.
    public interface HasMagnitude<TSelf>
    where TSelf : struct, HasMagnitude<TSelf>, VectorLike<TSelf> { }
    
    public static class VectorLikeExtensions {
        
        [Pure] public static Length X<T>(this T vectorLike) where T : struct, VectorLike => vectorLike.AsVector.x.Meters();
        [Pure] public static Length Y<T>(this T vectorLike) where T : struct, VectorLike => vectorLike.AsVector.y.Meters();
        [Pure] public static Length Z<T>(this T vectorLike) where T : struct, VectorLike => vectorLike.AsVector.z.Meters();

        [Pure] public static T WithX<T>(this T t, float x) where T : struct, HasMagnitude<T>, VectorLike<T> =>
            t.CopyWithDifferentValue(t.AsVector.WithX(x));
        [Pure] public static T WithY<T>(this T t, float y) where T : struct, HasMagnitude<T>, VectorLike<T> => 
            t.CopyWithDifferentValue(t.AsVector.WithY(y));
        [Pure] public static T WithZ<T>(this T t, float z) where T : struct, HasMagnitude<T>, VectorLike<T> => 
            t.CopyWithDifferentValue(t.AsVector.WithZ(z));

        [Pure] public static T WithX<T>(this T t, Func<float, float> f) where T : struct, HasMagnitude<T>, VectorLike<T> =>
            t.CopyWithDifferentValue(t.AsVector.WithX(f));
        [Pure] public static T WithY<T>(this T t, Func<float, float> f) where T : struct, HasMagnitude<T>, VectorLike<T> => 
            t.CopyWithDifferentValue(t.AsVector.WithY(f));
        [Pure] public static T WithZ<T>(this T t, Func<float, float> f) where T : struct, HasMagnitude<T>, VectorLike<T> => 
            t.CopyWithDifferentValue(t.AsVector.WithZ(f));

        [Pure] public static T WithX<T>(this T t, Length x) where T : struct, HasMagnitude<T>, VectorLike<T> => 
            t.WithX((float)x.AsMeters());
        [Pure] public static T WithY<T>(this T t, Length y) where T : struct, HasMagnitude<T>, VectorLike<T> => 
            t.WithY((float)y.AsMeters());
        [Pure] public static T WithZ<T>(this T t, Length z) where T : struct, HasMagnitude<T>, VectorLike<T> => 
            t.WithZ((float)z.AsMeters());

        [Pure] public static T WithX<T>(this T t, Func<Length, Length> f) where T : struct, HasMagnitude<T>, VectorLike<T> =>
            t.WithX(x => (float)f(x.Meters()).AsMeters());
        [Pure] public static T WithY<T>(this T t, Func<Length, Length> f) where T : struct, HasMagnitude<T>, VectorLike<T> => 
            t.WithY(y => (float)f(y.Meters()).AsMeters());
        [Pure] public static T WithZ<T>(this T t, Func<Length, Length> f) where T : struct, HasMagnitude<T>, VectorLike<T> => 
            t.WithZ(z => (float)f(z.Meters()).AsMeters());

        [Pure] public static T Map<T>(this T t, Func<Vector3, Vector3> f) where T : struct, HasMagnitude<T>, VectorLike<T> =>
            t.CopyWithDifferentValue(f(t.AsVector));

        [Pure] public static bool IsNearly<T>(this T left, T right, float equalityThreshold = 1E-30f) where T : struct, VectorLike =>
            left.AsVector.IsNearly(right.AsVector, equalityThreshold);

        [Pure]
        public static T Scale<T>(this T vectorLike, float scale) where T : struct, HasMagnitude<T>, VectorLike<T> =>
            vectorLike.Map(e => e * scale);

        [Pure] public static T LerpTo<T>(this T start, T end, double perc, bool shouldClamp = true)
            where T : struct, VectorLike<T> => start.LerpTo(end, perc, shouldClamp);
        
        [Pure] public static T SlerpTo<T>(this T start, T end, double perc, bool shouldClamp = true) 
            where T : struct, VectorLike<T> => start.SlerpTo(end, perc, shouldClamp);

        [Pure] public static T Min<T>(this T a, T b) where T : struct, HasMagnitude<T>, VectorLike<T> => 
            a.Map(v => Vector3.Min(v, b.AsVector));

        [Pure] public static T Max<T>(this T a, T b) where T : struct, HasMagnitude<T>, VectorLike<T> => 
            a.Map(v => Vector3.Max(v, b.AsVector));

    }

}
