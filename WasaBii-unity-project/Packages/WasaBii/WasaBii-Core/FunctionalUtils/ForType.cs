namespace BII.WasaBii.Core {
    /// <summary>
    /// Interface that signals that the function using this as a parameter
    /// is overloaded for the type <typeparamref name="TContext"/>.
    /// 
    /// No types should implement this.
    /// Parameters of this type should have the name `_`.
    /// `default` should be the only value passed to parameters of this type.
    /// </summary>
    /// <example><code>
    /// public interface Foo{T} {
    ///     void DoSomething(ForType{T} _);
    /// }
    /// </code></example>
    public interface ForInType<in TContext> { }
    
    /// <summary>
    /// Interface that signals that the function using this as a parameter
    /// is overloaded for the type <typeparamref name="TContext"/>.
    /// 
    /// No types should implement this.
    /// Parameters of this type should have the name `_`.
    /// `default` should be the only value passed to parameters of this type.
    /// </summary>
    /// <example><code>
    /// public interface Foo{T} {
    ///     void DoSomething(ForType{T} _);
    /// }
    /// </code></example>
    public interface ForOutType<out TContext> { }
}