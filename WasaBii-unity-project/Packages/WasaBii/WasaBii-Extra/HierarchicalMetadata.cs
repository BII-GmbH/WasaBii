#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using BII.WasaBii.Core;

namespace BII.WasaBii.Extra {
    
    // __ Hierarchical Metadata __
    // 
    // This file contains abstract generic containers that handle a set of *hierarchical metadata*.
    // This includes metadata of values whose types are constrained in a specific type hierarchy.
    // This metadata is added from outside and queried by *type*. If an abstract type is queried,
    // then these containers guarantee that only at most one subtype of that type can exist. 
    //
    // In practical terms, this means that all metadata types must be subtypes of `TSupertype`. 
    // In most cases, `TSupertype` will be an empty marker interface or abstract class.
    //
    // We do this in order to limit the possible types that can be queried, as well as to 
    // prevent inserting unrelated types such as simple `String`s and `float`s. These cases
    // mostly make no sense, as you could only have exactly one string or float... 
    //
    // The most basic case is to simply use either `HierarchicalMetadata<TSupertype>.Immutable`
    // or `HierarchicalMetadata<TSupertype>.Mutable`. Use the immutable variant when you
    // need a `[MustBeImmutable]` type and/or know all your metadata values ahead of time.
    // Use the mutable variant when you need to update the registered metadata after construction.
    // The mutable variant has some minor overhead to ensure consistency after modifications.
    // You can inherit from these if you want to, without any further issues or consideration.
    //
    // But for some cases, this simple implementation does not suffice.
    // Sometimes, your metadata values aren't just instances of `T : TSupertype`.
    // Instead, they may include additional data, e.g. some tags or some factory that produced the metadata.
    // For these cases, there is a more abstract version of `HierarchicalMetadata<TSupertype, TValue>`.
    // You still query with subtypes of `TSupertype`, but the values you get are actually of type `TValue`.
    //
    //     [C# has no dependent types, so we cannot return better subtypes of TValue depending in the queried T;
    //      casting the returned value is usually required after use]
    // 
    // In order to enable methods like `.Add(TValue)`, the hierarchical metadata collections need to know how
    // to obtain a `TSupertype` from a `TValue`. As these collections need to be serialized, we cannot just
    // pass a `Func` and be done with it. Instead, you need to manually implement a subclass of either
    // `HierarchicalMetadata<TSupertype, TValue>.Immutable` or `HierarchicalMetadata<TSupertype, TValue>.Mutable`.
    // In either case, all you have to do is implement the `TSupertype getKeyFromEntry(TValue entry)` method.
    // These types are otherwise safe for inheritance.
    //
    // Fun fact: The `HierarchicalMetadata<TSupertype>` variants implement `HierarchicalMetadata<TSupertype, TSupertype>`.
    // As such, they simply implement the default `TSupertype getKeyFromEntry(TSupertype entry) => entry;`. 
    // 
    // __ Examples __
    // 
    // Assume you have the following type hierarchy:
    // 
    //   Base - A ---- B - C
    //     \     \- D   \- E
    //      \-- F
    // 
    // In this case, you can have at most two values in the collection: One of type F and one that extends A.
    //
    // Assume you have values of types F and C in the collection.
    //
    // Calling `MetadataFor<C>` returns C.
    // Calling `MetadataFor<E>` returns None, as there is no E.
    // Calling `MetadataFor<B>` returns C, casted to B.
    // Calling `MetadataFor<D>` returns None, as there is no D.
    // Calling `MetadataFor<A>` returns C, casted to A.
    //
    // Now we call `.Add(new D())`.
    // If we would call `MetadataFor<A>`, we would now have an ambiguity: do we return D or C?
    // So the `.Add` call either replaces C with D or throws an exception, depending on the `throwOnAlreadyPresent` param.
    // F is in a separate sub-hierarchy, and is therefore always untouched.

    /// <summary>
    /// Collection that manages a number of metadata values which are all subtypes of <typeparamref name="TSupertype"/>.
    /// Enables querying by types. You can query an abstract type and get a more concrete instance instead.
    ///
    /// You should use either <see cref="HierarchicalMetadata{TSupertype}.Mutable"/> or
    /// <see cref="HierarchicalMetadata{TSupertype}.Immutable"/> depending on your needs.
    /// You can inherit from these types as well, if you want to, without any further thoughts.
    /// </summary>
    /// <remarks>For more details, refer to the docs in the source file.</remarks>
    public interface HierarchicalMetadata<TSupertype> : HierarchicalMetadata<TSupertype, TSupertype> 
    where TSupertype : class, IEquatable<TSupertype> {
        
        [Pure] public new Option<T> MetadataFor<T>() where T : TSupertype;
        
        #pragma warning disable CS0108 // hides inherited member; intentional in this case

        /// <inheritdoc cref="HierarchicalMetadata{TSupertype}"/>
        [Serializable]
        public class Immutable : HierarchicalMetadata<TSupertype, TSupertype>.Immutable, HierarchicalMetadata<TSupertype> {
            protected override TSupertype getKeyFromEntry(TSupertype entry) => entry;
            [Pure] public new Option<T> MetadataFor<T>() where T : TSupertype => data.TryGetValue(typeof(T)).Map(v => (T) v);
            
            public Immutable(IEnumerable<TSupertype> entries) : base(entries) { }
        }

        /// <inheritdoc cref="HierarchicalMetadata{TSupertype}"/>
        [Serializable]
        public class Mutable : HierarchicalMetadata<TSupertype, TSupertype>.Mutable, HierarchicalMetadata<TSupertype> {
            protected override TSupertype getKeyFromEntry(TSupertype entry) => entry;
            [Pure] public new Option<T> MetadataFor<T>() where T : TSupertype => data.TryGetValue(typeof(T)).Map(v => (T) v);

            public Mutable() { }
            public Mutable(IEnumerable<TSupertype> entries) : base(entries) { }
        }
        
        #pragma warning restore CS0108
    }

    /// <summary>
    /// Collection that manages a number of metadata values which are all subtypes of <typeparamref name="TValue"/>.
    /// All of these values are centered around a value of type <typeparamref name="TSupertype"/>. Queries are done
    /// via subtypes of <typeparamref name="TSupertype"/>, as that type is the upper bound of the type *hierarchy*. 
    /// 
    /// All instances usually either extend <see cref="HierarchicalMetadata{TSupertype,TValue}.Immutable"/>
    /// or <see cref="HierarchicalMetadata{TSupertype,TValue}.Mutable"/>.
    /// The concrete implementation of the <see cref="WithQueries.getKeyFromEntry"/> method
    /// defines how to get a key from each value contained in the collection.
    /// </summary>
    /// <remarks>For more details, refer to the docs in the source file.</remarks>
    // ReSharper disable once TypeParameterCanBeVariant // Intentional: We do not want a hierarchy of supertypes
    public interface HierarchicalMetadata<TSupertype, TValue> 
    where TSupertype : class where TValue : IEquatable<TValue> {

        [Pure] public Option<TValue> MetadataFor<T>() where T : TSupertype;
        [Pure] public Option<TValue> MetadataFor(Type keyType);

        [Pure] public bool HasMetadata<T>() where T : TSupertype;
        [Pure] public bool HasMetadata(Type keyType);
        
        [Pure] public IEnumerable<TValue> Values { get; }
        
        // Note CR: Extra class, because default interface methods can not be resolved for some reason
        [Serializable]
        public abstract class WithQueries : HierarchicalMetadata<TSupertype, TValue> {
            
            protected abstract IReadOnlyDictionary<Type, TValue> data { get; }

            [Pure] public Option<TValue> MetadataFor<T>() where T : TSupertype => data.TryGetValue(typeof(T));
            [Pure] public Option<TValue> MetadataFor(Type keyType) {
                Contract.Assert(typeof(TSupertype).IsAssignableFrom(keyType), $"Key type is not valid: {keyType}");
                return data.TryGetValue(keyType);
            }

            [Pure] public bool HasMetadata<T>() where T : TSupertype => data.ContainsKey(typeof(T));
            [Pure] public bool HasMetadata(Type keyType) {
                Contract.Assert(typeof(TSupertype).IsAssignableFrom(keyType), $"Key type is not valid: {keyType}");
                return data.ContainsKey(keyType);
            }

            [Pure] public IEnumerable<TValue> Values => data.Values.Distinct();

            protected abstract TSupertype getKeyFromEntry(TValue entry);
            
            /// Returns all types in the type hierarchy of t (interfaces and base classes),
            /// that implement the `TSupertype` interface either directly or indirectly.
            protected static IEnumerable<Type> allMetadataTypesFor(Type sourceType) {
                if (!typeof(TSupertype).IsAssignableFrom(sourceType))
                    yield break;
                yield return sourceType;
                var recRes = sourceType.GetInterfaces()
                    .ApplyIf(sourceType.BaseType != null, interfaces => interfaces.Prepend(sourceType.BaseType!))
                    .SelectMany(allMetadataTypesFor);
                foreach (var res in recRes)
                    if (res != typeof(TSupertype))
                        yield return res;
            }
        }
        
        /// <inheritdoc cref="HierarchicalMetadata{TSupertype,TValue}"/>
        /// 
        /// This is the safe and immutable base type, which should be used when all values are known during construction.
        /// There are no "efficient immutable modification" methods available.
        /// Just collect all the values and pass them to the constructor.
        ///
        /// All you need to do is implement the single `getKeyFromEntry` method. There is nothing else to consider.
        /// 
        /// <devremarks>
        /// Not marked as <see cref="MustBeImmutableAttribute"/>, because we do not enforce the values to be immutable.
        /// However, will check as immutable when used in another type marked as <see cref="MustBeImmutableAttribute"/>
        ///  as long as the contained values also type check appropriately.
        /// </devremarks>
        [Serializable]
        public abstract class Immutable : WithQueries {
            private readonly ImmutableDictionary<Type, TValue> _data;
            protected override IReadOnlyDictionary<Type, TValue> data => _data;
            
            // Note CR: Nullable so that this will work properly with e.g. JSON serialization without much boilerplate
            protected Immutable(IEnumerable<TValue>? entries) => 
                _data = entries?
                    .SelectMany(e => allMetadataTypesFor(getKeyFromEntry(e).GetType()).Select(t => (t, e)))
                    .ToImmutableDictionary() 
                ?? ImmutableDictionary<Type, TValue>.Empty;
        }

        /// <inheritdoc cref="HierarchicalMetadata{TSupertype,TValue}"/>
        /// 
        /// This is the mutable base type, which should be used when values are regularly added and removed.
        /// Provides adding, setting and removal functionality that always ensures that for every subtype `T`
        /// of `TSupertype`, when you query `T`, then there can be at most one unambiguous result.
        ///
        /// All you need to do is implement the single `getKeyFromEntry` method. There is nothing else to consider.
        [Serializable]
        public abstract class Mutable : WithQueries {
            private readonly Dictionary<Type, TValue> _data = new();
            
            // Note CR: As a possible optimization (depending on usage patterns),
            // we could just cache the whole hierarchy for each type, instead of just the (lowest) source type
            [NonSerialized] private readonly Dictionary<Type, Type> _insertionSourceOf = new();

            protected override IReadOnlyDictionary<Type, TValue> data => _data;

            protected Mutable() { }
            protected Mutable(IEnumerable<TValue> entries) => 
                entries.ForEach(e => Add(e, throwOnAlreadyPresent: true));

            public void Add(TValue entry, bool throwOnAlreadyPresent = true) {
                var source = getKeyFromEntry(entry).GetType();                  
                var keys = allMetadataTypesFor(source);
                if (!throwOnAlreadyPresent) Remove(source);
                foreach (var key in keys) {
                    if (_insertionSourceOf.TryGetValue(key, out var existingSource)) {
                        if (throwOnAlreadyPresent) 
                            throw new ArgumentException($"Cannot add metadata: Key {key} already present!");
                        else {
                            // cleanup existing data that overlaps in the type hierarchy with the current data
                            allMetadataTypesFor(existingSource).ForEach(t => {
                                _data.Remove(t);
                                _insertionSourceOf.Remove(t);
                            });
                        }
                    } 
                        
                    _data[key] = entry;
                    _insertionSourceOf[key] = source;
                }
            }

            public bool Remove<T>() where T : TSupertype => Remove(typeof(T));
            public bool Remove(TSupertype key) => Remove(key.GetType());
            
            public bool Remove(Type keyType) {
                Contract.Assert(typeof(TSupertype).IsAssignableFrom(keyType), $"Key type is not valid: {keyType}");
                // If any types in the hierarchy overlap,
                // we need to remove that type's entire existing hierarchy.
                return allMetadataTypesFor(keyType)
                    .Collect(_insertionSourceOf.TryGetValue)
                    .FirstOrNone()
                    .Map(source => {
                        var keys = allMetadataTypesFor(source);
                        foreach (var key in keys) {
                            _data.Remove(key);
                            _insertionSourceOf.Remove(key);
                        }
                        return true;
                    }).GetOrElse(() => false);
            }
            
            [OnDeserialized]
            protected void OnDeserialized(StreamingContext context) {
                // Restore sources on load; there's no reason to save these
                foreach (var (key, value) in _data) {
                    var source = getKeyFromEntry(value).GetType();
                    _insertionSourceOf.Add(key, source);
                }
            }

            public void Clear() {
                _data.Clear();
                _insertionSourceOf.Clear();
            }
        }
    }
}