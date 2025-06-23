using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class PropertyMemberTests : ConverterTestBase
{


    [Fact]
    public async Task TestPropertyAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    public int Test { get; set; }

    public int Test2
    {
        get
        {
            return 0;
        }
    }

    private int m_test3;

    public int Test3
    {
        get
        {
            if (7 == int.Parse(""7""))
                return default;
            return m_test3;
        }
        set
        {
            if (7 == int.Parse(""7""))
                return;
            m_test3 = value;
        }
    }
}
1 source compilation errors:
BC30124: Property without a 'ReadOnly' or 'WriteOnly' specifier must provide both a 'Get' and a 'Set'.", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestParameterizedPropertyAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public string get_FullName(bool lastNameFirst, bool isFirst)
    {
        if (lastNameFirst)
        {
            return LastName + "" "" + FirstName;
        }
        else
        {
            return FirstName + "" "" + LastName;
        }
    }
    // This comment belongs to the set method
    internal void set_FullName(bool lastNameFirst, bool isFirst, string value)
    {
        if (isFirst)
            FirstName = value;
    }

    public override string ToString()
    {
        set_FullName(false, true, ""hello"");
        return get_FullName(false, true);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestParameterizedPropertyRequiringConversionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class Class1
{
    public float get_SomeProp(int index)
    {
        return 1.5f;
    }
    public void set_SomeProp(int index, float value)
    {
    }

    public void Foo()
    {
        decimal someDecimal = 123.0m;
        set_SomeProp(123, (float)someDecimal);
    }
}", extension: "cs")
            );
        }
    }

    [Fact] //https://github.com/icsharpcode/CodeConverter/issues/642
    public async Task TestOptionalParameterizedPropertyAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public string get_FullName(bool isFirst = false)
    {
        return FirstName + "" "" + LastName;
    }
    // This comment belongs to the set method
    internal void set_FullName(bool isFirst = false, string value = default)
    {
        if (isFirst)
            FirstName = value;
    }

    public override string ToString()
    {
        set_FullName(true, ""hello2"");
        set_FullName(value: ""hello3"");
        set_FullName(value: ""hello4"");
        return get_FullName();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestParameterizedPropertyAndGenericInvocationAndEnumEdgeCasesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Linq;

public partial class ParameterizedPropertiesAndEnumTest
{
    public enum MyEnum
    {
        First
    }

    public string get_MyProp(int blah)
    {
        return blah.ToString();
    }
    public void set_MyProp(int blah, string value)
    {
    }


    public void ReturnWhatever(MyEnum m)
    {
        var enumerableThing = Enumerable.Empty<string>();
        switch (m)
        {
            case (MyEnum)(-1):
                {
                    return;
                }
            case MyEnum.First:
                {
                    return;
                }
            case (MyEnum)3:
                {
                    set_MyProp(4, enumerableThing.ToArray()[(int)m]);
                    return;
                }
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestParameterizedPropertyWithTriviaAsync()
    {
        //issue 1095
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class IndexedPropertyWithTrivia
{
    // a
    // b
    public int get_P(int i)
    {
        // 1
        int x = 1; // 2
        return default;
        // 3
    }

    // c
    public void set_P(int i, int value)
    {
        // 4
        int x = 1; // 5
                   // 6
        x = value + i; // 7
                       // 8
                       // d
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task PropertyWithMissingTypeDeclarationAsync()//TODO Check object is the inferred type
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class MissingPropertyType
{
    public object Max
    {
        get
        {
            double mx = 0d;
            return mx;
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestReadWriteOnlyInterfacePropertyAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface Foo
{
    string P1 { get; }
    string P2 { set; }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SynthesizedBackingFieldAccessAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private static int First { get; set; }

    private int Second = First;
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task PropertyInitializersAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;

internal partial class TestClass
{
    private List<string> First { get; set; } = new List<string>();
    private int Second { get; set; } = 0;
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestIndexerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private int[] _Items;

    public int this[int index]
    {
        get
        {
            return _Items[index];
        }
        set
        {
            _Items[index] = value;
        }
    }

    public int this[string index]
    {
        get
        {
            return 0;
        }
    }

    private int m_test3;

    public int this[double index]
    {
        get
        {
            return m_test3;
        }
        set
        {
            m_test3 = value;
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestWriteOnlyPropertiesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial interface TestInterface
{
    int[] Items { set; }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestImplicitPrivateSetterAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class SomeClass
{
    public int SomeValue { get; private set; }

    public void SetValue(int value1, int value2)
    {
        SomeValue = value1 + value2;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestParametrizedPropertyCalledWithNamedArgumentsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int get_Prop(int x = 1, int y = 2);
    void set_Prop(int x = 1, int y = 2, int value = default);
}
public partial class SomeClass : IFoo
{
    internal int get_Prop2(int x = 1, int y = 2)
    {
        return default;
    }
    internal void set_Prop2(int x = 1, int y = 2, int value = default)
    {
    }

    int IFoo.get_Prop(int x = 1, int y = 2) => get_Prop2(x, y);
    void IFoo.set_Prop(int x = 1, int y = 2, int value = default) => set_Prop2(x, y, value);

    public void TestGet()
    {
        IFoo foo = this;
        int a = get_Prop2() + get_Prop2(y: 20) + get_Prop2(x: 10) + get_Prop2(y: -2, x: -1) + get_Prop2(x: -1, y: -2);
        int b = foo.get_Prop() + foo.get_Prop(y: 20) + foo.get_Prop(x: 10) + foo.get_Prop(y: -2, x: -1) + foo.get_Prop(x: -1, y: -2);
    }

    public void TestSet()
    {
        set_Prop2(value: 1);
        set_Prop2(-1, -2, 1);
        set_Prop2(-1, value: 1);
        set_Prop2(y: 20, value: 1);
        set_Prop2(x: 10, value: 1);
        set_Prop2(y: -2, x: -1, value: 1);
        set_Prop2(x: -1, y: -2, value: 1);

        IFoo foo = this;
        foo.set_Prop(value: 1);
        foo.set_Prop(-1, -2, 1);
        foo.set_Prop(-1, value: 1);
        foo.set_Prop(y: 20, value: 1);
        foo.set_Prop(x: 10, value: 1);
        foo.set_Prop(y: -2, x: -1, value: 1);
        foo.set_Prop(x: -1, y: -2, value: 1);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestParametrizedPropertyCalledWithOmittedArgumentsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int get_Prop(int x = 1, int y = 2, int z = 3);
    void set_Prop(int x = 1, int y = 2, int z = 3, int value = default);
}
public partial class SomeClass : IFoo
{
    internal int get_Prop2(int x = 1, int y = 2, int z = 3)
    {
        return default;
    }
    internal void set_Prop2(int x = 1, int y = 2, int z = 3, int value = default)
    {
    }

    int IFoo.get_Prop(int x = 1, int y = 2, int z = 3) => get_Prop2(x, y, z);
    void IFoo.set_Prop(int x = 1, int y = 2, int z = 3, int value = default) => set_Prop2(x, y, z, value);

    public void TestGet()
    {
        IFoo foo = this;
        int a = get_Prop2() + get_Prop2(y: 20) + get_Prop2(10) + get_Prop2(y: 20) + get_Prop2(z: 30) + get_Prop2(10) + get_Prop2();
        int b = foo.get_Prop() + foo.get_Prop(y: 20) + foo.get_Prop(10) + foo.get_Prop(y: 20) + foo.get_Prop(z: 30) + foo.get_Prop(10) + foo.get_Prop();
    }

    public void TestSet()
    {
        set_Prop2(value: 1);
        set_Prop2(y: 20, value: 1);
        set_Prop2(10, value: 1);
        set_Prop2(y: 20, value: 1);
        set_Prop2(z: 30, value: 1);
        set_Prop2(10, value: 1);
        set_Prop2(value: 1);

        IFoo foo = this;
        foo.set_Prop(value: 1);
        foo.set_Prop(y: 20, value: 1);
        foo.set_Prop(10, value: 1);
        foo.set_Prop(y: 20, value: 1);
        foo.set_Prop(z: 30, value: 1);
        foo.set_Prop(10, value: 1);
        foo.set_Prop(value: 1);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestSetWithNamedParameterPropertiesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private int[] _Items;
    public int[] Items
    {
        get
        {
            return _Items;
        }
        set
        {
            _Items = value;
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestPropertyAssignmentReturnAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public string Foo
    {
        get
        {
            string FooRet = default;
            FooRet = """";
            return FooRet;
        }
    }
    public string X
    {
        get
        {
            string XRet = default;
            XRet = 4.ToString();
            XRet = (Conversions.ToDouble(XRet) * 2d).ToString();
            string y = ""random variable to check it isn't just using the value of the last statement"";
            return XRet;
        }
    }
    public string _y;
    public string Y
    {
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                Y = """";
            }
            else
            {
                _y = """";
            }
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestGetIteratorDoesNotGainReturnAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;

public partial class VisualBasicClass
{
    public static IEnumerable<object[]> SomeObjects
    {
        get
        {
            yield return new object[3];
            yield return new object[3];
        }
    }
}", extension: "cs")
            );
        }
    }
}