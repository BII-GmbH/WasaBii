# Splines Module

## Overview

This module provides an implementation of Catmull-Rom splines and high-level wrappers around them.
Splines are implicit mathematical representations of "curved lines" and can be used for Animations, Movement and curved Objects.

## Spline Fundamentals

These splines are defined by a collection of points, called handles.
All handles but the first and last handles are used to interpolate the curve. 
The first and last handles are called *margin handles*, and the other ones will be referred to as *interpolated handles*.
*Margin handles* do not contribute to the interpolation, but determine the curvature and the beginning and end of the spline.

- The `WithSpline` interface is implemented by any class that has an underlying spline, but that is not a spline itself.
- A `Spline` is the implementation of a Catmull-Rom-Spline with a list of handles. A valid spline must have at least 4 handles (2 margin, 2 interpolated).
- A `SplineSegment` is the segment between two interpolated handles. There are always exactly `handleCount - 3` of them.
- A `SplineSample` is a sampled location in between a segment. Whereas a segment is a curved line, the sample is a location on that line.

### Generics

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

### Sampling

A spline segment can be retrieved by indexing it at a specific `SplineSegmentIndex` like this:
```cs
Spline spline;
SplineSegment segment = spline[SplineSegmentIndex.At(3)];
```

The high-level spline API provides two ways of representing locations along a spline:
- The `SplineLocation` is the distance (in meters) to a location on the spline from the splines first interpolated handle. This distance is not the eucledian distance but the path length when traversing along the spline.
- The `NormalizedSplineLocation` is a number which describes a location on the spline based on spline segment 
  index and percentage of the next segment. For example, a value of `3.5f` indicates the point half-way between 
  the 4th and 5th handles.
  
`NormalizedSplineLocations` are faster (as they are used by the underlying spline calculations), but most problems require `SplineLocations`, since they can be converted to meters.

A spline sample can be retrieved by indexing a spline segment with a number or indexing a spline with a `SplineLocation` or `NormalizedSplineLocation`:
```cs
// Given these variables
Spline spline;
SplineSegment segment = spline[SplineSegmentIndex.At(3)];

// Sample at 30% between the two handles of the segment
// Must be between 0 and 1 (inclusive)
SplineSample s1 = segment.SampleAt(0.3); 

// Sample at 15 meters along the spline from its beginning
SplineSample s2 = spline[SplineLocation.From(15)]; 

// Sample within the 3rd segment at 50% between its handles
SplineSample s3 = spline[NormalizedSplineLocation.From(3.5)];

// Sample within the 1st segment at 0% between its handles (e.g. at the beginning of the segment)
SplineSample s4 = spline[NormalizedSplineLocation.From(1)];
```

## Common Use-Cases

### Creating splines:

Splines can be created in two ways:
- Using the builder methods in the `GenericSpline` class or their counterparts in `UnitySpline`,`GlobalSpline` and `LocalSpline`. E.g. `UnitySpline.FromHandles(beginMargin, handles, endMargin)`
- Using the extension method on `IEnumerable<TPos>`, found in `GenericEnumerableToSplineExtensions` or the aforementioned specialized classes.

### General spline information:

Here are the most commonly used operations on the fundamental spline classes:
- `.Length()` can be called on `Spline`s and `SplineSegment`s. It will return an approximation of the length of the spline's or segment's curve. (Not eucledian distance, but path length when traversing along the spline)
- `.Handles` can be called on the `Spline` to get all *interpolated handles*. The count thereof can be retrieved with `.HandleCount`. Variants that include the margin handles also exist.
- `.Position`, `.Tangent` and `.Curvature` can be called on a `SplineSample` to retrieve the respective value at that location on the spline.

Examples:
```cs
Spline<Vector3, Vector3> spline;

Length length = spline.Length();
Vector3 halfwayPosition = spline[length / 2.0f].Position;

Vector3 tangentAtBeginning = spline[NormalizedSplineLocation.Zero].Tangent;
```

### Closest position on spline:

To retrieve the closest point on a `Spline` (or the spline of a `WithSpline`) relative to a position in World-Space, the `.QueryClosestPositionOnSplineTo(TPos)` extension method can be used.
The returned `ClosestOnSplineQueryResult` contains information about the SplineLocation, Distance and Position on spline.
There also exists a variant that can be used on `IEnumerable<WithSpline>` called `.QueryClosestPositionOnSplinesTo(TPos)`.

Given these algorithms' greedy implementation, they don't work well on heavily curved splines.

For more options, refer to the contents of `ClosestOnSplineExtensions.cs`.

### Sampling splines:

In order to sample the positions on a spline, the `.SampleSplineEvery(...)` and `.SampleSplineBetween(...)` extension methods can be used. They can be configured to return the Positions, Tangents or Curvatures.

For more options, refer to `SplineSampleExtensions.cs`.

## Domain Specific Terms

### Definition of splines
In mathematics, a Catmull-Rom spline is defined by 4 handles: The 2nd and 3rd are interpolated, while the other 2 only determine the course.

The high-level spline API of this module defines a spline as the following: A Begin (margin) handle, 2 or more (normal) handles and an end (margin) handle.
- These handles are positions
- The normal handles are all interpolated in the order they appear
- The margin handles are not interpolated and can be omitted in some cases (as they will be autogenerated)

### External Links

- [Splines](https://en.wikipedia.org/wiki/Spline_(mathematics\))
- [Catmull Rom Splines](https://en.wikipedia.org/wiki/Centripetal_Catmull%E2%80%93Rom_spline)