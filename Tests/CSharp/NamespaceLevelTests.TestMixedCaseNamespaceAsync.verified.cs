
namespace Aaa
{
    internal partial class A
    {
        public static void Foo()
        {
        }
    }

    internal partial class Z
    {
    }
    internal partial class Z
    {
    }

    internal abstract partial class Base
    {
        public abstract void UPPER();
        public abstract bool FOO { get; set; }
    }
    internal partial class NotBase : Base
    {

        public override void UPPER()
        {
        }
        public override bool FOO { get; set; }
    }
}

namespace aaa
{
    internal partial class B
    {
        public static void Bar()
        {
        }
    }
}

internal static partial class C
{
    public static void Main()
    {
        var x = new Aaa.A();
        var y = new aaa.B();
        var z = new Aaa.A();
        var a = new aaa.B();
        var b = new Aaa.A();
        var c = new aaa.B();
        var d = new Aaa.A();
        var e = new aaa.B();
        var f = new Aaa.Z();
        var g = new Aaa.Z();
        Aaa.A.Foo();
        Aaa.A.Foo();
        aaa.B.Bar();
        aaa.B.Bar();
    }
}