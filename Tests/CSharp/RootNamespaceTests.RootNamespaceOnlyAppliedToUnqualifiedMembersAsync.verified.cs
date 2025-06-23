
namespace NotNestedWithinRoot
{
    internal partial class AClassInANamespace
    {
    }
}

namespace TheRootNamespace
{
    // Comment from start of file moves within the namespace
    internal partial class AClassInRootNamespace // Becomes nested - 1
    {
    } // Becomes nested - 2

    namespace NestedWithinRoot
    {
        internal partial class AClassInANamespace
        {
        }
    }
}