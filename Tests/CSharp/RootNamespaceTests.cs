using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class RootNamespaceTests : ConverterTestBase
{
    public RootNamespaceTests() : base("TheRootNamespace")
    {
    }

    [Fact]
    public async Task RootNamespaceIsExplicitAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RootNamespaceIsExplicitWithSingleClassAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace TheRootNamespace
{
    internal partial class AClassInRootNamespace
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RootNamespaceIsAddedToExistingNamespaceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace TheRootNamespace.A.B
{
    public partial class Class1
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RootNamespaceIsAddedToExistingNamespaceWithDeclarationCasingAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NestedNamespacesRemainRelativeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace TheRootNamespace.A.B
{
    namespace C
    {
        public partial class Class1
        {
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NestedNamespaceWithRootClassRemainRelativeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RootNamespaceIsNotAddedToExistingGlobalNamespaceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace A.B
{
    public partial class Class1
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RootNamespaceIsExplicitForSingleNamespaceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace TheRootNamespace.NestedWithinRoot
{
    internal partial class AClassInANamespace
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RootNamespaceNotAppliedToFullyQualifiedNamespaceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace NotNestedWithinRoot
{
    internal partial class AClassInANamespace
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RootNamespaceOnlyAppliedToUnqualifiedMembersAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }
}