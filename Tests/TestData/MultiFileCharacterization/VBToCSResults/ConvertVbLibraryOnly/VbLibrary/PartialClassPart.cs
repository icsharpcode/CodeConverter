/// <summary>
/// Test that partial class modifiers are added to both parts
/// </summary>

namespace VbLibrary
{
    internal partial class AClass
    {
        static int[] initialAnArrayWithNonStaticInitializerReferencingOtherPart() => new int[anInt + 1];

        private int[] anArrayWithNonStaticInitializerReferencingOtherPart;
    }
}