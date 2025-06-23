using System;
using System.Collections.Generic;
using System.Linq;

public partial class TestFunc
{
    public Func<int, int> pubIdent = (row) => row;
    public Func<int, object> pubWrite = (row) => Console.WriteLine(row);
    private bool isFalse(int row) => false;
    private void write0() => Console.WriteLine(0);

    private void TestMethod()
    {
        bool index(List<string> pList) => pList.All(x => true);
        bool index2(List<string> pList) => pList.All(x => false);
        bool index3(List<int> pList) => pList.All(x => true);
        bool isTrue(List<string> pList) => pList.All(x => true);
        bool isTrueWithNoStatement(List<string> pList) => pList.All(x => true);
        void write() => Console.WriteLine(1);
    }
}
1 source compilation errors:
BC30491: Expression does not produce a value.
2 target compilation errors:
CS0029: Cannot implicitly convert type 'void' to 'object'
CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type