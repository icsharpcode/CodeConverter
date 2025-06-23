
using System;

public partial class Issue1148
{
    public static Func<TestObjClass> FuncClass = FunctionReturningClass;
    public static Func<TestBaseObjClass> FuncBaseClass = FunctionReturningClass;
    public static Func<ITestObj> FuncInterface = FunctionReturningClass;
    public static Func<ITestObj, ITestObj> FuncInterfaceParam = CastObj;
    public static Func<TestObjClass, ITestObj> FuncClassParam = CastObj;

    public static TestObjClass FunctionReturningClass()
    {
        return new TestObjClass();
    }

    public static TestObjClass CastObj(ITestObj obj)
    {
        return (TestObjClass)obj;
    }

}

public partial class TestObjClass : TestBaseObjClass, ITestObj
{
}

public partial class TestBaseObjClass
{
}

public partial interface ITestObj
{
}
