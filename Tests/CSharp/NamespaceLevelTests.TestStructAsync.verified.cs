using System;

internal partial struct MyType : IComparable<MyType>
{

    private void Test()
    {
    }
}
1 source compilation errors:
BC30149: Structure 'MyType' must implement 'Function CompareTo(other As MyType) As Integer' for interface 'IComparable(Of MyType)'.
1 target compilation errors:
CS0535: 'MyType' does not implement interface member 'IComparable<MyType>.CompareTo(MyType)'