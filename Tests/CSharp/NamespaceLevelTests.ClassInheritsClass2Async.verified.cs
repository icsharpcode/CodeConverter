using System.IO;

internal partial class ClassInheritsClass2 : InvalidDataException
{
}
1 source compilation errors:
BC30299: 'ClassInheritsClass2' cannot inherit from class 'InvalidDataException' because 'InvalidDataException' is declared 'NotInheritable'.
1 target compilation errors:
CS0509: 'ClassInheritsClass2': cannot derive from sealed type 'InvalidDataException'