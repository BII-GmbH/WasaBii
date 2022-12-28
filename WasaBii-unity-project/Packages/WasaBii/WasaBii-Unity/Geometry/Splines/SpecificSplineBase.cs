using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.Splines;
using BII.WasaBii.Splines.Bezier;
using BII.WasaBii.Splines.CatmullRom;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Unity.Geometry.Splines {

    /// <summary>
    /// Base class for non-generic spline implementations like <see cref="UnitySpline"/> that hides
    /// the type of a low-level spline like <see cref="BezierSpline{TPos,TDiff}"/>
    /// or <see cref="CatmullRomSpline{TPos,TDiff}"/> by wrapping it.
    /// </summary>
    [Serializable]
    public abstract class SpecificSplineBase<TSelf, TPos, TDiff> : Spline<TPos, TDiff>.Copyable
    where TSelf : SpecificSplineBase<TSelf, TPos, TDiff> where TPos : unmanaged where TDiff : unmanaged {

        public readonly Spline<TPos, TDiff> Wrapped;

        protected SpecificSplineBase(Spline<TPos, TDiff> wrapped) => Wrapped = wrapped switch {
            SpecificSplineBase<TSelf, TPos, TDiff> {Wrapped: var actualWrapped} => actualWrapped,
            _ => wrapped
        };

        [Pure] public Option<T> As<T>() where T : Spline<TPos, TDiff> => Wrapped switch {
            T t => t.Some(),
            _ => Option.None
        };

        [Pure] public T AsOrThrow<T>() where T : Spline<TPos, TDiff> => Wrapped is T t
            ? t
            : throw new InvalidCastException($"{Wrapped.GetType().Name} is no {nameof(T)}");

        public int SegmentCount => Wrapped.SegmentCount;
        public IEnumerable<SplineSegment<TPos, TDiff>> Segments => Wrapped.Segments;

        public SplineSegment<TPos, TDiff> this[SplineSegmentIndex index] => Wrapped[index];

        public SplineSample<TPos, TDiff> this[NormalizedSplineLocation location] => Wrapped[location];

        public GeometricOperations<TPos, TDiff> Ops => Wrapped.Ops;

        [Pure] public Spline<TPosNew, TDiffNew> Map<TPosNew, TDiffNew>(
            Func<TPos, TPosNew> positionMapping,
            GeometricOperations<TPosNew, TDiffNew> newOps
        ) where TPosNew : unmanaged where TDiffNew : unmanaged => Wrapped.Map(positionMapping, newOps);

        /// <inheritdoc cref="ClosestOnSplineExtensions.QueryClosestPositionOnSplineToOrThrow{TPos, TDiff}"/>
        [Pure]
        public ClosestOnSplineQueryResult<TPos, TDiff> QueryClosestPositionOnSplineToOrThrow(
            TPos position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) => this.QueryClosestPositionOnSplineToOrThrow<TPos, TDiff>(position, samples);

        /// <inheritdoc cref="ClosestOnSplineExtensions.QueryClosestPositionOnSplineTo{TPos, TDiff}"/>
        [Pure]
        public Option<ClosestOnSplineQueryResult<TPos, TDiff>> QueryClosestPositionOnSplineTo(
            TPos position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) => this.QueryClosestPositionOnSplineTo<TPos, TDiff>(position, samples);

        /// <inheritdoc cref="Spline{TPos,TDiff}.Copyable.CopyWithOffset"/>
        [Pure]
        public TSelf CopyWithOffset(Func<TDiff, TDiff> tangentToOffset) => 
            mkNew(((Spline<TPos, TDiff>.Copyable)this).CopyWithOffset(tangentToOffset));

        Spline<TPos, TDiff> Spline<TPos, TDiff>.Copyable.CopyWithOffset(Func<TDiff, TDiff> tangentToOffset) =>
            Wrapped switch {
                Spline<TPos, TDiff>.Copyable copyable => copyable.CopyWithOffset(tangentToOffset),
                _ => throw new Exception($"Spline type {Wrapped.GetType()} does not implement {nameof(Spline<TPos, TDiff>.Copyable)}")
            };

        /// <inheritdoc cref="Spline{TPos,TDiff}.Copyable.CopyWithStaticOffset"/>
        [Pure]
        public TSelf CopyWithStaticOffset(TDiff offset) =>
            mkNew(((Spline<TPos, TDiff>.Copyable)this).CopyWithStaticOffset(offset));

        Spline<TPos, TDiff> Spline<TPos, TDiff>.Copyable.CopyWithStaticOffset(TDiff offset) =>
            Wrapped switch {
                Spline<TPos, TDiff>.Copyable copyable => copyable.CopyWithStaticOffset(offset),
                _ => throw new Exception($"Spline type {Wrapped.GetType()} does not implement {nameof(Spline<TPos, TDiff>.Copyable)}")
            };

        /// <inheritdoc cref="Spline{TPos,TDiff}.Copyable.CopyWithDifferentHandleDistance"/>
        [Pure]
        public TSelf CopyWithDifferentHandleDistance(Length desiredHandleDistance) =>
            mkNew(((Spline<TPos, TDiff>.Copyable)this).CopyWithDifferentHandleDistance(desiredHandleDistance));

        Spline<TPos, TDiff> Spline<TPos, TDiff>.Copyable.CopyWithDifferentHandleDistance(Length desiredHandleDistance) =>
            Wrapped switch {
                Spline<TPos, TDiff>.Copyable copyable => copyable.CopyWithDifferentHandleDistance(desiredHandleDistance),
                _ => throw new Exception($"Spline type {Wrapped.GetType()} does not implement {nameof(Spline<TPos, TDiff>.Copyable)}")
            };

        /// <inheritdoc cref="Spline{TPos,TDiff}.Copyable.Reversed"/>
        [Pure] public TSelf Reversed => mkNew(((Spline<TPos, TDiff>.Copyable)this).Reversed);
        
        Spline<TPos, TDiff> Spline<TPos, TDiff>.Copyable.Reversed =>
            Wrapped switch {
                Spline<TPos, TDiff>.Copyable copyable => copyable.Reversed,
                _ => throw new Exception($"Spline type {Wrapped.GetType()} does not implement {nameof(Spline<TPos, TDiff>.Copyable)}")
            };

        [Pure] protected abstract TSelf mkNew(Spline<TPos, TDiff> toWrap);

    }
    
}