using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class RootNamespaceTests : ConverterTestBase
    {
        public RootNamespaceTests() : base("TheRootNamespace")
        {
        }

        [Fact]
        public void RootNamespaceIsExplicit()
        {
            // Auto comment testing not used since it can't handle the added namespace
            TestConversionVisualBasicToCSharpWithoutComments(@"Class AClassInRootNamespace
End Class

Namespace NestedWithinRoot
    Class AClassInANamespace
    End Class
End Namespace",
                @"namespace TheRootNamespace
{
    class AClassInRootNamespace
    {
    }

    namespace NestedWithinRoot
    {
        class AClassInANamespace
        {
        }
    }
}");
        }

        [Fact]
        public void RootNamespaceIsExplicitWithSingleClass()
        {
            // Auto comment testing not used since it can't handle the added namespace
            TestConversionVisualBasicToCSharpWithoutComments(@"Class AClassInRootNamespace
End Class",
                @"namespace TheRootNamespace
{
    class AClassInRootNamespace
    {
    }
}");
        }

        [Fact]
        public void RootNamespaceIsExplicitForSingleNamespace()
        {
            // Auto comment testing not used since it can't handle the added namespace
            TestConversionVisualBasicToCSharpWithoutComments(@"
Namespace NestedWithinRoot
    Class AClassInANamespace
    End Class
End Namespace",
                @"namespace TheRootNamespace
{
    namespace NestedWithinRoot
    {
        class AClassInANamespace
        {
        }
    }
}");
        }

        [Fact]
        public void RootNamespaceNotAppliedToFullyQualifiedNamespace()
        {
            // Auto comment testing not used since it can't handle the added namespace
            TestConversionVisualBasicToCSharpWithoutComments(@"
Namespace Global.NotNestedWithinRoot
    Class AClassInANamespace
    End Class
End Namespace",
                @"namespace NotNestedWithinRoot
{
    class AClassInANamespace
    {
    }
}");
        }

        [Fact]
        public void RootNamespaceOnlyAppliedToUnqualifiedMembers()
        {
            // Auto comment testing not used since it can't handle the added namespace
            TestConversionVisualBasicToCSharpWithoutComments(@"
Class AClassInRootNamespace
End Class

Namespace Global.NotNestedWithinRoot
    Class AClassInANamespace
    End Class
End Namespace

Namespace NestedWithinRoot
    Class AClassInANamespace
    End Class
End Namespace",
                @"
namespace NotNestedWithinRoot
{
    class AClassInANamespace
    {
    }
}

namespace TheRootNamespace
{
    class AClassInRootNamespace
    {
    }

    namespace NestedWithinRoot
    {
        class AClassInANamespace
        {
        }
    }
}");
        }
    }
}