using System;

internal partial class test : IComparable
{
}
1 source compilation errors:
BC30149: Class 'test' must implement 'Function CompareTo(obj As Object) As Integer' for interface 'IComparable'.
1 target compilation errors:
CS0535: 'test' does not implement interface member 'IComparable.CompareTo(object)'