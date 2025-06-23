using System;

internal partial class ClassImplementsInterface2 : IComparable
{
}
1 source compilation errors:
BC30149: Class 'ClassImplementsInterface2' must implement 'Function CompareTo(obj As Object) As Integer' for interface 'IComparable'.
1 target compilation errors:
CS0535: 'ClassImplementsInterface2' does not implement interface member 'IComparable.CompareTo(object)'