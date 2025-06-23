using System;

public partial class TestClass2
{
    public void DoesNotThrow()
    {
        var rand = new Random();
        switch (rand.Next(8))
        {
            case var @case when @case < 4:
                {
                    break;
                }
            case 4:
                {
                    break;
                }
            case var case1 when case1 > 4:
                {
                    break;
                }

            default:
                {
                    throw new Exception();
                }
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code