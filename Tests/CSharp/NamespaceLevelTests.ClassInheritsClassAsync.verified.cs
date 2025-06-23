using System.IO;

internal partial class ClassInheritsClass : InvalidDataException
{
}
1 source compilation errors:
BC30299: 'ClassInheritsClass' cannot inherit from class 'InvalidDataException' because 'InvalidDataException' is declared 'NotInheritable'.
1 target compilation errors:
CS0509: 'ClassInheritsClass': cannot derive from sealed type 'InvalidDataException'