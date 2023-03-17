namespace BII.WasaBii.Geometry
{
    /// <summary>
    /// Defines the handedness of the coordinate system. This has an influence on some operations like calculating
    /// signed angles, but has no influence on others like calculating a dot product.
    /// See https://en.wikipedia.org/wiki/Orientation_(vector_space) for more information.
    /// </summary>
    /// <remarks>Unity uses a left-handed coordinate system, System.Numerics uses a
    /// right-handed one, so the <see cref="Default"/> reflects that.</remarks>
    public enum Handedness
    {
        Default =
        #if UNITY_2022_1_OR_NEWER
            Left,
        #else
            Right,
        #endif
        Left = 1,
        Right = 2
    }
}