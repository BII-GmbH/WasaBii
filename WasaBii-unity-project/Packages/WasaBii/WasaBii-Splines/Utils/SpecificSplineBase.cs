using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {

    /// <summary>
    /// Base class for non-generic spline implementations like <see cref="UnitySpline"/> that hides
    /// the type of a low-level spline like <see cref="BII.WasaBii.Splines.Bezier.BezierSpline{TPos,TDiff,TTime,TVel}"/>
    /// or <see cref="BII.WasaBii.Splines.CatmullRom.CatmullRomSpline{TPos,TDiff,TTime,TVel}"/> by wrapping it.
    /// </summary>
    [Serializable]
    public abstract class SpecificSplineBase<TSelf, TPos, TDiff, TTime, TVel> : Spline<TPos, TDiff, TTime, TVel>.Copyable
    where TSelf : SpecificSplineBase<TSelf, TPos, TDiff, TTime, TVel> 
    where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged
    {

        public readonly Spline<TPos, TDiff, TTime, TVel> Wrapped;

        protected SpecificSplineBase(Spline<TPos, TDiff, TTime, TVel> wrapped) => Wrapped = wrapped switch {
            SpecificSplineBase<TSelf, TPos, TDiff, TTime, TVel> {Wrapped: var actualWrapped} => actualWrapped,
            _ => wrapped
        };

        [Pure] public Option<T> As<T>() where T : Spline<TPos, TDiff, TTime, TVel> => Wrapped switch {
            T t => t.Some(),
            _ => Option.None
        };

        [Pure] public T AsOrThrow<T>() where T : Spline<TPos, TDiff, TTime, TVel> => Wrapped is T t
            ? t
            : throw new InvalidCastException($"{Wrapped.GetType().Name} is no {nameof(T)}");

        public Length Length => Wrapped.Length;
        public TTime TotalDuration => Wrapped.TotalDuration;

        public int SegmentCount => Wrapped.SegmentCount;
        public IEnumerable<SplineSegment<TPos, TDiff, TTime, TVel>> Segments => Wrapped.Segments;

        public SplineSample<TPos, TDiff, TTime, TVel> this[TTime t] => Wrapped[t];
        public SplineSegment<TPos, TDiff, TTime, TVel> this[SplineSegmentIndex index] => Wrapped[index];
        public SplineSample<TPos, TDiff, TTime, TVel> this[SplineLocation location] => Wrapped[location];
        public SplineSample<TPos, TDiff, TTime, TVel> this[NormalizedSplineLocation location] => Wrapped[location];

        public ImmutableArray<Length> SpatialSegmentOffsets => Wrapped.SpatialSegmentOffsets;
        public ImmutableArray<TTime> TemporalSegmentOffsets => Wrapped.TemporalSegmentOffsets;
        public GeometricOperations<TPos, TDiff, TTime, TVel> Ops => Wrapped.Ops;

        [Pure] public Spline<TPosNew, TDiffNew, TTime, TVelNew> Map<TPosNew, TDiffNew, TVelNew>(
            Func<TPos, TPosNew> positionMapping, GeometricOperations<TPosNew, TDiffNew, TTime, TVelNew> newOps
        ) where TPosNew : unmanaged where TDiffNew : unmanaged where TVelNew : unmanaged => 
            Wrapped.Map(positionMapping, newOps);

        /// <inheritdoc cref="ClosestOnSplineExtensions.QueryClosestPositionOnSplineTo{TPos, TDiff, TTime, TVel}"/>
        [Pure]
        public ClosestOnSplineQueryResult<TPos, TDiff, TTime, TVel> QueryClosestPositionOnSplineTo(
            TPos position,
            int initialSamples = ClosestOnSplineExtensions.DefaultInitialSamplingCount,
            int iterations = ClosestOnSplineExtensions.DefaultIterations,
            double minStepSize = ClosestOnSplineExtensions.DefaultMinStepSize
        ) => this.QueryClosestPositionOnSplineTo<TPos, TDiff, TTime, TVel>(position, initialSamples, iterations, minStepSize);

        /// <inheritdoc cref="Spline{TPos,TDiff,TTime,TVel}.Copyable.CopyWithOffset"/>
        [Pure]
        public TSelf CopyWithOffset(Func<TVel, TDiff> tangentToOffset) => 
            mkNew(((Spline<TPos, TDiff, TTime, TVel>.Copyable)this).CopyWithOffset(tangentToOffset));

        Spline<TPos, TDiff, TTime, TVel> Spline<TPos, TDiff, TTime, TVel>.Copyable.CopyWithOffset(Func<TVel, TDiff> tangentToOffset) =>
            Wrapped switch {
                Spline<TPos, TDiff, TTime, TVel>.Copyable copyable => copyable.CopyWithOffset(tangentToOffset),
                _ => throw new Exception($"Spline type {Wrapped.GetType()} does not implement {nameof(Spline<TPos, TDiff, TTime, TVel>.Copyable)}")
            };

        /// <inheritdoc cref="Spline{TPos,TDiff,TTime,TVel}.Copyable.CopyWithStaticOffset"/>
        [Pure]
        public TSelf CopyWithStaticOffset(TDiff offset) =>
            mkNew(((Spline<TPos, TDiff, TTime, TVel>.Copyable)this).CopyWithStaticOffset(offset));

        Spline<TPos, TDiff, TTime, TVel> Spline<TPos, TDiff, TTime, TVel>.Copyable.CopyWithStaticOffset(TDiff offset) =>
            Wrapped switch {
                Spline<TPos, TDiff, TTime, TVel>.Copyable copyable => copyable.CopyWithStaticOffset(offset),
                _ => throw new Exception($"Spline type {Wrapped.GetType()} does not implement {nameof(Spline<TPos, TDiff, TTime, TVel>.Copyable)}")
            };

        /// <inheritdoc cref="Spline{TPos,TDiff,TTime,TVel}.Copyable.CopyWithDifferentHandleDistance"/>
        [Pure]
        public TSelf CopyWithDifferentHandleDistance(Length desiredHandleDistance) =>
            mkNew(((Spline<TPos, TDiff, TTime, TVel>.Copyable)this).CopyWithDifferentHandleDistance(desiredHandleDistance));

        Spline<TPos, TDiff, TTime, TVel> Spline<TPos, TDiff, TTime, TVel>.Copyable.CopyWithDifferentHandleDistance(Length desiredHandleDistance) =>
            Wrapped switch {
                Spline<TPos, TDiff, TTime, TVel>.Copyable copyable => copyable.CopyWithDifferentHandleDistance(desiredHandleDistance),
                _ => throw new Exception($"Spline type {Wrapped.GetType()} does not implement {nameof(Spline<TPos, TDiff, TTime, TVel>.Copyable)}")
            };

        /// <inheritdoc cref="Spline{TPos,TDiff,TTime,TVel}.Copyable.Reversed"/>
        public TSelf Reversed => mkNew(((Spline<TPos, TDiff, TTime, TVel>.Copyable)this).Reversed);
        
        Spline<TPos, TDiff, TTime, TVel> Spline<TPos, TDiff, TTime, TVel>.Copyable.Reversed =>
            Wrapped switch {
                Spline<TPos, TDiff, TTime, TVel>.Copyable copyable => copyable.Reversed,
                _ => throw new Exception($"Spline type {Wrapped.GetType()} does not implement {nameof(Spline<TPos, TDiff, TTime, TVel>.Copyable)}")
            };

        [Pure] protected abstract TSelf mkNew(Spline<TPos, TDiff, TTime, TVel> toWrap);

    }
    
}