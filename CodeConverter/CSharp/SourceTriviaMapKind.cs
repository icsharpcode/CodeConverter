namespace ICSharpCode.CodeConverter.CSharp
{
    internal enum SourceTriviaMapKind
    {
        /// <summary>
        /// Only apply when a node is being used outside its original context (e.g. reusing the class identifier as the constructor identifier)
        /// </summary>
        None,
        /// <summary>
        /// Apply when the first/last line of the node being visited won't match up with what's returned from the visitor (e.g. if a declaration is split)
        /// </summary>
        SubNodesOnly,
        /// <summary>
        /// Apply this by default
        /// </summary>
        All
    }
}
