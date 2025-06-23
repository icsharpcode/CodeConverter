using System;

public partial class AnonymousLambdaTypeConversionTest
{
    public void CallThing(Delegate thingToCall)
    {
    }

    public void SomeMethod()
    {
    }

    public void Foo()
    {
        CallThing(new Action(() => SomeMethod()));
        CallThing(new Action<object>(a => SomeMethod()));
        CallThing(new Func<bool>(() =>
        {
            SomeMethod();
            return false;
        }));
        CallThing(new Func<object, bool>(a => false));
    }
}