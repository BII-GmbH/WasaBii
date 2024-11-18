# Splines Module

## Overview

This module provides an implementation of Catmull-Rom and Bezier splines and high-level wrappers around them.
Splines are implicit mathematical representations of "curved lines" and can be used for Animations, Movement and curved Objects.

## Spline Fundamentals

A spline is a collection of spline segment, where each segment is a curved line between two points. A segment's trajectory is usually influenced by additional points, depending on the spline type.
The collection of a spline's points is called its *handles*.
Additionally, spline segments each have a duration, describing how long it takes to traverse them. Thus, the spline itself has a duration, which is simply the sum of its segments'.

- A `Spline` is the base interface for all spline types. Built-in implementations are the `CatmullRomSpline` and the `BezierSpline`.
- A `SplineSegment` is the segment between two interpolated handles.
- A `SplineSample` is a sampled location somewhere on a spline. It provides information about the position, velocity / tangent and acceleration / curvature of the spline (segment) at that location.

### Generics

#### Handles

Splines are generic over their handle type. This means that you are not forced to build them using System.Numerics or Unity `Vector3`s, but any type you want.
For example, you could define a spline with `LocalPosition`s or `GlobalPosition`s, thus giving more context to the spline. However
depending on your use case, you might want to distinguish between "positions" and "differences between positions" on a type system
level. This separation is built into the spline system, so a spline built on `LocalPosition`s as handles would be a `Spline<LocalPosition, LocalOffset>`.
For the necessary mathematical calculations, you need to supply a `GeometricOperations` instance which acts like a type class.
Its responsibility is to execute the relevant operations like addition and subtraction of your custom types.

As a result, you can work with any handle / diff types of your liking, as long as you can implement `GeometricOperations` for them. E.g.:
- 3D Vectors
- 2D Vectors
- 10D Vectors
- Lazy\<Vector\>s (Note that you will be forced to execute the calculation for cases like the dot product)
- GPU buffers
- `Nothing`

For the most common use cases (namely `UnityEngine.Vector3`, `LocalPosition`/`LocalOffset` and `GlobalPosition`/`GlobalOffset`), an implementation of
the `GeometricOperations` as well as some utilities are given. Thus, the classes `UnitySpline`, `LocalSpline` and `GlobalSpline` should be
good starting points in most situations.

#### Time

Splines are also generic over their duration type. This means that you can define splines whose duration is a `float`, `double`, `UnitSystem.Duration`, `TimeSpan` or any other type that represents time.
Likewise, a spline is generic over the type of its first derivative with respect to time. For example, the derivative of a spline with handle type `GlobalPosition` and time type `Duration` would be `GlobalVelocity`.

If you do not care about time, you can use so-called "uniform" splines. These splines have a duration of 1.0 (double) and their derivative is the same as their handle diff type. Each segment has the same duration.

### Sampling

There are three ways to represent a location on a spline:
- The aforementioned time type (e.g. `Duration`) represents the time elapsed since the spline's (temporal) beginning. Must be at least 0 and at most the spline's duration.
- The `SplineLocation` is the distance (in meters) of a location on the spline from the spline's (spatial) beginning. This distance is not the Euclidean distance to the first handle but the path length when traversing along the spline. Must be at least 0 and at most the spline's length.
- The `NormalizedSplineLocation` is a number which describes a location on the spline based on spline segment 
  index and the progress along that segment. For example, a value of `3.5f` indicates the point half-way along 
  the fourth segment. Must be at least 0 and at most the spline's segment count.
  
`SplineLocations` are used a lot, especially when you want to traverse a spline at a custom speed independent of the spline's duration and the segment's individual durations. Note that they are computationally more expensive than the other two types, as they require conversion to a `NormalizedSplineLocation` to retrieve information about the spline at that location.

A spline sample can be retrieved by indexing a spline with one of the aforementioned location types. E.g.:
```cs
// Given a non-uniform global spline
Spline<GlobalPosition, GlobalOffset, Duration, GlobalVelocity> spline = GlobalSpline.FromHandles(...);

// Sample the position at 20 seconds after the spline's beginning
GlobalPosition p = spline[20.Seconds()].Position;

// Sample the velocity at 50% of the 4th segment
GlobalVelocity v = spline[NormalizedSplineLocation.From(3.5)].Velocity; 

// Sample the time at 15 meters along the spline
Duration d = spline[SplineLocation.From(15)].GlobalT;
```

## Common Use-Cases

### Creating splines:

Splines can be created in two ways:
- Using the builder methods in the `GenericSpline` class or their counterparts in `UnitySpline`,`GlobalSpline` and `LocalSpline`. E.g. `UnitySpline.FromHandles(beginMargin, handles, endMargin)`
- Using the extension method on `IEnumerable<TPos>`, found in `GenericEnumerableToSplineExtensions` or the aforementioned specialized classes.

### Closest position on spline:

To retrieve the closest point on a `Spline` (or the spline of a `WithSpline`) relative to a position in World-Space, the `.QueryClosestPositionOnSplineTo(TPos)` extension method can be used.
The returned `ClosestOnSplineQueryResult` contains information about the SplineLocation, Distance and Position on spline.
There also exists a variant that can be used on `IEnumerable<WithSpline>` called `.QueryClosestPositionOnSplinesTo(TPos)`.

Given these algorithms' greedy implementation, they don't work well on heavily curved splines.

For more options, refer to the contents of `ClosestOnSplineExtensions.cs`.

### Sampling splines:

In order to sample many positions on a spline at once, the `.SampleSplineEvery(...)` and `.SampleSplineBetween(...)` extension methods can be used. They return a list of spline samples, from which you can retrieve data like Positions, Tangents or Curvatures.

For more options, refer to `SplineSampleExtensions.cs`.

## Domain Specific Terms

### Definition of splines
In mathematics, a Catmull-Rom segment is defined by 4 handles: The 2nd and 3rd are interpolated, while the other 2 only determine the course.

The high-level spline API of this module defines a catmull-rom spline as the following: A Begin (margin) handle, 2 or more (normal) handles and an end (margin) handle.
- These handles are positions
- The normal handles are all interpolated in the order they appear
- The margin handles are not interpolated and can be omitted in some cases (as they will be autogenerated)

### External Links

- [Splines](https://en.wikipedia.org/wiki/Spline_(mathematics\))
- [Catmull Rom Splines](https://en.wikipedia.org/wiki/Centripetal_Catmull%E2%80%93Rom_spline)