using System;
using System.Collections.Generic;
using System.Linq;

internal partial class ContrivedFuncInferenceExample
{
    private void TestMethod()
    {
        for (Blah index = (pList) => pList.All(x => true), loopTo = new Blah(); new Blah() >= 0 ? index <= loopTo : index >= loopTo; index += new Blah())
        {
            bool buffer = index.Check(new List<string>());
            Console.WriteLine($"{buffer}");
        }
    }

    public partial class Blah
    {
        public readonly Func<List<string>, bool> Check;

        public Blah(Func<List<string>, bool> check = null)
        {
            check = check;
        }

        public static implicit operator Blah(Func<List<string>, bool> p1)
        {
            return new Blah(p1);
        }
        public static implicit operator Func<List<string>, bool>(Blah p1)
        {
            return p1.Check;
        }
        public static Blah operator -(Blah p1, Blah p2)
        {
            return new Blah();
        }
        public static Blah operator +(Blah p1, Blah p2)
        {
            return new Blah();
        }
        public static bool operator <=(Blah p1, Blah p2)
        {
            return p1.Check(new List<string>());
        }
        public static bool operator >=(Blah p1, Blah p2)
        {
            return p2.Check(new List<string>());
        }
    }
}
2 target compilation errors:
CS1660: Cannot convert lambda expression to type 'ContrivedFuncInferenceExample.Blah' because it is not a delegate type
CS0019: Operator '>=' cannot be applied to operands of type 'ContrivedFuncInferenceExample.Blah' and 'int'