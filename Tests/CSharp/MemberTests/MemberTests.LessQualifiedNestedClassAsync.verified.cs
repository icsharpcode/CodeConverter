
internal partial class ClA
{
    public static void MA()
    {
        ClassB.MB();
        MyClassC.MC();
    }

    public partial class ClassB
    {
        public static ClassB MB()
        {
            MA();
            MyClassC.MC();
            return MB();
        }
    }
}

internal partial class MyClassC
{
    public static void MC()
    {
        ClA.MA();
        ClA.ClassB.MB();
    }
}