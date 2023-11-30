namespace BII.WasaBii.Unity {
    /// <summary>
    /// Flag for component search utilities that specify
    /// in which gameObjects to search for components.
    /// Specifying anything other than <see cref="Search.InWholeHierarchy"/>
    /// or <see cref="Search.InSiblings"/>
    /// causes the lookups to search until either the scene root is
    /// reached or there are no more children.
    /// When specifying <see cref="Search.InWholeHierarchy"/>,
    /// the parents are always searched first for performance reasons,
    /// as the hierarchy of parents can be traversed linearly, while
    /// searching children results in a depth-first-search.
    /// <see cref="Search.InSiblings"/> linearly traverses all children
    /// of the target's parent, including the target itself. 
    /// </summary>
    /// <seealso cref="ComponentQueryExtensions"/>
    public enum Search {
        /// <inheritdoc cref="Search"/>
        InObjectOnly,
        /// <inheritdoc cref="Search"/>
        InChildren,
        /// <inheritdoc cref="Search"/>
        InChildrenOnly,
        /// <inheritdoc cref="Search"/>
        InParents,
        /// <inheritdoc cref="Search"/>
        InSiblings,
        /// <inheritdoc cref="Search"/>
        InWholeHierarchy
    }
}