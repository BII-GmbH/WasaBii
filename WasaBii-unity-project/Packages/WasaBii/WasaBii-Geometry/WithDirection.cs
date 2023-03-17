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

}
