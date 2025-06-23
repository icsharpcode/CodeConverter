using System;
using System.Runtime.CompilerServices;

public static partial class Program
{
    public static void Main(string[] args)
    {
        var c = new SomeClass(new SomeDependency());
        Console.WriteLine("Done");
    }
}

public partial class SomeDependency
{
    public event EventHandler SomeEvent;
}

public partial class SomeClass
{
    private SomeDependency __dep;

    private SomeDependency _dep
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return __dep;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (__dep != null)
            {
                __dep.SomeEvent -= _dep_SomeEvent;
            }

            __dep = value;
            if (__dep != null)
            {
                __dep.SomeEvent += _dep_SomeEvent;
            }
        }
    }

    public SomeClass(object dep)
    {
        _dep = (SomeDependency)dep;
    }

    private void _dep_SomeEvent(object sender, EventArgs e)
    {
        // Do Something
    }
}
