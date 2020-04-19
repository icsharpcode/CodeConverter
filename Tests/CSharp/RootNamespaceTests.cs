using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp
{
    public class RootNamespaceTests : ConverterTestBase
    {
        public RootNamespaceTests() : base("TheRootNamespace")
        {
        }

        [Fact]
        public async Task RootNamespaceIsExplicitAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class AClassInRootNamespace
End Class

Namespace NestedWithinRoot
    Class AClassInANamespace
    End Class
End Namespace",
                @"
namespace TheRootNamespace
{
    internal partial class AClassInRootNamespace
    {
    }

    namespace NestedWithinRoot
    {
        internal partial class AClassInANamespace
        {
        }
    }
}");
        }

        [Fact]
        public async Task RootNamespaceIsExplicitWithSingleClassAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class AClassInRootNamespace
End Class",
                @"
namespace TheRootNamespace
{
    internal partial class AClassInRootNamespace
    {
    }
}");
        }

        [Fact]
        public async Task RootNamespaceIsAddedToExistingNamespaceAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Namespace A.B
    Public Class Class1
    End Class
End Namespace",
                @"
namespace TheRootNamespace.A.B
{
    public partial class Class1
    {
    }
}");
        }

        [Fact]
        public async Task RootNamespaceIsAddedToExistingNamespaceWithDeclarationCasingAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Namespace AAA.AAaB.AaA
    Public Class Class1
    End Class
End Namespace

Namespace Aaa.aAAb.aAa
    Public Class Class2
    End Class
End Namespace",
                @"
namespace TheRootNamespace.AAA.AAaB.AaA
{
    public partial class Class1
    {
    }
}

namespace TheRootNamespace.Aaa.aAAb.aAa
{
    public partial class Class2
    {
    }
}");
        }

        [Fact]
        public async Task NestedNamespacesRemainRelativeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Namespace A.B
    Namespace C
        Public Class Class1
        End Class
    End Namespace
End Namespace",
                @"
namespace TheRootNamespace.A.B
{
    namespace C
    {
        public partial class Class1
        {
        }
    }
}");
        }

        [Fact]
        public async Task NestedNamespaceWithRootClassRemainRelativeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Namespace A.B
    Namespace C
        Public Class Class1
        End Class
    End Namespace
End Namespace

Public Class RootClass
End Class",
                @"
namespace TheRootNamespace
{
    namespace A.B
    {
        namespace C
        {
            public partial class Class1
            {
            }
        }
    }

    public partial class RootClass
    {
    }
}");
        }

        [Fact]
        public async Task RootNamespaceIsNotAddedToExistingGlobalNamespaceAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Namespace Global.A.B
    Public Class Class1
    End Class
End Namespace",
                @"
namespace A.B
{
    public partial class Class1
    {
    }
}");
        }

        [Fact]
        public async Task RootNamespaceIsExplicitForSingleNamespaceAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Namespace NestedWithinRoot
    Class AClassInANamespace
    End Class
End Namespace",
                @"
namespace TheRootNamespace.NestedWithinRoot
{
    internal partial class AClassInANamespace
    {
    }
}");
        }

        [Fact]
        public async Task RootNamespaceNotAppliedToFullyQualifiedNamespaceAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Namespace Global.NotNestedWithinRoot
    Class AClassInANamespace
    End Class
End Namespace",
                @"
namespace NotNestedWithinRoot
{
    internal partial class AClassInANamespace
    {
    }
}");
        }

        [Fact]
        public async Task RootNamespaceOnlyAppliedToUnqualifiedMembersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Class AClassInRootNamespace ' Becomes nested - 1
End Class ' Becomes nested - 2

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
    internal partial class AClassInANamespace
    {
    }
}

namespace TheRootNamespace
{
    internal partial class AClassInRootNamespace // Becomes nested - 1
    {
    } // Becomes nested - 2

    namespace NestedWithinRoot
    {
        internal partial class AClassInANamespace
        {
        }
    }
}");
        }
    }
}