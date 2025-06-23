using System;

public partial class Class1
{
    public event EventHandler MyEvent;
    protected override string Foo()
    {
        string FooRet = default;
        MyEvent += (_, __) => Foo();
        FooRet = FooRet + "";
        FooRet += nameof(Foo);
        return FooRet;
    }
}
1 source compilation errors:
BC30284: function 'Foo' cannot be declared 'Overrides' because it does not override a function in a base class.
1 target compilation errors:
CS0115: 'Class1.Foo()': no suitable method found to override